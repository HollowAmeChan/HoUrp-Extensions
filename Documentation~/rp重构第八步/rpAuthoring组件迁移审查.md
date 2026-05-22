# RP Authoring 组件迁移审查

## 当前组件

当前 runtime 已有：

```text
Runtime/Semantic/ObjectSemanticAuthoring.cs
Runtime/Semantic/MaterialSemanticAuthoring.cs
```

它们已经能通过 `MaterialPropertyBlock` 写入新 `_HoUrp*` 属性，并被 AOV fallback shader 读取。

第八阶段不替换这条链路，只把它整理成明确 Capability UI。

## ObjectSemanticAuthoring 映射

| 字段类型 | Domain | 类型 | 消费者 |
| --- | --- | --- | --- |
| mask weight | ObjectDomain | Policy | AovOutput, SemanticPost |
| object id | ObjectDomain | Semantic policy | AovOutput, Debug |
| group id | ObjectDomain | Semantic policy | AovOutput, SemanticPost |
| flags | ObjectDomain | Semantic policy | AovOutput, SemanticPost |
| custom0-7 | ObjectDomain | Capability / region policy | SemanticPost, future CharacterSpecialization |

建议新增 helper：

```text
ApplyObjectPreset(...)
ResetObjectSemantics()
SetObjectCustomBit(index, enabled)
```

helper 需要保持 runtime 可测试，不依赖 `UnityEditor`。

## MaterialSemanticAuthoring 映射

| 字段类型 | Domain | 类型 | 消费者 |
| --- | --- | --- | --- |
| material class | MaterialDomain | Semantic policy | AovOutput, SemanticPost |
| sss profile | MaterialDomain | Semantic policy | SSS |
| thickness | MaterialDomain | Shading/material policy | SSS, SemanticPost |
| curvature | MaterialDomain | Shading/material policy | SSS, SemanticPost |
| sss source color | ShadingDomain | Source input | SSS |
| sss weight | ShadingDomain | Source input | SSS, SemanticPost |
| material custom0-3 | MaterialDomain | Policy | SemanticPost |

建议新增 helper：

```text
ApplyMaterialPreset(...)
ResetMaterialSemantics()
SetMaterialCustom(channel, value)
```

## 与旧实现关系

旧参考：

```text
lilToon-URP-Extensions/Runtime/AOV/HoAovSubject.cs
lilToon-URP-Extensions/Runtime/AOV/HoAovGroup.cs
```

保留：

- authoring component 写 `MaterialPropertyBlock`。
- object custom bitmask 的可用性。
- character / part / flags 是 ObjectDomain。

不保留：

- 旧全局 group priority resolver。
- 旧 renderer user value 是唯一真实来源的假设。
- 旧 `_HoAov*` 命名作为新公共 ABI。
- 旧 inspector 结构。

## 验收问题

- 没有 authoring component 时，AOV 是否仍能保持清零或 fallback。
- preset 是否能被清除。
- child renderer target 是否明确。
- 多个 authoring component 同时写同一个 renderer 时，是否有文档说明先不支持或后者覆盖。
