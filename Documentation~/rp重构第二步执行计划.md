# RP 重构第二步执行计划

> 第二步目标：把第一步冻结的契约推进到 **最小可验证 AOV 绘制闭环**。  
> 本文是执行大纲，供审查。审查通过后，先输出各步骤的审查文档，再进入对应代码实现。

---

## 0. 必读前置

执行本计划前，先读：

- `Documentation~/rp设计哲学底线.md`
- `Documentation~/rp重构初步大纲.md`
- `Documentation~/rp重构第一步执行计划.md`
- `Documentation~/rp重构第一步/rp第一阶段验收清单.md`
- `Documentation~/旧实现快速定位索引.md`

当前第二步的起点是：

- 已有最小 runtime contract 类型。
- 已有 `Aov.MaskId` / `Aov.NormalDepth` Resource Registry。
- 已有 `AOV / Mask`、`AOV / Object ID`、`AOV / Linear Depth`、`AOV / World Normal` DebugView Registry。
- 已有 RenderGraph 资源声明工具。
- 已有 `AovOutputRendererFeature` 的最小资源声明 pass。
- 已有 `SemanticPostProcessRendererFeature` 的只读 AOV 依赖 pass。

第二步不能突破的底线：

- 不复制旧 `HoAovRendererFeature.cs`。
- 不迁移旧 compatibility path。
- 不把旧 `_lilHoAov*` 命名提升为长期 ABI。
- 不把 `lilToon/lilPBR` 当作新 RP 核心。
- 不在没有审查文档的情况下新增完整 HoSSS、Shoost、OIT、ShadowCast、CharacterSpecialization。
- 不让 shader 隐式写全局纹理后再由后处理偷偷读取。

---

## 1. 第二步的真实目标

第二步不是“完整迁移 HoAOV”，也不是“让旧材质无痛跑起来”。

第二步只做一个最小闭环：

```text
契约注册
  -> RenderGraph resource declaration
  -> AovOutput 最小绘制
  -> AOV DebugView 最小显示
  -> SemanticPost-like 最小只读消费
  -> 测试 / 编译 / 行为验收
```

这个闭环要证明：

1. `AovOutput` 不是只声明资源，而是真的能写入 `Aov.MaskId` / `Aov.NormalDepth`。
2. 写入路径遵守 Resource Registry、Feature Descriptor 和 Pass Stage。
3. 后续消费者只通过显式 Resource Handle / Registry 读取，不绕回旧全局 RT。
4. DebugView 是注册系统的一部分，不是临时 shader mode。
5. 旧实现只作为行为参照，不把旧结构搬进新核心。

---

## 2. 本阶段不做什么

明确不做：

- 不迁移完整旧 HoAOV 多 MRT。
- 不迁移 `Aov.TangentNormal`、`Aov.SurfaceData`、`Aov.MaterialCustom0_3`、`Aov.ObjectCustom0_3`、`Aov.ObjectCustom4_7`、`Aov.SssSource`。
- 不迁移 `HoAOVSSS`。
- 不接完整 HoPost rule 系统。
- 不接完整 Debug HUD / Debug Panel。
- 不接新材质生成系统。
- 不接旧材质 inspector。
- 不建立旧 shader property 长期兼容层。
- 不修改 `lilToon` / `lilPBR`。

允许做：

- 为最小测试写新 shader / hidden shader。
- 写最小 RendererFeature / RenderGraph pass。
- 写最小 debug blit / debug composite。
- 写最小测试场景或测试资源清单。
- 查旧实现确认 pass 顺序、格式和编码。
- 在文档里记录旧名到新名的临时对照。

---

## 3. 第二步建议新增的审查文档产物

第二步先产出以下文档，再按文档推进代码：

```text
Documentation~/rp重构第二步/
├── rp第二阶段实现边界审查.md              # 第二步范围、不做项、风险边界
├── rpAovOutput最小绘制链路审查.md          # AOV 最小绘制 pass、renderer list、shader pass 策略
├── rpAov资源声明与绑定审查.md              # RenderGraph resource declaration、global binding、lifetime
├── rpAov最小编码审查.md                    # MaskId / NormalDepth 的第一版编码、格式、clear 值
├── rpAov最小Shader契约审查.md              # 新 shader pass / hidden shader / LightMode 命名边界
├── rpDebugView最小显示链路审查.md          # AOV debug view 的最小显示方式
├── rpSemanticPost最小消费者审查.md         # 只读 AOV consumer 的输入、输出和不做项
├── rp第二阶段测试与验收清单.md             # 编译、PlayMode/EditMode、Frame Debug / RenderDoc 检查
└── rp第二阶段未决项登记.md                 # 本阶段确认不了但必须留档的项
```

如果需要压缩文档数量，最少保留四份：

```text
rp第二阶段实现边界审查.md
rpAovOutput最小绘制链路审查.md
rpDebugView与SemanticPost最小闭环审查.md
rp第二阶段测试与验收清单.md
```

---

## 4. 执行顺序总览

```text
Step 1. 冻结第二阶段实现边界
Step 2. 审查最小 AOV 绘制链路
Step 3. 审查 AOV 资源声明与 shader binding
Step 4. 审查 MaskId / NormalDepth 编码
Step 5. 审查最小 shader 契约
Step 6. 审查 DebugView 最小显示链路
Step 7. 审查 SemanticPost 最小消费者
Step 8. 实现最小闭环代码
Step 9. 验证、记录差异、冻结第二阶段
```

原则：

- Step 1-7 先写审查文档。
- Step 8 才写代码。
- Step 9 写验收清单。

---

## 5. Step 1：冻结第二阶段实现边界

### 5.1 目标

把第二步做什么、不做什么、允许临时留白什么写清楚。

### 5.2 必须回答

- 第二步是否只覆盖 `Aov.MaskId` / `Aov.NormalDepth`。
- 第二步是否允许新增最小 shader。
- 第二步是否允许使用 fallback drawing。
- 第二步是否需要新材质系统参与。
- 第二步是否需要旧 `lilToon/lilPBR` shader 参与。
- 第二步的成功标准是“资源可写可读”，还是必须视觉接近旧 HoAOV。

### 5.3 建议判定

- 只覆盖 `Aov.MaskId` / `Aov.NormalDepth`。
- 允许新增 `Hidden/HoURP/AOV/...` 最小 shader。
- 不接旧材质系统。
- 不要求视觉接近完整旧 HoAOV，只要求数据链路正确。
- 旧 HoAOV 输出仅作为编码和行为参考。

### 5.4 产物

产物写入：

- `rp第二阶段实现边界审查.md`

---

## 6. Step 2：审查最小 AOV 绘制链路

### 6.1 目标

把 `AovOutputRendererFeature` 从“声明资源”推进到“能写入资源”。

### 6.2 候选链路

最小链路：

```text
AovOutputRendererFeature
  -> AovOutputPass.RecordRenderGraph
  -> Declare Aov.MaskId / Aov.NormalDepth
  -> Build renderer list
  -> Draw override/fallback material or explicit test material
  -> Write MRT: MaskId + NormalDepth
```

### 6.3 需要审查的选择

| 项 | 候选 | 建议 |
| --- | --- | --- |
| 绘制对象来源 | scene renderer list / full screen triangle / test-only generated geometry | scene renderer list |
| shader 来源 | 新 hidden shader / 旧 HoAovFallback.shader / 旧材质 pass | 新 hidden shader |
| LightMode | 新 `HoUrpAovOutput` / 旧 `HoAOV` | 新 `HoUrpAovOutput`，旧名只留对照 |
| fallback 策略 | override material / material pass / 不做 fallback | 第一版使用 override material |
| filtering | opaque only / all render queue / settings 可配 | opaque first |
| pass stage | `GeometrySemanticAov` / `MaterialShadingSemanticAov` | 先 `GeometrySemanticAov` |

### 6.4 不进入本步

- 不支持透明 AOV。
- 不支持 alpha clip 完整一致。
- 不支持 SSS source。
- 不支持 material custom / object custom。
- 不支持旧材质原生 `HoAOV` pass。

### 6.5 产物

产物写入：

- `rpAovOutput最小绘制链路审查.md`

---

## 7. Step 3：审查 AOV 资源声明与 shader binding

### 7.1 目标

明确资源如何从 Resource Registry 进入 RenderGraph，再如何被 shader 绑定和消费者读取。

### 7.2 需要审查的资源

| Resource | Producer | Consumer | Binding Need | 本阶段 |
| --- | --- | --- | --- | --- |
| `Aov.MaskId` | `AovOutput` | DebugView, SemanticPost-like | may need global binding after pass | 必做 |
| `Aov.NormalDepth` | `AovOutput` | DebugView, SemanticPost-like | may need global binding after pass | 必做 |

### 7.3 需要审查的问题

- 是否允许 `builder.SetGlobalTextureAfterPass`。
- 新 shader property 名是否本阶段定案。
- global binding 是正式 ABI 还是 shader backend detail。
- DebugView 是否只能从 `HoUrpRenderGraphResources` 读取。
- `HoUrpRenderGraphResources` 是否需要记录 producer pass。
- `ResourceDefinition` 是否需要扩展 shader binding 字段。

### 7.4 建议判定

- 逻辑资源名继续用 `Aov.MaskId` / `Aov.NormalDepth`。
- 本阶段可新增 shader binding 名，但必须登记在审查文档。
- shader binding 不是 Resource Registry 的主键。
- consumer 首选 `HoUrpRenderGraphResources`，必要 shader 绑定使用集中映射。

### 7.5 产物

产物写入：

- `rpAov资源声明与绑定审查.md`

---

## 8. Step 4：审查 MaskId / NormalDepth 最小编码

### 8.1 目标

确定第二步最小写入格式，避免实现时随手编码。

### 8.2 候选编码

`Aov.MaskId`：

```text
R = Object.MaskWeight / Coverage
G = Object.Id
B = Object.GroupId
A = Object.Flags
```

`Aov.NormalDepth`：

```text
RGB = encoded world normal
A   = linear depth or normalized device depth
```

### 8.3 必须审查的问题

- Object ID / Group ID 第一版是否写常量。
- Flags 第一版是否写 0。
- normal 是否写 world normal 还是 view normal。
- depth 是否写 linear eye depth、raw depth 还是 0。
- clear neutral normal 是否继续是 `(0.5, 0.5, 1, 1)`。
- normal/depth debug view 是否需要 decode 函数。

### 8.4 建议判定

- 第一版允许 Object ID / Group ID / Flags 写常量。
- `MaskWeight` 写 1，背景 clear 0。
- `WorldNormal` 写 encoded world normal。
- `LinearDepth` 如果无法可靠取得，先写 normalized depth 并在文档登记。
- Debug decode 必须和编码文档一致。

### 8.5 产物

产物写入：

- `rpAov最小编码审查.md`

---

## 9. Step 5：审查最小 shader 契约

### 9.1 目标

定义第二步新增 shader 的职责，不让它变成旧材质系统入口。

### 9.2 候选 shader

```text
Shaders/Hidden/HoURP/AOV/AovOutputFallback.shader
Shaders/Hidden/HoURP/Debug/AovDebug.shader
Shaders/Hidden/HoURP/SemanticPost/AovReadProbe.shader
```

### 9.3 最小 shader 输入

AOV fallback：

- Object to clip transform。
- world normal。
- position WS / depth。
- optional per-object constants。

Debug：

- source texture handle。
- selected debug view。
- decode parameters。

SemanticPost probe：

- `Aov.MaskId`。
- `Aov.NormalDepth`。
- camera color if later需要叠加。

### 9.4 禁止项

- 不 include 旧 `lil_pass_hoaov.hlsl`。
- 不 include 旧 `HoAovSampling.hlsl` 作为正式依赖。
- 不使用旧 `_lilHoAov*` 作为逻辑资源名。
- 不把旧材质 property 当作新 shader ABI。

### 9.5 产物

产物写入：

- `rpAov最小Shader契约审查.md`

---

## 10. Step 6：审查 DebugView 最小显示链路

### 10.1 目标

让 DebugView 从注册表走到画面输出，而不是每个 Feature 自己开临时模式。

### 10.2 最小链路

```text
DebugViewRegistry
  -> selected DebugView id
  -> source Resource
  -> DebugComposite / AovDebug pass
  -> camera color replace or overlay
```

### 10.3 本阶段 DebugView

- `AOV / Mask`
- `AOV / Object ID`
- `AOV / Linear Depth`
- `AOV / World Normal`

### 10.4 需要审查的问题

- Debug selection 放在哪里。
- 是否需要 Volume / RendererFeature setting。
- replace mode 是否足够。
- overlay 是否本阶段做。
- DebugView 无源资源时如何显示。
- 是否需要全白 enabled status view。

### 10.5 建议判定

- 第一版只做 Replace。
- Debug selection 先放 RendererFeature serialized field。
- 无源资源时不执行 pass，并记录 warning 策略。
- overlay / HUD / capture 进入后续阶段。

### 10.6 产物

产物写入：

- `rpDebugView最小显示链路审查.md`

---

## 11. Step 7：审查 SemanticPost 最小消费者

### 11.1 目标

证明 AOV 不是只能被 debug 读取，也能被正式 consumer 显式读取。

### 11.2 最小消费者候选

```text
SemanticPostProcessRendererFeature
  -> Read Aov.MaskId / Aov.NormalDepth
  -> Apply trivial full-screen effect or write probe result
```

### 11.3 候选效果

| 效果 | 说明 | 建议 |
| --- | --- | --- |
| no-op read pass | 只声明读取 | 过弱，只能验证依赖 |
| mask tint | 读取 mask 后给 camera color 染色 | 推荐 |
| depth heat | 读取 depth 显示热力 | 可作为 debug，不作为 post |
| outline / edge | 接近 HoPost，但范围变大 | 延后 |

### 11.4 不进入本步

- 不做 HoPost layer stack。
- 不做 AOV rule language。
- 不做 subject mask。
- 不做 CustomMaterial。
- 不做 Volume stack。

### 11.5 产物

产物写入：

- `rpSemanticPost最小消费者审查.md`

---

## 12. Step 8：实现最小闭环代码

### 12.1 前置条件

必须先完成 Step 1-7 的审查文档。

### 12.2 建议实现顺序

```text
1. 补 AOV shader property / binding 名集中定义。
2. 完善 AovOutputRendererFeature 设置项。
3. 实现 AovOutput 最小 renderer list 绘制。
4. 实现 AOV fallback shader。
5. 实现 DebugView replace pass。
6. 实现 SemanticPost mask tint 或等价最小消费者。
7. 补测试。
8. 写验收清单。
```

### 12.3 建议新增代码位置

```text
Runtime/
├── Features/
│   ├── AovOutputRendererFeature.cs
│   └── SemanticPostProcessRendererFeature.cs
├── RenderGraph/
│   ├── HoUrpRenderGraphResources.cs
│   └── ...
├── Debug/
│   └── DebugCompositeRendererFeature.cs 或 AovDebugRendererFeature.cs
└── Shaders/
    └── Hidden/HoURP/...
```

如果 Unity package 不适合把 shader 放在 `Runtime/Shaders`，需要在审查文档里先确定路径。

### 12.4 测试建议

- Registry count / link tests。
- Resource descriptor tests。
- Shader property mapping tests。
- RenderGraph resource declaration tests。
- Unity Editor compile。
- 最小 scene 手动验证。
- 必要时 RenderDoc 截帧确认 resource written/read。

---

## 13. Step 9：验证、记录差异、冻结第二阶段

### 13.1 验收条件

第二阶段完成时必须满足：

- `AovOutput` 有 Descriptor、Resource、Semantic、DebugView 对齐。
- `Aov.MaskId` / `Aov.NormalDepth` 由 RenderGraph pass 写入。
- `SemanticPostProcess` 能显式读取这两个 AOV 资源。
- 至少一个 DebugView 能显示 AOV 内容。
- 没有依赖旧 compatibility path。
- 没有修改 `lilToon/lilPBR`。
- 没有把旧 `_lilHoAov*` 作为逻辑资源名。
- 新增 shader binding 有登记。
- 不能完成的视觉一致项有留档。

### 13.2 产物

产物写入：

- `rp第二阶段测试与验收清单.md`
- `rp第二阶段未决项登记.md`

---

## 14. 第二阶段未决项预登记

| Item | 本阶段处理方式 | 后续阶段 |
| --- | --- | --- |
| 新 shader property 最终命名 | 本阶段定最小 AOV/debug binding，并留档 | shader migration 前复核 |
| AOV exact encoding | 本阶段只定 MaskId / NormalDepth | 扩到 SurfaceData/SSS 前复核 |
| 旧材质 HoAOV pass 接入 | 不做 | 新材质系统或 legacy validation 阶段 |
| Object semantic authoring UI | 不做完整 UI | Capability UI 阶段 |
| Material semantic producer | 不做 | 材质系统阶段 |
| Debug overlay / HUD / capture | 只做最小 replace | Debug Framework 阶段 |
| RenderDoc 自动验收 | 只登记需求 | 有完整 Unity Project 后接入 |

---

## 15. 第二阶段审查清单

审查本文时，请重点看：

- 第二步是否足够小。
- 是否误把旧 HoAOV 整体迁进来了。
- 是否仍然遵守 RenderGraph-first。
- 是否所有新增 pass 都能回答生产/消费关系。
- 是否把 shader binding 和逻辑资源名分开了。
- 是否有无登记的临时全局纹理。
- 是否 DebugView 真正走注册表。
- 是否 SemanticPost 只是最小消费者，而不是 HoPost stack 迁移。
- 是否保留了后续扩展到 SurfaceData / Custom / SSS Source 的空间。

---

## 16. 推荐执行节奏

建议按 4 个小批次推进。

### Batch A：边界与编码审查

产物：

- `rp第二阶段实现边界审查.md`
- `rpAov最小编码审查.md`
- `rpAov资源声明与绑定审查.md`

目标：

- 定清楚只做 `MaskId / NormalDepth`。
- 定清楚格式、clear、binding。

### Batch B：AOV 输出审查

产物：

- `rpAovOutput最小绘制链路审查.md`
- `rpAov最小Shader契约审查.md`

目标：

- 决定 renderer list、shader pass、override material。

### Batch C：Debug 与消费者审查

产物：

- `rpDebugView最小显示链路审查.md`
- `rpSemanticPost最小消费者审查.md`

目标：

- 决定最小 debug view。
- 决定最小 semantic post read effect。

### Batch D：代码与验收

产物：

- 实现代码。
- `rp第二阶段测试与验收清单.md`
- `rp第二阶段未决项登记.md`

目标：

- 最小 AOV 闭环能编译、能写入、能读取、能调试。

---

## 17. 第二阶段完成定义

第二阶段完成时，应该能回答：

- `Aov.MaskId` 和 `Aov.NormalDepth` 是在哪里声明的。
- 谁写这两个资源。
- 谁读这两个资源。
- shader binding 名是什么，是否只是 backend detail。
- MaskId / NormalDepth 的编码是什么。
- DebugView 如何找到 source resource。
- SemanticPost-like consumer 如何显式声明依赖。
- 哪些旧 HoAOV 行为本阶段没有迁移。
- 下一阶段扩展到哪些资源最合理。

如果这些问题答不上来，就不能进入 SurfaceData / SSS / ObjectCustom / HoPost rule 的迁移。

---

## 18. 最重要的底线

第二阶段做的是 **最小 AOV 闭环验证**，不是 **完整 HoAOV 迁移**。

只要 `Aov.MaskId` / `Aov.NormalDepth` 能按新契约被生产、读取和调试，就可以冻结第二阶段。其它旧能力都应留给后续阶段逐个进入契约和验收。
