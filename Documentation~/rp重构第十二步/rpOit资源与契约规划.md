# OIT 资源与契约规划

## 目标

第十二阶段要把材质侧已经声明的 OIT-ready 输出接到 runtime resource owner 上。

当前已有：

```text
GeneratedMaterial
  produces:
    Transparent.Color
    Transparent.Alpha
    Transparent.Coverage
    OIT.AccumulationInput
    OIT.RevealageInput
  capabilities:
    SupportsOit
    ParticipatesOit
  pass:
    HoUrpOitAccumulation
```

第十二阶段新增：

```text
TransparentOit
  owns:
    Oit.OpaqueColor
    Oit.Accumulation
    Oit.Revealage
    Oit.CompositeSource
  consumes:
    OIT.AccumulationInput
    OIT.RevealageInput
```

## Built-in Names

建议新增到 `HoUrpBuiltInNames`：

```text
Features.TransparentOit = "TransparentOit"

Resources.OitOpaqueColor = "Oit.OpaqueColor"
Resources.OitAccumulation = "Oit.Accumulation"
Resources.OitRevealage = "Oit.Revealage"
Resources.OitCompositeSource = "Oit.CompositeSource"

DebugViews.OitAccumulation = "OIT.Accumulation"
DebugViews.OitRevealage = "OIT.Revealage"
DebugViews.OitCompositeWeight = "OIT.CompositeWeight"
```

注意：

- Resource 使用 `Oit.*`。
- DebugView 使用 `OIT.*`，和现有 AOV / SSS debug 命名风格保持一致。
- 不建议资源名带 `Weighted`，算法不是资源身份。

## Feature Descriptor

新增 `TransparentOit` descriptor：

```text
Id:
  TransparentOit

Domain:
  Composite

Stage:
  TransparentOit

ProducedResources:
  Oit.OpaqueColor
  Oit.Accumulation
  Oit.Revealage
  Oit.CompositeSource

ConsumedResources:
  camera color / depth are URP frame resources

ProducedSemantics:
  none in first version

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

## Resource Definitions

### `Oit.OpaqueColor`

```text
Kind: Texture2D
Format: CameraColor
Scale: Full
Lifetime: PerCamera
ClearPolicy: CopySource
Producer: TransparentOit
Consumer: TransparentOit
DebugView: None first version
```

用途：accumulation 前的 camera color snapshot。

### `Oit.Accumulation`

```text
Kind: Texture2D
Format: HighPrecisionRgba16Float
Scale: Full
Lifetime: PerCamera
ClearPolicy: ClearZero
Producer: TransparentOit
Consumer: TransparentOit / DebugComposite
DebugView: OIT.Accumulation
```

用途：weighted color / alpha accumulation。

### `Oit.Revealage`

```text
Kind: Texture2D
Format: SingleChannelFloat or MaskR8 after implementation check
Scale: Full
Lifetime: PerCamera
ClearPolicy: ClearOne
Producer: TransparentOit
Consumer: TransparentOit / DebugComposite
DebugView: OIT.Revealage
```

需要新增 clear policy：

```text
ClearOne
```

如果当前 `ResourceClearPolicy` 不适合单通道 white clear，可新增 `ClearWhite` 或 `ClearOne`，实现时统一命名。

### `Oit.CompositeSource`

```text
Kind: Texture2D
Format: CameraColor
Scale: Full
Lifetime: PerCamera
ClearPolicy: CopySource
Producer: TransparentOit
Consumer: TransparentOit
DebugView: None first version
```

用途：composite 前的 camera color snapshot，避免读写 camera color 冲突。

## Semantics 边界

已有：

```text
OIT.AccumulationInput
OIT.RevealageInput
```

这两个是材质输出语义，不是 runtime texture resource。

不要新增：

```text
Composite.OitAccumulation
Composite.OitRevealage
```

除非后续确实需要其它 Feature 消费 OIT 结果。第一版不需要。

## Capabilities

已有：

```text
SupportsOit
ParticipatesOit
```

第十二步要让它们进入 runtime 判定：

- `SupportsOit`：preset / material 具备 OIT accumulation pass。
- `ParticipatesOit`：当前实例参与 OIT draw list。

实现注意：

- 第一版可以先通过 layer mask / queue range / pass tag draw。
- material instance capability 的 runtime filtering 后续可加。
- 不应依赖旧 `_lilOITEnabled`。

## Shader Property IDs

建议新增：

```text
public const string WeightedOitCompositeShaderName = "Hidden/HoURP/OIT/WeightedComposite";

OitOpaqueColorTexture = _HoUrpOitOpaqueColorTexture
OitAccumulationTexture = _HoUrpOitAccumulationTexture
OitRevealageTexture = _HoUrpOitRevealageTexture
OitCompositeSourceTexture = _HoUrpOitCompositeSourceTexture
OitActive = _HoUrpOitActive
OitWeight = _HoUrpOitWeight
OitAlphaClipThreshold = _HoUrpOitAlphaClipThreshold
```

测试必须确认旧名字不存在。

## Registry 测试

新增测试建议：

```text
MinimalAovRegistryLinksTransparentOitRuntimeResources
GeneratedMaterialDoesNotOwnOitRuntimeResources
TransparentOitConsumesGeneratedOitSemantics
OitResourceDescriptorsUseExpectedFormatAndClear
OitShaderBindingsUseHoUrpNames
OitShaderBindingsDoNotUseLegacyLilNames
```

核心断言：

```text
registry.Features.Get(TransparentOit).ProducedResources contains Oit.Accumulation
registry.Resources.Get(Oit.Accumulation).ProducerFeature == TransparentOit
registry.Semantics.Get(OIT.AccumulationInput).Producer == GeneratedMaterial
registry.Semantics.Get(OIT.AccumulationInput).Consumers contains TransparentOit
registry.Resources.TryGet("Oit.Accumulation") == true after stage 12
registry.Resources.TryGet("_lilOITAccumulationTexture") == false
```

## 与第十一步 Post 的关系

`TransparentOit` 不使用 `PostResourceRequest` 第一版。

理由：

- OIT 是 renderers draw + MRT accumulation，不是 post layer。
- 它有自己的 RenderGraph resource owner。
- Post planner 不应该管理透明 draw list。

但第十二步要检查顺序：

```text
TransparentOit composite
  before ScreenPost / ImagePost if those should see transparent result
  after transparent accumulation
```

第一版建议：

```text
OIT composite -> ScreenPost -> ImagePost
```

如项目 renderer feature 排序不同，应在验收文档记录。

