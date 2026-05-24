# SSS FilterKit 落地执行计划

> 本文用于把真 screen-space SSS 第一版接到轻量 FilterKit。目标是先得到可调试、可解释、边界清楚的 SSS diffusion，不追求一步到位的高端 temporal / denoise。

## 当前边界

第十四步之后，SSS runtime 边界已经明确：

- `Aov.MaskId.a` 承载 `ReceivesSss` gate。
- `Aov.Diffuse.rgb` 只提供 source color。
- `Aov.Diffuse.a` 不再承载 SSS weight。
- `Sss.Source.a` 是 SSS runtime 自己维护的 participation / source validity。
- `Sss.Diffusion.a` 是 SSS runtime 自己维护的 composite weight。

第十五步不能破坏这些结论。

## Step 1. 拆旧 HoSSS 公式

来源：

- `lilToon-URP-Extensions/Runtime/SubsurfaceScattering/HoSubsurfaceScattering.shader`

迁移到：

```text
Runtime/Filter/Shaders/HoUrpFilterBurleyDiffusion.hlsl
Runtime/Filter/Shaders/HoUrpFilterDepthNormalGate.hlsl
Runtime/Filter/SSS/HoUrpSssFilter.hlsl
```

拆分方式：

- Burley profile eval/sample/weight 放 `HoUrpFilterBurleyDiffusion.hlsl`。
- depth / normal / profile / mask gate 放 `HoUrpFilterDepthNormalGate.hlsl`。
- AOV / SSS texture decode、profile table、source prepare 放 `HoUrpSssFilter.hlsl`。

禁止：

- 把旧 `_lilHoSSS*` 名字带进新 shader。
- 把旧 `_lilHoAovSssTexture` 带进新链路。
- 在通用 Filter include 里绑定 AOV / SSS 纹理。

验收：

- `rg "_lilHoSSS|_lilHoAovSssTexture" Runtime/Filter Runtime/Shaders/Hidden/HoURP/SSS` 无新增长期路径。
- `HoUrpFilterBurleyDiffusion.hlsl` 不包含 `TEXTURE2D`。

## Step 2. 定义 SSS shader pass

新增或拆分：

```text
Runtime/Filter/SSS/HoUrpSssDiffusion.shader
```

Pass 建议：

```text
Pass 0 "HoURP SSS Source Prepare"
Pass 1 "HoURP SSS Profile Diffusion"
Pass 2 "HoURP SSS Composite"
Pass 3 "HoURP SSS Debug"
```

第一版先用单 pass disk gather 或 X/Y 两 pass 都可以，但 pass name 必须清楚。如果采用 X/Y 两 pass：

```text
Pass 1 "HoURP SSS Profile Diffusion X"
Pass 2 "HoURP SSS Profile Diffusion Y"
Pass 3 "HoURP SSS Composite"
Pass 4 "HoURP SSS Debug"
```

要求：

- pass index 写进 `HoUrpFilterIds.cs`。
- 每个 pass 顶部注释输入、输出、采样预算。
- Source Prepare 和 Composite 不混在同一个 pass，方便 debug。

验收：

- Frame Debugger 能看到 Source Prepare / Diffusion / Composite。
- RenderDoc 中每个 pass 输出可单独检查。

## Step 3. Source Prepare

输入：

- `Aov.MaskId`
- `Aov.NormalDepth`
- `Aov.SurfaceData`
- `Aov.Diffuse`

输出：

- `Sss.Source`
  - RGB：source color。
  - A：participation。

计算规则：

```text
ReceivesSss = Aov.MaskId.a
GeometryValid = normalDepth has valid normal/depth
ProfileValid = profile id enabled
Thickness = Material.Thickness / Aov.SurfaceData channel
Participation = ReceivesSss * GeometryValid * ProfileValid * Thickness
Source.rgb = Aov.Diffuse.rgb
Source.a = Participation
```

明确不读：

- `Aov.Diffuse.a`
- fSSS forward lobe
- 旧 HoAOV SSS source texture

验收：

- 非 receiver 对象 `Sss.Source.a == 0`。
- `Aov.Diffuse.a` 改变不影响 `Sss.Source.a`。
- Debug 能显示 source RGB 和 participation。

## Step 4. Profile Diffusion

输入：

- `Sss.Source`
- `Aov.NormalDepth`
- `Aov.SurfaceData`
- profile table settings

输出：

- `Sss.Diffusion`
  - RGB：diffused color。
  - A：composite weight。

第一版算法：

- 从旧 HoSSS 迁移 Burley-like disk gather。
- 采样数保留三档：8 / 16 / 24。
- 半径支持 Full / Half / Quarter render scale compensation。
- 每个 sample 使用：
  - source alpha gate
  - depth gate
  - normal gate
  - profile id gate
  - thickness / radius multiplier

建议伪代码：

```text
center = load normal/depth/profile/thickness/source
if center.source.a <= 0: return 0

for sample in sampleBudget:
  offset = goldenAngleOffset(sample)
  sampleData = load guide + source
  gate = sample.source.a
  gate *= depthGate
  gate *= normalGate
  gate *= profileGate
  weight = burleyWeight(radius, profileColor) * gate
  accum += sample.source.rgb * weight
  weightSum += weight

diffused.rgb = accum / max(weightSum, eps)
diffused.a = center.source.a * compositeStrength
```

验收：

- profile radius 改变扩散范围。
- thickness 改变参与强度或半径。
- depth / normal discontinuity 不明显跨边界漏色。
- Debug 能显示 gate 或 weight。

## Step 5. Composite

输入：

- camera color copy 或 current camera color
- `Sss.Source`
- `Sss.Diffusion`

输出：

- camera color

规则：

- 不直接覆盖 camera color。
- `Sss.Diffusion.a` 作为 composite weight。
- 保留 source preserve 参数，避免过度糊化。

建议：

```text
sssColor = lerp(Sss.Diffusion.rgb, Sss.Source.rgb, sourcePreserve)
out.rgb = lerp(camera.rgb, sssColor, Sss.Diffusion.a * globalStrength)
```

验收：

- 关闭 SSS feature 后无残留。
- composite weight debug 与视觉强度一致。

## Step 6. RendererFeature 显式调用

修改：

- `Runtime/Features/SubsurfaceScatteringRendererFeature.cs`

要求：

- 显式创建 `Sss.Source`、`Sss.Diffusion`、必要的 `Sss.Temp`。
- 显式添加 Source Prepare / Diffusion / Composite pass。
- 显式绑定所有输入 texture。
- 不通过隐藏 scheduler。
- 不长期持有 full-res temp。

RenderGraph 路径建议：

```text
RecordSourcePrepare(renderGraph, aovMaskId, aovNormalDepth, aovSurfaceData, aovDiffuse, sssSource)
RecordDiffusion(renderGraph, sssSource, aovNormalDepth, aovSurfaceData, sssDiffusion)
RecordComposite(renderGraph, cameraColor, sssSource, sssDiffusion)
```

验收：

- pass data 结构中能看到每个输入输出。
- 没有 `SetGlobalTexture` 作为长期跨 pass 数据链。

## Step 7. Debug view

必须保留：

- `Sss.Source`
- `Sss.Diffusion`
- `Sss.CompositeWeight`
- `SssProfileId`
- `Sss.Thickness`
- `Sss.Curvature`
- `ReceivesSss`

新增建议：

- `Sss.DiffusionGate`
- `Sss.SampleWeight`

验收：

- debug view 不依赖最终 composite。
- 能单独确认 receiver gate、profile、thickness、diffusion weight。

## Step 8. 第一版不做

- 不引入 NRD。
- 不做 temporal SSS。
- 不做 skin random walk。
- 不做 pbrt runtime 等价。
- 不把 fSSS forward lobe 当作 source weight。
- 不在材质 UI 里塞 screen-space SSS runtime 的私有资源控制。

## 完成条件

- 真 SSS 不再依赖旧 `_lilHoSSS*` ABI。
- `ReceivesSss` gate 生效。
- `Aov.Diffuse.a` 不参与 SSS weight。
- SSS shader 复用 FilterKit include。
- Source / Diffusion / Composite pass 可单独 debug。
