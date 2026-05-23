# RP 重构第十二步执行计划

> 第十二步目标：在第十一步完成 `ScreenPost / ImagePost` 最小可执行后处理基础设施、动态资源请求、排序 UI，并删除旧 `SemanticPostProcessRendererFeature` 后，回到此前延后的 **Weighted OIT runtime 最小闭环**。本阶段只验证透明 OIT 的 RenderGraph-first 数据流，不迁移 HoShadowCast、CharacterSpecialization、Planar Reflection，也不继续扩充 Post effect catalog。

---

## 0. 前置状态

当前新包已经具备：

- `AovOutputRendererFeature`：生产 AOV object / material / geometry / SSS source 资源。
- `SubsurfaceScatteringRendererFeature`：消费 AOV SSS 输入并生产 `Sss.Source` / `Sss.Diffusion`。
- `ScreenPostPrototypeRendererFeature`：替代旧 `SemanticPostProcessRendererFeature`，以显式 rule / resource request 消费 AOV / SSS。
- `ImagePostPrototypeRendererFeature`：通过 ImageChain / WorkA / WorkB 执行最小 image stack。
- `PostGraphPlanner` / `PostResourceRequest` / `ImageChain`：已证明 frame-local request、双缓冲和多输入声明可行。
- Generated material prototype：已经声明 `HoUrpOitAccumulation` pass、`SupportsOit` / `ParticipatesOit` capability、`OIT.AccumulationInput` / `OIT.RevealageInput` 语义。
- 旧 `SemanticPost.Mask`、旧 `SemanticPostProcess` feature、旧 semantic post shader 已移除，后处理语义入口收敛到 `ScreenPost`。

因此第十二步不再继续扩大 Post 基础设施，而是让第十步已经准备好的 OIT-ready 材质 pass 进入真实 runtime。

---

## 1. 为什么第十二步做 OIT

第十一步原计划中 OIT runtime 被延后，是因为当时后处理资源架构还未稳定。现在后处理基础路径已经可执行，下一条最需要闭环的旧能力是透明排序：

- OIT 已有材质侧输出契约，但没有 runtime consumer。
- 旧 `WeightedOITRendererFeature` 已证明数据流可行：opaque copy、accumulation/revealage MRT、composite、active 状态握手。
- 新系统需要验证 material pass、RenderGraph resource、Feature Descriptor 和 debug 能否完整串起来。
- 透明链路会影响后续 CharacterSpecialization、hair / glass / cloth 等材质验证，不能长期只停留在“材质 ABI 声明”。

第十二步的成功标准不是“透明效果完美”，而是 **OIT 数据流以新资源名、新 pass tag、新 RenderGraph 声明跑通，并且不继承旧 ABI**。

---

## 2. 范围

### 做

- 新增 OIT runtime 契约：
  - Feature：`WeightedOit` 或 `TransparentOit`，最终命名需在实现审查中确定。
  - Resources：`Oit.OpaqueColor`、`Oit.Accumulation`、`Oit.Revealage`、`Oit.CompositeSource`。
  - Semantics：消费 `OIT.AccumulationInput` / `OIT.RevealageInput`，产出 camera color composite。
  - Capabilities：消费 `SupportsOit` / `ParticipatesOit`。
- 新增 RenderGraph-first `WeightedOitRendererFeature` 最小实现。
- 新增 `Runtime/OIT` 模块：
  - settings
  - resource declarations
  - RenderGraph resources / pass data
  - shader constants
- 新增 composite shader：
  - `Hidden/HoURP/OIT/WeightedComposite`
  - 只使用 `_HoUrpOit*` 新名字。
- 明确执行链路：
  - reset OIT active state
  - copy opaque color
  - clear accumulation / revealage
  - draw `LightMode = "HoUrpOitAccumulation"`
  - composite accumulation result back to camera color
  - reset active state
- 补齐自动测试：
  - registry contract
  - shader property ABI
  - resource descriptor
  - generated material pass tag
  - old OIT ABI 禁止项
- 手动 Unity 验收：
  - 用 `HoUrpDebugLitMinimal` 或最小 OIT-ready 透明材质观察透明叠加。

### 不做

- 不复制旧 `WeightedOITRendererFeature` 的 compatibility path。
- 不继承 `_lilOIT*`、`lilToonOIT`、`Hidden/lilToon/URP/WeightedOITComposite`。
- 不做完整透明材质系统。
- 不做排序 UI。
- 不做 per-material runtime toggle UI。
- 不做复杂透明折射、玻璃、多层水体。
- 不做 CharacterSpecialization / hair drop shadow / eye reveal。
- 不做 HoShadowCast。
- 不做 Planar Reflection。
- 不把 OIT 资源发布为 ImagePost / ScreenPost 可随意读取的公共后处理输入。

---

## 3. 新命名约束

旧实现事实：

```text
lilToonOIT
_lilOITAccumulationTexture
_lilOITRevealageTexture
_lilOITOpaqueTexture
_lilOITCompositeSourceTexture
_lilOITActive
_lilOITWeight
_lilOITAlphaClipThreshold
Hidden/lilToon/URP/WeightedOITComposite
```

新实现建议：

```text
HoUrpOitAccumulation
Oit.OpaqueColor
Oit.Accumulation
Oit.Revealage
Oit.CompositeSource
_HoUrpOitOpaqueColorTexture
_HoUrpOitAccumulationTexture
_HoUrpOitRevealageTexture
_HoUrpOitCompositeSourceTexture
_HoUrpOitActive
_HoUrpOitWeight
_HoUrpOitAlphaClipThreshold
Hidden/HoURP/OIT/WeightedComposite
```

原则：

- 旧名字只能出现在测试的 `Does.Not.Contain` 或 Legacy 文档中。
- `HoUrpOitAccumulation` 是新材质 pass tag，不兼容旧 `lilToonOIT`。
- `_HoUrpOitActive` 只能作为必要的 shader phase gate，不作为资源生命周期管理手段。
- 所有 OIT texture 生命周期必须由 RenderGraph resource / `HoUrpRenderGraphResources` 等显式层管理。

---

## 4. 核心数据流

### 4.1 Frame Flow

```text
Before transparent accumulation:
  OIT Reset
  OIT Opaque Copy
  OIT Clear

Transparent OIT accumulation:
  Draw renderers with LightMode = HoUrpOitAccumulation
  Write Oit.Accumulation
  Write Oit.Revealage

After transparents:
  Copy camera color to Oit.CompositeSource if needed
  Composite Oit.CompositeSource + Oit.Accumulation + Oit.Revealage -> camera color
  OIT Reset
```

### 4.2 Resource Rules

| Resource | Kind | Format | Clear | Lifetime |
| --- | --- | --- | --- | --- |
| `Oit.OpaqueColor` | Texture2D | camera color | copy source | per camera frame |
| `Oit.Accumulation` | Texture2D | `R16G16B16A16_SFloat` first version | clear zero | per camera frame |
| `Oit.Revealage` | Texture2D | `R8_UNorm` or `R16_SFloat` after implementation check | clear one | per camera frame |
| `Oit.CompositeSource` | Texture2D | camera color | copy source | per camera frame |

第一版可先全分辨率。render scale 只登记为 planned，不进入最小实现。

### 4.3 Pass Rules

- `OpaqueCopy` 必须从 camera color copy 到独立 texture，不能把正在写的 camera color 直接发布为全局输入。
- `Clear` 必须显式写 `Oit.Accumulation` / `Oit.Revealage`。
- `Accumulation` 必须使用 RenderGraph raster pass 声明 MRT write。
- `Composite` 必须显式读取 accumulation / revealage / composite source，并写回 camera color。
- 禁止同一 pass 对同一 TextureHandle 既读又写。
- 如果没有 accumulation/revealage，composite pass 必须跳过并 reset active。

---

## 5. Contract 规划

### 5.1 Built-in Names

建议新增：

```text
Features.TransparentOit

Resources.OitOpaqueColor
Resources.OitAccumulation
Resources.OitRevealage
Resources.OitCompositeSource

DebugViews.OitAccumulation
DebugViews.OitRevealage
DebugViews.OitCompositeWeight
```

命名审查点：

- Feature 名用 `TransparentOit` 更符合 Domain；RendererFeature 可叫 `WeightedOitRendererFeature`，表示算法。
- Resource 名统一 `Oit.*`，不要把 `Weighted` 放进资源名，避免后续替换透明算法时 ABI 被算法名绑死。

### 5.2 Feature Descriptor

```text
TransparentOit
  Domain: Composite
  Stage: TransparentOit
  ProducedResources:
    Oit.OpaqueColor
    Oit.Accumulation
    Oit.Revealage
    Oit.CompositeSource
  ConsumedSemantics:
    Transparent.Color
    Transparent.Alpha
    Transparent.Coverage
    OIT.AccumulationInput
    OIT.RevealageInput
  RequiredCapabilities:
    SupportsOit
    ParticipatesOit
  DebugViews:
    OIT.Accumulation
    OIT.Revealage
```

注意：`GeneratedMaterial` 仍只声明材质侧能生产 OIT-ready semantics，不拥有 runtime resources。`TransparentOit` 才拥有 OIT runtime resources。

---

## 6. Shader / Material ABI

当前 `HoUrpDebugLitMinimal.shader` 已有：

```text
Name "HoUrpOitAccumulation"
Tags { "LightMode" = "HoUrpOitAccumulation" }
HoUrpTransparentOutputData
HoUrpOitAccumulationData
```

第十二步需要补齐 runtime consumer，不改回旧 pass tag。

OIT composite shader 第一版只做：

```text
sourceColor = Oit.CompositeSource
accum = Oit.Accumulation
revealage = Oit.Revealage
resolved = accum.rgb / max(accum.a, epsilon)
out = lerp(resolved, sourceColor, revealage)
```

最终混合公式在实现审查中以旧 shader 行为为参考，但不能继承旧属性名。

---

## 7. 实施步骤

### Step 1. OIT 旧实现审查

读取：

```text
D:\Unity_Fork\lilToon-URP-Extensions\Runtime\OIT\WeightedOITRendererFeature.cs
D:\Unity_Fork\lilToon-URP-Extensions\Runtime\OIT\WeightedOITSettings.cs
D:\Unity_Fork\lilToon-URP-Extensions\Runtime\OIT\WeightedOIT.hlsl
D:\Unity_Fork\lilToon-URP-Extensions\Runtime\OIT\WeightedOITComposite.shader
```

输出审查文档：

```text
Documentation~/rp重构第十二步/rpWeightedOit旧实现数据流审查.md
```

必须记录：

- 旧 pass 顺序。
- 旧资源格式 / clear 值。
- 旧 composite 公式。
- 旧 `_lilOITActive` 的真实用途。
- 哪些内容只作为行为参考，不能成为新 ABI。

### Step 2. Contract / Registry

新增或更新：

```text
Runtime/Core/HoUrpBuiltInNames.cs
Runtime/Core/HoUrpBuiltInContracts.cs
Runtime/Core/HoUrpShaderPropertyIds.cs
Runtime/Resources/ResourceClearPolicy.cs
Runtime/RenderGraph/HoUrpRenderGraphTextureDescFactory.cs
```

测试：

```text
Tests/Runtime/HoUrpContractRegistryTests.cs
Tests/Runtime/HoUrpRenderGraphResourceDeclarationTests.cs
Tests/Runtime/HoUrpMaterialShaderAbiTests.cs
```

验收：

- `GeneratedMaterial` 仍不拥有 OIT resources。
- `TransparentOit` 拥有 OIT resources。
- 旧 `_lilOIT*` / `lilToonOIT` 不出现在新 shader / constants。

### Step 3. OIT Runtime Skeleton

新增：

```text
Runtime/OIT/WeightedOitRendererFeature.cs
Runtime/OIT/WeightedOitSettings.cs
Runtime/OIT/WeightedOitResources.cs
Runtime/OIT/WeightedOitShaderConstants.cs
Runtime/OIT.meta
```

第一版 settings：

```text
enabled
layerMask
renderQueueMin
renderQueueMax
weight
alphaClipThreshold
```

暂不做：

- render scale
- per-camera persistent RT
- compatibility path
- custom material override

### Step 4. RenderGraph Passes

实现最小 pass：

```text
ResetPass
OpaqueCopyPass
ClearPass
AccumulationPass
CompositePass
FinalResetPass
```

要求：

- 每个 pass 都有清楚的 profiling name。
- 每个 texture 都通过 builder 显式 read/write。
- `DrawRenderers` 使用 `ShaderTagId("HoUrpOitAccumulation")`。
- 不直接使用旧 `WeightedOITRenderTargets` / RTHandle 持久缓存模式。
- 不用 compatibility `Execute()` 作为长期路径。

### Step 5. Composite Shader

新增：

```text
Runtime/Shaders/Hidden/HoURP/OIT/WeightedComposite.shader
```

要求：

- Shader 名：`Hidden/HoURP/OIT/WeightedComposite`。
- Texture 属性使用 `_HoUrpOit*`。
- 不 include 旧 OIT hlsl。
- 不写 `_CameraOpaqueTexture`，除非明确作为 URP 兼容输入审查通过；第一版优先只用 `Oit.OpaqueColor`。

### Step 6. Debug

第一版 debug 可只做 resource debug view：

- `OIT.Accumulation`
- `OIT.Revealage`
- 可选 `OIT.CompositeWeight`

如果 AOV Debug 不适合承载 OIT，可先建立文档和 contract，runtime debug tile 延后；但 registry 里必须能查询 producer / consumer。

### Step 7. Manual Unity 验收

场景要求：

- opaque 背景。
- 至少两个半透明 OIT-ready quad / mesh，深度交错。
- 使用 `HoUrpDebugLitMinimal` 或最小 generated material。
- Renderer Features 顺序包含：
  - AOV/SSS/Post 可选。
  - `HoURP Weighted OIT`。
  - `HoURP ScreenPost Prototype` / `HoURP ImagePost Prototype` 用于回归观察。

检查：

- OIT 关闭：透明按普通 URP 路径或不参与 OIT。
- OIT 开启：`HoUrpOitAccumulation` pass 被绘制。
- accumulation/revealage 非空。
- composite 后 camera color 改变。
- `_HoUrpOitActive` 在 accumulation 后 reset。
- Post stack 仍能在 OIT composite 后正常工作。

---

## 8. 测试清单

### Contract Tests

- `TransparentOit` feature descriptor 存在。
- `TransparentOit` stage 是 `HoUrpPassStage.TransparentOit`。
- `TransparentOit` produced resources 包含 `Oit.OpaqueColor` / `Oit.Accumulation` / `Oit.Revealage` / `Oit.CompositeSource`。
- `GeneratedMaterial` 仍不 produced OIT resources。
- `OIT.AccumulationInput` producer 仍是 generated material semantics。

### Resource Tests

- `Oit.Accumulation` 使用 HDR accumulation 格式。
- `Oit.Revealage` clear 为 white / one。
- `Oit.OpaqueColor` / `Oit.CompositeSource` 使用 camera color format。

### Shader ABI Tests

- generated shader contains `LightMode = "HoUrpOitAccumulation"`。
- generated shader does not contain `lilToonOIT`。
- runtime composite shader contains `_HoUrpOitAccumulationTexture` / `_HoUrpOitRevealageTexture`。
- runtime composite shader does not contain `_lilOIT`。

### Runtime Structure Tests

如果无法在 editmode 直接跑 RenderGraph，则至少补结构测试：

- settings 默认值合理。
- resource name / property id 稳定。
- disabled feature 不 enqueue passes。
- layer mask / queue range 被传给 drawing settings。

---

## 9. 风险点

- 直接复制旧 compatibility path，导致新包长期维护双路径。
- 继续依赖 `_CameraOpaqueTexture`，绕过 `Oit.OpaqueColor` 显式资源。
- 把 `_HoUrpOitActive` 当成主调度机制，而不是作为材质 phase gate。
- generated material 的普通 transparent forward 与 OIT accumulation 重复绘制。
- revealage 格式 / clear 值错误，导致 composite 全黑或全透明。
- OIT composite 与 ScreenPost / ImagePost 顺序冲突。
- 用旧 `_lilOIT*` 属性名快速跑通，污染新 ABI。
- 过早支持 render scale / custom weight policy / transparent sorting options，掩盖最小数据流问题。

---

## 10. 成功标准

- 新包存在 `HoURP Weighted OIT` 或同等命名的 runtime feature。
- `HoUrpOitAccumulation` 材质 pass 能被 runtime draw。
- `Oit.Accumulation` / `Oit.Revealage` 由 RenderGraph 显式创建、清理、写入、读取。
- composite shader 用新 `_HoUrpOit*` ABI。
- `GeneratedMaterial` 与 `TransparentOit` 的 producer / consumer 边界清楚。
- 禁用 feature 后不残留 active state 或 stale texture。
- 与第十一步 ScreenPost / ImagePost 顺序可解释，不发生 camera color 读写冲突。
- 自动测试覆盖 contract / resource / shader ABI。
- 手动 Unity 场景中能观察到两个交错透明对象的 OIT composite 结果。

---

## 11. 第十二步之后

第十二步完成后再决定后续分支：

- 如果 OIT runtime 稳定：进入 `HoShadowCast` 契约审查或 CharacterSpecialization 输入资源审查。
- 如果透明材质 pass 暴露 ABI 问题：回到 HoNpr / generated material side 修正 OIT output。
- 如果 camera color 顺序与 Post stack 冲突：补一轮 FrameGraph / pass ordering 文档，不急着扩展视觉效果。

