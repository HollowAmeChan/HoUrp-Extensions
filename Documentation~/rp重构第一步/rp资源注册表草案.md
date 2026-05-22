# RP 资源注册表草案

> 第一阶段 Resource Registry 草案。Resource 是实际数据载体，不等同于 Semantic。旧全局纹理名只保存在 `旧资源名` 字段中。

---

## 0. 表字段

| 字段 | 含义 |
| --- | --- |
| `Resource` | 新 RP 逻辑资源名 |
| `ResourceKind` | Texture / Buffer / Scalar / Imported / External 等 |
| `Semantic` | 主要承载的语义 |
| `Producer Feature` | 生产者 |
| `Consumer Feature` | 消费者 |
| `Format` | 第一版格式意图 |
| `Scale` | full / half / quarter / external |
| `Lifetime` | Transient / PerCamera / Persistent / Imported |
| `Clear Policy` | 清理策略 |
| `DebugView` | 对应 debug |
| `旧资源名` | 旧 ABI 事实 |

---

## 1. AOV Resources

| Resource | ResourceKind | Semantic | Producer Feature | Consumer Feature | Format | Scale | Lifetime | Clear Policy | DebugView | 旧资源名 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Aov.MaskId` | Texture2D | `Object.MaskWeight`, `Object.Id`, `Object.GroupId`, `Object.Flags` | AovOutput | SubsurfaceScattering, CharacterSpecialization, SemanticPostProcess, ImagePostProcess, DebugComposite | mask/id packed; old preferred `R8G8B8A8_UNorm` | Full first; scale option allowed | PerCamera RenderGraph resource | clear to zero each camera | `AOV / Mask ID` | `_lilHoAovMaskIdTexture` |
| `Aov.NormalDepth` | Texture2D | `Geometry.WorldNormal`, `Geometry.ViewNormal`, `Geometry.LinearDepth` | AovOutput | SubsurfaceScattering, CharacterSpecialization, SemanticPostProcess, DebugComposite | high precision; old preferred `R16G16B16A16_SFloat` | Full first; scale option allowed | PerCamera RenderGraph resource | clear to neutral normal/depth far | `AOV / Normal Depth` | `_lilHoAovNormalDepthTexture` |
| `Aov.TangentNormal` | Texture2D | `Geometry.TangentNormal` | AovOutput | SemanticPostProcess, DebugComposite | high precision; old preferred `R16G16B16A16_SFloat` | Full first; scale option allowed | PerCamera RenderGraph resource | clear to neutral tangent normal | `AOV / Tangent Normal` | `_lilHoAovTangentNormalTexture` |
| `Aov.SurfaceData` | Texture2D | `Material.Class`, `Material.SssProfile`, `Material.Thickness`, `Material.Curvature`, `Material.Utility` | AovOutput | SubsurfaceScattering, SemanticPostProcess, DebugComposite | high precision; old preferred `R16G16B16A16_SFloat` | Full first; scale option allowed | PerCamera RenderGraph resource | clear to zero | `AOV / Surface Data` | `_lilHoAovSurfaceDataTexture` |
| `Aov.MaterialCustom0_3` | Texture2D | `Material.Custom0-3` | AovOutput | SemanticPostProcess, ImagePostProcess, DebugComposite | high precision; old preferred `R16G16B16A16_SFloat` | Full first; scale option allowed | PerCamera RenderGraph resource | clear to zero | `AOV / Material Custom 0-3` | `_lilHoAovCustom0_3Texture` |
| `Aov.ObjectCustom0_3` | Texture2D | `Object.Custom0-3` | AovOutput | CharacterSpecialization, SemanticPostProcess, DebugComposite | high precision; old preferred `R16G16B16A16_SFloat` | Full first; scale option allowed | PerCamera RenderGraph resource | clear to zero | `AOV / Object Custom 0-3` | `_lilHoAovObjectCustom0_3Texture` |
| `Aov.ObjectCustom4_7` | Texture2D | `Object.Custom4-7` | AovOutput | CharacterSpecialization, SemanticPostProcess, DebugComposite | high precision; old preferred `R16G16B16A16_SFloat` | Full first; scale option allowed | PerCamera RenderGraph resource | clear to zero | `AOV / Object Custom 4-7` | `_lilHoAovObjectCustom4_7Texture` |
| `Aov.SssSource` | Texture2D | `Shading.SssSourceColor`, `Shading.SssWeight` | AovOutput | SubsurfaceScattering, DebugComposite | HDR/high precision | Full first | PerCamera RenderGraph resource | clear to transparent black | `AOV / SSS Source` | `_lilHoAovSssTexture` |
| `Aov.Depth` | DepthTexture | `Geometry.LinearDepth` / AOV write depth | AovOutput | AovOutput, DebugComposite, possible CharacterSpecialization | depth/stencil; old uses camera fallback then D24/D32 | Full first; follows AOV scale if possible | PerCamera RenderGraph resource | depth clear | `AOV / Depth` | `_lilHoAovDepthTexture` |

---

## 2. SSS Resources

| Resource | ResourceKind | Semantic | Producer Feature | Consumer Feature | Format | Scale | Lifetime | Clear Policy | DebugView | 旧资源名 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Sss.Source` | Texture2D | `Shading.SssSourceColor` | SubsurfaceScattering | SubsurfaceScattering | camera HDR fallback, old prefers camera format then `R16G16B16A16_SFloat` | Full/half candidate | Transient PerCamera | copy/clear from AOV SSS source | `SSS / Source` | `_lilHoSSSSourceTexture` |
| `Sss.Diffusion` | Texture2D | diffused SSS | SubsurfaceScattering | SubsurfaceScattering composite | HDR | Full/half candidate | Transient PerCamera | clear to black | `SSS / Diffusion` | `_lilHoSSSDiffusedTexture` |
| `Sss.Transmission` | Texture2D | transmission gather | SubsurfaceScattering | SubsurfaceScattering composite | HDR | Full/half candidate | Transient PerCamera | clear to black | `SSS / Transmission` | `_lilHoSSSTransmissionTexture` |
| `Sss.TransmissionTemp` | Texture2D | intermediate transmission blur | SubsurfaceScattering | SubsurfaceScattering | HDR | Full/half candidate | Transient PerCamera | clear to black | `SSS / Transmission Temp` | `_lilHoSSSTransmissionTempTexture` |
| `Sss.CompositeSource` | Texture2D | camera color before SSS composite | SubsurfaceScattering | SubsurfaceScattering | camera color format | Full | Transient PerCamera | copy camera color | `SSS / Composite Source` | `_lilHoSSSCompositeSourceTexture` |

---

## 3. OIT Resources

| Resource | ResourceKind | Semantic | Producer Feature | Consumer Feature | Format | Scale | Lifetime | Clear Policy | DebugView | 旧资源名 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Oit.Accumulation` | Texture2D | weighted transparent color and alpha | WeightedOit | WeightedOit composite | old `R16G16B16A16_SFloat` | Full/half/quarter candidate | Transient PerCamera | clear to zero | `OIT / Accumulation` | `_lilOITAccumulationTexture` |
| `Oit.Revealage` | Texture2D | revealage/alpha | WeightedOit | WeightedOit composite | old `R8_UNorm` | Full/half/quarter candidate | Transient PerCamera | clear to zero | `OIT / Revealage` | `_lilOITRevealageTexture` |
| `Oit.OpaqueColor` | Texture2D | opaque camera color copy | WeightedOit | transparent material sampling, composite | camera color format | Full or OIT scale | PerCamera RenderGraph resource | copy after skybox / before accumulation | `OIT / Opaque Color` | `_lilOITOpaqueTexture` |
| `Oit.CompositeSource` | Texture2D | camera color before OIT composite | WeightedOit | WeightedOit composite | camera color format | Full | Transient PerCamera | copy camera color | `OIT / Composite Source` | `_lilOITCompositeSourceTexture` |

---

## 4. Shadow Resources

| Resource | ResourceKind | Semantic | Producer Feature | Consumer Feature | Format | Scale | Lifetime | Clear Policy | DebugView | 旧资源名 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `ShadowCast.Atlas` | Texture2D/Depth | shadow caster atlas | ShadowCast | material forward receiver, DebugComposite | raw depth atlas | configured atlas size | PerCamera / PerLight frame resource | depth clear | `ShadowCast / Atlas` | `_HoShadowCastAtlas` |
| `ShadowCast.SecondDirectionalAtlas` | Texture2D/Depth | second directional cascade atlas | ShadowCast | material forward receiver, DebugComposite | raw depth atlas | configured atlas size | PerCamera / PerLight frame resource | depth clear | `ShadowCast / Second Directional Atlas` | `_HoShadowCastSecondDirectionalAtlas` |
| `ShadowCast.LightData` | GlobalBuffer/Array | light type, position, direction, color, attenuation | ShadowCast | material forward receiver | float4 arrays | n/a | PerCamera | reset count to zero | `ShadowCast / Light Data` | `_HoShadowCastLightData0-2`, `_HoShadowCastLightColor`, `_HoShadowCastLightAttenuation` |
| `ShadowCast.SliceData` | GlobalBuffer/Array | atlas slice rect/depth info | ShadowCast | material forward receiver | float4 array | n/a | PerCamera | reset count to zero | `ShadowCast / Light Slices` | `_HoShadowCastSliceData` |
| `ShadowCast.WorldToShadow` | GlobalBuffer/Array | world-to-shadow matrix rows | ShadowCast | material forward receiver | float4 arrays | n/a | PerCamera | reset to identity/zero count | `ShadowCast / Matrices` | `_HoShadowCastWorldToShadowRow0-3` |
| `ShadowCast.PcssParams` | GlobalVector | PCSS control parameters | ShadowCast | material forward receiver | float4 | n/a | PerCamera | set default params | `ShadowCast / PCSS` | `_HoShadowCastPcssParams*` |

---

## 5. Character Resources

| Resource | ResourceKind | Semantic | Producer Feature | Consumer Feature | Format | Scale | Lifetime | Clear Policy | DebugView | 旧资源名 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Character.EyeColor` | Texture2D | captured eye/character color | CharacterSpecialization | CharacterSpecialization composite | old preferred `R16G16B16A16_SFloat` | Full first | Transient PerCamera | clear to transparent | `Character / Eye Color` | `_lilHoCharacterEyeColorTexture` |
| `Character.EyeData` | Texture2D | eye reveal / hair shadow data | CharacterSpecialization | CharacterSpecialization composite | old preferred `R16G16B16A16_SFloat` | Full first | Transient PerCamera | clear to zero | `Character / Eye Data` | `_lilHoCharacterEyeDataTexture` |
| `Character.CaptureDepth` | DepthTexture | character capture depth | CharacterSpecialization | CharacterSpecialization composite | camera depth fallback / D24/D32 | Full first | Transient PerCamera | depth clear | `Character / Capture Depth` | `_lilHoCharacterCaptureDepthTexture` |
| `Character.CompositeSource` | Texture2D | camera color before character composite | CharacterSpecialization | CharacterSpecialization composite | camera color format | Full | Transient PerCamera | copy camera color | `Character / Composite Source` | `_lilHoCharacterCompositeSource` |

---

## 6. Post / Image Resources

| Resource | ResourceKind | Semantic | Producer Feature | Consumer Feature | Format | Scale | Lifetime | Clear Policy | DebugView | 旧资源名 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `SemanticPost.LayerTempA` | Texture2D | intermediate semantic post layer | SemanticPostProcess | SemanticPostProcess | camera color format | Full | Transient PerCamera | undefined/copy source as needed | `SemanticPost / Layer A` | `_lilHoPostProcessTempA` |
| `SemanticPost.LayerTempB` | Texture2D | intermediate semantic post layer | SemanticPostProcess | SemanticPostProcess | camera color format | Full | Transient PerCamera | undefined/copy source as needed | `SemanticPost / Layer B` | `_lilHoPostProcessTempB` |
| `SemanticPost.SubjectMask` | Texture2D | subject mask for forward fallback rules | SemanticPostProcess | SemanticPostProcess | single/packed mask | Full | Transient PerCamera | clear to zero | `SemanticPost / Subject Mask` | `_lilHoPostSubjectMaskTexture` |
| `ImagePost.LayerTempA` | Texture2D | intermediate image post layer | ImagePostProcess | ImagePostProcess | camera color format | Full | Transient PerCamera | undefined/copy source as needed | `ImagePost / Layer A` | `_lilShoostPostProcessTempA` |
| `ImagePost.LayerTempB` | Texture2D | intermediate image post layer | ImagePostProcess | ImagePostProcess | camera color format | Full | Transient PerCamera | undefined/copy source as needed | `ImagePost / Layer B` | `_lilShoostPostProcessTempB` |
| `ImagePost.LayerTempC` | Texture2D | multipass image post temp | ImagePostProcess | ImagePostProcess | camera color format | Full | Transient PerCamera | undefined/copy source as needed | `ImagePost / Layer C` | `_lilShoostPostProcessTempC` |
| `ImagePost.AovCompositeMask` | Texture2D or in-pass mask | Image AOV composite mask | ImagePostProcess | ImagePostProcess | normalized mask | Full | PerPass transient | clear to zero | `ImagePost / AOV Composite Mask` | old in-pass Shoost AOV composite |

---

## 7. Reflection Resources

| Resource | ResourceKind | Semantic | Producer Feature | Consumer Feature | Format | Scale | Lifetime | Clear Policy | DebugView | 旧资源名 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Reflection.PlanarColor` | External Texture2D / RenderTexture | planar reflected color | PlanarReflection | material forward receiver | configured render texture | external resolution | Persistent per surface | camera clear / reflection render | `Reflection / Planar Color` | `_LILPBRPlanarReflectionTexture` |
| `Reflection.PlanarMatrix` | Matrix | reflection UV projection matrix | PlanarReflection | material forward receiver | float4x4 | n/a | PerSurface / PerFrame | set per rendered surface | `Reflection / Planar Matrix` | `_LILPBRPlanarReflectionTextureMatrix` |
| `Reflection.PlanarParams` | Vector | enabled/flip/fade params | PlanarReflection | material forward receiver | float4 | n/a | PerSurface / PerFrame | disabled value when inactive | `Reflection / Planar Params` | `_LILPBRPlanarReflectionParams`, `_UsePlanarReflection` |

---

## 8. Imported Resources

| Resource | ResourceKind | Semantic | Producer Feature | Consumer Feature | Format | Scale | Lifetime | Clear Policy | DebugView | 旧资源名 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Camera.Color` | Imported Texture2D | active camera color | URP | most composite/post features | camera format | Full | Imported PerCamera | external | `Camera / Color` | URP activeColorTexture |
| `Camera.Depth` | Imported Texture2D | camera depth | URP | AovOutput, SemanticPostProcess, SSS | camera depth format | Full | Imported PerCamera | external | `Camera / Depth` | `_CameraDepthTexture` |
| `Camera.Normals` | Imported Texture2D | camera normals if present | URP | SemanticPostProcess fallback | URP normal format | Full | Imported PerCamera | external | `Camera / Normals` | `_CameraNormalsTexture` |
| `Camera.OpaqueColor` | Imported or produced Texture2D | opaque camera color | URP / WeightedOit | transparent and post consumers | camera format | Full | Imported or PerCamera | copy when needed | `Camera / Opaque Color` | `_CameraOpaqueTexture` |

---

## 9. First Version Lifetime Decisions

- AOV, SSS, OIT, Character and post temps should be RenderGraph-declared resources, not implicit global RT owners.
- Shader global binding is allowed only as binding/export for shader code, not as ownership or lifetime definition.
- `Reflection.PlanarColor` remains external/persistent because the old system is component-driven and per-surface; later it needs a formal imported resource path.
- Almost every resource needs at least one DebugView. If a resource has no visual debug, the DebugView can initially be a presence/status view.
