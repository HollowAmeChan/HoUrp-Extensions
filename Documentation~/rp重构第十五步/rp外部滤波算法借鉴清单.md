# 外部滤波算法借鉴清单

> 本文记录第十五步可以打开借鉴的具体代码位置。默认策略是“看、摘公式、改写、登记来源”，不是复制目录。所有进入 runtime 的外部代码都必须有 `SOURCE.md` / license / commit 记录。

## 来源 commit

本轮浅 clone 只用于目录和算法定位：

| 仓库 | commit |
| --- | --- |
| `NVIDIA-RTX/NRD` | `76073627829f7abc4f4624ef1127c8e6464ce1c2` |
| `NVIDIAGameWorks/Falcor` | `eb540f6748774680ce0039aaf3ac9279266ec521` |
| `GPUOpen-LibrariesAndSDKs/FidelityFX-SDK` | `e236f2304dcda35f282fdddd085f41e2ff48c86a` |
| `google/filament` | `c28dfde4b23af06f4ed796e2b35c9cf2ae8152e8` |
| `mmp/pbrt-v4` | `7154d8268ba1f512b20f25e6826999e346d02a15` |

## 成本等级

| 等级 | 含义 |
| --- | --- |
| S | 一个 shader/pass 级别，可直接改写调试。 |
| M | 多 pass 或需要调用方管理 transient。 |
| L | 需要 history、motion vector、guide、compute 或较多 debug。 |
| XL | 依赖 native SDK、swapchain、ray tracing 或完整 renderer，近期只登记。 |

## 最优先借鉴

| 来源 | 位置 | 用途 | 成本 | 第十五步动作 |
| --- | --- | --- | --- | --- |
| 旧 HoSSS | `lilToon-URP-Extensions/Runtime/SubsurfaceScattering/HoSubsurfaceScattering.shader` | Burley-like diffusion、depth/normal/profile gate、transmission blur | M | 拆出 `HoUrpFilterBurleyDiffusion.hlsl` 和 SSS gate 调用 |
| 旧 HoSSS settings | `lilToon-URP-Extensions/Runtime/SubsurfaceScattering/HoSubsurfaceScatteringSettings.cs` | 8/16/24 taps、Full/Half/Quarter、最多 8 profile、debug modes | S | 转成 HoURP SSS settings / debug 名称 |
| Falcor GaussianBlur | `Source/RenderPasses/Utils/GaussianBlur/GaussianBlur.ps.slang` | separable Gaussian blur | S | 对照写 `HoUrpFilterBlur.shader` |
| Filament blur | `filament/src/materials/separableGaussianBlur.fs` | separable Gaussian blur | S | 对照采样和移动端写法 |
| Filament SSAO blur | `filament/src/materials/ssao/bilateralBlur.mat`、`ssaoUtils.fs` | depth/normal guided bilateral gate | M | 对照 `HoUrpFilterDepthNormalGate.hlsl` |
| FidelityFX SPD | `Kits/FidelityFX/upscalers/fsr3/include/gpu/spd/ffx_spd.h` | single-pass downsample / pyramid | M | 第一版只参考，不复制；先写普通 pyramid shader |

## 旧项目已在用

| 能力 | 旧位置 | 当前判断 |
| --- | --- | --- |
| SSS diffusion | `Runtime/SubsurfaceScattering/HoSubsurfaceScattering.shader` | 最适合做第一版 screen-space SSS 来源。 |
| SSS quality/profile/debug | `Runtime/SubsurfaceScattering/HoSubsurfaceScatteringSettings.cs` | 可直接转成新 settings 思路。 |
| RGBBlurV2 | `Runtime/ShoostPostProcessing/Renderer/Effects/ShoostPostProcessPass.RGBBlurV2.cs` | 后续迁移到 ImageChain + FilterKit。 |
| Glow | `Runtime/ShoostPostProcessing/Renderer/Effects/ShoostPostProcessPass.Glow.cs` | 后续迁移到 pyramid shader。 |
| Kuwahara | `Runtime/ShoostPostProcessing/Shaders/Shoost/Kuwahara.shader` | 可作为 Image filter 第二批；注意 O(r^2) 成本。 |
| PCSS | `Runtime/ShadowCast/Shaders/HoShadowCastSampling.hlsl` | 已在 ShadowCast 链路，不参与 SSS 第一版。 |

## 近期不抄，只登记

| 来源 | 位置 | 原因 |
| --- | --- | --- |
| NRD REBLUR | `Shaders/REBLUR_*.cs.hlsl`、`Source/Denoisers/Reblur_*.hpp` | 多 pass temporal denoise，依赖 hit distance / history / validation，太重。 |
| NRD RELAX | `Shaders/RELAX_*.cs.hlsl`、`Source/Denoisers/Relax_*.hpp` | atrous + history clamp，适合未来 ray denoise，不适合 SSS 第一版。 |
| NRD SIGMA | `Shaders/SIGMA_*.cs.hlsl` | shadow denoise 候选，等 ShadowCast noisy signal 出现后再看。 |
| Falcor SVGF | `Source/RenderPasses/SVGFPass/*` | 需要 reprojection/moments/history。 |
| Falcor TAA | `Source/RenderPasses/TAA/*` | 需要 jitter / motion vector / history reset。 |
| FidelityFX FSR2/FSR3 | `Kits/FidelityFX/upscalers/fsr3/include/gpu/fsr2/*`、`gpu/fsr3upscaler/*` | temporal upscale 依赖完整 history 和 reactive mask。 |
| FidelityFX frame interpolation | `Kits/FidelityFX/framegeneration/fsr3/include/gpu/frameinterpolation/*` | 依赖 swapchain / UI composition / optical flow。 |
| Filament DOF | `filament/src/materials/dof/*` | 后处理专项，等 ImageChain 稳定后再做。 |
| pbrt BSSRDF | `src/pbrt/bssrdf.cpp` | 离线物理参考，不进 runtime。 |

## 登记规则

如果从外部库移植任何公式、shader 结构或代码片段，必须同步新增或更新：

```text
Runtime/Filter/ThirdParty/EXTERNAL_RENDERING_REFERENCES.md
```

记录：

- 来源仓库 URL。
- commit。
- 文件路径。
- license。
- 使用方式：reference only / formula rewrite / shader port / native backend。
- HoURP 落点文件。

禁止：

- 把外部仓库目录直接复制进 `Runtime/Filter/Shaders`。
- 改名后当作 HoURP 原生代码。
- 在没有 license 记录时进入 asmdef 编译范围。
