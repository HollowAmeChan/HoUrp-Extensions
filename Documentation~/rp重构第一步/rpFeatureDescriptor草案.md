# RP Feature Descriptor 草案

> 每个 Feature 必须先声明自己，再执行自己。本文是第一批 Feature 的 descriptor 草案。

---

## 0. Descriptor 模板

```text
FeatureDescriptor
{
    Name
    Domain
    Stage
    Producers
    Consumers
    RequiredShaderPasses
    RequiredResources
    ProducedResources
    ProducedSemantics
    ConsumedSemantics
    Capabilities
    DebugViews
    LegacyReference
    MigrationDecision
}
```

---

## 1. Feature 表

| Feature | Domain | Stage | Produces | Consumes | Shader Pass | Resources | Debug | Legacy File | Migration Decision |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `AovOutput` | GeometryDomain / MaterialDomain / ShadingDomain | `GeometrySemanticAov`, `MaterialShadingSemanticAov` | `Aov.*` resources; Object/Material/Geometry/Shading semantics | object authoring, material semantic producer, camera depth | new pass names TBD; old refs `HoAOV`, `HoAOVSSS` | Produces `Aov.MaskId`, `Aov.NormalDepth`, `Aov.TangentNormal`, `Aov.SurfaceData`, `Aov.MaterialCustom0_3`, `Aov.ObjectCustom0_3`, `Aov.ObjectCustom4_7`, `Aov.SssSource`, `Aov.Depth` | AOV mask/id/normals/custom/SSS source | `Runtime/AOV/HoAovRendererFeature.cs` | KeepConceptRename |
| `SubsurfaceScattering` | ShadingDomain / CompositeDomain | `ScreenSss` | `Sss.Source`, `Sss.Diffusion`, `Sss.Transmission`, `Sss.TransmissionTemp` and composite result | `Aov.MaskId`, `Aov.NormalDepth`, `Aov.SurfaceData`, `Aov.SssSource`, `Camera.Color` | full screen material passes, not material LightMode | Consumes AOV, writes SSS temps, composites camera color | SSS source/diffusion/transmission/profile/thickness | `Runtime/SubsurfaceScattering/HoSubsurfaceScatteringRendererFeature.cs` | KeepConceptRename |
| `WeightedOit` | CompositeDomain | `TransparentOit` | `Oit.Accumulation`, `Oit.Revealage`, `Oit.OpaqueColor`, composite result | transparent material producer, camera color/depth | new transparent accumulation pass; old ref `lilToonOIT` | OIT accumulation/revealage/opaque/composite source | OIT accumulation/revealage/active status | `Runtime/OIT/WeightedOITRendererFeature.cs` | KeepConceptRename |
| `ShadowCast` | ShadowDomain | `ShadowLightingPrepass` | `ShadowCast.Atlas`, `ShadowCast.SecondDirectionalAtlas`, light/slice/matrix/PCSS globals | scene lights, shadow caster materials | old ref `ShadowCaster`; new receiver contract TBD | shadow atlas resources and global arrays | atlas, second atlas, light slices, PCSS | `Runtime/ShadowCast/HoShadowCastRendererFeature.cs` | KeepConceptRename |
| `CharacterSpecialization` | CompositeDomain | `CharacterComposite` | `Character.EyeColor`, `Character.EyeData`, `Character.CaptureDepth`, composite result | `Aov.MaskId`, `Aov.NormalDepth`, `Aov.ObjectCustom0_7`, camera color | old ref `HoCharacterCapture`; new capture pass TBD | character capture textures and composite source | eye color/data, hair shadow, eye reveal | `Runtime/CharacterSpecialization/HoCharacterSpecializationRendererFeature.cs` | KeepConceptRename |
| `SemanticPostProcess` | CompositeDomain | `SemanticPost` | semantic post layer outputs and masks | AOV resources, camera color/depth/normals | full screen layer shaders; old subject mask uses forward LightModes | `SemanticPost.LayerTempA/B`, `SemanticPost.SubjectMask` | AOV mask, layer output, subject mask | `Runtime/HoPostProcessing/HoPostProcessRendererFeature.cs` | KeepConceptRename |
| `ImagePostProcess` | ImageDomain | `ImagePost` | final image style layers, optional AOV composite mask | camera color, optional AOV resources | Shoost effect shaders | `ImagePost.LayerTempA/B/C`, optional in-pass masks | layer output, AOV composite mask, effect status | `Runtime/ShoostPostProcessing/*` | KeepConceptRename |
| `PlanarReflection` | ImageDomain / MaterialDomain | before material receiver shading; external per-surface render | `Reflection.PlanarColor`, `Reflection.PlanarMatrix`, `Reflection.PlanarParams` | source camera, surface component, receiver material capability | material forward receiver samples reflection | external render texture and MPB/global bindings | planar color/matrix/status | `Runtime/PlanarReflection/LILPlanarReflectionSurface.cs` | KeepConceptRename |
| `DebugComposite` | DebugDomain | `DebugComposite` | debug overlays, capture outputs, HUD state | registered DebugViews and source resources | debug composite shaders TBD | source debug resources + camera color | all registered views | old per-feature debug passes | Replace with unified system |

---

## 2. Descriptor Details

### AovOutput

Required declarations:

- Produces geometry semantics separately from material/shading semantics.
- Exposes resource handles through Resource Registry.
- Does not export `_lilHoAov*` as logical resource names.
- Supports fallback only as migration validation; new long-term material producer must implement the new contract.

First split:

| Pass | Produces | Notes |
| --- | --- | --- |
| `GeometrySemanticAov` | `Aov.MaskId`, `Aov.NormalDepth`, `Aov.TangentNormal`, `Aov.Depth`, object custom data | Can move earlier than material shading if producer exists |
| `MaterialShadingSemanticAov` | `Aov.SurfaceData`, `Aov.MaterialCustom0_3`, `Aov.SssSource` | Depends on material semantic producer |

### SubsurfaceScattering

Required declarations:

- Consumes AOV; does not own AOV resources.
- Composite stays before transparent in first version unless visual validation proves otherwise.
- Profile count first version may mirror old 8-slot behavior, but profile registry must be documented.

### WeightedOit

Required declarations:

- Opaque copy happens before accumulation and remains aligned with old behavior in first version.
- `Oit.OpaqueColor` is a resource, not a hidden `_CameraOpaqueTexture` side effect.
- `_lilOITActive` style handshake is a migration fact; new material capability is `SupportsOit` / `ParticipatesOit`.
- Implementation is intentionally deferred until after the material ABI stage. First define transparent/OIT material pass contract, then migrate the RenderGraph runtime.

### ShadowCast

Required declarations:

- Shadow atlas remains material-forward receiver based in first version.
- Screen-space shadow trace is out of scope for this first RP contract and must not replace `ShadowCast` receiver path here.
- Light capability is initially placeholder but the feature must own light/slice/PCSS declarations.

### CharacterSpecialization

Required declarations:

- Belongs to `CompositeDomain`.
- Capture may need transparent-aware ordering because eyelashes/hair can be semi-transparent; first version keeps old after-transparents composite baseline and documents any capture split later.
- Object custom bits remain formal inputs until a higher-level character semantic UI is introduced.

### SemanticPostProcess

Required declarations:

- Reads semantics through AOV/registry resources.
- Owns AOV rule evaluation as a first-class semantic query, not an ad hoc shader mode.
- HoPost effects are not merged into ImagePostProcess.

### ImagePostProcess

Required declarations:

- Final image stack; AOV composite is optional/local.
- `SupportsAovComposite` metadata remains useful, but AOV composite must not define object/material semantics.
- Stateful and multipass effects must declare resources through the resource model before migration.

### PlanarReflection

Required declarations:

- Current confirmed feature is planar reflection, not SSR.
- Per-surface external resources need an imported/persistent resource story before runtime migration.
- Material participation is a material capability, not an inspector-side random toggle in the new system.

### DebugComposite

Required declarations:

- Every first-batch Feature registers at least one DebugView.
- Supports `Replace`, `Overlay`, `Split`, `PictureInPicture`, `ChannelInspect`, `Heatmap` as display modes.
- Debug capture is an explicit capability, not a late utility pass.
