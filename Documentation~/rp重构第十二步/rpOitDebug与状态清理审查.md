# OIT Debug 与状态清理审查

## 目标

第十二阶段的 debug 目标不是做完整透明分析器，而是确保 OIT runtime 不变成黑盒：

- 能查询 OIT resource producer / consumer。
- 能观察 accumulation / revealage。
- 禁用 OIT 后不残留 active state。
- 未启用 OIT 时 debug 不显示上一帧旧图。

## Debug View 规划

第一版建议注册：

```text
OIT.Accumulation
OIT.Revealage
```

可选：

```text
OIT.CompositeWeight
```

### `OIT.Accumulation`

```text
SourceResource: Oit.Accumulation
SourceSemantic: OIT.AccumulationInput
ProducerFeature: TransparentOit
Range: HdrColor or ZeroToOne after debug shader decision
DisplayModes: Replace / ChannelInspect
```

### `OIT.Revealage`

```text
SourceResource: Oit.Revealage
SourceSemantic: OIT.RevealageInput
ProducerFeature: TransparentOit
Range: ZeroToOne
DisplayModes: Replace / Heatmap
```

## 是否接入 AovDebugRendererFeature

当前 `AovDebugRendererFeature` 实际已经承担了 AOV / SSS registered debug tiles。第十二阶段有两种选择：

| 方案 | 优点 | 缺点 | 建议 |
| --- | --- | --- | --- |
| 直接扩展 AovDebugRendererFeature | 最快；AllRegistered 可见 | 名字越来越不准确 | 第一版可接受 |
| 新建 DebugCompositeRendererFeature | 边界正确 | 工作量增加 | 后续统一 debug 阶段再做 |

第一版建议：

- registry 先注册 OIT debug view。
- `AllRegistered` 如果能自动发现 resource，则显示。
- 单项 enum 可先加 `OitAccumulation` / `OitRevealage`。

## Shader debug mode

如果复用 AOV debug shader，需要新增 mode：

```text
OitAccumulationColor
OitAccumulationAlpha
OitRevealage
```

或第一版只用普通 color display：

```text
Oit.Accumulation.rgb
Oit.Revealage.r
```

不要为了 debug 新增旧 `_lilOIT*` binding。

## Active State 清理

必须保证：

```text
_HoUrpOitActive = 0
```

发生在：

- camera begin 或 OIT reset pass。
- OIT composite 结束。
- OIT disabled 时。
- composite material missing 且 pass 被跳过时。

风险：

- accumulation pass 设置 active=1 后 composite pass 因资源缺失跳过，导致后续 shader 误判。

应对：

- final reset pass 必须独立存在。
- disabled path 不 enqueue accumulation，但 reset path 仍可执行，或在 AddRenderPasses 不 enqueue 时通过 begin camera hook reset。优先 RenderGraph reset pass。

## Stale Texture 清理

OIT resource 是 per camera frame。

禁止：

- 保存上一帧 `TextureHandle`。
- 禁用 OIT 后继续让 debug view 显示上一帧 accumulation。
- 用静态 texture field 充当当前 OIT resource。

如果 debug view 找不到当前 frame resource：

```text
skip tile
or display unavailable
```

不要 fallback 到旧 texture。

## Console / Diagnostic

建议 warning：

- composite shader not found。
- unsupported render scale requested。
- accumulation / revealage invalid when composite wants to run。

不建议 spam：

- no OIT objects drawn。第一版不容易可靠判断，先不报。

## 自动测试建议

```text
OitDebugViewsAreRegistered
OitDebugViewsPointToTransparentOitResources
DebugCompositeListsOitViews
OitDisabledDoesNotExposeActiveRuntimeRequest
OitShaderPropertyIdsUseHoUrpActiveName
OitShaderPropertyIdsDoNotUseLilActiveName
```

## 手动验收

步骤：

1. 开启 OIT。
2. 观察 `OIT.Accumulation` debug。
3. 观察 `OIT.Revealage` debug。
4. 禁用 OIT。
5. 再观察 AllRegistered。

期望：

- 开启时 OIT debug tile 有变化。
- 禁用后不显示上一帧 OIT 图。
- 控制台无 missing shader 之外的异常。
- `_HoUrpOitActive` 不会让后续 pass 进入 OIT 状态。

