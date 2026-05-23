# RP 重构第十一步执行计划

> 第十一步目标：在 AOV / SSS / SemanticPost / Debug 以及材质生产者契约已经形成基础闭环后，暂时跳过原本排队的 Weighted OIT runtime 视觉验证，优先以旧 `HoPost` / `Shoost` 为来源，建立新 **ScreenPost / ImagePost、PostGraph、ImageChain、动态资源请求、多输入声明、双缓冲资源复用和最小可验证 stack**。
>
> 本阶段不是完整复制旧 `HoPostProcessRendererFeature` / `ShoostPostProcessRendererFeature`，也不是迁移全部后处理效果。重点是先把后处理 stack 从“旧实现能跑”整理成新 RP 可管理、可注册、可注销、可调试的 RenderGraph-first 契约。
>
> 2026-05-23 追加：第十一步不进入“第十二步”。当前 `HoURP ScreenPost Prototype` 与 `HoURP ImagePost Prototype` 已经能进入 RenderGraph 并实际影响屏幕，可作为最小闭环验收；后续仍留在第十一步内，继续补齐 ScreenPost rule 系统、可排序 Post stack、可拖拽 UI 与更完整的基础设施。

---

## 0. 前置状态

当前新包已经具备：

- `HoUrpBuiltInContracts`：登记资源、语义、feature、debug view。
- `AovOutputRendererFeature`：生产 `Aov.MaskId`、`Aov.NormalDepth`、`Aov.SurfaceData`、`Aov.MaterialCustom0_3`、`Aov.SssSource` 等资源。
- `ObjectSemanticAuthoring`：通过 RSUV / MPB 写入对象静态语义。
- `MaterialSemanticAuthoring` 与第十步 generated material 契约：可以作为材质语义 producer。
- `SubsurfaceScatteringRendererFeature`：消费 AOV SSS 输入并生成 `Sss.Source` / `Sss.Diffusion`。
- `SemanticPostProcessRendererFeature`：已经证明 AOV / SSS 可以进入语义后处理。
- `AovDebugRendererFeature`：能观察 AOV、SSS 和 SemanticPost 结果。
- 第十步已经给后续 OIT 保留 `HoUrpOitAccumulation`、`SupportsOit` / `ParticipatesOit` 契约。

HoNpr 侧已经在把材质系统改成更显式、可管理的声明系统。HoUrp-Extensions 侧因此可以先把后处理搬迁的资源架构定下来，让后续材质 producer 与 post consumer 都落在同一套显式声明模型上。

---

## 1. 为什么第十一步转向 ScreenPost / ImagePost

原本第十一步建议做 Weighted OIT runtime 最小验证，因为第十步已经准备了 OIT-ready 材质 pass。但当前更高优先级是：

- HoNpr 侧正在收拢材质 producer，材质语义来源会越来越显式。
- HoUrp-Extensions 侧已经有 AOV、SSS、SemanticPost 和 Debug，足够开始承载语义感知后处理。
- 旧 HoPost / Shoost 已经存在大量可迁移能力，但旧实现的资源策略、全局纹理、compatibility path 和 per-layer RT 创建不应直接进入新包。
- 后处理 stack 会直接触碰多输入、动态启停、双缓冲、AOV composite、original source、history / pyramid 等资源问题，必须先规划清楚。

因此本阶段“跳过验证”只表示 **不把 OIT runtime 视觉验证作为第十一步优先项**。ScreenPost / ImagePost 搬迁本身仍必须有契约验证、自动测试、RenderGraph 资源检查和手动 Unity 验收。

---

## 2. 本阶段范围

### 做

- 定义后处理分层：
  - `ScreenPost`：需要输入 RT / 语义资源的屏幕空间处理，消费 AOV / Semantic Registry / SSS / object / material / geometry 语义。
  - `ImagePost`：最终图像处理 / 风格栈，以纯 image-space 效果为主，只允许轻量 AOV composite。
- 建立第一版 post 描述模型：
  - `PostEffectDefinition`
  - `PostLayerDefinition`
  - `PostStackDefinition`
  - `PostResourceRequest`
  - `PostGraphPlan`
  - `ImagePassDescriptor`
- 建立动态资源请求 / 释放规则：
  - 启用的 layer / effect 才进入当前 frame 的 `PostGraphPlan`。
  - 关闭或移除的 effect 不保留 stale `TextureHandle`、全局纹理、debug active 状态或 persistent request。
  - 所有 request 都必须有 owner、lifetime、resolution、format、clear policy 和 debug name。
- 建立 `ImageChain` 双缓冲模型：
  - `ImageChain.Read -> pass -> ImageChain.Write -> swap`。
  - `WorkA` / `WorkB` 只作为 frame transient 工作区。
  - 线性纯图像 pass 数量增加时，不按 layer 数量线性增长全分辨率中间 RT。
- 明确多输入 / 例外资源：
  - original source。
  - AOV / depth / normal / material / object semantic。
  - SSS / SemanticPost 结果。
  - multi-output / MRT。
  - pyramid / blur ping-pong。
  - history / temporal。
- 先迁移最小可验证能力：
  - ScreenPost rule mask / layer blit 原型。
  - ImagePost single-pass image effect 原型。
  - ImagePost optional AOV composite 原型只做契约和一条最小路径，不扩展成第二套 ScreenPost。
- 更新 feature / resource / debug descriptor。
- 补测试，确认启停 effect 时资源 request 和 debug view 会正确注册 / 注销。
- 在最小可运行 prototype 之后继续打基础设施：
  - 建立 ScreenPost rule 系统，rule 的输入、operator、combine、debug mask 全部集中管理。
  - 建立可排序 Post stack，用户拖拽列表顺序即可改变滤镜叠加顺序。
  - 规划 Editor UI，确保 ScreenPost / ImagePost layer 都由同一套显式 descriptor 与 layer snapshot 驱动。

### 不做

- 不迁移完整 ScreenPost effect catalog。
- 不迁移完整 ImagePost effect catalog。
- 不复制旧 `HoPostProcessRendererFeature` / `ShoostPostProcessRendererFeature` 的 compatibility path。
- 不继承旧 `_lilHoPost*`、`_lilShoost*`、旧 layer property 名或旧全局纹理名作为新 ABI。
- 不把所有后期塞进一个万能 stack。
- 不让 ImagePost 变成第二套 ScreenPost。
- 不让 ScreenPost rule language 散落在各 effect shader 中。
- 不做 Bloom / IrisBlur / RGBBlur / VHS / CRT 等完整效果迁移。
- 不做 temporal / history 资源。
- 不做 depth pyramid。
- 不做 Weighted OIT runtime。
- 不做 CharacterSpecialization 搬迁。
- 不接旧材质 UI 或旧 Volume UI 的全部字段。
- 不在第十一阶段基础设施完成前开启第十二步。

---

## 3. 包边界

`HoUrp-Extensions` 负责：

```text
Runtime/
  PostProcess/
  Image/
  Composite/
  Resources/
  Semantic/
  Capability/
  Debug/
```

本阶段可以新增或规划：

```text
Runtime/PostProcess/
  PostEffectDefinition.cs
  PostLayerDefinition.cs
  PostStackDefinition.cs
  PostResourceRequest.cs
  PostGraphPlanner.cs

Runtime/Image/
  ImageChain.cs
  ImageChainContext.cs
  ImagePassDescriptor.cs
  ImageWorkTexturePool.cs

Runtime/Shaders/Post/
  HoUrpPostLayerBlit.shader
  HoUrpPostAovMask.hlsl

Runtime/Shaders/Image/
  HoUrpImageLayerBlit.shader
```

实际路径可按当前包结构调整，但职责必须保持清楚：

- `PostProcess` 负责语义感知 post stack 的声明和执行计划。
- `Image` 负责纯 image-space chain 与双缓冲资源策略。
- `Composite` 负责跨语义合成，不承载具体风格效果 catalog。
- `Debug` 负责观察 plan、resource request、read/write swap 和 effect 输出。

---

## 4. 核心模型

### 4.1 `PostEffectDefinition`

`PostEffectDefinition` 描述 effect 的静态能力，不读取 Volume 当前值，不直接创建 RenderGraph 资源。

建议字段：

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
  DefaultShader
  LegacySource
```

`ExecutionKind` 第一版建议：

| Kind | 说明 |
| --- | --- |
| `SingleImagePass` | 纯图像单 pass，默认走 ImageChain |
| `SemanticImagePass` | 图像 pass + AOV / semantic 输入 |
| `MultiPassLocal` | 自带局部 ping-pong 或多 pass |
| `MultiResolution` | 需要 pyramid / downsample / upsample |
| `Stateful` | 需要 history 或跨帧资源 |
| `Removed` | 旧 effect 暂不迁移 |

### 4.2 `PostLayerDefinition`

`PostLayerDefinition` 描述一个 layer 的运行时配置快照。它来自 Volume / settings，但不让 Volume 字段直接变成 shader ABI。

建议字段：

```text
PostLayerDefinition
  LayerId
  EffectId
  Enabled
  BlendMode
  Intensity
  Parameters
  AovRuleSetRef
  InputPolicy
  OutputPolicy
```

原则：

- `Enabled=false` 的 layer 不进入 `PostGraphPlan`。
- 参数只控制数值，不控制是否新增 pass / resource；结构差异由 `PostEffectDefinition.ExecutionKind` 决定。
- 需要 AOV 的 layer 必须声明 `AovRuleSetRef` 和语义输入，不能让 shader 偷读全局 AOV。

### 4.3 `PostResourceRequest`

`PostResourceRequest` 是本阶段最关键的契约。它用于把 HoNpr 提到的 frame-local resolve 思路扩展到 post stack：当前 frame 需要什么资源，就在当前 frame 显式请求；不需要时不注册。

建议字段：

```text
PostResourceRequest
  RequestId
  OwnerFeatureId
  OwnerLayerId
  ResourceKind
  Lifetime
  ResolutionPolicy
  FormatPolicy
  ClearPolicy
  InputSemantics
  OutputSemantic
  DebugName
```

`ResourceKind` 第一版建议：

| Kind | 说明 |
| --- | --- |
| `ImageChainWork` | `WorkA` / `WorkB` 双缓冲 |
| `OriginalSource` | camera color 或 chain begin source 的 copy |
| `SemanticInput` | AOV / SSS / SemanticPost / depth / normal 等输入 |
| `LocalPingPong` | blur / multi-pass effect 私有局部 ping-pong |
| `Pyramid` | 多分辨率链 |
| `History` | 跨帧资源，第一版只登记未实现 |
| `OutputAlias` | 最终输出或 copy back 目标 |

动态注册 / 注销规则：

- `PostGraphPlanner` 每 camera / frame 从当前 active stack 重建 `PostResourceRequest` 列表。
- `TextureHandle` 不跨 frame 保存。
- persistent GPU resource 只允许 `History` 这类明确声明的 request 使用，第一版不实现。
- effect 禁用后，对应 request 不再出现在 plan、resource registry frame view 和 debug active list 中。
- 如果某个 shader property 需要清空，必须通过统一 empty texture / disabled flag 绑定，不依赖上帧遗留全局纹理。
- Debug view 必须能显示“本帧未注册”，而不是继续显示旧图。

### 4.4 `ImageChain`

`ImageChain` 只服务线性纯图像域或可降级为线性图像 pass 的 ImagePost effect。

执行模型：

```text
Begin(source)
  current = copy/import source
  alternate = create matching transient

For each SingleImagePass:
  Read = current
  Write = alternate
  Record pass(Read -> Write)
  Swap current/alternate

End()
  Publish current as Image.Final for this chain
  Copy current to camera color when required
```

规则：

- `WorkA` / `WorkB` 不是公共 semantic resource。
- 每个 pass 仍必须在 RenderGraph 中声明 read / write。
- 如果 pass 需要 original source 和 current 同时输入，必须申请 `OriginalSource`。
- 如果 pass 需要 AOV / depth / normal，它不再是纯 ImageDomain pass，必须升级为 `SemanticImagePass`。
- 如果 pass 有多个输出，必须声明 `MultiOutput` 或专门 `PostResourceRequest`。

### 4.5 多输入约束

后处理 pass 可能不只处理一张图。第一版必须允许声明多输入，但不能回到隐式全局纹理。

输入分类：

| 输入 | 规则 |
| --- | --- |
| `PrimaryImage` | 当前 `ImageChain.Read` 或 explicit source |
| `OriginalSource` | 由 chain begin 显式 copy / import |
| `Aov.*` | 通过 semantic/resource registry 解析 |
| `Camera.Depth` / `Aov.NormalDepth` | 通过 pass request 声明 |
| `Sss.*` | 通过 producer resource 显式声明 |
| `SemanticPost.*` | 通过 producer resource 显式声明 |
| `History.*` | 本阶段只登记，不实现 |
| `Pyramid.*` | 本阶段只登记，不实现 |

禁止：

- shader 直接读取旧全局 `_lilHoAov*` / `_lilShoost*`。
- effect 私自缓存上一个 pass 输出。
- 同一 pass 对同一 TextureHandle 既读又写。
- 把 camera color 作为全局纹理发布后，又在后续 pass 中写同一张 camera attachment。

---

## 5. ScreenPost 搬迁边界

ScreenPost 是需要输入 RT / 语义资源的屏幕空间后处理，不属于纯 ImageDomain。

第一版只做：

- `ScreenPost.AovRuleMask` 规则声明模型。
- `ScreenPost.LayerBlit` 最小 layer blend。
- 一个示例 effect，例如 `EdgeLightPrototype` 或 `OutlinePrototype`，只用于证明 rule mask 和 layer blend。
- AOV / object / material / normal-depth 输入必须来自已注册资源。

ScreenPost rule 第一版建议：

```text
ScreenPostAovRule
  SourceSemantic
  Operator
  CompareValue
  Threshold
  Combine
```

允许先支持有限规则：

- object id / group id / flags。
- object custom bit。
- material class。
- thickness / curvature threshold。
- normal-depth edge 只作为 prototype，可延后完整化。

不做：

- 不一次迁移旧 HoPost 全部 rule operator。
- 不把 rule 编码塞进每个 effect shader。
- 不让 ScreenPost 直接写 ImagePost final style 参数。

---

## 6. ImagePost 搬迁边界

ImagePost 是最终图像处理 / 风格栈。它可以轻量使用 AOV composite，但不能变成第二套 ScreenPost。

第一版只做：

- `ImagePost.SingleImagePass` 描述模型。
- 一个纯图像 effect 原型，例如 `ColorAdjustPrototype` / `VignettePrototype` / `LayerBlitPrototype`。
- 可选 `ImagePost.AovCompositePrototype`：证明 ImagePost effect 可以声明轻量 AOV mask 输入，但不拥有 ScreenPost rule language。
- ImageChain 双缓冲执行。

ImagePost effect descriptor 第一版应能表达：

```text
ImagePostEffectDefinition
  Id
  RuntimeOrder
  ExecutionKind
  SupportsAovComposite
  RequiredInputs
  ResourcePolicy
  LegacySource
```

不做：

- 不迁移全部 Glow / IrisBlur / RGBBlur / VHS / CRT / Kuwahara / Weather / LogoOverlay。
- 不建立每 layer 一张 RT 的默认策略。
- 不把 AOV composite 支持做成所有 effect 默认能力。
- 不让 ImagePost effect 读取 object/material 细粒度语义；需要这类语义的效果应转入 ScreenPost 或 CharacterSpecialization。

---

## 7. 实施步骤

### Step 1. 审查旧 HoPost / Shoost 资源与输入

读取旧实现：

```text
lilToon-URP-Extensions/Runtime/HoPostProcessing
lilToon-URP-Extensions/Runtime/ShoostPostProcessing
```

输出两张表：

- 旧 HoPost effect / layer 读取哪些 AOV、depth、normal、material、object 输入。
- 旧 Shoost effect 哪些是 single pass、multi pass、stateful、AOV composite、removed。

结论只进入 `LegacyInterop` / 审查文档，不变成新 ABI。

### Step 2. 定义 Post / Image 描述模型

新增或规划：

```text
Runtime/PostProcess/PostEffectDefinition.cs
Runtime/PostProcess/PostLayerDefinition.cs
Runtime/PostProcess/PostStackDefinition.cs
Runtime/PostProcess/PostResourceRequest.cs
Runtime/PostProcess/PostGraphPlanner.cs
Runtime/Image/ImageChain.cs
Runtime/Image/ImageChainContext.cs
Runtime/Image/ImagePassDescriptor.cs
```

要求：

- effect 静态能力可查询。
- active layer 转换为 frame-local plan。
- disabled layer 不进入 plan。
- `ImageChain` 能表达 read/write swap。
- 多输入必须显式列在 descriptor / request 中。

### Step 3. 建立动态注册 / 注销测试

测试方向：

```text
Enabled layer creates PostResourceRequest
Disabled layer creates no PostResourceRequest
Disabling layer removes debug active entry
SemanticImagePass requires declared semantic inputs
SingleImagePass uses ImageChain WorkA / WorkB
ImageChain work textures are frame transient
OriginalSource is requested only when needed
No stale global texture binding remains after effect disabled
```

### Step 4. 实现 ImageChain 最小路径

目标：

- 从 camera color 或明确 source 开始。
- 复制到 `Image.WorkA` 或导入 source。
- 创建 `Image.WorkB`。
- 录制 1-2 个 `SingleImagePass`。
- 每个 pass 显式 read / write。
- 最终 copy 回 camera color 或输出到明确目标。

验收：

- 2 个以上纯图像 pass 不产生 2 个以上同规格全分辨率工作 RT。
- Frame Debugger / RenderGraph viewer 能看出每个 pass 的 read / write。
- Debug view 能显示当前 chain 的 read/write/swap 状态。

### Step 5. 实现 ScreenPost 最小语义路径

目标：

- 一个 ScreenPost layer 能声明 AOV rule mask。
- pass 显式读取 `Aov.MaskId` / `Aov.NormalDepth` / `Aov.SurfaceData` 中需要的资源。
- rule mask 结果用于 layer blend。
- 输出继续进入 ImageChain 或 explicit post output。

验收：

- 关闭 ScreenPost layer 后，不再请求 AOV semantic 输入。
- Debug view 能显示 rule mask 或 layer influence。
- 不读取旧 `_lilHoAov*`。

### Step 6. 实现 ImagePost 最小图像路径

目标：

- 一个 ImagePost single-pass effect 走 ImageChain。
- 可选 AOV composite prototype 必须声明 semantic input。
- effect order 来自 descriptor，不来自 hard-coded switch。

验收：

- 关闭 ImagePost effect 后，ImageChain pass 数量减少，资源 request 消失。
- 多个 single-pass effect 共用 WorkA / WorkB。
- AOV composite 未启用时不声明 AOV 输入。

### Step 7. 更新 registry / debug / docs

要求：

- `HoUrpBuiltInContracts` 或等价 registry 能查询 Post / Image feature。
- Debug 能列出 active post plan、effect、layer、resource request、read/write handle。
- 未启用 effect 不显示为 active producer。
- 文档记录哪些旧 effect 已迁移、哪些仅登记为 planned / removed。

### Step 8. 第十一步继续深化：Rule 系统

目标：

- 把 ScreenPost 的语义筛选从 prototype fallback 改成正式 rule set。
- rule evaluation 只存在一套公共模型和公共 HLSL，不散落到每个 effect shader。
- 每个 rule 根据 source semantic 推导所需 AOV / SSS / SemanticPost 输入，并写进 `PostResourceRequest`。

第一版只做有限规则：

```text
Source:
  Always
  Object.MaskWeight
  Object.Id
  Object.GroupId
  Object.Flags
  Object.Custom0_7
  Material.Class
  Material.Thickness
  Material.Curvature
  Geometry.LinearDepth
  Geometry.WorldNormalFacing

Operator:
  Always
  Greater
  Less
  Range
  EqualByte
  FlagsAny
  FlagsAll

Combine:
  Replace
  Or
  And
  Subtract
  Multiply
```

验收：

- 开启 / 关闭 rule 后，对应 semantic input request 会出现 / 消失。
- `ScreenPost.RuleMask` 可 debug。
- 没有 AOV 命中时不依赖 preview fallback 作为正式效果。
- 复杂规则仍归 ScreenPost，不允许 ImagePost 承载 object/material rule language。

### Step 9. 第十一步继续深化：可排序 Post Stack

目标：

- 用户在 UI 中拖动 layer / filter 列表顺序，即改变最终叠加顺序。
- 列表顺序是 runtime order 的事实来源之一，但仍必须转换成 `PostLayerDefinition` / `PostGraphPlan`，不能让 Inspector 字段直接决定资源结构。
- ScreenPost 与 ImagePost 可以先分为两个 stack，后续再评估是否需要统一 PostStack 视图。

第一版排序规则：

```text
ScreenPost stack:
  ordered ScreenPost layers
  each layer has rule set + blend + intensity

ImagePost stack:
  ordered ImagePost filters
  each layer has effect id + intensity + parameters

Planner:
  preserves UI list order
  disabled item skipped
  missing effect emits diagnostic
  generated pass list follows active item order
```

验收：

- 拖动 ImagePost 两个滤镜顺序，最终画面变化。
- 拖动 ScreenPost layer 顺序，blend 结果变化。
- RDG pass 顺序与 active plan 一致。
- WorkA / WorkB 数量不随 item 数量增长。

### Step 10. 第十一步继续深化：Editor UI 规划

目标：

- 使用可拖拽列表管理 layer / filter 顺序。
- 每个列表项清晰显示 enabled、effect 类型、名称、强度、主要颜色 / 参数摘要。
- 复杂 rule 进入展开详情，不在主列表里挤满所有字段。
- UI 只编辑 settings；运行时由 settings 生成 layer snapshot，再交给 planner。

建议落点：

```text
Editor/PostProcess/
  ScreenPostPrototypeRendererFeatureEditor.cs
  ImagePostPrototypeRendererFeatureEditor.cs
  PostLayerListDrawer.cs
  ScreenPostRuleSetDrawer.cs
```

UI 规则：

- 主列表支持拖拽排序、添加、复制、删除、启停。
- ScreenPost layer 展开后编辑 rule set。
- ImagePost filter 展开后编辑 effect 参数。
- 对需要 AOV / history / pyramid 的 effect，在 UI 中显示 resource badge。
- 对 unsupported planned effect，在 UI 中显示 disabled / planned 状态，不静默执行。

---

## 8. 成功标准

- 有第一版 Post / Image descriptor 文档和代码模型。
- 有 frame-local `PostGraphPlan` 或等价 planner。
- 动态启停 layer / effect 时，resource request、debug active entry 和 shader binding 不保留旧状态。
- 纯图像 ImagePost 原型走 `ImageChain` 双缓冲。
- ScreenPost 原型能显式声明并消费 AOV / semantic 输入。
- 多输入 effect 必须在 descriptor / request 中列出所有输入。
- `Image.WorkA` / `Image.WorkB` 不作为长期公共 resource 发布。
- 关闭 AOV composite 后，不再请求 AOV 输入。
- 没有旧 `_lilHoPost*`、`_lilShoost*`、`_lilHoAov*` 名称进入新 ABI。
- 没有直接复制旧 compatibility path。
- Weighted OIT-ready 材质契约未被删除，后续仍可回到 OIT runtime。

---

## 9. 风险点

- 直接复制旧 HoPost / Shoost renderer feature，导致新包继承 compatibility path 和旧全局纹理。
- 把 ScreenPost 与 ImagePost 合成一个万能 stack，破坏语义后处理和纯图像风格栈边界。
- 每个 layer 默认创建一张全分辨率 RT，重新引入资源数量随 layer 增长的问题。
- 只在 C# switch 中执行 effect，而没有 effect descriptor / resource request。
- effect 禁用后 debug view 或全局纹理仍显示上一帧结果。
- AOV composite 变成 ImagePost 默认能力，导致 ImagePost 读取对象/材质细粒度语义。
- 多输入 effect 通过 shader 全局纹理偷读，而不是在 RenderGraph pass 中声明。
- original source 与 current chain source 混淆，造成同一 camera color 读写冲突。
- 过早迁移 Bloom / VHS / CRT / IrisBlur 等复杂效果，掩盖基础资源架构问题。

第十一步的验收重点是 **Post stack 可声明、资源可动态注册/注销、多输入可追踪、纯图像链可双缓冲复用、ScreenPost / ImagePost 边界清楚**，不是一次性复刻旧视觉效果。
