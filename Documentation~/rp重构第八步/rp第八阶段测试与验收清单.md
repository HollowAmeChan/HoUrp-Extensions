# RP 第八阶段测试与验收清单

## 自动检查

| 检查 | 状态 |
| --- | --- |
| capability registry count / link tests | 已补：`WritesObjectCustom` / `ReceivesSemanticPost` |
| object preset mapping tests | 已补：Subject / Face / Hair / Eye / Accessory / Cloth / Prop / Clear |
| material preset mapping tests | 已补：SkinSss / Clear |
| authoring clamp tests | 已补：object byte / mask weight / material range |
| shader property mapping tests | 已补：ObjectId / ObjectGroupId / ObjectFlags |
| editor asmdef compile | 待 Unity 验证 |
| `git diff --check` | 已跑：仅 LF/CRLF warning |

## 手动 Unity 验收

挂载：

1. `HoURP AOV Output`
2. `HoURP Subsurface Scattering`
3. `HoURP Semantic Post Process`
4. `HoURP AOV Debug`

对象验收：

| 操作 | 期望 |
| --- | --- |
| 添加 `ObjectSemanticAuthoring` | Inspector 显示 Object Capability UI |
| 选择 `Subject` preset | AOV mask / Object Custom0 有输出 |
| 选择 `Face` preset | Object Custom0 和 Custom1 有输出 |
| 选择 `Hair` preset | Object Custom0 和 Custom2 有输出 |
| 关闭 `ReceivesSemanticPost` | `POST MASK` 对该对象归零或不再命中 |
| Clear preset | AOV object custom 归零 |

材质验收：

| 操作 | 期望 |
| --- | --- |
| 添加 `MaterialSemanticAuthoring` | Inspector 显示 Material Semantic UI |
| 选择 `SkinSss` preset | `SSS SRC` / `SSS WGT` / `SSS DIFF` 有输出 |
| 选择 `Hair` preset | material class / custom debug 有输出 |
| Clear preset | material custom / SSS weight 归零 |

SemanticPost 验收：

| 场景 | 期望 |
| --- | --- |
| Object Subject + SemanticPost ObjectCustom0 rule | `POST MASK` 显示对象 |
| Object Hair + Custom2 rule | 只命中 hair preset 对象 |
| Material Skin + SSS rule | 只命中 SSS 权重区域 |
| AllRegistered | AOV / SSS / POST MASK tile 一致更新 |

当前代码状态：

- `ObjectSemanticAuthoring` 已增加 `WritesAov` / `ReceivesSemanticPost` / `ObjectId` / `GroupId` / `Flags` 和对象 preset helper。
- `MaterialSemanticAuthoring` 已增加材质 preset helper 和 custom channel clamp。
- `AovOutputFallback` 已写出 ObjectId / GroupId / Flags 到 `Aov.MaskId.gba`。
- `SemanticPostProcess` 默认 rule 已改为 `ObjectCustom0`，与 Subject preset 对齐。
- 已新增 `ObjectSemanticAuthoringEditor` / `MaterialSemanticAuthoringEditor`。

## 风险点

- Inspector 编译依赖 UnityEditor，runtime asmdef 不能引用 Editor。
- Preset 写入字段后没有触发 apply，导致 Scene 里看不到变化。
- 多 renderer 写入范围不清楚。
- UI 字段名看起来像长期 ABI。
- Capability 关掉但底层 policy 值未清，造成 debug 判断混乱。

## 未决项

| 项 | 当前处理 |
| --- | --- |
| 多个 authoring component 冲突 | 第八阶段先不做 resolver，文档说明不支持 |
| renderer user value | 保留为旧实现参考，不作为第一版核心路径 |
| Light Capability | 延后 |
| Material preset/generator | 延后到材质系统重构 |
