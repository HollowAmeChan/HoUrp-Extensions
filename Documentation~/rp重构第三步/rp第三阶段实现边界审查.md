# RP 第三阶段实现边界审查

## 本阶段目标

第三阶段只做 ObjectCustom 对象语义纵切：

```text
Object authoring
  -> AovOutput writes ObjectCustom MRT
  -> DebugView displays ObjectCustom channels
  -> SemanticPost reads ObjectCustom explicitly
```

它不是完整 HoAOV 迁移，也不是角色特化系统迁移。

## 做什么

- 新增 `Aov.ObjectCustom0_3` 与 `Aov.ObjectCustom4_7` 资源。
- 新增 `Object.Custom0` 到 `Object.Custom7` 语义。
- 新增 ObjectCustom debug views。
- 新增最小对象 authoring component，用 MaterialPropertyBlock 写入新 `_HoUrp*` 属性。
- 扩展 fallback AOV shader，写入两个 ObjectCustom MRT。
- 扩展 SemanticPost probe，用 ObjectCustom channel 做最小 tint mask。

## 不做什么

- 不做 `SurfaceData`、`SssSource`、`MaterialCustom`。
- 不做 HoCharacter capture / eye reveal / hair drop shadow。
- 不做 HoPost rule stack。
- 不复制旧 `HoAovGroup` 的全局优先级系统。
- 不把旧 `objectCustom0 = 主体` 等中文角色名固化为长期 ABI。
- 不修改旧材质包。

## 关键边界

ObjectCustom 是对象/区域语义，不是材质派生语义。

旧 `HoAovGroup` 同时处理角色 ID、部件 ID、flags 和 object custom mask。第三步不照搬它，只保留“对象可以显式声明 8 个区域位”这个概念。角色 ID、part ID、优先级和批量分组 UI 后续再进 Capability / Object Semantic UI 阶段。

## 成功标准

- ObjectCustom 两张 RT 能被 AovOutput 写入。
- DebugView 能看至少 8 个通道中的任意一个。
- SemanticPost 通过 Resource Registry 显式读取 ObjectCustom，而不是读旧全局纹理。
- 所有新增 shader binding 都使用 `_HoUrp*`。

