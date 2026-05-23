# RP 第十一阶段测试与验收清单

## 自动检查

当前最小闭环状态（2026-05-23）：

- `HoURP ScreenPost Prototype` 已进入 RDG 并产生可见屏幕效果。
- `HoURP ImagePost Prototype` 已进入 RDG 并产生可见屏幕效果。
- 第十一阶段仍继续推进 rule system / sortable stack / UI，不进入第十二步。

| 检查 | 期望 |
| --- | --- |
| Post model | 存在 `PostEffectDefinition` / `PostLayerDefinition` / `PostResourceRequest` / `PostGraphPlan` 或等价实现 |
| Planner | disabled layer 不生成 active node |
| Planner | disabled effect 不生成 resource request |
| Planner | missing effect 生成 diagnostic，不执行 fallback |
| Resource request | 每个 request 有 owner / lifetime / resolution / format / debug name |
| Resource request | frame transient request 不保存 `TextureHandle` |
| Resource request | disabled effect 后 request 从 frame plan 消失 |
| ImageChain | single image pass 使用 WorkA / WorkB |
| ImageChain | 多个 single image pass 不按 pass 数创建同规格 full-res RT |
| ImageChain | WorkA / WorkB 不发布为长期 public resource |
| ImageChain | original source 只在需要时请求 |
| Multi-input | semantic input 必须由 descriptor 声明 |
| Multi-input | pure ImagePost image effect 不声明 object/material semantic |
| ScreenPost | prototype rule 声明 AOV / semantic 输入 |
| ScreenPost | disabled ScreenPost layer 不请求 AOV 输入 |
| ImagePost | prototype single-pass effect 走 ImageChain |
| ImagePost | AOV composite 关闭时不请求 AOV 输入 |
| Legacy | 新代码 / shader 不包含 `_lilHoPost`、`_lilShoost`、`_lilHoAov`、`_HoAov` ABI |
| RenderGraph | 无 camera color 同 pass 读写冲突 |
| Debug | active plan / resource request / image chain 状态可查询 |
| `git diff --check` | 无空白错误 |
| Sortable stack | list order 改变 pass order |
| Sortable stack | disabled item 不产生 pass / request |
| ScreenPost rule | rule source 推导 AOV request |
| ScreenPost rule | preview fallback 关闭后不影响天空 / 空 AOV 区域 |

## 建议新增测试

```text
Tests/Runtime/HoUrpPostGraphPlannerTests.cs
Tests/Runtime/HoUrpPostResourceRequestTests.cs
Tests/Runtime/HoUrpImageChainTests.cs
Tests/Runtime/HoUrpPostInputDeclarationTests.cs
Tests/Runtime/HoUrpScreenPostRuleTests.cs
Tests/Runtime/HoUrpImagePostEffectDescriptorTests.cs
```

测试方向：

```csharp
DisabledLayerDoesNotCreateNode
DisabledLayerDoesNotCreateResourceRequest
MissingEffectCreatesDiagnostic
EnabledSingleImagePassRequestsImageChainWork
SemanticImagePassRequestsSemanticInputs
ImageChainUsesTwoWorkTexturesForMultiplePasses
ImageChainRequestsOriginalOnlyWhenNeeded
SingleImagePassRejectsAovInput
ScreenPostRuleDeclaresAovMaskIdInput
DisabledScreenPostLayerRemovesSemanticInputs
ImagePostAovCompositeRequestsAovOnlyWhenEnabled
ImagePostPureImageEffectCannotDeclareMaterialSemanticInput
NoRequestUsesLegacyGlobalTextureName
```

## 手动 Unity 验收

### 基础场景

挂载：

1. `HoURP AOV Output`
2. `HoURP Subsurface Scattering` 可选
3. `HoURP Semantic Post Process` 可选
4. `HoURP AOV Debug`
5. `HoURP ScreenPost Prototype`
6. `HoURP ImagePost Prototype`

当前 prototype 验收结论：

- ScreenPost 可见效果已经确认；如果开启 preview fallback，空 AOV 区域也会被 tint，此行为仅用于 debug。
- ImagePost 可见效果已经确认；当前是全屏 color adjust，用于验证 ImageChain 写回。

准备：

- 两个对象有不同 object group / material class。
- 至少一个对象有非零 material class / thickness。
- camera color 中有明显颜色区域，方便观察 image effect。

### ImageChain 验收

启用两个 ImagePost pure image prototype layer。

期望：

| 检查 | 期望 |
| --- | --- |
| pass count | 至少两个 image pass |
| WorkA / WorkB | 交替 read/write |
| full-res RT 数量 | 不随 layer 数线性增长 |
| final copy | current chain output 写回 camera color |
| debug | 显示 pass list / current / alternate |
| console | 无 RenderGraph read/write conflict |

### ImagePost 启停验收

操作：

1. 启用 layer A。
2. 启用 layer B。
3. 禁用 layer B。

期望：

- layer B 禁用后，planner active node 减少。
- layer B resource request 消失。
- ImageChain pass 数减少。
- Debug 不显示 layer B active output。
- 不显示上一帧 layer B 结果。

### 可排序 Stack 验收

操作：

1. 新增两个 ImagePost filter item。
2. 设置明显不同的 color / brightness / contrast。
3. 拖动列表顺序。

期望：

- RDG pass 顺序跟随列表顺序。
- 最终画面随顺序变化。
- WorkA / WorkB 仍只有两张全分辨率工作纹理。

ScreenPost 同理：

- 新增两个 ScreenPost layer。
- 设置不同 rule / blend / color。
- 拖动顺序后，重叠区域合成结果变化。

### ScreenPost rule 验收

启用一个 ScreenPost prototype layer：

```text
Rule:
  SourceSemantic = Object.GroupId or Material.Class
  Operator = Equal
  CompareValue = target value
```

期望：

| 检查 | 期望 |
| --- | --- |
| AOV input | RenderGraph pass 声明需要的 AOV |
| rule mask | 只命中目标对象 |
| layer blend | 只影响命中区域 |
| debug | 可看 `ScreenPost.RuleMask` 或 influence |
| disable | 关闭 layer 后 AOV request 消失 |

### ImagePost AOV Composite 验收

启用一个 ImagePost AOV composite prototype。

期望：

- 只有 AOV composite layer 启用时，ImagePost 才请求 AOV 输入。
- 关闭 AOV composite 后，ImagePost 回到纯 ImageChain。
- ImagePost AOV composite 不暴露 ScreenPost rule language。
- 复杂 material / object rule 仍应放 ScreenPost。

## RenderGraph 验收

| 检查 | 期望 |
| --- | --- |
| camera color | 先 copy / import 成独立 source，不直接同 pass 读写 |
| WorkA / WorkB | frame transient |
| OriginalSource | 只在需要时创建 |
| Semantic inputs | 每个 consumer pass 都 `UseTexture` |
| Outputs | 每个 writing pass 都 `SetRenderAttachment` |
| disabled effects | 不录制 pass，不创建 request |
| unsupported resources | pyramid/history/multi-output 返回 diagnostic，不静默创建 |

## Debug 验收

Debug 必须能回答：

- 当前有哪些 post effect active？
- 当前有哪些 resource request？
- 哪个 layer 请求了 `Aov.MaskId`？
- ImageChain 当前 read/write 是谁？
- effect 关闭后为什么 debug view inactive？
- 这个 pass 是 ScreenPost 还是 ImagePost？

建议 debug entries：

```text
PostGraph.ActivePlan
PostGraph.ResourceRequests
PostGraph.Diagnostics
ImageChain.PassList
ImageChain.Current
ScreenPost.RuleMask
ImagePost.LayerOutput
ImagePost.AovCompositeMask
```

## 当前代码落地检查

第十一阶段完成时，应能在代码中找到或解释未落地原因：

```text
Runtime/PostProcess/PostEffectDefinition.cs
Runtime/PostProcess/PostLayerDefinition.cs
Runtime/PostProcess/PostResourceRequest.cs
Runtime/PostProcess/PostGraphPlan.cs
Runtime/PostProcess/PostGraphPlanner.cs
Runtime/Image/ImageChain.cs
Runtime/Image/ImageChainContext.cs
Runtime/Image/ImagePassDescriptor.cs
Runtime/Shaders/Post/HoUrpPostAovMask.hlsl
Runtime/Shaders/Post/HoUrpPostLayerBlit.shader
Runtime/Shaders/Image/HoUrpImageLayerBlit.shader
Tests/Runtime/HoUrpPostGraphPlannerTests.cs
Tests/Runtime/HoUrpImageChainTests.cs
```

如果第一版只做纯 C# planner，不接 RenderGraph，也必须在未决项中说明。

## 未决项

| 项 | 当前处理 |
| --- | --- |
| 完整 ScreenPost effect catalog | 不做，后续分批 |
| 完整 ImagePost effect catalog | 不做，后续分批 |
| Bloom / Glow / IrisBlur / RGBBlur | 不做，需要 local ping-pong / pyramid |
| VHS / CRT / Kuwahara | 不做，后续按 descriptor 迁移 |
| History / temporal | 只登记 request kind，不实现 |
| Pyramid | 只登记 request kind，不实现 |
| MultiOutput / MRT | 只登记 diagnostic，不实现 |
| CharacterSpecialization | 不做 |
| Weighted OIT runtime | 后续透明阶段 |

## 风险点

- 新增后处理效果但没有 descriptor。
- disabled layer 仍创建 resource request。
- ImageChain 中间 RT 数量随 layer 增长。
- ImagePost 直接读取 material / object 语义。
- ScreenPost rule 分散到多个 shader。
- AOV composite 默认成为所有 ImagePost effect 能力。
- original source 被每个 effect 单独 copy。
- 旧 `_lil*` 全局名进入新 shader。
- RenderGraph pass 采样未声明纹理。

## 通过标准

- 自动测试覆盖 planner、request、ImageChain、ScreenPost prototype、ImagePost prototype。
- 手动场景能看到 ScreenPost rule mask 和 ImagePost image effect。
- Debug 能解释 active plan 与 resource request。
- 关闭 layer / effect 后不保留 stale 状态。
- `git diff --check` 无错误。
- 没有把旧 HoPost / Shoost ABI 带入新核心。
