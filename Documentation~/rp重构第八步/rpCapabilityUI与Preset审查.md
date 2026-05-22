# RP Capability UI 与 Preset 审查

## 目标

第八阶段的 UI 不是完整工具链，而是第一版稳定 authoring 入口：

```text
用户选择 Capability / Preset
  -> authoring component 写明确语义字段
  -> AOV / SSS / SemanticPost 消费
  -> Debug tile 可验证
```

## Object Capability UI

推荐 inspector 区域：

| 区域 | 字段 | 说明 |
| --- | --- | --- |
| Participation | `WritesAov`, `ReceivesSemanticPost` | 第一版可生效 |
| Identity | `Object.Id`, `Object.GroupId`, `Object.Flags` | 写入 `Aov.MaskId` |
| Object Custom | Custom0-7 | 写入 `Aov.ObjectCustom0_3` / `Aov.ObjectCustom4_7` |
| Policy | `MaskWeight` | 基础参与权重 |
| Target | self / children renderers | authoring 写入范围 |
| Tools | apply / clear / reset | Editor 辅助 |

第一版 object preset：

| Preset | MaskWeight | Custom bits | 说明 |
| --- | --- | --- | --- |
| Subject | 1 | Custom0 | 主要语义对象 |
| Face | 1 | Custom0 + Custom1 | 角色脸部 |
| Hair | 1 | Custom0 + Custom2 | 头发 / 前发 |
| Eye | 1 | Custom0 + Custom3 | 眼睛 |
| Accessory | 1 | Custom0 + Custom4 | 配件 |
| Cloth | 1 | Custom0 + Custom5 | 衣物 |
| Prop | 1 | Custom6 | 道具 |
| Clear | 0 | none | 清空参与 |

## Material Semantic UI

推荐 inspector 区域：

| 区域 | 字段 | 说明 |
| --- | --- | --- |
| Class | material class | `Material.Class` |
| SSS | profile / thickness / curvature / source / weight | SSS 输入 |
| Custom | custom0-3 | 第一版语义后处理输入 |
| Preset | Skin / Hair / Eye / Cloth / Metal / Default | 写入建议值 |
| Tools | apply / clear / reset | Editor 辅助 |

第一版 material preset：

| Preset | MaterialClass | SSS | Custom |
| --- | --- | --- | --- |
| DefaultOpaque | 0 | off | zero |
| SkinSss | 1 | profile 1, thickness > 0, weight > 0 | stylize weight 1 |
| Hair | 2 | low / off | edge weight 1 |
| Eye | 3 | optional | region mask 1 |
| Cloth | 4 | off | stylize weight 0.5 |
| Metal | 5 | off | zero |

这些数值是第一版建议值，不是长期 ABI。正式 ABI 是语义名和资源名。

## UI 原则

- 使用 foldout 分区，而不是一个长字段列表。
- Preset 只负责写字段，不隐藏字段。
- 每个 preset 应可被用户修改后继续保留。
- 不在 Inspector 里做 RenderFeature 调度。
- 不用材质名、Layer、Tag 推断 capability。
- 不把旧中文角色 preset 直接变成新系统核心枚举。

## 风险

- Preset 太早固定为 ABI。
- Inspector 逻辑过厚，复刻旧材质 UI 的问题。
- Object UI 修改 MaterialDomain 字段。
- Material UI 修改 ObjectDomain 字段。
