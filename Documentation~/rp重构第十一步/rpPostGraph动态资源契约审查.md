# RP 第十一阶段 PostGraph 动态资源契约审查

## 目标

把旧 HoPost / Shoost 的运行时 layer 列表整理成一个可查询、可测试、可调试的新 `PostGraphPlan`。

核心要求：

```text
Active stack
  -> PostGraphPlanner
  -> PostGraphPlan
  -> PostResourceRequest[]
  -> RenderGraph pass recording
```

第十一阶段不要求 planner 支持复杂依赖排序。第一版只要能稳定表达：

- 哪些 layer / effect 当前启用。
- 每个 layer / effect 需要哪些输入。
- 每个 layer / effect 请求哪些 transient resource。
- 哪些 request 会在 effect 关闭后消失。
- 哪些 debug view 当前有效。

## 数据模型

### `PostEffectDefinition`

静态 effect 能力定义。它是事实来源，不读取 Volume 当前值。

```text
PostEffectDefinition
  Id
  DisplayName
  Domain
  ExecutionKind
  RequiredInputs
  OptionalInputs
  ProducedOutputs
  ResourcePolicy
  SupportedDebugViews
  DefaultShaderName
  LegacySource
  Status
```

字段要求：

| Field | 要求 |
| --- | --- |
| `Id` | 稳定英文 ID，不使用旧 enum 直接作为 ABI |
| `Domain` | `ScreenPost` / `ImagePost` / `Image` / `Composite` |
| `ExecutionKind` | 决定 planner 如何请求资源 |
| `RequiredInputs` | 显式输入，不允许 shader 偷读 |
| `ProducedOutputs` | 本阶段可只输出 `Image.Current` / `ScreenPost.Mask` 等最小项 |
| `LegacySource` | 记录旧来源文件或旧 effect 名 |
| `Status` | `Prototype` / `Planned` / `Removed` / `Native` |

### `PostLayerDefinition`

运行时 layer 快照。它可以来自 Volume stack，但不能让 Volume 字段决定结构。

```text
PostLayerDefinition
  LayerId
  EffectId
  Enabled
  RuntimeOrder
  BlendMode
  Intensity
  ParameterBlock
  AovRuleSet
  InputPolicy
  OutputPolicy
```

规则：

- `Enabled=false`：planner 完全跳过。
- `Intensity=0`：默认也可跳过，除非 effect descriptor 标记必须执行。
- `EffectId` 找不到 definition：plan 失败并记录 diagnostic，不静默执行 custom path。
- `ParameterBlock` 只能提供数值、颜色、纹理引用，不能新增 pass。

### `PostResourceRequest`

动态资源请求是本阶段关键。

```text
PostResourceRequest
  RequestId
  OwnerFeatureId
  OwnerLayerId
  OwnerEffectId
  ResourceKind
  Lifetime
  ResolutionPolicy
  FormatPolicy
  ClearPolicy
  ReadSemantics
  WriteSemantic
  DebugName
  Required
```

建议枚举：

```text
ResourceKind:
  ImageChainWork
  OriginalSource
  SemanticInput
  LocalPingPong
  Pyramid
  History
  OutputAlias

Lifetime:
  Frame
  CameraFrame
  PersistentHistory

ResolutionPolicy:
  CameraFull
  CameraHalf
  CameraQuarter
  SourceMatch
  Explicit

FormatPolicy:
  ColorDefault
  ColorHDR
  MaskR8
  MaskR16
  DepthCompatible
```

第十一阶段只实现：

```text
ImageChainWork
OriginalSource
SemanticInput
OutputAlias
```

`LocalPingPong`、`Pyramid`、`History` 先允许 descriptor 声明为 planned，但 planner 不实际创建。

## Planner 行为

### 输入

```text
PostGraphPlannerInput
  CameraData
  ResourceRegistry
  SemanticRegistry
  EffectDefinitions
  ActiveLayers
  CameraColorSource
  ExistingAovResources
  ExistingSssResources
  ExistingSemanticPostResources
```

### 输出

```text
PostGraphPlan
  CameraId
  FrameIndex
  ActiveNodes
  ResourceRequests
  ImageChainRequired
  SemanticInputs
  DebugViews
  Diagnostics
```

### 处理流程

```text
1. Filter disabled layers
2. Resolve effect definitions
3. Sort by runtime order
4. Build node list
5. Gather required inputs
6. Build resource requests
7. Validate unsupported resource kinds
8. Build debug active list
9. Emit diagnostics
```

## 动态注册 / 注销规则

### 启用 effect

当 layer 从 disabled 变为 enabled：

- planner 生成新的 node。
- 生成对应 request。
- Debug active list 出现 effect / layer。
- RenderGraph pass 由当前 frame plan 录制。

### 禁用 effect

当 layer 从 enabled 变为 disabled：

- planner 不再生成 node。
- 对应 request 不再出现。
- Debug active list 不再显示 active effect。
- shader binding 必须绑定 disabled flag 或 empty texture，不能沿用旧全局纹理。

### 删除 effect definition

如果 layer 引用不存在的 effect：

- 不执行 fallback。
- plan 记录 error diagnostic。
- Debug 可显示 missing effect。
- 不创建资源。

### Camera / frame 作用域

- `PostGraphPlan` 每 camera / frame 重建。
- `TextureHandle` 只存在于当前 RecordRenderGraph 调用。
- frame transient request 不能进入静态 registry。
- persistent request 只有 `History` 可以使用，本阶段不实现。

## 与 RenderGraph 的关系

Planner 不直接创建 `TextureHandle`。推荐分工：

```text
PostGraphPlanner:
  decides what is needed

PostGraphResourceResolver:
  maps requests to TextureHandle during RecordRenderGraph

RenderFeature:
  records passes using resolved handles
```

禁止：

- planner 保存 `TextureHandle`。
- effect 保存 `TextureHandle`。
- request 直接调用 `renderGraph.CreateTexture`。
- 在 request 之外临时创建同规格 RT。

## Debug 要求

新增或扩展 debug view：

```text
PostGraph.ActivePlan
PostGraph.ResourceRequests
PostGraph.Diagnostics
ImageChain.ReadWrite
ScreenPost.RuleMask
ImagePost.LayerOutput
```

每个 debug view 必须能回答：

- 当前 frame 是否 active。
- 谁请求了这个资源。
- 输入语义来自哪里。
- 输出写到哪里。
- effect 关闭后为什么没有图。

## 自动测试

建议测试文件：

```text
Tests/Runtime/HoUrpPostGraphPlannerTests.cs
Tests/Runtime/HoUrpPostResourceRequestTests.cs
```

测试项：

```csharp
DisabledLayerDoesNotCreateNode()
DisabledLayerDoesNotCreateResourceRequest()
EnabledSingleImagePassRequestsImageChainWork()
SemanticImagePassRequestsSemanticInputs()
MissingEffectCreatesDiagnostic()
EffectOrderIsDeterministic()
DisablingEffectRemovesDebugActiveEntry()
OriginalSourceRequestedOnlyWhenPolicyRequires()
UnsupportedHistoryRequestCreatesDiagnosticInStage11()
NoRequestUsesLegacyGlobalTextureName()
```

## 实现顺序

1. 定义 enums 和纯数据 structs/classes。
2. 建立一个 hard-coded prototype effect catalog。
3. 编写 planner，不接 RenderGraph。
4. 编写 planner tests。
5. 接入 debug active list。
6. 再接 ImageChain / ScreenPost / ImagePost RenderGraph path。

## 成功标准

- 不打开 Unity 场景也能通过测试验证 planner。
- active layer 到 resource request 的结果可预测。
- disabled layer 不留 active 状态。
- dynamic request 不污染静态 registry。
- debug 能看到本帧 active plan。
- 没有旧全局名进入 request / debug name。

## 风险点

- 把 planner 写成 RendererFeature 内部临时 list，无法测试。
- effect descriptor 不完整，执行时又回到 switch 特判。
- 关闭 effect 后只是不录 pass，但 debug / binding 仍保留旧状态。
- request 太接近 RenderGraph API，导致生命周期边界不清楚。
- 过早支持 history / pyramid，拖慢最小闭环。
