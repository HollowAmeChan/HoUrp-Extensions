# RP 第二阶段实现边界审查

> 本文冻结第二阶段的实现边界。第二阶段只验证最小 AOV 闭环，不迁移完整旧 HoAOV。

## 目标

第二阶段只覆盖以下闭环：

```text
HoUrpBuiltInContracts
  -> Aov.MaskId / Aov.NormalDepth resource declaration
  -> AovOutput RenderGraph renderer list draw
  -> DebugComposite registered debug view replace
  -> SemanticPostProcess explicit AOV read
  -> tests / compile / manual validation notes
```

成功标准是资源按新契约可写、可读、可调试。视觉效果不要求接近完整旧 HoAOV。

## 范围判定

| 问题 | 判定 |
| --- | --- |
| 是否只覆盖 `Aov.MaskId` / `Aov.NormalDepth` | 是。其它 AOV 资源延后。 |
| 是否允许新增最小 shader | 是，新增 `Hidden/HoURP/...` shader。 |
| 是否允许 fallback drawing | 是，第一版使用 override material 绘制 scene renderer list。 |
| 是否需要新材质系统参与 | 不需要。 |
| 是否需要旧 `lilToon/lilPBR` shader 参与 | 不需要。 |
| 成功标准 | 数据链路正确，不要求完整旧 HoAOV 视觉一致。 |

## 不做项

- 不复制旧 `HoAovRendererFeature.cs`。
- 不迁移旧 compatibility path。
- 不修改 `lilToon` / `lilPBR`。
- 不接入旧 `HoAOV` / `HoAOVSSS` material pass。
- 不新增 `Aov.TangentNormal`、`Aov.SurfaceData`、custom、object custom、SSS source。
- 不做完整 HoPost rule stack。
- 不做 Debug HUD、overlay、capture 面板。

## 允许留白

| 留白项 | 本阶段处理 |
| --- | --- |
| Object ID / Group ID / Flags authoring | shader 写常量，后续 Capability/Object UI 阶段接入。 |
| Alpha clip 一致性 | 不保证。override material 第一版只验证几何绘制。 |
| 透明 AOV | 不做。只绘制 opaque queue。 |
| 旧 shader property 兼容 | 不建长期兼容层，只在文档登记新旧对照。 |
| RenderDoc 自动验收 | 仅登记需求。 |

## 风险边界

最主要的风险是把“最小闭环”写成“旧 HoAOV 迁移”。本阶段所有代码必须能回答 producer / consumer / resource / debug view 关系；无法回答的旧能力不得进入 runtime。
