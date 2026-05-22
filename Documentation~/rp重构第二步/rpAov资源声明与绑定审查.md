# RP AOV 资源声明与绑定审查

## 目标

明确 `Aov.MaskId` / `Aov.NormalDepth` 如何从 Resource Registry 进入 RenderGraph，并如何被 shader 和消费者读取。

## 逻辑资源

| Resource | Producer | Consumer | Lifetime | Format | Clear |
| --- | --- | --- | --- | --- | --- |
| `Aov.MaskId` | `AovOutput` | `DebugComposite`, `SemanticPostProcess` | PerCamera | `R8G8B8A8_UNorm` | `(0,0,0,0)` |
| `Aov.NormalDepth` | `AovOutput` | `DebugComposite`, `SemanticPostProcess` | PerCamera | `R16G16B16A16_SFloat` | `(0,0,0,1)` |

Clear is owned by RenderGraph resource declaration: `TextureDesc.clearBuffer` / `TextureDesc.clearColor` are set from `ResourceClearPolicy` by `HoUrpRenderGraphTextureDescFactory`. `AovOutput` binds AOV attachments as `ReadWrite` partial writes so non-covered pixels keep the resource clear value. Do not add per-resource clear passes or hand-written MRT clear passes for AOV background cleanup.

## Shader Binding

| 用途 | Shader property | 说明 |
| --- | --- | --- |
| AOV mask source | `_HoUrpAovMaskIdTexture` | 新 shader backend binding，不是 Resource Registry 主键 |
| AOV normal/depth source | `_HoUrpAovNormalDepthTexture` | 新 shader backend binding，不是 Resource Registry 主键 |
| Debug mode | `_HoUrpAovDebugMode` | debug shader pass 参数 |
| Semantic tint color | `_HoUrpSemanticPostTintColor` | SemanticPost 最小消费者参数 |

旧名 `_lilHoAovMaskIdTexture` / `_lilHoAovNormalDepthTexture` 只保留在 `legacyName` 和文档对照里，不作为新逻辑名。

## Global Binding 判定

本阶段不把 AOV 资源发布成长期全局纹理 ABI。Debug 与 SemanticPost 通过 `HoUrpRenderGraphResources` 取 `TextureHandle`，再在 pass 内用 `MaterialPropertyBlock` 绑定到最小 shader。

`builder.SetGlobalTextureAfterPass` 允许作为未来材质迁移期的 backend detail，但本阶段不需要使用。

## Resource Registry 判定

`ResourceDefinition` 当前不扩展 shader binding 字段。逻辑资源名、旧名、shader property 继续分开：

- 逻辑资源名：`Aov.MaskId`
- 旧资源名：`_lilHoAovMaskIdTexture`
- 新 shader binding：`_HoUrpAovMaskIdTexture`

如后续需要多个 backend，可以新增集中映射类型，而不是把 shader property 变成资源主键。
