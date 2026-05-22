# RP Capability 模型草案

> Capability 表示“允许参与什么”；Policy 表示“如何参与”。第一阶段用 Capability 替代旧 Layer/Tag/材质开关散落规则。

---

## 0. Capability 与 Policy

| 类型 | 含义 | 例子 |
| --- | --- | --- |
| Capability | 允许参与的能力 | `WritesAov`, `SupportsSss`, `ParticipatesOit` |
| Policy | 参与方式和参数 | `SSSQuality=High`, `ShadowSoftness=Medium`, `OitWeight=1.0` |

Rules:

- Object Capability 由对象/Renderer authoring UI 接收。
- Material Capability 由新材质 preset 或生成系统声明，不由临时 inspector 决定。
- Light Capability 第一版先占位，后续细化。
- Feature Capability 描述 Feature 自己的依赖和可选能力。

---

## 1. Capability 表

| Capability | Domain | Owner | Data Source | Affects | Default | UI Surface | Legacy Reference | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `WritesAov` | ObjectDomain | Renderer/Object | Object capability component | AovOutput participation | enabled when component present | Object Capability UI | `HoAovSubject` | replaces scattered MPB write intent |
| `WritesObjectCustom` | ObjectDomain | Renderer/Object | Object capability component | `Object.Custom0-7` | disabled | Object Capability UI | `HoAovGroup.objectCustom0-7` | first version keeps custom bit names |
| `ReceivesSemanticPost` | ObjectDomain | Renderer/Object | Object capability component | SemanticPostProcess masks | enabled if AOV mask exists | Object Capability UI | HoPost AOV rules | can be controlled by object semantic |
| `ReceivesCharacterComposite` | ObjectDomain | Renderer/Object | Object capability component | CharacterSpecialization | disabled except character objects | Object Capability UI | `HoAovGroup` character bits | maps face/hair/eye regions |
| `CastsHoShadow` | ObjectDomain | Renderer/Object | Renderer/shadow setting | ShadowCast caster draw | material/renderer shadow default | Object Capability UI | `ShadowCaster` pass | object-level participation |
| `ReceivesHoShadow` | ObjectDomain | Renderer/Object | material + object capability | material forward receiver | material default | Object Capability UI | `_HoShadowStrength` | receiver policy can be material-owned |
| `ParticipatesOit` | ObjectDomain | Renderer/Object | object flag + material support | WeightedOit transparent pass | material default | Object Capability UI | `_lilOITEnabled` | object can opt out even if material supports |
| `WritesMaterialSemantic` | MaterialDomain | Material preset/generator | new material contract | AovOutput material channels | preset-dependent | Material preset metadata | `_HoAovMaterialClass`, `_HoAovCustom*` | not decided by runtime inspector |
| `WritesSssSource` | MaterialDomain | Material preset/generator | shader pass capability | `Aov.SssSource` | false unless material supports SSS | Material preset metadata | `HoAOVSSS` pass | producer contract |
| `SupportsSss` | MaterialDomain | Material preset/generator | material preset | SubsurfaceScattering | preset-dependent | Material preset metadata | `_HoSSSProfileId`, `_HoSSSThicknessScale` | material capability |
| `SupportsOit` | MaterialDomain | Material preset/generator | material preset | WeightedOit | false by default | Material preset metadata | `_lilOITEnabled`, `lilToonOIT` | material capability |
| `SupportsPlanarReflection` | MaterialDomain | Material preset/generator | material preset | PlanarReflection receiver | false by default | Material preset metadata | `_UsePlanarReflection` | material capability |
| `SupportsCharacterCapture` | MaterialDomain | Material preset/generator | shader pass capability | CharacterSpecialization | false by default | Material preset metadata | `HoCharacterCapture`, `_HoCharacterCaptureOpacity` | material capability |
| `CastsHoShadow` | LightDomain | Light | light capability placeholder | ShadowCast light collection | default from light shadow setting | Light Capability UI | `HoShadowCastController` | first version placeholder |
| `UsesSecondDirectionalAtlas` | LightDomain | Light/ShadowCast | light policy | ShadowCast second atlas | disabled | Light Capability UI | second directional atlas | placeholder |
| `SupportsPcss` | LightDomain | Light/ShadowCast | light/shadow policy | PCSS sampling | feature default | Light Capability UI | `_HoShadowCastPcssParams*` | placeholder |
| `RequiresAov` | CapabilityDomain | Feature | FeatureDescriptor | scheduler/resource validation | feature-specific | Feature Inspector | HoSSS/HoPost/Character consuming AOV | Feature Capability |
| `RequiresDepth` | CapabilityDomain | Feature | FeatureDescriptor | scheduler/resource validation | feature-specific | Feature Inspector | depth consumers | Feature Capability |
| `RequiresNormal` | CapabilityDomain | Feature | FeatureDescriptor | scheduler/resource validation | feature-specific | Feature Inspector | normal-depth consumers | Feature Capability |
| `RequiresMotion` | CapabilityDomain | Feature | FeatureDescriptor | scheduler/resource validation | false first version | Feature Inspector | motion vectors | Deferred |
| `SupportsHalfResolution` | CapabilityDomain | Feature | FeatureDescriptor | resource scale | feature-specific | Feature Inspector | AOV/OIT render scale | resource policy |
| `SupportsDebugView` | DebugDomain | Feature | FeatureDescriptor | DebugRegistry | required | Feature Inspector / Debug Panel | old per-feature debug modes | must be true for first batch |
| `SupportsDebugCapture` | DebugDomain | DebugComposite | Debug system config | capture output | enabled in debug builds | Debug Panel | new | first-stage contract |

---

## 2. Object Capability UI Draft

Object UI should expose object-owned semantics and capabilities only:

| Section | Fields |
| --- | --- |
| Object Semantic | `Object.Id`, `Object.GroupId`, `Object.CharacterId`, `Object.PartId`, `Object.Flags` |
| Object Custom | `Object.Custom0-7`, `Object.CustomMask` |
| Participation | `WritesAov`, `ReceivesSemanticPost`, `ReceivesCharacterComposite`, `CastsHoShadow`, `ReceivesHoShadow`, `ParticipatesOit` |
| Debug | `ShowSemantic`, `ShowMask`, `ShowCapabilityStatus` |

Material properties must not be edited here except through explicit object overrides that are documented as overrides.

---

## 3. Material Capability Draft

Material capability comes from new material preset/generator metadata:

| Material Capability | Required Producer Output |
| --- | --- |
| `WritesMaterialSemantic` | `Material.Class`, optional `Material.Custom0-3`, optional `Material.Thickness/Curvature/Utility` |
| `WritesSssSource` | `Shading.SssSourceColor`, `Material.SssProfile`, SSS policy params |
| `SupportsOit` | transparent accumulation pass and blend/clip policy |
| `SupportsPlanarReflection` | receiver sampling path for `Reflection.PlanarColor/Matrix/Params` |
| `SupportsCharacterCapture` | capture output pass and opacity policy |
| `ReceivesHoShadow` | receiver sampling path for `ShadowCast.Atlas` |

Rule:

- The new material inspector must not dynamically decide shader structure.
- Preset/generator output declares capabilities; inspector only edits supported policy values.

---

## 4. Feature Capability Draft

| Feature | Required Capabilities | Optional Capabilities | Notes |
| --- | --- | --- | --- |
| `AovOutput` | `SupportsDebugView` | `SupportsHalfResolution` | requires material/object producers |
| `SubsurfaceScattering` | `RequiresAov`, `RequiresNormal`, `SupportsDebugView` | `SupportsHalfResolution` | profile policy first version mirrors old 8 slots |
| `WeightedOit` | `SupportsDebugView` | `SupportsHalfResolution` | requires material `SupportsOit` |
| `ShadowCast` | `SupportsDebugView` | `SupportsPcss` | light capabilities placeholder |
| `CharacterSpecialization` | `RequiresAov`, `SupportsDebugView` | n/a | consumes object custom bits |
| `SemanticPostProcess` | `RequiresAov`, `SupportsDebugView` | n/a | per-layer semantic queries |
| `ImagePostProcess` | `SupportsDebugView` | optional AOV composite | final image stack |
| `PlanarReflection` | `SupportsDebugView` | n/a | per-surface external resource |
| `DebugComposite` | `SupportsDebugCapture` | overlay/HUD modes | central debug owner |

---

## 5. First Version Decisions

- `HoAovSubject` / `HoAovGroup` capability intent becomes Object Capability UI.
- Material Capability is declared by new material preset/generator, not by runtime inspector decisions.
- OIT, SSS and PlanarReflection are Material Capability when discussing material participation.
- Light Capability is included as placeholder in first version; detailed implementation can wait.
- Capability names must not depend on old `lilToon/lilPBR` naming.
