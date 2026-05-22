# RP SemanticPost 最小消费者审查

## 目标

证明 AOV 不只被 debug 使用，也能被正式 consumer 显式读取。

## 最小链路

```text
SemanticPostProcessRendererFeature
  -> HoUrpRenderGraphResources.TryGetTexture(Aov.MaskId)
  -> HoUrpRenderGraphResources.TryGetTexture(Aov.NormalDepth)
  -> copy active camera color to temp
  -> AovReadProbe reads AOV + temp color
  -> write active camera color
```

## 效果判定

选择 `mask tint`：

- 读取 `MaskId.r` 作为 tint 权重。
- 读取 `NormalDepth.a` 作为显式依赖输入。
- 对 camera color 叠加轻微 tint。

这不是 HoPost rule，也不做 outline、edge、subject mask、layer stack 或 Volume stack。

## RenderGraph 依赖

| Resource | Access |
| --- | --- |
| `Aov.MaskId` | read |
| `Aov.NormalDepth` | read |
| active camera color copy | read |
| active camera color | write |

## 不做项

- 不做 HoPost layer stack。
- 不做 AOV rule language。
- 不做 CustomMaterial。
- 不做 Volume 参数系统。
- 不修改旧材质。
