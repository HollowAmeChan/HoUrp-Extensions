# RP DebugView 注册草案

> Debug 是一等系统能力，不是每个 Feature 自己临时画一张图。本文定义第一批 DebugView 和统一显示模式。

---

## 0. DebugView 模板

| DebugView | Domain | Source Resource | Source Semantic | Producer | Display Mode | Range | Owner Feature | Legacy Reference |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |

Display modes:

- `Replace`
- `Overlay`
- `Split`
- `PictureInPicture`
- `ChannelInspect`
- `Heatmap`

Lifetimes:

- `FrameOnly`
- `Sticky`
- `Persistent`
- `SceneLocal`
- `Temporary`

---

## 1. AOV Debug Views

| DebugView | Domain | Source Resource | Source Semantic | Producer | Display Mode | Range | Owner Feature | Legacy Reference |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `AOV / Mask` | DebugDomain | `Aov.MaskId` | `Object.MaskWeight` | AovOutput | Replace/Overlay/ChannelInspect | 0-1 | AovOutput | `HoAovDebugMode.Mask` |
| `AOV / Object ID` | DebugDomain | `Aov.MaskId` | `Object.Id` | AovOutput | Replace/Heatmap | 0-255 | AovOutput | `HoAovDebugMode.Id` |
| `AOV / Group ID` | DebugDomain | `Aov.MaskId` | `Object.GroupId` | AovOutput | Replace/Heatmap | 0-255 | AovOutput | old HoPost `GroupId` source |
| `AOV / Flags` | DebugDomain | `Aov.MaskId` | `Object.Flags` | AovOutput | Replace/ChannelInspect | bitmask | AovOutput | `HoAovDebugMode.Flags` |
| `AOV / Linear Depth` | DebugDomain | `Aov.NormalDepth`, `Aov.Depth` | `Geometry.LinearDepth` | AovOutput | Replace/Heatmap | camera near/far remap | AovOutput | `HoAovDebugMode.LinearDepth` |
| `AOV / World Normal` | DebugDomain | `Aov.NormalDepth` | `Geometry.WorldNormal` | AovOutput | Replace | -1..1 encoded | AovOutput | `HoAovDebugMode.WorldNormal` |
| `AOV / View Normal` | DebugDomain | `Aov.NormalDepth` | `Geometry.ViewNormal` | AovOutput | Replace | -1..1 encoded | AovOutput | `HoAovDebugMode.ViewNormal` |
| `AOV / Tangent Normal` | DebugDomain | `Aov.TangentNormal` | `Geometry.TangentNormal` | AovOutput | Replace | -1..1 encoded | AovOutput | `HoAovDebugMode.TangentNormal` |
| `AOV / Thickness` | DebugDomain | `Aov.SurfaceData` | `Material.Thickness` | AovOutput | Replace/Heatmap | 0-1 | AovOutput | `HoAovDebugMode.Thickness` |
| `AOV / Curvature` | DebugDomain | `Aov.SurfaceData` | `Material.Curvature` | AovOutput | Replace/Heatmap | -1..1 or 0..1 display | AovOutput | `HoAovDebugMode.Curvature` |
| `AOV / Material` | DebugDomain | `Aov.SurfaceData` | `Material.Class`, `Material.SssProfile` | AovOutput | Replace/Heatmap | 0-255 | AovOutput | `HoAovDebugMode.Material` |
| `AOV / Utility` | DebugDomain | `Aov.SurfaceData` | `Material.Utility` | AovOutput | Replace/Heatmap | 0-1 | AovOutput | `HoAovDebugMode.Utility` |
| `AOV / Custom 0` | DebugDomain | `Aov.MaterialCustom0_3` | `Material.Custom0` | AovOutput | Replace/ChannelInspect | 0-1 | AovOutput | `HoAovDebugMode.Custom0` |
| `AOV / Custom 1` | DebugDomain | `Aov.MaterialCustom0_3` | `Material.Custom1` | AovOutput | Replace/ChannelInspect | 0-1 | AovOutput | `HoAovDebugMode.Custom1` |
| `AOV / Custom 2` | DebugDomain | `Aov.MaterialCustom0_3` | `Material.Custom2` | AovOutput | Replace/ChannelInspect | 0-1 | AovOutput | `HoAovDebugMode.Custom2` |
| `AOV / Custom 3` | DebugDomain | `Aov.MaterialCustom0_3` | `Material.Custom3` | AovOutput | Replace/ChannelInspect | 0-1 | AovOutput | `HoAovDebugMode.Custom3` |
| `AOV / Object Custom 0-3` | DebugDomain | `Aov.ObjectCustom0_3` | `Object.Custom0-3` | AovOutput | Replace/ChannelInspect | 0-1 | AovOutput | `HoAovDebugMode.ObjectCustom0-3` |
| `AOV / Object Custom 4-7` | DebugDomain | `Aov.ObjectCustom4_7` | `Object.Custom4-7` | AovOutput | Replace/ChannelInspect | 0-1 | AovOutput | `HoAovDebugMode.ObjectCustom4-7` |
| `AOV / RSUV Packed` | DebugDomain | `Aov.MaskId`, renderer binding status | `Object.CustomMask`, `Object.CharacterId`, `Object.PartId`, `Object.Flags` | ObjectSemanticBinding / AovOutput | Replace/ChannelInspect | packed bytes | AovOutput | `HoAovDebugMode.RsuvPacked` |
| `AOV / SSS Source` | DebugDomain | `Aov.SssSource` | `Shading.SssSourceColor` | AovOutput | Replace | HDR color | AovOutput | `HoAovDebugMode.Sss` |

---

## 2. SSS Debug Views

| DebugView | Domain | Source Resource | Source Semantic | Producer | Display Mode | Range | Owner Feature | Legacy Reference |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `SSS / Enabled Status` | DebugDomain | feature state | n/a | SubsurfaceScattering | Overlay | on/off | SubsurfaceScattering | old settings debug mode |
| `SSS / Mask` | DebugDomain | `Aov.MaskId`, `Aov.SurfaceData` | `Shading.SssWeight`, `Material.SssProfile` | SubsurfaceScattering | Replace/Heatmap | 0-1 | SubsurfaceScattering | old SSS debug |
| `SSS / Source` | DebugDomain | `Sss.Source` | `Shading.SssSourceColor` | SubsurfaceScattering | Replace | HDR color | SubsurfaceScattering | `_lilHoSSSSourceTexture` |
| `SSS / Diffusion` | DebugDomain | `Sss.Diffusion` | diffused SSS | SubsurfaceScattering | Replace | HDR color | SubsurfaceScattering | `_lilHoSSSDiffusedTexture` |
| `SSS / Transmission` | DebugDomain | `Sss.Transmission` | transmission | SubsurfaceScattering | Replace | HDR color | SubsurfaceScattering | `_lilHoSSSTransmissionTexture` |
| `SSS / Profile ID` | DebugDomain | `Aov.SurfaceData` | `Material.SssProfile` | SubsurfaceScattering | Replace/Heatmap | 0-255 | SubsurfaceScattering | profile arrays |
| `SSS / Thickness` | DebugDomain | `Aov.SurfaceData` | `Material.Thickness` | SubsurfaceScattering | Replace/Heatmap | 0-1 | SubsurfaceScattering | thickness source |

---

## 3. OIT Debug Views

| DebugView | Domain | Source Resource | Source Semantic | Producer | Display Mode | Range | Owner Feature | Legacy Reference |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `OIT / Enabled Status` | DebugDomain | feature state | n/a | WeightedOit | Overlay | on/off | WeightedOit | `_lilOITActive` |
| `OIT / Accumulation` | DebugDomain | `Oit.Accumulation` | weighted transparent color | WeightedOit | Replace/ChannelInspect | HDR | WeightedOit | `_lilOITAccumulationTexture` |
| `OIT / Revealage` | DebugDomain | `Oit.Revealage` | revealage | WeightedOit | Replace/Heatmap | 0-1 | WeightedOit | `_lilOITRevealageTexture` |
| `OIT / Opaque Color` | DebugDomain | `Oit.OpaqueColor` | opaque color | WeightedOit | Replace/Split | color | WeightedOit | `_lilOITOpaqueTexture` |

---

## 4. Shadow Debug Views

| DebugView | Domain | Source Resource | Source Semantic | Producer | Display Mode | Range | Owner Feature | Legacy Reference |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `ShadowCast / Enabled Status` | DebugDomain | feature state | n/a | ShadowCast | Overlay | on/off | ShadowCast | `_HoShadowCastActive` |
| `ShadowCast / Atlas` | DebugDomain | `ShadowCast.Atlas` | shadow depth atlas | ShadowCast | Replace/PictureInPicture | depth | ShadowCast | old debug atlas |
| `ShadowCast / Second Directional Atlas` | DebugDomain | `ShadowCast.SecondDirectionalAtlas` | second directional atlas | ShadowCast | Replace/PictureInPicture | depth | ShadowCast | old debug atlas |
| `ShadowCast / Light Slices` | DebugDomain | `ShadowCast.SliceData` | slice metadata | ShadowCast | Overlay/ChannelInspect | index/rect | ShadowCast | `_HoShadowCastSliceData` |
| `ShadowCast / PCSS` | DebugDomain | `ShadowCast.PcssParams` | PCSS params | ShadowCast | Overlay | params | ShadowCast | `_HoShadowCastPcssParams*` |

---

## 5. Character Debug Views

| DebugView | Domain | Source Resource | Source Semantic | Producer | Display Mode | Range | Owner Feature | Legacy Reference |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Character / Enabled Status` | DebugDomain | feature state | n/a | CharacterSpecialization | Overlay | on/off | CharacterSpecialization | old feature debug |
| `Character / Eye Color` | DebugDomain | `Character.EyeColor` | `Composite.CharacterEye` | CharacterSpecialization | Replace | HDR color | CharacterSpecialization | `_lilHoCharacterEyeColorTexture` |
| `Character / Eye Data` | DebugDomain | `Character.EyeData` | eye reveal data | CharacterSpecialization | Replace/ChannelInspect | packed data | CharacterSpecialization | `_lilHoCharacterEyeDataTexture` |
| `Character / Capture Depth` | DebugDomain | `Character.CaptureDepth` | capture depth | CharacterSpecialization | Replace/Heatmap | depth | CharacterSpecialization | `_lilHoCharacterCaptureDepthTexture` |
| `Character / Hair Shadow` | DebugDomain | `Character.EyeData`, composite intermediate | `Composite.CharacterFrontHair` | CharacterSpecialization | Overlay | 0-1 | CharacterSpecialization | hair shadow params |
| `Character / Eye Reveal` | DebugDomain | `Character.EyeData`, `Aov.ObjectCustom4_7` | `Composite.EyeRevealArea` | CharacterSpecialization | Overlay | 0-1 | CharacterSpecialization | eye reveal params |

---

## 6. Post / Image Debug Views

| DebugView | Domain | Source Resource | Source Semantic | Producer | Display Mode | Range | Owner Feature | Legacy Reference |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `SemanticPost / Enabled Status` | DebugDomain | feature state | n/a | SemanticPostProcess | Overlay | on/off | SemanticPostProcess | old HoPost stack |
| `SemanticPost / AOV Mask` | DebugDomain | in-pass mask / AOV resources | `Composite.SemanticPostMask` | SemanticPostProcess | Replace/Overlay | 0-1 | SemanticPostProcess | `_LayerAovDebugOutput` |
| `SemanticPost / Subject Mask` | DebugDomain | `SemanticPost.SubjectMask` | subject mask | SemanticPostProcess | Replace | 0-1 | SemanticPostProcess | `_lilHoPostSubjectMaskTexture` |
| `SemanticPost / Layer Output` | DebugDomain | `SemanticPost.LayerTempA/B` | layer output | SemanticPostProcess | Split/Replace | color | SemanticPostProcess | old layer debug |
| `ImagePost / Enabled Status` | DebugDomain | feature state | n/a | ImagePostProcess | Overlay | on/off | ImagePostProcess | old Shoost stack |
| `ImagePost / AOV Composite Mask` | DebugDomain | in-pass mask / AOV resources | `Composite.ImageAovMask` | ImagePostProcess | Replace/Overlay | 0-1 | ImagePostProcess | Shoost AOV composite |
| `ImagePost / Layer Output` | DebugDomain | `ImagePost.LayerTempA/B/C` | `Image.FinalStyleLayer` | ImagePostProcess | Split/Replace | color | ImagePostProcess | Shoost layer result |

---

## 7. Reflection Debug Views

| DebugView | Domain | Source Resource | Source Semantic | Producer | Display Mode | Range | Owner Feature | Legacy Reference |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Reflection / Planar Status` | DebugDomain | feature state / surface registry | `Image.PlanarReflectionWeight` | PlanarReflection | Overlay | active/inactive | PlanarReflection | `LILPlanarReflectionSurface` |
| `Reflection / Planar Color` | DebugDomain | `Reflection.PlanarColor` | reflected color | PlanarReflection | PictureInPicture/Replace | color | PlanarReflection | `_LILPBRPlanarReflectionTexture` |
| `Reflection / Planar Matrix` | DebugDomain | `Reflection.PlanarMatrix` | projection | PlanarReflection | Overlay | matrix | PlanarReflection | `_LILPBRPlanarReflectionTextureMatrix` |

---

## 8. System Debug Views

| DebugView | Domain | Source Resource | Source Semantic | Producer | Display Mode | Range | Owner Feature | Legacy Reference |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Graph / Pass List` | DebugDomain | graph registry | pass dependency | DebugComposite | Overlay | n/a | DebugComposite | new |
| `Graph / Resource Lifetime` | DebugDomain | resource registry | lifetime | DebugComposite | Overlay | n/a | DebugComposite | new |
| `Semantic / Registry` | DebugDomain | semantic registry | semantic definitions | DebugComposite | Overlay | n/a | DebugComposite | new |
| `Capability / Active Set` | DebugDomain | capability registry | capabilities | DebugComposite | Overlay | n/a | DebugComposite | new |
| `Debug / Capture` | DebugDomain | selected view output | selected debug view | DebugComposite | Capture | n/a | DebugComposite | new |

---

## 9. Contract Rules

- Every first-batch Feature must register at least one status DebugView.
- Every first-batch Resource should register a visual or status DebugView.
- DebugView must state its source Resource or Semantic.
- Missing source must be displayed as a debug status, not fail silently.
- Debug overlay and debug capture are first-stage contract items.
