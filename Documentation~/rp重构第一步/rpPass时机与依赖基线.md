# RP Pass 时机与依赖基线

> 本文先记录旧事实顺序，再冻结新 RP 第一版候选顺序。旧顺序用于迁移验证，不是最终锁死的架构顺序。

---

## 0. 旧事实基线

```text
Per-camera reset
  - _lilHoAovActive = 0
  - _lilOITActive = 0
  - _HoShadowCastActive / light counts reset

Object semantic binding
  - HoAovSubject writes MaterialPropertyBlock
  - HoAovGroup writes renderer user value or MPB fallback

HoShadowCast
  - RenderPassEvent.BeforeRenderingPrePasses
  - ShadowCaster pass -> _HoShadowCastAtlas / _HoShadowCastSecondDirectionalAtlas
  - publish light/slice/matrix/PCSS globals

Opaque / Forward / GBuffer
  - old lilToon / lilPBR consume HoShadowCastAttenuation(positionWS)

HoAOV
  - RenderPassEvent.AfterRenderingOpaques
  - LightMode = HoAOV
  - fallback material when native pass is missing
  - outputs mask/id, normal/depth, tangent normal, surface data, custom, object custom
  - LightMode = HoAOVSSS -> _lilHoAovSssTexture

HoSSS
  - source default AfterRenderingSkybox
  - composite constrained before transparents
  - consumes AOV mask/normal-depth/surfaceData/SSS source

Transparent / OIT
  - opaque copy before accumulation
  - accumulation default BeforeRenderingTransparents
  - composite default AfterRenderingTransparents

HoCharacterSpecialization
  - default AfterRenderingTransparents
  - LightMode = HoCharacterCapture
  - eye reveal / hair drop shadow composite

HoPost
  - AfterRenderingPostProcessing
  - semantic-aware layer stack

Shoost
  - AfterRenderingPostProcessing + 1
  - final image style stack

Debug
  - old debug passes are feature-local

Final Output
```

---

## 1. 新 RP 第一版候选顺序

```text
Frame / Camera Init
Object Semantic Binding
Shadow / Lighting Prepass
Geometry / Depth / Normal Semantic
Opaque Shading
Material / Shading Semantic AOV
Screen SSS
Transparent / OIT
Character Composite
Semantic Post
Image Post
Debug Composite
Final Output
```

Key decisions:

- AOV 拆成 `GeometrySemanticAov` 和 `MaterialShadingSemanticAov`。
- HoSSS 第一版仍以 transparent 前 composite 为目标，除非实测要求调整。
- CharacterSpecialization 第一版保留旧 after-transparents baseline，但记录捕获/合成可能拆分。
- OIT opaque copy 和 accumulation 的相对时机第一版沿用旧实现。
- ShadowCast 第一版仍由材质 forward 接收，屏幕空间 shadow trace 不纳入第一阶段。

---

## 2. Pass 依赖表

| Pass | Before | After | Produces | Consumes | Can Move Earlier | Can Move Later | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `FrameCameraInit` | all feature passes | frame begin | feature active state, imported resource bindings, reset counts | camera/frame context | No | No | replaces scattered `_Active = 0` style reset |
| `ObjectSemanticBinding` | AOV semantic passes | `FrameCameraInit` | `Object.*` semantic authoring data | object components, material/renderer data | No | limited | must run before renderers produce AOV |
| `ShadowLightingPrepass` | `OpaqueShading` | `FrameCameraInit` / object binding | `ShadowCast.*` resources and globals | lights, shadow caster materials | limited | Yes, but before material receivers | old default `BeforeRenderingPrePasses` |
| `GeometrySemanticAov` | `OpaqueShading` or `MaterialShadingSemanticAov` depending implementation | `ObjectSemanticBinding` | geometry/object AOV resources | object semantic binding, geometry pass | Yes | Yes | may become earlier pre-semantic pass |
| `OpaqueShading` | `MaterialShadingSemanticAov`, `ScreenSss` | `ShadowLightingPrepass` | camera color/depth/gbuffer | material forward, shadow receiver | No | No | URP anchor, not custom feature |
| `MaterialShadingSemanticAov` | `ScreenSss`, `SemanticPost`, `ImagePost` | `OpaqueShading` if it depends on shaded/material pass | material/shading AOV resources | material semantic producer | limited | Yes, before consumers | old HoAOV after opaques baseline |
| `ScreenSss` | `TransparentOit` | `MaterialShadingSemanticAov` | SSS composite color, SSS temps | AOV, camera color | No for first version | limited | old settings clamp composite before transparents |
| `TransparentOit` | `CharacterComposite`, `SemanticPost` | opaque/skybox baseline | OIT accumulation/revealage/composite | transparent material pass, opaque color | No for first version | limited | opaque copy before accumulation |
| `CharacterComposite` | `SemanticPost` | `TransparentOit` first version | character capture/composite outputs | AOV object custom, camera color | possible capture earlier | composite later possible | semi-transparent hair/eyelash risk |
| `SemanticPost` | `ImagePost` | all semantic producers | semantic post layer output | AOV resources, camera color/depth | No | Yes before final style | replaces HoPost |
| `ImagePost` | `DebugComposite` / `FinalOutput` | `SemanticPost` | final style image | camera color, optional AOV | No | No | replaces Shoost final stack |
| `DebugComposite` | `FinalOutput` unless replace mode earlier | registered resources available | debug overlay/capture/HUD | DebugRegistry entries | mode-dependent | mode-dependent | unified DebugDomain |

---

## 3. Resource Dependency Baseline

| Consumer | Requires | Hard dependency? | Notes |
| --- | --- | --- | --- |
| `SubsurfaceScattering` | `Aov.MaskId`, `Aov.NormalDepth`, `Aov.SurfaceData`, `Aov.SssSource`, `Camera.Color` | Yes | feature should skip or show empty debug if missing AOV |
| `WeightedOit` | `Camera.Color`, transparent OIT material producer | Yes | `Oit.OpaqueColor` must exist before accumulation |
| `ShadowCast` | shadow caster materials, lights | Yes for feature output | material receiver still consumes global bindings first version |
| `CharacterSpecialization` | `Aov.MaskId`, `Aov.NormalDepth`, `Aov.ObjectCustom0_7`, `Camera.Color` | Yes | object custom bits define face/hair/eye areas |
| `SemanticPostProcess` | `Camera.Color`; AOV resources when rules enabled | Conditional | layer should declare which AOV semantics it uses |
| `ImagePostProcess` | `Camera.Color`; AOV resources when `SupportsAovComposite` and enabled | Conditional | AOV composite must not redefine semantics |
| `DebugComposite` | selected DebugView source | Conditional | missing source must be visible as debug status |

---

## 4. Movement Decisions

| Old Feature | Can Move Earlier | Can Move Later | First Version Decision | Risk |
| --- | --- | --- | --- | --- |
| HoAOV | partly; geometry/object semantics can move earlier | material/shading AOV must stay before consumers | split into `GeometrySemanticAov` and `MaterialShadingSemanticAov` | material producer availability |
| HoSSS | source may move within opaque-to-transparent range | composite after transparents would change visual layering | keep before transparents first | transparent contamination |
| OIT | opaque copy tied to accumulation | composite tied after transparents | keep old relative timing | wrong background for transparent |
| HoCharacter | capture may need split for semi-transparent parts | composite can stay after transparent | keep old baseline, document split | eyelashes/front hair ordering |
| HoPost | no, needs AOV and camera color | yes before final image stack | `SemanticPost` before `ImagePost` | semantic masks unavailable |
| Shoost | no, final style stack | no, should stay near end | `ImagePost` after `SemanticPost` | AOV composite overreach |
| ShadowCast | must precede forward receiver | can move only if receiver path changes | keep material receiver path | screen-space shadow path out of scope |

---

## 5. OIT Scheduling Note

Weighted OIT runtime is not implemented in stages 2-10, but stage 10 prepares the material side so a later transparent stage can test OIT directly. The original baseline placed this at stage 11; the current stage 11 priority has moved to HoPost / Shoost PostGraph and ImageChain planning.

| Stage | Scope | Notes |
| --- | --- | --- |
| Stage 10 | OIT-ready material ABI | Define `SupportsOit` / `ParticipatesOit`, `HoUrpOitAccumulation`, transparent alpha / coverage / weight output, and phase policy. |
| Later transparent stage | Weighted OIT runtime minimum | Create `Oit.*` resources, clear accumulation/revealage, draw `HoUrpOitAccumulation`, composite, and expose debug views. |
| Later transparent stage | Weighted OIT completion | Add quality policy, alpha clip / weight policy, transparent semantic expansion, character ordering, and old behavior parity checks. |

This split prevents old `lilToonOIT`, `_lilOITEnabled`, and `_lilOITActive` from becoming new long-term ABI.

## 5. First Implementation Gate

Before code migration, the implementation plan must answer:

- Which pass writes each registered resource?
- Which pass reads each registered resource?
- Which pass exports shader globals only as binding, not ownership?
- Which pass owns debug views?
- Which pass can be disabled without leaving stale resource state?
- Which old visual behavior is being validated by screenshot/frame comparison?
