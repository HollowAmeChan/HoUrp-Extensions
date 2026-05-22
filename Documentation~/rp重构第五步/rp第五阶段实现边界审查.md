# RP 第五阶段实现边界审查

## 本阶段目标

第五阶段只做 SSS 输入语义纵切：

```text
SSS input authoring
  -> AovOutput writes Aov.SssSource MRT
  -> DebugView displays SssSource / SssWeight
  -> Consumer probe reads Aov.SssSource explicitly
```

它不是完整 HoSSS 迁移，也不是 SSS 视觉一致性验收。

## 做什么

- 新增 `Aov.SssSource` 资源。
- 新增 `Shading.SssSourceColor` 与 `Shading.SssWeight` 语义。
- 新增 SSS 输入 DebugView。
- 扩展最小 authoring，写入 SSS source color / weight。
- 扩展 AOV fallback shader，写入 `Aov.SssSource` MRT。
- 扩展 SemanticPost probe 或新增最小 SSS input consumer。

## 不做什么

- 不做 SSS diffusion blur。
- 不做 transmission gather / blur。
- 不做 SSS composite。
- 不做 SSS profile kernel。
- 不做 half-resolution SSS。
- 不接旧 `HoAOVSSS` pass。
- 不修改旧材质包。

## 关键边界

`Aov.SssSource` 是 SSS 输入资源，不是 SSS 效果本身。第五步只验证数据能被生产、调试和显式消费。

旧 HoSSS 的 source / diffusion / transmission / composite 行为只能作为后续迁移参照，不能在第五步一次性搬入。

## 成功标准

- `Aov.SssSource` 能被 AovOutput 写入。
- DebugView 能显示 source color 和 weight。
- Consumer 通过 Resource Registry 显式读取 `Aov.SssSource`。
- 所有新增 shader binding 使用 `_HoUrp*`。

