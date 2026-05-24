# 第十五步 FilterKit 具体实施步骤

> 本文只规划可直接落地的轻量 FilterKit。第十五步不实现 `FilterRequest`、不做 scheduler、不封装第二套 RenderGraph。滤波调用链必须能在 shader、RendererFeature、Frame Debugger / RenderDoc 里直接追踪。

## Step 1. 建立最小目录

新增：

```text
Runtime/Filter/
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
```

要求：

- `Runtime/Filter/*.cs` 只允许放轻量 helper。
- C# helper 不创建 RenderGraph pass。
- C# helper 不长期持有 RTHandle / TextureHandle。
- Shader 文件必须有稳定 pass name。
- HLSL include 不声明业务纹理，避免通用函数偷偷绑定 AOV / SSS 资源。

验收：

- `rg "class .*FilterGraph|FilterRequest|scheduler" Runtime/Filter` 没有结果。
- `rg "CreateTexture|RTHandle|TextureHandle" Runtime/Filter/HoUrpFilter*.cs` 没有长期资源创建。

## Step 2. 定义 pass id 和 property id

新增：`Runtime/Filter/HoUrpFilterIds.cs`

建议内容：

```csharp
namespace Hollow.Rendering.HoUrp.Filter
{
    internal static class HoUrpFilterIds
    {
        public const string BlurShaderName = "Hidden/HoURP/Filter/Blur";
        public const string PyramidShaderName = "Hidden/HoURP/Filter/Pyramid";
        public const string SssDiffusionShaderName = "Hidden/HoURP/Filter/SSS/Diffusion";

        public const int CopyPass = 0;
        public const int SeparableBlurPass = 1;
        public const int DepthNormalAwareBlurPass = 2;

        public const int PyramidDownsamplePass = 0;
        public const int PyramidUpsamplePass = 1;

        public const int SssSourcePreparePass = 0;
        public const int SssDiffusionPass = 1;
        public const int SssCompositePass = 2;
        public const int SssDebugPass = 3;
    }
}
```

要求：

- RendererFeature 里不能散落魔法 pass index。
- shader pass 调整时必须同步这里。
- pass name 和 const 名称保持一致，方便 Frame Debugger 定位。

验收：

- `rg "material, [0-9]" Runtime/Features Runtime/Image Runtime/PostProcess` 不出现新增魔法 index。
- shader pass name 能和 `HoUrpFilterIds` 对上。

## Step 3. 写通用 HLSL include

新增：`Runtime/Filter/Shaders/HoUrpFilterCommon.hlsl`

内容边界：

- `HoFilterSafeNormalize`
- `HoFilterGetTexelSize`
- `HoFilterLuma`
- `HoFilterSaturateWeight`
- `HoFilterEncodeDebugWeight`

不允许：

- 绑定 `_CameraDepthTexture`。
- 绑定 `Aov.*`。
- 绑定 `Sss.*`。

验收：

- `HoUrpFilterCommon.hlsl` 不包含 `TEXTURE2D`。
- 它可以被 SSS / Image / Shadow 任意 include。

## Step 4. 写 sampling helper

新增：`Runtime/Filter/Shaders/HoUrpFilterSampling.hlsl`

建议函数：

```hlsl
float HoFilterInterleavedNoise(float2 uv, float2 screenSize);
float2 HoFilterGoldenAngleOffset(int sampleIndex, float radius, float phase);
float2 HoFilterAxisOffset(float2 texelSize, float2 direction, float radius, int index);
```

来源参考：

- 旧 HoSSS：`HoSSSInterleavedNoise`、`HoSSSRotateOffset`
- Falcor GaussianBlur：`Source/RenderPasses/Utils/GaussianBlur/GaussianBlur.ps.slang`
- Filament separable blur：`filament/src/materials/separableGaussianBlur.fs`

验收：

- 函数不采样纹理。
- 所有 sample count 由调用 shader 控制。

## Step 5. 写 depth/normal/profile gate

新增：`Runtime/Filter/Shaders/HoUrpFilterDepthNormalGate.hlsl`

建议函数：

```hlsl
float HoFilterDepthGate(float sampleDepth, float centerDepth, float tolerance);
float HoFilterNormalGate(float3 sampleNormal, float3 centerNormal, float tolerance);
float HoFilterByteProfileGate(float sampleProfileByte, float centerProfileByte);
float HoFilterMaskGate(float mask);
```

来源参考：

- 旧 HoSSS：`HoSSSDepthGate`、`HoSSSNormalGate`、`HoSSSProfileGate`
- Filament SSAO bilateral blur：`filament/src/materials/ssao/bilateralBlur.mat`、`ssaoUtils.fs`

要求：

- 所有 tolerance 从调用 shader 传入。
- 不在 include 内决定读取哪个 AOV 通道。
- profile gate 只处理 byte-like id，不负责解码材质语义。

验收：

- SSS diffusion shader 使用这里的 gate。
- 通用 image blur 不 include 本文件。

## Step 6. 写 Burley diffusion helper

新增：`Runtime/Filter/Shaders/HoUrpFilterBurleyDiffusion.hlsl`

建议迁移：

- 旧 HoSSS `HoSSSEvalBurleyDiffusionProfile`
- 旧 HoSSS `HoSSSSampleBurleyDiffusionProfile`
- 旧 HoSSS `HoSSSBurleyProfileWeight`

要求：

- 函数只处理半径、profile color、pdf / weight。
- 不采样 `Sss.Source`。
- 不读取 AOV。
- 保留注释说明来源是旧 HoSSS 的 Burley-like realtime approximation，不声明为 pbrt 等价物。

验收：

- 可用单元/静态测试扫描到 `HoFilterBurley` 前缀。
- SSS shader 只 include 这个文件，而不是复制公式。

## Step 7. 写基础 blur shader

新增：`Runtime/Filter/Shaders/HoUrpFilterBlur.shader`

Pass：

```text
Pass 0 "HoURP Filter Copy"
Pass 1 "HoURP Filter Separable Blur"
Pass 2 "HoURP Filter Depth Normal Aware Blur"
```

输入建议：

- `_HoFilterSourceTex`
- `_HoFilterGuideDepthTex`
- `_HoFilterGuideNormalTex`
- `_HoFilterParams0`：radius、sampleCount、depthTolerance、normalTolerance
- `_HoFilterDirection`：x/y axis

采样预算：

- `SeparableBlur` 第一版：5/9/13 taps 三档。
- `DepthNormalAwareBlur` 第一版：5/9 taps 两档。

Debug：

- pass 2 可通过 keyword 或 `_HoFilterDebugMode` 输出 gate weight。

验收：

- shader 不读取 `Aov.*` 固定全局名。
- 调用方必须显式绑定 guide texture。
- Frame Debugger pass name 可读。

## Step 8. 写 pyramid shader

新增：`Runtime/Filter/Shaders/HoUrpFilterPyramid.shader`

Pass：

```text
Pass 0 "HoURP Filter Pyramid Downsample"
Pass 1 "HoURP Filter Pyramid Upsample"
```

来源参考：

- FidelityFX SPD：`Kits/FidelityFX/upscalers/fsr3/include/gpu/spd/ffx_spd.h`
- Filament bloom：`filament/src/materials/bloom/*`

第一版不要实现完整 SPD compute。先做普通 downsample / upsample，保证 Glow / Bloom / SSS downscale 可用。

验收：

- 不引入 FidelityFX include 到 runtime，除非先补 `SOURCE.md`。
- pyramid level 由调用方创建，shader 不管理纹理数组或 mip 链。

## Step 9. 写 Filter README

新增：`Runtime/Filter/README.md`

必须写清：

- FilterKit 是源码库，不是调度框架。
- 调用方负责 RenderGraph pass 和 transient resource。
- 通用 blur 不读 AOV。
- SSS / AO / Shadow 这类 edge-aware pass 必须显式绑定 guide。
- 外部来源改写必须记录在 `Runtime/Filter/ThirdParty/EXTERNAL_RENDERING_REFERENCES.md`。

验收：

- README 中能搜到 `FilterKit is not a scheduler` 或等价中文。
- README 中列出每个 shader pass index。

## Step 10. 第一轮落地顺序

建议执行：

1. 先建 `HoUrpFilterIds.cs`。
2. 再建三个 include：Common、Sampling、DepthNormalGate。
3. 从旧 HoSSS 拆 Burley helper。
4. 写 `HoUrpFilterBlur.shader` 的 Copy 和 Separable pass。
5. 给一个最小 image blur 或 SSS diffusion 调用，证明 shader 可用。
6. 再补 depth-normal aware blur。
7. 最后补 pyramid shader。

完成条件：

- `Runtime/Filter` 没有复杂调度类。
- 至少一个调用方真实使用 FilterKit shader / include。
- RenderDoc/Frame Debugger 中能看到明确 pass 名。
