# RP 第十阶段 OIT-ready 材质契约审查

## 目标

第十阶段不实现 Weighted OIT runtime，但必须让材质侧提前可用。第十一步应该可以直接做：

```text
DrawRenderers(shaderTag = HoUrpOitAccumulation)
  -> Oit.Accumulation / Oit.Revealage
```

而不是回头修改材质 ABI。

## 第十步必须完成的材质侧契约

| 契约 | 要求 |
| --- | --- |
| 新 pass 名 | `HoUrpOitAccumulation` |
| 新 capability | `SupportsOit` |
| 实例参与策略 | `ParticipatesOit` |
| 透明输出 | `TransparentOutputData` |
| accumulation 输出 | `OitAccumulationData` |
| 旧 pass 名 | `lilToonOIT` 只作为 legacy mapping |
| 旧 active flag | `_lilOITActive` 不作为新材质 ABI |
| 旧 enabled flag | `_lilOITEnabled` 不作为新材质 ABI |

## Pass 行为

`HoUrpOitAccumulation` pass 应：

- 使用第十阶段独立 shader ABI。
- 读取同一套 `SurfaceData`。
- 输出 weighted color / alpha 和 revealage 所需数据。
- 不写 camera color。
- 不依赖 `Oit.*` 资源存在。
- 不采样 live camera color。
- 不依赖旧 OIT 全局状态。

第十阶段可以只保证 pass 编译和能被 Frame Debugger / RenderDoc 识别；真正 MRT attachment 和 composite 由第十一步实现。

## Phase Policy

第十阶段只定义策略，不实现 runtime 调度：

```text
ParticipatesOit = true:
  OIT runtime 绘制 HoUrpOitAccumulation
  普通透明 forward 是否跳过由 preset / phase policy 决定

ParticipatesOit = false:
  OIT runtime 不绘制
  可继续走普通透明 forward
```

最小 prototype 可以采用：

```text
ParticipatesOit = true
NormalTransparentForward = Skip
```

如果第十步还没有普通透明 forward runtime，就只记录策略并通过 descriptor / preset 测试验证。

## 与第十一步的接口

第十一步 Weighted OIT runtime 只应该依赖：

- pass 名：`HoUrpOitAccumulation`
- material / preset capability：`SupportsOit`、`ParticipatesOit`
- shader 输出约定：`OitAccumulationData`

不应该依赖：

- `_lilOITEnabled`
- `_lilOITActive`
- `lilToonOIT`
- 旧材质包 include

## 自动验收建议

| 检查 | 期望 |
| --- | --- |
| shader 文本 | 包含 `LightMode` = `HoUrpOitAccumulation` |
| shader 文本 | 不包含 `lilToonOIT` |
| shader 文本 | 不包含 `_lilOITEnabled` / `_lilOITActive` |
| preset descriptor | 声明 `SupportsOit` |
| preset descriptor | 声明 `ParticipatesOit` |
| preset descriptor | 声明 `HoUrpOitAccumulation` |
| C# contracts | 能查询 prototype material 是 OIT-ready |

## 手动验收建议

在没有 Weighted OIT runtime 的情况下，第十步仍应能验证：

- prototype shader 编译通过。
- 材质显示正常。
- AOV 输出正常。
- Frame Debugger / RenderDoc 能找到 `HoUrpOitAccumulation` pass 或至少能通过 shader pass 列表确认存在。
- 切换 `ParticipatesOit` 元数据不会影响 AOV / SSS 输出。

## 风险

- 第十步只做 opaque AOV shader，第十一步 runtime 无法绘制透明 accumulation。
- 让 OIT pass 读取旧 `_lilOITActive`，导致旧 runtime phase 泄漏进新 ABI。
- `HoUrpOitAccumulation` 输出结构和第十一步 `Oit.Accumulation` 格式不匹配。
- 把 OIT runtime 的 clear / composite 逻辑提前塞进材质 shader。
