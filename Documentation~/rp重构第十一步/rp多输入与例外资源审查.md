# RP 第十一阶段多输入与例外资源审查

## 目标

第十一步必须承认 ScreenPost / ImagePost 不是永远“一张图进，一张图出”。某些效果需要多输入、多输出、多分辨率或历史帧。

本阶段不实现所有复杂资源，但必须先定义判定和声明方式，避免 effect 通过全局纹理偷读。

## 输入分类

| 输入类型 | 说明 | 第一版状态 |
| --- | --- | --- |
| `PrimaryImage` | 当前 ImageChain read | 实现 |
| `OriginalSource` | chain begin source copy | 实现 |
| `Aov.MaskId` | object / group / flags / id | 实现 request，依赖已有 AOV |
| `Aov.NormalDepth` | normal / depth | 实现 request，依赖已有 AOV |
| `Aov.SurfaceData` | material class / SSS profile / thickness / curvature | 实现 request，依赖已有 AOV |
| `Aov.MaterialCustom0_3` | material custom | 实现 request，依赖已有 AOV |
| `Aov.SssSource` | SSS source input | 实现 request，依赖已有 AOV |
| `Sss.Source` / `Sss.Diffusion` | SSS 输出 | 实现 request，依赖已有 SSS |
| `SemanticPost.Mask` | 现有语义 mask | 实现 request，依赖已有 SemanticPost |
| `Camera.Depth` | camera depth | 可登记，谨慎使用 |
| `History.*` | 跨帧输入 | planned，不实现 |
| `Pyramid.*` | 多分辨率输入 | planned，不实现 |

## Effect 输入声明

每个 effect 必须在 descriptor 中声明输入。

示例：

```text
PostEffectDefinition: ScreenPost.EdgeLightPrototype
  ExecutionKind: SemanticImagePass
  RequiredInputs:
    - PrimaryImage
    - Aov.MaskId
    - Aov.NormalDepth
  OptionalInputs:
    - Aov.SurfaceData
```

```text
PostEffectDefinition: ImagePost.ColorAdjustPrototype
  ExecutionKind: SingleImagePass
  RequiredInputs:
    - PrimaryImage
```

```text
PostEffectDefinition: ImagePost.AovCompositePrototype
  ExecutionKind: SemanticImagePass
  RequiredInputs:
    - PrimaryImage
    - Aov.MaskId
```

## 例外资源分类

### Original Source

用于同时读取原始画面和当前过滤结果。

允许场景：

- blend original / filtered。
- mask 只作用 filtered，但 final mix 需要 original。
- before/after 调试。

规则：

- 由 ImageChain Begin 创建一次。
- 不由 effect 私自 copy。
- 不作为长期 public resource。

### Local Ping-Pong

用于 effect 内部多次迭代，例如 separable blur。

第十一阶段处理：

- 允许 descriptor 声明 `ResourceKind.LocalPingPong`。
- planner 生成 diagnostic：planned but unsupported。
- 不实际创建。

后续实现要求：

- 必须声明分辨率。
- 必须声明 iteration count。
- 必须声明最终输出回 ImageChain 还是 standalone output。

### Pyramid

用于 bloom、large blur、SSR depth pyramid 等。

第十一阶段处理：

- 只登记 `ResourceKind.Pyramid`。
- 不实现。

要求：

- pyramid 不是 ImagePost 默认资源。
- 必须有 mip count / scale / format / owner。
- Debug 能看 slice / mip。

### History

用于 temporal effect。

第十一阶段处理：

- 只登记 `ResourceKind.History`。
- 不实现 persistent resource。

要求：

- 必须有 invalidation policy。
- 必须有 camera id / resolution change 处理。
- 必须有 debug reset。

### MultiOutput

用于一个 pass 输出多个逻辑资源。

第十一阶段处理：

- 不实现 MRT。
- descriptor 可以声明，但 planner 给 unsupported diagnostic。

禁止：

- effect 临时写第二张 RT 而不声明。
- shader 用 UAV / global side effect 输出隐藏结果。

## RenderGraph 约束

每个输入必须对应：

```text
PostEffectDefinition.RequiredInputs
  -> PostResourceRequest.ReadSemantics
  -> RenderGraph builder.UseTexture(...)
```

每个输出必须对应：

```text
PostEffectDefinition.ProducedOutputs
  -> PostResourceRequest.WriteSemantic or ImageChain.Write
  -> RenderGraph builder.SetRenderAttachment(...)
```

禁止：

- shader 直接采样未声明 global texture。
- C# 在 pass 中临时设置旧全局名。
- pass 同时读写同一 handle。
- 用 effect 名推断资源。

## ScreenPost / ImagePost 判定

如果 effect 需要以下输入，默认不是纯 ImagePost：

```text
Aov.MaskId
Aov.SurfaceData
Object.Custom
Material.Class
Material.Custom
Sss.Diffusion
SemanticPost.Mask
```

例外：

- ImagePost 可声明轻量 `AovComposite`，但只用于最终图像混合 mask。
- ImagePost 的 AOV composite 不拥有 ScreenPost rule language。
- 如果需要复杂 object/material rule，应转 ScreenPost。

## 自动测试

建议：

```text
Tests/Runtime/HoUrpPostInputDeclarationTests.cs
```

测试项：

```csharp
SingleImagePassRejectsAovInput()
SemanticImagePassRequiresDeclaredSemanticInputs()
OriginalSourceCreatesOneRequestForMultipleEffects()
UnsupportedPyramidRequestCreatesDiagnostic()
UnsupportedHistoryRequestCreatesDiagnostic()
UnsupportedMultiOutputCreatesDiagnostic()
ImagePostAovCompositeCannotDeclareMaterialRuleLanguage()
ScreenPostEffectCanDeclareAovMaskIdAndNormalDepth()
NoEffectInputUsesLegacyGlobalTextureName()
```

## 手动验收

场景：

1. 开启一个纯 ImagePost layer。
2. 开启一个 ImagePost AOV composite prototype。
3. 开启一个 ScreenPost rule mask prototype。

检查：

- 纯 ImagePost 只请求 `PrimaryImage`。
- ImagePost AOV composite 请求 `PrimaryImage + Aov.MaskId`。
- ScreenPost 请求其 rule 所需 AOV / semantic。
- 关闭 AOV composite 后，ImagePost 不再请求 AOV。
- Debug active plan 中输入列表变化正确。

## 成功标准

- 多输入不依赖隐式全局纹理。
- 例外资源都有明确 request kind。
- 未实现资源返回 diagnostic，不静默创建。
- OriginalSource 只创建一次。
- ScreenPost 与 ImagePost 的输入边界清楚。
- RenderGraph pass 的 UseTexture / SetRenderAttachment 与 descriptor 一致。

## 风险点

- 把所有 effect 都做成 `SemanticImagePass`，导致 ImagePost 边界失效。
- 因为某个旧 shader 方便，继续读 `_lilHoAov*`。
- OriginalSource 每个 effect 各 copy 一次。
- unsupported resource 被临时实现成私有 RT。
- MultiOutput 没声明就偷偷写。
