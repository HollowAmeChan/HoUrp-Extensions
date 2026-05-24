# RP 重构第十五步执行计划

> 第十五步目标：在第十四步已经完成 HoURP / HoNpr 真 SSS 契约对齐之后，先暂停具体效果堆叠，建立 **轻量 FilterKit、外部渲染库参考策略、真 screen-space SSS 具体实现路径、以及后续 ScreenPost / ImagePost 滤波复用规则**。本阶段的核心不是“先造一个滤波调度框架”，而是沉淀一组可直接打开、可逐个 debug、可被 SSS / Image / Shadow 复用的 HLSL + C# 文件。

## 0. 为什么第十五步要先做 Filter

第十四步之后，HoURP 已经具备 SSS runtime 的清晰边界：

- `Aov.MaskId` / `Aov.NormalDepth` / `Aov.SurfaceData` / `Aov.Diffuse` 是通用 MBuffer 输入。
- `Object.FeatureFlags bit 2` 通过 `Aov.MaskId.a` gating `ReceivesSss`。
- `Aov.Diffuse` 只提供 diffuse / source color。
- `Sss.Source.a` 和 `Sss.Diffusion.a` 才是 SSS runtime 自己维护的参与权重和 composite weight。
- SSS runtime 拥有 `Sss.Source`、`Sss.Diffusion`、可选 transmission / temp 等运行时资源。

真正开始做 screen-space SSS 时，马上会遇到过滤问题：

- 皮肤扩散需要 profile-aware、depth / normal guided 的 spatial filter。
- transmission / thin scatter 可能需要独立 blur 或 gather。
- 后续 ScreenPost / ImagePost / Shoost 会大量用 blur、pyramid、Kawase、RGB blur、Iris blur、Kuwahara、DOF、Glow、motion trail、temporal accumulation。
- Shadow / AO / SSR / volumetric 等后续功能也会需要 edge-aware 或 temporal filtering。

如果每个 Feature 都复制一份 blur / bilateral / pyramid / diffusion 公式，后面会重新回到旧系统的问题：同类算法重复实现、参数口径不同、debug 时不知道某个效果到底用了哪份滤波代码。

第十五步先建立 FilterKit，是为了让真 SSS 直接复用同一套滤波函数和 pass 模板，而不是让 SSS 又开一套私有 shader。FilterKit 不负责复杂调度，只负责把可复用算法放在稳定位置。

## 1. 本阶段设计底线

### 1.1 Filter 是基础设施，不是一个后处理效果

`Filter/` 归属 HoURP runtime 基础设施，服务多个 Domain：

- `ShadingDomain`：SSS diffusion、SSAO、screen-space shadow / lighting filter。
- `LightingDomain`：shadow denoise、volumetric light filter。
- `ImageDomain`：Bloom、RGB blur、Iris blur、Kawase、DOF、Glow、film effects。
- `CompositeDomain`：semantic composite mask blur、character-specific composite smoothing。

Filter 不能被某一个效果私有化，也不能被 Shoost / HoPost 反向定义。

### 1.2 FilterKit 不是调度框架

FilterKit 是一组可复用源码，不是新的 RenderGraph 包装层：

- HLSL include 负责通用采样、权重、depth / normal gate、Burley profile、pyramid down/up、Kawase / Gaussian kernel。
- Shader 文件负责提供可直接调用和单步调试的 pass。
- C# helper 只负责 shader id、descriptor 推导、临时纹理命名、通用 blit 参数设置。
- Feature 自己显式创建 RenderGraph pass，显式传入 source / destination / guide textures。
- 不引入 `FilterRequest`、scheduler、隐藏 pass graph、自动资源链。

实际资源生命周期仍由调用方的 RendererFeature / ImageChain / RenderGraph pass 管，FilterKit 不偷偷创建长期资源。

### 1.3 外部渲染库只能作为受控 third-party 来源

计划参考 `NVIDIA-RTX/NRD`、`NVIDIAGameWorks/Falcor`、`GPUOpen-LibrariesAndSDKs/FidelityFX-SDK`、`google/filament`、`mmp/pbrt-v4`，但不能让任何一个外部库直接成为 HoURP 架构核心。

外部库的事实边界：

- NRD 是 API-agnostic 的 spatio-temporal denoising library，主要面向 noisy signal denoising，常见输入包括 normal、roughness、viewZ、motion vector 等 guide，包含 REBLUR、RELAX、SIGMA 等 denoiser 家族。
- Falcor 是 real-time rendering research / prototype framework，重点参考 render graph、modular renderer、RTX SDK integration，不引入它的框架、窗口、scene、build system。
- FidelityFX-SDK 是 AMD production GPU effect SDK，重点参考 upscaling、frame generation、denoise、shader/sample organization，不在 HoURP 运动矢量、history、presentation pipeline 稳定前集成 frame generation。
- Filament 是跨平台 real-time PBR engine，重点参考 material compiler、PBR/post/color/tonemap/mobile resource discipline，不替代 HoNpr 材质语义，也不把 HoURP 变成 Filament runtime。
- pbrt-v4 是 offline physically based renderer，重点参考 BSDF / BSSRDF / sampling / reference image，不把 offline runtime code 引入 Unity package。

HoURP 的边界：

- HoURP 不能因为任何外部库的 resource model 改写自己的 Semantic / Resource / RenderGraph 契约。
- Unity / URP RenderGraph 下是否能直接使用 native integration，需要单独验证，不能在第十五步默认可行。
- 如果 clone 外部库，必须固定 commit、保留 license、记录来源、隔离目录，不允许复制后改名成 HoURP 原生代码。
- 从外部库迁移出的算法或 shader 片段必须登记来源，且只有在许可和工程边界明确后才能进入 runtime。

## 2. 本阶段不做什么

- 不直接把 `lilToon-URP-Extensions/Runtime/SubsurfaceScattering` 整目录复制进新包。
- 不直接把 NRD / Falcor / FidelityFX-SDK / Filament / pbrt-v4 仓库内容混进 `Runtime/Filter` 并当作 HoURP 原生代码。
- 不让 SSS 自己复制一套私有 blur / diffusion shader 公式。
- 不一次性重写 Shoost 全部效果。
- 不把 ScreenPost 和 ImagePost 混成一个万能 post stack。
- 不把 FilterKit 做成第二套 RenderGraph 或新的 pass scheduler。
- 不在没有许可审查和固定来源记录前，把任何 external rendering library 代码编译进发布 runtime。

## 3. 前置输入

新 HoURP：

- `Runtime/Core/HoUrpBuiltInNames.cs`
- `Runtime/Core/HoUrpBuiltInContracts.cs`
- `Runtime/Resources/`
- `Runtime/RenderGraph/`
- `Runtime/Features/SubsurfaceScatteringRendererFeature.cs`
- `Runtime/PostProcess/`
- `Runtime/Image/`
- `Runtime/Debug/`
- `Runtime/Shaders/Hidden/HoURP/SSS/SubsurfaceScattering.shader`

旧实现参考：

- `D:/Unity_Fork/lilToon-URP-Extensions/Runtime/SubsurfaceScattering/HoSubsurfaceScatteringRendererFeature.cs`
- `D:/Unity_Fork/lilToon-URP-Extensions/Runtime/SubsurfaceScattering/HoSubsurfaceScattering.shader`
- `D:/Unity_Fork/lilToon-URP-Extensions/Runtime/ShoostPostProcessing/Renderer/Effects/*`
- `D:/Unity_Fork/lilToon-URP-Extensions/Runtime/ShoostPostProcessing/Shaders/Shoost/*`
- `D:/Unity_Fork/lilToon-URP-Extensions/Runtime/HoPostProcessing`

外部参考：

- `https://github.com/NVIDIA-RTX/NRD`
- `https://github.com/NVIDIA-RTX/NRD-Sample`
- `https://github.com/NVIDIAGameWorks/Falcor`
- `https://github.com/GPUOpen-LibrariesAndSDKs/FidelityFX-SDK`
- `https://github.com/google/filament`
- `https://github.com/mmp/pbrt-v4`

## 4. 推荐目录结构

第十五步建议先建立以下目录和文档边界：

```text
Runtime/
  Filter/
    README.md
    HoUrpFilterIds.cs
    HoUrpFilterUtils.cs
    HoUrpFilterResources.cs
    Shaders/
      HoUrpFilterCommon.hlsl
      HoUrpFilterSampling.hlsl
      HoUrpFilterDepthNormalGate.hlsl
      HoUrpFilterBurleyDiffusion.hlsl
      HoUrpFilterBlur.shader
      HoUrpFilterPyramid.shader
      HoUrpFilterDebug.shader
    SSS/
      HoUrpSssFilter.hlsl
      HoUrpSssDiffusion.shader
    Image/
      HoUrpImageBlur.shader
      HoUrpImageKuwahara.shader
      HoUrpImageRgbBlur.shader
    Nrd/
      README.md
      SOURCE.md
      LICENSE.txt
      Vendor/
    ThirdParty/
      README.md
      EXTERNAL_RENDERING_REFERENCES.md
  Image/
    ImageChain.cs
    ImageChainContext.cs
    ImagePassDescriptor.cs
    ImageWorkTexturePolicy.cs
  Features/
    SubsurfaceScatteringRendererFeature.cs
```

说明：

- `Runtime/Filter/*.cs` 只放轻量 helper：property id、pass index、descriptor 推导、debug name，不放调度框架。
- `Runtime/Filter/Shaders` 是通用 HLSL include 和可直接调用的基础 shader。
- `Runtime/Filter/SSS` 是 SSS 专用滤波 glue：复用通用 include，但保留 SSS debug 友好的 pass。
- `Runtime/Filter/Image` 是纯图像滤波 shader，给 ImageChain / Shoost 迁移使用。
- `Runtime/Filter/Nrd` 是 NRD 来源与可选 backend 集成隔离区，不允许直接污染 Core API。
- `Runtime/Filter/ThirdParty` 是外部渲染库参考矩阵和来源登记区，不是源码混放目录。
- `Runtime/Image` 承载纯 ImageDomain 的 ping-pong chain，不替代 RenderGraph。
- SSS 作为 `Features/SubsurfaceScatteringRendererFeature`，显式调用 `HoUrpSssDiffusion.shader`，不通过隐藏 request scheduler。

如果后续决定不把 NRD 源码放入 Unity package，可改为：

```text
External/NRD/
Documentation~/third_party/NRD_SOURCE.md 或 Runtime/Filter/Nrd/SOURCE.md
Runtime/Filter/Nrd/README.md
```

但无论放哪里，都必须固定来源并保留 license。

## 5. FilterKit 第一版契约

### 5.1 文件级契约

第一版不做泛型 request / scheduler，只规定文件该放什么、怎么被调用。

```text
HoUrpFilterCommon.hlsl
  basic math, uv, texel, safe normalize, color helpers

HoUrpFilterSampling.hlsl
  point/linear sample wrappers, mirrored offset helpers, golden-angle offsets

HoUrpFilterDepthNormalGate.hlsl
  depth gate, normal gate, profile/material gate, object mask gate

HoUrpFilterBurleyDiffusion.hlsl
  Burley profile eval/sample/weight, no texture binding

HoUrpFilterBlur.shader
  pass 0 copy
  pass 1 separable Gaussian/Kawase baseline
  pass 2 depth-normal aware blur

HoUrpSssDiffusion.shader
  pass 0 source prepare
  pass 1 profile-aware diffusion X / disk gather
  pass 2 profile-aware diffusion Y / optional second pass
  pass 3 composite
  pass 4 debug
```

第一批只实现这些可以直接 debug 的算法：

- `Copy`
- `SeparableBlur`
- `KawaseBlur`
- `DepthNormalAwareBlur`
- `ProfileAwareDiffusion`
- `PyramidDownsample`
- `PyramidUpsample`

### 5.2 GuideInputs

Guide input 不做成对象模型，但在 shader 和 C# 调用处必须显式命名：

```text
_HoFilterSourceTex
_HoFilterDepthTex 或 Aov.NormalDepth.a
_HoFilterNormalTex 或 Aov.NormalDepth.rgb
_HoFilterObjectMaskTex 或 Aov.MaskId.a
_HoFilterProfileTex 或 Aov.SurfaceData.b
_HoFilterThicknessTex 或 Aov.SurfaceData.r
_HoFilterCurvatureTex 或 Aov.SurfaceData.*
```

规则：

- 普通 image blur 不允许偷读 AOV。
- Edge-aware / diffusion shader 的 Properties / HLSL binding 必须直接列出 guide。
- SSS 专用 shader 可以读取 SSS 已声明输入，但不能藏在通用 blur shader 里。
- 缺少 guide 时，由调用方选择 fallback pass 或跳过，FilterKit 不做隐式降级。

### 5.3 Resource 生命周期

FilterKit 不拥有资源。调用方只遵守四类命名：

- `Input`：由上游 Feature 或 ImageChain 提供。
- `Output`：由当前 pass 产出，并由调用方注册给下游。
- `FrameTransient`：当前 frame 内可 alias 的工作纹理，由调用方创建。
- `PersistentHistory`：跨帧保存，必须由调用方显式声明 reset 条件。

禁止：

- Filter helper 在内部偷偷 `CreateTexture`。
- 每个 Feature 自己维护固定 `TempA/TempB` 字段。
- 把 ImageChain 的 WorkA / WorkB 发布成长期公共资源。
- 在同一个 RenderGraph pass 里同时读写同一 TextureHandle。

## 6. 外部渲染库参考与引入策略

### 6.1 参考库矩阵

| 来源 | 主要价值 | HoURP 用法 | 默认策略 |
| --- | --- | --- | --- |
| NRD | spatio-temporal denoise、guide inputs、permanent/transient resource、REBLUR / RELAX / SIGMA | 参考 denoise shader 结构 / guide checklist / temporal validation | 先 reference only，SSS 第一版不依赖 |
| Falcor | render graph、modular renderer、RTX path tracing / sample organization | 参考 pass graph、resource alias、debug / sample layout | 只读设计，不引入 framework |
| FidelityFX-SDK | production GPU effects、FSR、denoise、frame interpolation、cross-vendor shader packaging | 参考 effect packaging、quality preset、shader include / permutation、upscale / denoise 接口 | 只读设计，frame generation 延后 |
| Filament | PBR material system、color management、post pipeline、mobile / tile GPU 约束 | 参考 material/post/color discipline 和轻量资源策略 | 只读设计，不替换 HoNpr |
| pbrt-v4 | physical reference、BSDF / BSSRDF、sampling、ground-truth image | 作为 SSS / BSSRDF / diffusion profile 的离线验证参考 | 不导入 runtime code |

这张矩阵的作用是防止“看到现成库就搬代码”。每个库先回答三个问题：它能提供什么事实、哪些思想适合 HoURP、哪些工程边界不能继承。

### 6.2 第十五步只做受控导入，不承诺立即集成

建议流程：

1. clone 外部库只能落到隔离目录，例如 `External/<Name>` 或 `Runtime/Filter/<Name>/Vendor/<Name>`。
2. 固定 commit hash，写入 `SOURCE.md` 或 `Documentation~/third_party/<Name>_SOURCE.md`。
3. 保留 `LICENSE.txt`、`README.md`、版本号、来源 URL。
4. 建立 `<Name>ImportManifest.md`，列出哪些目录只是参考，哪些可能被编译。
5. 先不把 native integration、sample framework、build scripts 编进 Unity package。
6. 单独评估 Unity URP 是否能提供外部 backend 所需 graphics API handles / memory ownership / synchronization。
7. 如果不能稳定接入 native library，则只迁移算法思想和 shader 模式，HoURP 自己实现 RenderGraph compute/raster pass。

### 6.3 NRD 在 HoURP 中的合理定位

短期：

- 作为 filter / denoise 架构参考。
- 作为 future temporal denoise 的 guide input checklist。
- 作为 memory pool / permanent vs transient resource 设计参考。
- 作为 debug / validation 对照来源。

中期：

- 尝试把 SIGMA / REBLUR / RELAX 的某个最小 denoiser改写为 HoURP 显式 compute/raster pass。
- 只允许在明确 D3D12 / Vulkan / platform support 后启用。
- 对非支持平台提供 HoURP-native fallback。

长期：

- 如果 native integration 稳定，可让 NRD 成为独立 optional backend。
- HoURP 原生 FilterKit 文件不随 NRD API 变化。

### 6.4 Falcor 在 HoURP 中的合理定位

Falcor 更适合作为“完整渲染器如何组织 pass 和 sample”的参考，而不是可移植代码来源。

可参考：

- render graph 对 pass、resource、alias、barrier 的表达方式。
- renderer / sample / shader / UI debug 的模块边界。
- RTX / ray tracing feature 的最小样例组织。
- path tracing / denoise / post chain 的调试视图。

不可继承：

- Window、scene、material、camera、asset loading、build system。
- Falcor 自己的 resource abstraction 不能替代 HoURP Semantic / Resource / RenderGraph 契约。
- 不能为了复用 Falcor sample 改 HoURP 的 Unity 生命周期。

### 6.5 FidelityFX-SDK 在 HoURP 中的合理定位

FidelityFX-SDK 更适合作为 production GPU effects 的 packaging 和质量分级参考。

可参考：

- effect context / dispatch description / scratch resource 的封装方式。
- shader include、permutation、quality preset、platform capability 的管理方式。
- denoise / blur / upscale / frame interpolation 对 motion vector、depth、exposure、history 的输入契约。
- debug validation 和 sample app 参数组织。

不可继承：

- frame generation 在第十五步不进入目标，因为它依赖稳定 motion vector、history、present timing 和 UI composite 边界。
- FSR / upscale 不能和 SSS / post filter 混成同一个 FilterKind。
- SDK native backend 不应绕过 URP RenderGraph resource ownership。

### 6.6 Filament 在 HoURP 中的合理定位

Filament 更适合作为 real-time PBR、后处理和移动端资源纪律的参考。

可参考：

- material compiler / shading model 如何限制参数膨胀。
- tone mapping、color pipeline、exposure、bloom 等 post effect 的顺序和边界。
- mobile / tile GPU 下的 transient resource、format、bandwidth 控制。
- debug view / material variant 的组织方式。

不可继承：

- HoNpr 的材质语义不能被 Filament PBR 模型重写。
- 不把 Filament runtime 或 material compiler 纳入 HoURP package。
- Filament 的 engine abstraction 不能替代 Unity / URP 生命周期。

### 6.7 pbrt-v4 在 HoURP 中的合理定位

pbrt-v4 是离线物理参考，不是 runtime SDK。

可参考：

- BSSRDF、diffusion profile、sampling、energy conservation 的 ground truth。
- 用离线 reference image 验证 HoURP screen-space SSS 的 profile 形状和能量趋势。
- 为 shader 近似提供物理量级和测试场景，而不是直接决定美术参数。

不可继承：

- CPU renderer、scene format、offline integrator 不进入 Unity runtime。
- pbrt 的完整物理模型不能直接要求 screen-space SSS 实时等价。
- 参考结果必须通过 HoURP 自己的 artistic profile 和 debug view 转译。

### 6.8 NRD 不能直接解决的问题

NRD 不是皮肤 SSS diffusion 的直接替代品。

原因：

- SSS diffusion 是 profile / thickness / normal-depth guided 的确定性扩散和合成问题。
- NRD 主要面向 noisy radiance / occlusion / shadow signal denoising。
- 皮肤 SSS 需要可控的 artistic profile、composite weight、ReceivesSss gating。

所以真 SSS 第一版应使用 HoURP-native `ProfileAwareDiffusion`，NRD 只作为后续 ray traced shadow / AO / GI / reflection denoise 的参考或 backend。

### 6.9 可实际抄的代码位置与成本分级

本节记录的是“可以打开看代码、必要时改写进 HoURP”的精确入口，不等于允许直接复制源码。所有条目进入 runtime 前仍要走 `SOURCE.md`、license、commit、port note。

本次定位使用的浅 clone commit：

- NRD: `76073627829f7abc4f4624ef1127c8e6464ce1c2`
- Falcor: `eb540f6748774680ce0039aaf3ac9279266ec521`
- FidelityFX-SDK: `e236f2304dcda35f282fdddd085f41e2ff48c86a`
- Filament: `c28dfde4b23af06f4ed796e2b35c9cf2ae8152e8`
- pbrt-v4: `7154d8268ba1f512b20f25e6826999e346d02a15`

成本等级：

- `S`：1 个 shader/pass 级别，可快速改写，适合第十五步或第十六步落地。
- `M`：多 pass / 需要调用方显式管理 transient 资源，适合基础 shader 成型后迁移。
- `L`：依赖 history / motion vector / 多 guide / compute backend，适合专项。
- `XL`：依赖 native SDK、ray tracing、swapchain、presentation 或完整 renderer，不进近期实现。

| 来源 | 精确位置 | 算法 / 机制 | 成本 | 复杂度 | HoURP 当前关系 | 建议 |
| --- | --- | --- | --- | --- | --- | --- |
| 旧 HoSSS | `lilToon-URP-Extensions/Runtime/SubsurfaceScattering/HoSubsurfaceScattering.shader` | Burley-like disk diffusion、depth/normal/profile gate、transmission blur | M | 中 | 已经在旧项目使用，是 SSS 第一版最直接来源 | 抄思想和公式，重写命名、AOV 读取、资源生命周期 |
| 旧 HoSSS settings | `lilToon-URP-Extensions/Runtime/SubsurfaceScattering/HoSubsurfaceScatteringSettings.cs` | renderScale Full/Half/Quarter、quality 8/16/24 taps、最多 8 profiles、debug modes | S | 低 | 新 HoURP 已有 SSS semantic 和 debug contract | 可以直接转成 HoURP settings / profile table 设计 |
| 旧 Shoost Kuwahara | `lilToon-URP-Extensions/Runtime/ShoostPostProcessing/Shaders/Shoost/Kuwahara.shader` | 四象限 / 多窗口 Kuwahara、Sobel edge、posterize、noise | S-M | 中 | 旧 Shoost 已用；新 ImageChain 需要此类 filter 消费者 | 可作为 `Runtime/Filter/Image/HoUrpImageKuwahara.shader`，注意 radius O(r^2) 很贵 |
| 旧 Shoost RGBBlurV2 | `lilToon-URP-Extensions/Runtime/ShoostPostProcessing/Renderer/Effects/ShoostPostProcessPass.RGBBlurV2.cs` | downscale + 2-6 次 ping-pong blur + channel offset composite | M | 中 | 旧项目已用；新项目已有 Image.History planned | 可拆成显式 Kawase/Separable blur shader + final RGB composite |
| 旧 Shoost Glow | `lilToon-URP-Extensions/Runtime/ShoostPostProcessing/Renderer/Effects/ShoostPostProcessPass.Glow.cs` | 多 pass glow/bloom-like chain | M | 中 | 旧项目已用 | 迁移到显式 pyramid shader 调用，不保留私有 RTHandle |
| 旧 HoShadow PCSS | `HoUrp-Extensions/Runtime/ShadowCast/HoShadowCastRendererFeature.cs`、`lilToon-URP-Extensions/Runtime/ShadowCast/Shaders/HoShadowCastSampling.hlsl` | blocker search + PCSS filter，最高 32/64 samples | L | 中高 | 新 HoURP 已有 ShadowCast | 后续可接 shadow denoise helper，但不和 SSS 第一版绑定 |
| Falcor GaussianBlur | `Source/RenderPasses/Utils/GaussianBlur/GaussianBlur.ps.slang` | separable Gaussian blur pass | S | 低 | HoURP 需要基础 blur | 适合改写为 `SeparableBlur` 对照实现 |
| Falcor TAA | `Source/RenderPasses/TAA/TAA.ps.slang`、`Source/RenderPasses/TAA/TAA.cpp` | temporal AA、history reprojection、jitter、clamp | L | 高 | HoURP 只有 planned history 概念 | 先抄输入契约和 debug 项，不急着实现 |
| Falcor SVGF | `Source/RenderPasses/SVGFPass/SVGFAtrous.ps.slang`、`SVGFReproject.ps.slang`、`SVGFFilterMoments.ps.slang` | reprojection + moments + atrous wavelet denoise | L | 高 | 对未来 shadow/AO/GI denoise 有价值 | 作为 `DepthNormalAwareTemporalDenoise` 专项参考，不用于 SSS 第一版 |
| Falcor NRDPass | `Source/RenderPasses/NRDPass/NRDPass.cpp`、`PackRadiance.cs.slang` | Falcor 对 NRD 的接入包装 | XL | 高 | 可验证 native denoiser backend 怎么包 | 只看资源/参数桥接，不移植 framework |
| Falcor RenderGraph | `Source/Falcor/RenderGraph/*` | pass reflection、resource cache、graph compile/exe | XL | 高 | HoURP 已以 URP RenderGraph 为底座 | 只参考表达方式，不复制架构 |
| NRD REFERENCE | `Shaders/REFERENCE_TemporalAccumulation.cs.hlsl` | 最小 temporal accumulation reference | L | 中 | HoURP 后续 temporal filter 需要 | 比 REBLUR/RELAX 更适合作为第一份 temporal 阅读材料 |
| NRD SIGMA | `Shaders/SIGMA_*.cs.hlsl`、`Source/Denoisers/Sigma_Shadow*.hpp` | shadow denoise / temporal stabilization | L | 高 | 对 ShadowCast/RT shadow 后续有价值 | 未来 noisy shadow backend 参考，不用于皮肤 SSS |
| NRD REBLUR | `Shaders/REBLUR_*.cs.hlsl`、`Source/Denoisers/Reblur_*.hpp` | diffuse/specular/occlusion denoise，hit distance reconstruction，temporal accumulation | XL | 很高 | 对 ray traced AO/GI/reflection 有价值 | 只登记，不在 FilterKit 第一版实现 |
| NRD RELAX | `Shaders/RELAX_*.cs.hlsl`、`Source/Denoisers/Relax_*.hpp` | atrous + history clamp + hit distance reconstruction | XL | 很高 | 对高级 denoise 有价值 | 只做算法地图和 guide checklist |
| FidelityFX SPD | `Kits/FidelityFX/upscalers/fsr3/include/gpu/spd/ffx_spd.h`、`Kits/Cauldron2/dx12/framework/shaders/fidelityfx/spd/ffx_spd.h` | single-pass downsampler / mip pyramid | M | 中 | HoURP Bloom/Glow/SSS downscale 都需要 pyramid | 值得优先研究，改写成显式 pyramid shader/compute pass |
| FidelityFX FSR1 CAS/RCAS | `Kits/FidelityFX/upscalers/fsr3/include/gpu/fsr1/ffx_fsr1.h`、`Kits/FidelityFX/upscalers/fsr3/include/gpu/fsr2/ffx_fsr2_rcas.h` | edge-adaptive upscale / sharpening | S-M | 中 | 后处理可能需要锐化/低成本 upscale | 可作为 ImageChain sharpen/upscale 参考，license 登记后再 port |
| FidelityFX FSR2/FSR3 upscaler | `Kits/FidelityFX/upscalers/fsr3/include/gpu/fsr2/*`、`gpu/fsr3upscaler/*`、`internal/shaders/*` | temporal upscale、reactive mask、lock、luma pyramid、reproject | XL | 很高 | HoURP 尚未稳定 motion/history/presentation | 暂缓，先抄输入契约和 debug overlay 思路 |
| FidelityFX frame interpolation | `Kits/FidelityFX/framegeneration/fsr3/include/gpu/frameinterpolation/*`、`internal/shaders/*` | optical flow、disocclusion、inpainting、swapchain UI composition | XL | 很高 | 需要 present/swapchain/UI 边界 | 明确不进第十五步，只做远期参考 |
| FidelityFX denoiser | `Kits/FidelityFX/denoisers/include/ffx_denoiser.h`、`Samples/Denoisers/FidelityFX_Denoiser/dx12/*` | ray denoise sample/API wrapper | XL | 高 | HoURP 还没有 native backend | 只看 API 组织和 sample debug，不移植 |
| Filament separable blur | `filament/src/materials/separableGaussianBlur.fs`、`.mat`、`.vs` | separable Gaussian blur | S | 低 | HoURP FilterKit 第一版需要 | 可和 Falcor GaussianBlur 对照，改写成本低 |
| Filament bloom | `filament/src/materials/bloom/*` | bloom downsample/upsample chain | M | 中 | 旧 Shoost Glow 已有类似需求 | 适合作为 Bloom/Glow pyramid 设计参考 |
| Filament SSAO blur | `filament/src/materials/ssao/bilateralBlur.mat`、`bilateralBlurBentNormals.mat`、`ssaoUtils.fs` | depth/normal guided bilateral blur | M | 中 | SSS diffusion 和 AO 都需要 edge-aware gate | 很值得借鉴 gate 设计，但要接 HoURP guide inputs |
| Filament DOF | `filament/src/materials/dof/*` | CoC、tiles、dilate、median、combine | L | 高 | 旧 HoPost 有 DOF | 后处理专项参考，不进 SSS 阶段 |
| Filament TAA/FXAA | `filament/src/materials/antiAliasing/taa/*`、`fxaa/*` | TAA / FXAA material passes | M-L | 中高 | HoURP 将来需要 image temporal | FXAA 可先看，TAA 等 history 成熟后再做 |
| Filament FSR1 | `filament/src/materials/fsr/*` | FSR1 EASU/RCAS mobile-oriented implementation | S-M | 中 | 和 FidelityFX FSR1 同源，但更接近 engine shader port | 比完整 FidelityFX-SDK 更适合作为轻量参考 |
| Filament post manager | `filament/src/PostProcessManager.cpp`、`.h` | post chain 编排、resource discipline | L | 高 | HoURP 正在规划 ImageChain | 只参考 pass 顺序和资源策略，不移植 engine abstraction |
| Filament subsurface shading | `shaders/src/surface_shading_model_subsurface.fs`、`samples/materials/sandboxSubsurface.mat` | material-level subsurface / thickness / cloth-like shading | S-M | 中 | fSSS forward lobe 可参考，但 screen-space SSS 不直接用 | 可帮助修 forward thin scatter，不替代 screen-space diffusion |
| pbrt BSSRDF | `src/pbrt/bssrdf.cpp`、`src/pbrt/bssrdf.h`、`src/pbrt/wavefront/subsurface.cpp` | BeamDiffusionSS/MS、BSSRDF table、profile CDF | M-L | 高 | HoURP SSS profile 需要物理参考 | 不抄 runtime；可离线生成/校验 diffusion profile |
| pbrt sampling | `src/pbrt/util/sampling.cpp`、`.h` | sampling/CDF utilities | M | 中 | SSS profile sampling、blue-noise/importance ideas可参考 | 只取思想，实时 shader 用简化版本 |

### 6.10 近期优先级判断

第十五步 / 第十六步最值得做：

1. 旧 HoSSS Burley-like diffusion + depth/normal/profile gate：这是我们已有、可控、最贴近需求的 SSS 第一版来源。
2. Filament / Falcor separable Gaussian blur：作为 `SeparableBlur` baseline，成本最低。
3. Filament SSAO bilateral blur：作为 `DepthNormalAwareBlur` / `ProfileAwareDiffusion` guide gate 对照。
4. FidelityFX SPD：作为 pyramid / Bloom / Glow / downscale 资源策略参考。
5. 旧 Shoost RGBBlurV2 / Glow / Kuwahara：作为 FilterKit 第二批消费者，验证 ImageChain 和 transient resource。

暂缓但要登记：

- NRD REBLUR / RELAX / SIGMA：算法和资源链路太重，适合未来 noisy shadow / AO / GI / reflection，不适合作为皮肤 SSS 第一版。
- Falcor SVGF / TAA：需要稳定 motion vector、history、jitter、reprojection debug。
- FidelityFX FSR2 / FSR3 / frame interpolation：需要 motion/history/presentation/UI composition 全链路，不应混进 FilterKit 第一版。
- Filament DOF：和后处理专项更相关，等 ImageChain 成熟后做。

已经在我们系统里有对应物的内容：

- SSS profile / Burley disk diffusion / transmission blur：旧 HoSSS 已实现，新 HoURP 已有 `Sss.Source` / `Sss.Diffusion` / profile semantic。
- Glow / RGBBlur / IrisBlur / Kuwahara：旧 Shoost 已实现，新系统需要通过 ImageChain + FilterKit 重写 shader 复用和资源管理。
- PCSS soft shadow：HoUrp-Extensions 和旧项目都有 ShadowCast PCSS，后续可以接 denoise，但不是 SSS 依赖。
- History 概念：新 HoURP 已有 `Image.History` / `Image.History.Planned` prototype，但还没有完整 temporal filter。

## 7. 真 screen-space SSS 第一版实现规划

### 7.1 输入

SSS runtime 消费：

- `Aov.MaskId`
- `Aov.NormalDepth`
- `Aov.SurfaceData`
- `Aov.Diffuse`
- camera color copy 或 current scene color
- optional depth texture

从语义上读取：

- `Object.FeatureFlags.ReceiveSss`
- `Material.SssProfile`
- `Material.Thickness`
- `Material.Curvature`
- `Aov.Diffuse.rgb` as source color

不再读取：

- fSSS forward lobe 结果。
- `Aov.Diffuse.a` 作为 SSS weight。
- 旧 `_lilHoAovSssTexture`。

### 7.2 输出

SSS runtime 生产：

- `Sss.Source`
  - RGB：prepared source color。
  - A：runtime participation / source validity。
- `Sss.Diffusion`
  - RGB：diffused color。
  - A：composite weight。
- 可选 `Sss.Transmission`
- 可选 `Sss.DebugMask`

这些属于 SSS runtime 私有资源，不回写 AOV。

### 7.3 Pass 链路

第一版建议：

```text
AOV already produced
  -> SSS Source Prepare
      reads Aov.MaskId / NormalDepth / SurfaceData / Diffuse
      writes Sss.Source
  -> HoUrpSssDiffusion pass
      reads Sss.Source + depth/normal/profile/thickness/curvature
      writes Sss.Diffusion
  -> SSS Composite
      reads CameraColorCopy + Sss.Source + Sss.Diffusion
      writes CameraColor
```

如果需要 separable 版本：

```text
Sss.Source
  -> Sss.TempX
  -> Sss.Diffusion
```

但 `Sss.TempX` 必须是调用方在当前 RenderGraph 里显式创建的 frame transient，不是 SubsurfaceScatteringRendererFeature 长期持有的私有 RTHandle 字段。

### 7.4 第一版算法选择

从旧实现迁移时按优先级处理：

1. `Source Prepare`
   - 重建 ReceivesSss gate。
   - 计算 profile / thickness / curvature gate。
   - 维护 `Sss.Source.a`。
2. `ProfileAwareDiffusion`
   - 第一版可先用旧 Burley-like disk 或 separable diffusion 思路。
   - 必须用 depth / normal gate 防止跨边界漏色。
   - radius 由 profile radius * thickness / mask 控制。
3. `Composite`
   - `Sss.Diffusion.a` 作为 composite weight。
   - 不把 diffusion 结果直接覆盖 camera color。
   - debug view 能单独看 source / diffusion / weight / profile。

### 7.5 Profile 数据

第一版沿用 “最多 8 个 profile” 可以接受，但要改成 HoURP runtime profile table：

```text
SssProfile
{
    Id
    Radius
    SourcePreserve
    DiffusionColor
    Shape
    CompositeStrength
    TransmissionStrength
}
```

要求：

- profile table 属于 SSS runtime settings，不属于材质系统。
- 材质只输出 profile id / thickness / curvature / source color。
- debug view 显示当前 pixel 使用的 profile id、radius、participation。

## 8. ImageChain 与后处理承接

第十五步不重写 Shoost，但要为后续第十六步或后续阶段定边界。

### 8.1 ImageChain 第一版

`ImageChain` 服务纯 ImageDomain：

```text
Begin(cameraColorCopy)
  Current = WorkA
  Next = WorkB

For each image pass:
  Record(Current -> Next)
  Swap()

End()
  Copy Current -> CameraColor
```

适用：

- Color adjust
- VHS / CRT 类单 pass 风格
- simple RGB offset
- simple blur
- film grain
- posterize / tone / color curve

不适用：

- 需要 AOV rule 的 ScreenPost。
- 需要 SSS / OIT / Shadow / Character semantic 的 composite。
- 需要 history 的 temporal effect。
- 需要 pyramid 的 bloom / glow。
- 需要多输出的复杂 effect。

### 8.2 Image effect 使用 FilterKit 的方式

普通 image pass 不直接创建 blur texture。

当 effect 需要 filter：

```text
ImagePassDescriptor
{
    Input = ImageChain.Current
    Output = ImageChain.Next
    NeedsOriginalSource
    UsesFilterShader
    ExtraInputs
    DebugView
}
```

例如 RGBBlurV2：

- 显式调用 `HoUrpImageRgbBlur.shader` 或 `HoUrpFilterBlur.shader`。
- ImageChain / effect 自己创建 blur transient。
- Composite pass 读取 blurred result。

例如 Glow / Bloom：

- 显式调用 `HoUrpFilterPyramid.shader` 的 downsample / upsample pass。
- pyramid resources 由 Glow / Bloom 调用方创建，但命名、格式、pass 顺序参考 FilterKit 文档。

例如 MotionTrail：

- 使用后续 temporal helper。
- 调用方明确 history texture、reset 条件、camera cut 规则。

## 9. 旧实现迁移对照

### 9.1 旧 HoSSS

旧实现有价值的部分：

- `Source`、`Diffusion`、`Transmission`、`Composite` 的阶段拆分。
- profile table 和 quality 控制。
- depth / normal / profile gate。
- 先 source、再扩散、再 composite 的执行顺序。

不能继承的部分：

- 旧 `_lilHoSSS*` 命名。
- feature 私有 temp RT 生命周期。
- compatibility path。
- old HoAOV SSS source texture ABI。
- 直接用全局纹理作为跨 pass 数据流。

### 9.2 旧 Shoost / HoPost

旧实现有价值的部分：

- Shoost effect descriptor / registry。
- HoPost AOV rule 语言。
- ping-pong temp 思路。
- 多 pass effect 的拆分经验。
- RGBBlurV2、Kuwahara、IrisBlur、Glow 的资源需求样本。

不能继承的部分：

- 每个 effect 自己私有创建 blur / temp / history。
- RenderGraph 路径中按 layer 任意创建独立 texture。
- 旧 `_lilShoost*` 命名。
- Shoost 反向定义 ImageDomain 的资源策略。

## 10. 本阶段计划输出

### 15.a Filter / 外部渲染库架构审查文档

输出：

- `Documentation~/rp重构第十五步/rpFilter与外部渲染库架构审查.md`
- `Documentation~/rp重构第十五步/rp第十五步具体执行总清单.md`
- `Documentation~/rp重构第十五步/rp第十五步FilterKit具体实施步骤.md`
- `Documentation~/rp重构第十五步/rp外部滤波算法借鉴清单.md`

内容：

- 当前 HoURP 已有 Filter / Image / PostProcess 目录和缺口。
- 旧 HoSSS、HoPost、Shoost 中实际存在的 filter / temp / history 需求。
- NRD / Falcor / FidelityFX-SDK / Filament / pbrt-v4 的可用边界、不可直接继承点、license / source tracking 要求。
- FilterKit 的第一版文件清单、可抄算法入口、debug 约束和资源生命周期规则。

### 15.b External Rendering References / Vendor 引入清单

输出：

- `Runtime/Filter/Nrd/README.md`
- `Runtime/Filter/Nrd/SOURCE.md`
- 可选 `Runtime/Filter/Nrd/Vendor/NRD/`
- `Runtime/Filter/ThirdParty/EXTERNAL_RENDERING_REFERENCES.md`

内容：

- 外部库仓库 URL。
- 固定 commit hash。
- 引入日期。
- license 文件位置。
- 未编译 / 已编译目录标记。
- HoURP 使用方式：reference only、algorithm port、shader port、native backend、offline validation、disabled。
- 平台支持状态。

验收：

- 没有来源记录时，不允许出现外部库代码文件。
- 没有 license 文件时，不允许进入 runtime。
- vendor 目录不能被误注册为 HoURP 原生 namespace。

### 15.c FilterKit 最小源码库

输出：

- `Runtime/Filter/HoUrpFilterIds.cs`
- `Runtime/Filter/HoUrpFilterUtils.cs`
- `Runtime/Filter/HoUrpFilterResources.cs`
- `Runtime/Filter/Shaders/HoUrpFilterCommon.hlsl`
- `Runtime/Filter/Shaders/HoUrpFilterSampling.hlsl`
- `Runtime/Filter/Shaders/HoUrpFilterDepthNormalGate.hlsl`
- `Runtime/Filter/Shaders/HoUrpFilterBurleyDiffusion.hlsl`
- `Runtime/Filter/Shaders/HoUrpFilterBlur.shader`
- `Runtime/Filter/Shaders/HoUrpFilterPyramid.shader`
- `Runtime/Filter/Shaders/HoUrpFilterDebug.shader`

第一批规则：

- C# helper 不能创建 RenderGraph pass，只能提供公共 id、pass index、descriptor / viewport / texel 参数工具。
- Shader pass index 必须写进 `HoUrpFilterIds.cs`，不能靠魔法数字散落。
- 每个 shader 顶部写输入纹理、输出语义、pass 用途、预期采样数。
- 每个算法先提供一个最小 debug mode：source、weight/gate、result。

验收：

- SSS 可以直接 include Burley / gate 函数。
- 一个 simple image blur 可以直接调用 `HoUrpFilterBlur.shader`。
- pyramid 可以用单独 shader pass 显式调用。
- 没有 `FilterRequest` / scheduler / 自动调度类。
- 任意一个 filter pass 都能在 Frame Debugger / RenderDoc 中看到明确 pass name。

### 15.d HoURP-native SSS Filter 第一版

输出：

- `Documentation~/rp重构第十五步/rpSSS_FilterKit落地执行计划.md`
- 更新 `Runtime/Features/SubsurfaceScatteringRendererFeature.cs`。
- 更新 `Runtime/Shaders/Hidden/HoURP/SSS/SubsurfaceScattering.shader` 或拆分 SSS shader。
- 新增或拆分 `Runtime/Filter/SSS/HoUrpSssDiffusion.shader`。
- 新增 debug view / tests。

工作：

- Source Prepare 继续维护 `Sss.Source.a`。
- Diffusion 显式调用 SSS diffusion pass，复用 FilterKit HLSL include。
- Composite 读取 `Sss.Diffusion.a`。
- 旧 HoSSS 的 Burley / separable 思路只作为算法参考。
- 不引入 NRD 或其他 external rendering library 作为 SSS 第一版必要依赖。

验收：

- 没有 ReceivesSss 的对象不进入 `Sss.Source.a`。
- `Aov.Diffuse.a` 不影响 SSS weight。
- profile / thickness / curvature 改变 diffusion 范围和强度。
- depth / normal discontinuity 不明显漏色。
- Debug 能显示 `Sss.Source`、`Sss.Diffusion`、`Sss.CompositeWeight`、`SssProfileId`。

### 15.e ImageChain 资源策略文档与最小骨架

输出：

- `Documentation~/rp重构第十五步/rpImageChain与FilterKit资源策略.md`
- `Runtime/Image/ImageChain.cs` 等最小骨架。

工作：

- 定义 WorkA / WorkB 的 frame transient 生命周期。
- 定义 OriginalSource copy 何时需要。
- 定义 pure image pass 与 semantic post pass 的边界。
- 定义何时可以复用 FilterKit shader，何时 effect 必须自己显式创建额外 transient。

验收：

- 普通线性 image pass 数量增加时，全分辨率工作 RT 不随 pass 数量线性增长。
- 需要 pyramid / history / original source 的 pass 必须显式声明例外。
- Debug 能观察 ImageChain 当前 read / write / final source。

### 15.f 静态测试与违规扫描

输出：

- `Documentation~/rp重构第十五步/rp第十五阶段测试与验收清单.md`
- `Tests/Runtime/HoUrpFilterContractTests.cs`
- 扩展已有资源 / debug / material ABI 测试。

测试重点：

- FilterKit helper 不能创建隐藏 RenderGraph pass 或长期 RT。
- SSS diffusion shader 必须声明 depth / normal / profile / thickness guide。
- 通用 blur shader 不允许读取 AOV。
- SSS runtime 不读取旧 `_lilHoSSS*` / `_lilHoAovSssTexture`。
- SSS runtime 不从 `Aov.Diffuse.a` 读取 weight。
- External vendor 如果存在，必须存在 `SOURCE.md` 和 `LICENSE.txt`。
- HoURP runtime 不出现未登记的 external source file 编译入口。
- ImageChain WorkA / WorkB 不注册为长期公共 semantic resource。

## 11. 建议实施顺序

1. 写 `rpFilter与外部渲染库架构审查.md`，核对旧 HoSSS / Shoost 的 filter 需求。
2. 建立 `Runtime/Filter/` 目录和最小 HLSL / shader / C# helper 文件。
3. 建立 external rendering references / vendor 规则文档；如要 clone，先只落隔离目录和 manifest。
4. 把旧 HoSSS Burley / depth-normal-profile gate 拆进 FilterKit include。
5. 补 SSS shader / renderer feature 的具体 pass 计划。
6. 补 debug view：source、diffusion、weight、profile、guide failure。
7. 建立 ImageChain 最小骨架和资源策略。
8. 将 ImagePost prototype 改为走 ImageChain。
9. 选择一个简单 image blur 作为 FilterKit 第二个消费者。
10. 再考虑 Glow / RGBBlurV2 / IrisBlur / Kuwahara 等多 pass effect 的迁移。

## 12. 风险

- 直接复制 NRD / Falcor / FidelityFX-SDK / Filament / pbrt-v4 代码后忘记 license / commit / 来源，污染仓库。
- 误以为 NRD 可以直接替代皮肤 SSS diffusion，或误以为 Falcor / FidelityFX-SDK 可以直接替代 HoURP 的 RenderGraph / post pipeline。
- FilterKit 又被包成过重的 request / scheduler，调用链变得看不懂。
- SSS 为了快速看到效果复制私有滤波公式，后续很难收敛。
- ImageChain 被 HoPost / Shoost 混用，导致语义后处理和纯图像后处理边界再次模糊。
- 每个效果都创建独立 full-res temp，失去资源复用价值。
- Debug 仍只看最终画面，看不到 filter guide / intermediate resource。

## 13. 验收标准

- 第十五步文档明确：FilterKit 是轻量源码库，外部渲染库是受控 third-party 来源，不是 HoURP 核心 ABI。
- `Runtime/Filter/` 有可扩展但简洁的 HLSL / shader / C# helper 文件，至少能支撑 SSS diffusion 和 simple image blur。
- 真 SSS 具体实现计划不依赖旧 `_lilHoSSS*` 命名和旧私有 temp RT。
- SSS 的 source / diffusion / composite 权重边界保持第十四步结论。
- 外部渲染库如果被 clone，必须固定 commit、保留 license、写来源 manifest。
- ImageChain 和 FilterKit 的职责分开：ImageChain 管线性 image pass 和 transient，FilterKit 只提供可复用算法文件。
- Debug / tests 能阻止旧 ABI、私有 temp RT、未登记 third-party 代码回流。

## 14. 第十五步之后

第十五步完成后，下一阶段再进入：

- 真 SSS 的实际代码实现和场景验收。
- ScreenPost rule mask 的稳定化。
- ImagePost / Shoost effect 逐步迁移到 ImageChain。
- Bloom / Glow / RGBBlur / IrisBlur / Kuwahara 等滤波效果迁移。
- Temporal / motion vector / history 体系。
- NRD backend 可行性验证，优先用于 noisy shadow / AO / reflection，而不是第一版皮肤 SSS。
- FidelityFX-SDK / Falcor 的 upscale、denoise、frame generation、render graph 样例继续作为专项审查，不进入第十五步默认实现范围。

第十五步底线：**先把滤波变成一组看得懂、调得动、能复用的 HLSL + C# 文件，再让 SSS 和后处理显式调用它；旧实现和外部渲染库都只能提供事实与算法参考，不能反向定义 HoURP 的资源和语义契约。**

## 15. 本轮执行记录

已落地：

- 新增 `Runtime/Filter/` 轻量 FilterKit 骨架：`HoUrpFilterIds.cs`、`HoUrpFilterUtils.cs`、`HoUrpFilterResources.cs`。
- 新增通用 include：`HoUrpFilterCommon.hlsl`、`HoUrpFilterSampling.hlsl`、`HoUrpFilterDepthNormalGate.hlsl`、`HoUrpFilterBurleyDiffusion.hlsl`。
- 新增基础 shader：`Runtime/Filter/Shaders/HoUrpFilterBlur.shader`，包含 Copy、Separable Blur、Depth Normal Aware Blur 三个可定位 pass。
- 新增 SSS glue include：`Runtime/Filter/SSS/HoUrpSssFilter.hlsl`。
- `SubsurfaceScatteringRendererFeature` 已改用 `HoUrpFilterIds` 的 SSS pass index，RenderGraph pass name 与 shader pass name 对齐。
- `SubsurfaceScattering.shader` 已 include FilterKit/SSS helper，Source / Diffusion / Composite pass 名称改为 `HoURP SSS Source Prepare`、`HoURP SSS Profile Diffusion`、`HoURP SSS Composite`。
- SSS 参数已从固定 8 sample 扩展为 `Low/Medium/High = 8/16/24`，并新增 Full/Half/Quarter render scale；Diffusion pass 会按 render scale 补偿半径。
- SSS Profile 已新增 `compositeStrength` 与 `transmissionStrength`，profile 数据打包进 `_HoUrpSssProfileShapeParams`，不再只靠全局强度控制最终混合。
- SSS Composite 已新增 debug mode：Source、SourceMask、Diffusion、CompositeWeight、ProfileId、Thickness、DiffusionRadius、TransmissionApprox。
- SSS Composite 已加入无额外 RT 的近似透射项：全局 `transmissionStrength/radius/edgeBoost/color` 乘以 profile transmission、source alpha、surface guide 后叠加到 composite target。
- SSS 已新增额外 RT `Sss.Transmission`：Source / Diffusion 后增加 `HoURP SSS Transmission` pass，显式写入 `_HoUrpSssTransmissionTexture`，Composite 只消费该 RT 并叠加 `transmission.rgb`。
- `Sss.Transmission` 已进入 contract：新增 `Shading.SssTransmissionColor` semantic、`SSS.Transmission` debug view、RenderCacheDebug shader mode 44，以及资源声明/测试覆盖。
- SSS Transmission 已补投射方向与投射轮廓：新增 `_HoUrpSssTransmissionDirection` 与 `_HoUrpSssTransmissionShapeParams`，Transmission pass 按旧 HoSSS 方向模型由主光 view-space 方向、view normal 出射方向、轮廓切线混合得到屏幕投射方向；显式方向只作为主光方向退化时的 fallback。
- SSS Transmission 已针对方向性投射条带/分界做软化：depth / normal / thickness / profile gate 改为 smoothstep 过渡，方向投射 taps 提升到 6-16，并加入距离 falloff 与同 profile 小邻域稳定。
- SSS feature debug mode 已拆出 Transmission、TransmissionDirection、TransmissionContour，方便分别验收透射 RT、方向场和轮廓权重。
- ImagePost prototype 增加可选 simple blur 消费者，调用 `HoUrpFilterBlur.shader` 的 separable blur pass，并继续复用 ImageChain 风格的 WorkA / WorkB ping-pong，不增加长期 full-res RT 字段。
- 新增 `Runtime/Filter/ThirdParty/EXTERNAL_RENDERING_REFERENCES.md`，登记旧 HoSSS、Falcor、Filament、FidelityFX、NRD、pbrt-v4 的本轮用途与 `reference only` 状态。
- 新增 `Tests/Runtime/HoUrpFilterContractTests.cs`，覆盖 FilterKit 无 scheduler/request、通用 include 不绑定纹理、通用 blur 不读 AOV/SSS、SSS 不读旧 ABI、pass index 常量存在等静态契约。
- 扩展 `HoUrpShaderPropertyIds` 与 ABI 静态测试，锁定 `_HoUrpSssParams`、`_HoUrpSssDebugMode`、`_HoUrpSssTransmissionParams`、`_HoUrpSssTransmissionColor`。

未做项：

- 未实现 NRD backend。
- 未做 temporal SSS / temporal denoise。
- 未做 FidelityFX FSR / frame interpolation。
- 未做 DOF。
- 未实现完整 pyramid shader、Kuwahara、RGB blur、Glow 迁移。
- 未在本轮引入任何外部库源码到 runtime。

本轮静态验收：

- `Runtime/Filter` 代码和 shader 中未发现 `FilterRequest`、`class .*FilterGraph`、`scheduler`。
- `Runtime/Filter/HoUrpFilter*.cs` 未发现 `CreateTexture`、`RTHandle`、`TextureHandle` 长期资源字段。
- `Runtime/Features`、`Runtime/Image`、`Runtime/PostProcess` 未发现新增 `material, [0-9]` 魔法 pass index。
- `Runtime/Filter` 和 SSS shader 未发现 `_lilHoSSS` / `_lilHoAovSssTexture`。
- `HoUrpFilterCommon.hlsl` 与 `HoUrpFilterBurleyDiffusion.hlsl` 未声明 `TEXTURE2D`。
- `SubsurfaceScattering.shader` 中 `_HoUrpAovDiffuseTexture` 只在 Source Prepare pass 读取，Diffusion / Composite 不回读 diffuse AOV。
- `git diff --check` 已通过，仅报告当前工作区 LF/CRLF 提示。
