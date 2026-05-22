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
| Capability | `WritesAov`, `ReceivesSemanticPost` | 第一版可生效；`ReceivesSemanticPost` 不清空对象语义 |
| Object Semantics | Subject / Face / Hair / Eye / Accessory / Cloth / Prop / Reserved | 写入 `Aov.ObjectCustom0_3` / `Aov.ObjectCustom4_7` |
| Advanced Identity | `Object.Id`, `Object.GroupId`, `Object.Flags` | 写入 `Aov.MaskId`；只作为高级字段暴露 |
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

## Object 语义统一契约

第八阶段后，Object authoring 里的几个概念必须分开：

| 概念 | 正式语义 | 当前资源映射 | UI / Debug 命名 |
| --- | --- | --- | --- |
| 对象参与权重 | `Object.MaskWeight` | `Aov.MaskId.r` | `MASK` |
| 对象 ID | `Object.Id` | `Aov.MaskId.g` | `OBJECT ID` |
| 对象分组 | `Object.GroupId` | `Aov.MaskId.b` | 高级字段，暂不新增独立 tile |
| 对象 flags | `Object.Flags` | `Aov.MaskId.a` | `POST RX`, `FLAG 1-7` |
| 对象语义位 | `Object.Custom0-7` | `Aov.ObjectCustom0_3` / `Aov.ObjectCustom4_7` | `SUBJECT`, `FACE`, `HAIR`, `EYE`, `ACCESS`, `CLOTH`, `PROP`, `RESERVED` |

`Object.Custom0-7` 在第八阶段不再作为“裸 custom channel”暴露给用户，而是第一版对象语义位：

| Bit | UI / Debug | 语义 |
| --- | --- | --- |
| Custom0 | Subject | 语义后处理和角色类能力的基础主体位 |
| Custom1 | Face | 脸部 |
| Custom2 | Hair | 头发 / 前发 |
| Custom3 | Eye | 眼睛 |
| Custom4 | Accessory | 配件 |
| Custom5 | Cloth | 衣物 |
| Custom6 | Prop | 道具 |
| Custom7 | Reserved | 保留 |

`Object.Flags` 当前只固化一个语义：

| Bit | UI / Debug | 语义 |
| --- | --- | --- |
| Flag0 | Post Receiver / `POST RX` | `ReceivesSemanticPost` 的消费门控 |
| Flag1-7 | `FLAG 1-7` | 暂无语义，保留为后续对象级过滤位 |

关键约束：

- `ReceivesSemanticPost` 只写 `Object.Flags` bit0，不清空 `Object.Custom0-7`。
- `POST MASK` 表示 SemanticPost 经过 flag 门控和规则评估后的最终消费结果。
- `SUBJECT` / `HAIR` 等 AOV Debug tile 表示对象声明的语义位本身，不表示后处理一定消费它。
- 不为 `GroupId` / `Flags` 新增 AOV 资源；它们继续复用 `Aov.MaskId`。
- 第八阶段不为 flag1-7 定义业务语义，只要求它们可写、可 debug、可被后续规则消费。

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
