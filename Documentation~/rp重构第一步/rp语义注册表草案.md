# RP 语义注册表草案

> 第一阶段 Semantic Registry 草案。每个语义必须有 Domain、来源、Producer、Consumer、阶段、精度和 DebugView。旧来源用于迁移定位，不是新 ABI。

---

## 0. 表字段

| 字段 | 含义 |
| --- | --- |
| `Semantic` | 新 RP 内部语义名 |
| `Domain` | 所属 Domain |
| `旧来源` | 旧资源、属性、renderer user value 或 shader 计算来源 |
| `旧编码位置` | 旧 RT 通道或打包位 |
| `Producer` | 第一版生产者 |
| `Consumer` | 第一版消费者 |
| `阶段` | Pass 阶段 |
| `精度` | 逻辑精度或编码方式 |
| `DebugView` | 对应 debug view |
| `新 RP 判定` | 迁移判定 |

---

## 1. ObjectDomain

| Semantic | Domain | 旧来源 | 旧编码位置 | Producer | Consumer | 阶段 | 精度 | DebugView | 新 RP 判定 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Object.Id` | ObjectDomain | `_HoAovObjectId` | `_lilHoAovMaskIdTexture` ID 区域 | Object authoring / AovOutput | SemanticPostProcess, DebugComposite | ObjectSemanticBinding -> GeometrySemanticAov | 0-255 或 float encoded | `AOV / Object ID` | KeepConceptRename |
| `Object.GroupId` | ObjectDomain | `_HoAovGroupId`, renderer user value byte 8 | `_lilHoAovMaskIdTexture` | Object authoring / AovOutput | SemanticPostProcess, CharacterSpecialization | ObjectSemanticBinding -> GeometrySemanticAov | 0-255 | `AOV / Group ID` | KeepConceptRename |
| `Object.Flags` | ObjectDomain | `_HoAovFlags`, renderer user value byte 24 | `_lilHoAovMaskIdTexture` | Object authoring / AovOutput | SemanticPostProcess flags rules | ObjectSemanticBinding -> GeometrySemanticAov | 8-bit flags first version | `AOV / Flags` | KeepConceptRename |
| `Object.CharacterId` | ObjectDomain | `HoAovGroup.characterId` | renderer user value byte 8 | Object authoring | CharacterSpecialization, DebugComposite | ObjectSemanticBinding | 0-255 | `AOV / RSUV Character ID` | KeepConceptRename |
| `Object.PartId` | ObjectDomain | `HoAovGroup.partId` | renderer user value byte 16 | Object authoring | CharacterSpecialization, DebugComposite | ObjectSemanticBinding | 0-255 | `AOV / RSUV Part ID` | KeepConceptRename |
| `Object.CustomMask` | ObjectDomain | `HoAovGroup.objectCustom0-7`, `_HoAovObjectCustomMask` | renderer user value byte 0 | Object authoring | CharacterSpecialization, SemanticPostProcess | ObjectSemanticBinding -> GeometrySemanticAov | 8-bit bitmask | `AOV / Object Custom Packed` | KeepConceptRename |
| `Object.Custom0` | ObjectDomain | object custom bit 0 | `_lilHoAovObjectCustom0_3Texture.r` | AovOutput | CharacterSpecialization, SemanticPostProcess | GeometrySemanticAov | 0/1 or mask weight | `AOV / Object Custom 0` | KeepConceptRename |
| `Object.Custom1` | ObjectDomain | object custom bit 1 | `_lilHoAovObjectCustom0_3Texture.g` | AovOutput | CharacterSpecialization, SemanticPostProcess | GeometrySemanticAov | 0/1 or mask weight | `AOV / Object Custom 1` | KeepConceptRename |
| `Object.Custom2` | ObjectDomain | object custom bit 2 | `_lilHoAovObjectCustom0_3Texture.b` | AovOutput | CharacterSpecialization, SemanticPostProcess | GeometrySemanticAov | 0/1 or mask weight | `AOV / Object Custom 2` | KeepConceptRename |
| `Object.Custom3` | ObjectDomain | object custom bit 3 | `_lilHoAovObjectCustom0_3Texture.a` | AovOutput | CharacterSpecialization, SemanticPostProcess | GeometrySemanticAov | 0/1 or mask weight | `AOV / Object Custom 3` | KeepConceptRename |
| `Object.Custom4` | ObjectDomain | object custom bit 4 | `_lilHoAovObjectCustom4_7Texture.r` | AovOutput | CharacterSpecialization, SemanticPostProcess | GeometrySemanticAov | 0/1 or mask weight | `AOV / Object Custom 4` | KeepConceptRename |
| `Object.Custom5` | ObjectDomain | object custom bit 5 | `_lilHoAovObjectCustom4_7Texture.g` | AovOutput | CharacterSpecialization, SemanticPostProcess | GeometrySemanticAov | 0/1 or mask weight | `AOV / Object Custom 5` | KeepConceptRename |
| `Object.Custom6` | ObjectDomain | object custom bit 6 | `_lilHoAovObjectCustom4_7Texture.b` | AovOutput | SemanticPostProcess, DebugComposite | GeometrySemanticAov | 0/1 or mask weight | `AOV / Object Custom 6` | KeepConceptRename |
| `Object.Custom7` | ObjectDomain | object custom bit 7 | `_lilHoAovObjectCustom4_7Texture.a` | AovOutput | SemanticPostProcess, DebugComposite | GeometrySemanticAov | 0/1 or mask weight | `AOV / Object Custom 7` | KeepConceptRename |
| `Object.MaskWeight` | ObjectDomain | `_HoAovMaskWeight` | `_lilHoAovMaskIdTexture` mask channel | Object authoring / material producer | All AOV consumers | GeometrySemanticAov | normalized float | `AOV / Mask` | KeepConceptRename |

---

## 2. MaterialDomain

| Semantic | Domain | 旧来源 | 旧编码位置 | Producer | Consumer | 阶段 | 精度 | DebugView | 新 RP 判定 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Material.Class` | MaterialDomain | `_HoAovMaterialClass` | `_lilHoAovSurfaceDataTexture` material/profile field | Material semantic producer / AovOutput | SemanticPostProcess, DebugComposite | MaterialShadingSemanticAov | 0-255 encoded | `AOV / Material` | KeepConceptRename |
| `Material.SssProfile` | MaterialDomain | `_HoSSSProfileId` | surface data profile field | Material semantic producer / AovOutput | SubsurfaceScattering | MaterialShadingSemanticAov -> ScreenSss | 0-255; first runtime supports 8 profiles | `SSS / Profile ID` | KeepConceptRename |
| `Material.Thickness` | MaterialDomain | `_HoAovThickness`, material SSS thinness fallback | `_lilHoAovSurfaceDataTexture` | Material semantic producer / AovOutput | SubsurfaceScattering, SemanticPostProcess | MaterialShadingSemanticAov | normalized float | `AOV / Thickness` | KeepConceptRename |
| `Material.Curvature` | MaterialDomain | `_HoAovCurvature`, transmission boost fallback | `_lilHoAovSurfaceDataTexture` | Material semantic producer / AovOutput | SubsurfaceScattering, SemanticPostProcess | MaterialShadingSemanticAov | signed/normalized float | `AOV / Curvature` | KeepConceptRename |
| `Material.Utility` | MaterialDomain | `_HoAovUtility`, transmission radius fallback | `_lilHoAovSurfaceDataTexture` | Material semantic producer / AovOutput | SemanticPostProcess, future AO/trace | MaterialShadingSemanticAov | normalized float | `AOV / Utility` | KeepConceptRename |
| `Material.Custom0` | MaterialDomain | `_HoAovCustomValues0.x` or `_HoAovCustom0Tex` | `_lilHoAovCustom0_3Texture.r` | Material semantic producer / AovOutput | SemanticPostProcess, ImagePostProcess AOV composite | MaterialShadingSemanticAov | normalized float | `AOV / Custom 0` | KeepConceptRename |
| `Material.Custom1` | MaterialDomain | `_HoAovCustomValues0.y` or `_HoAovCustom1Tex` | `_lilHoAovCustom0_3Texture.g` | Material semantic producer / AovOutput | SemanticPostProcess, ImagePostProcess AOV composite | MaterialShadingSemanticAov | normalized float | `AOV / Custom 1` | KeepConceptRename |
| `Material.Custom2` | MaterialDomain | `_HoAovCustomValues0.z` or `_HoAovCustom2Tex` | `_lilHoAovCustom0_3Texture.b` | Material semantic producer / AovOutput | SemanticPostProcess, ImagePostProcess AOV composite | MaterialShadingSemanticAov | normalized float | `AOV / Custom 2` | KeepConceptRename |
| `Material.Custom3` | MaterialDomain | `_HoAovCustomValues0.w` or `_HoAovCustom3Tex` | `_lilHoAovCustom0_3Texture.a` | Material semantic producer / AovOutput | SemanticPostProcess, ImagePostProcess AOV composite | MaterialShadingSemanticAov | normalized float | `AOV / Custom 3` | KeepConceptRename |

---

## 3. GeometryDomain

| Semantic | Domain | 旧来源 | 旧编码位置 | Producer | Consumer | 阶段 | 精度 | DebugView | 新 RP 判定 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Geometry.LinearDepth` | GeometryDomain | AOV depth output | `_lilHoAovNormalDepthTexture` and `_lilHoAovDepthTexture` | AovOutput | SubsurfaceScattering, SemanticPostProcess, CharacterSpecialization | GeometrySemanticAov | camera linear depth | `AOV / Linear Depth` | KeepConceptRename |
| `Geometry.WorldNormal` | GeometryDomain | shader normal output | `_lilHoAovNormalDepthTexture` | AovOutput | SubsurfaceScattering, SemanticPostProcess | GeometrySemanticAov | high precision normal | `AOV / World Normal` | KeepConceptRename |
| `Geometry.ViewNormal` | GeometryDomain | old debug decode path | `_lilHoAovNormalDepthTexture` derived | AovOutput / DebugComposite | DebugComposite | GeometrySemanticAov | derived normal | `AOV / View Normal` | KeepConceptRename |
| `Geometry.TangentNormal` | GeometryDomain | shader tangent normal output | `_lilHoAovTangentNormalTexture` | AovOutput | SemanticPostProcess, DebugComposite | GeometrySemanticAov | high precision normal | `AOV / Tangent Normal` | KeepConceptRename |
| `Geometry.Coverage` | GeometryDomain | mask/subject coverage | mask channel and depth coverage | AovOutput | all AOV consumers | GeometrySemanticAov | normalized float | `AOV / Coverage` | KeepConceptRename |
| `Geometry.Velocity` | GeometryDomain / DeformationDomain candidate | old channel enum placeholder | not consistently produced | Future motion feature | Future temporal/filter consumers | Deferred | motion vector | `AOV / Velocity` | Defer |

---

## 4. ShadingDomain

| Semantic | Domain | 旧来源 | 旧编码位置 | Producer | Consumer | 阶段 | 精度 | DebugView | 新 RP 判定 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Shading.SssWeight` | ShadingDomain | material SSS terms and mask | surface data / SSS source | Material semantic producer / AovOutput | SubsurfaceScattering | MaterialShadingSemanticAov -> ScreenSss | normalized float | `SSS / Mask` | KeepConceptRename |
| `Shading.SssSourceColor` | ShadingDomain | `HoAOVSSS` pass | `_lilHoAovSssTexture` | AovOutput | SubsurfaceScattering | MaterialShadingSemanticAov -> ScreenSss | HDR color | `AOV / SSS Source` | KeepConceptRename |
| `Shading.TransmissionStrength` | ShadingDomain | `_HoSSSTransmissionStrength` | SSS params / surface-derived utility | Material semantic producer | SubsurfaceScattering | MaterialShadingSemanticAov -> ScreenSss | normalized/parameter float | `SSS / Transmission` | KeepConceptRename |
| `Shading.TransmissionRadius` | ShadingDomain | `_HoSSSTransmissionRadius` | SSS params / utility fallback | Material semantic producer | SubsurfaceScattering | MaterialShadingSemanticAov -> ScreenSss | parameter float | `SSS / Transmission Radius` | KeepConceptRename |

---

## 5. CompositeDomain

| Semantic | Domain | 旧来源 | 旧编码位置 | Producer | Consumer | 阶段 | 精度 | DebugView | 新 RP 判定 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Composite.CharacterEye` | CompositeDomain | `Object.Custom3` convention | object custom bit 3 | Object authoring / AovOutput | CharacterSpecialization | CharacterComposite | mask | `Character / Eye` | KeepConceptRename |
| `Composite.CharacterFace` | CompositeDomain | `Object.Custom1` convention | object custom bit 1 | Object authoring / AovOutput | CharacterSpecialization | CharacterComposite | mask | `Character / Face` | KeepConceptRename |
| `Composite.CharacterFrontHair` | CompositeDomain | `Object.Custom2` convention | object custom bit 2 | Object authoring / AovOutput | CharacterSpecialization | CharacterComposite | mask | `Character / Front Hair` | KeepConceptRename |
| `Composite.EyeRevealArea` | CompositeDomain | `Object.Custom4` convention | object custom bit 4 | Object authoring / AovOutput | CharacterSpecialization | CharacterComposite | mask | `Character / Eye Reveal` | KeepConceptRename |
| `Composite.SemanticPostMask` | CompositeDomain | HoPost AOV rule result | runtime layer mask | SemanticPostProcess | SemanticPostProcess layer shaders, DebugComposite | SemanticPost | normalized float | `SemanticPost / AOV Mask` | KeepConceptRename |
| `Composite.ImageAovMask` | CompositeDomain | Shoost AOV composite mask | runtime layer mask | ImagePostProcess | ImagePostProcess layer shaders, DebugComposite | ImagePost | normalized float | `ImagePost / AOV Composite Mask` | KeepConceptRename |

---

## 6. ShadowDomain

| Semantic | Domain | 旧来源 | 旧编码位置 | Producer | Consumer | 阶段 | 精度 | DebugView | 新 RP 判定 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `ShadowCast.LightCount` | ShadowDomain | `_HoShadowCastLightCount` | global int | ShadowCast | material forward receiver, DebugComposite | ShadowLightingPrepass | int | `ShadowCast / Light Count` | KeepConceptRename |
| `ShadowCast.SliceCount` | ShadowDomain | `_HoShadowCastSliceCount` | global int | ShadowCast | material forward receiver, DebugComposite | ShadowLightingPrepass | int | `ShadowCast / Slice Count` | KeepConceptRename |
| `ShadowCast.WorldToShadow` | ShadowDomain | `_HoShadowCastWorldToShadowRow0-3` | global matrix rows | ShadowCast | material forward receiver | ShadowLightingPrepass | float4 arrays | `ShadowCast / Light Slices` | KeepConceptRename |
| `ShadowCast.PcssParams` | ShadowDomain | `_HoShadowCastPcssParams*` | global vectors | ShadowCast | material forward receiver | ShadowLightingPrepass | float4 params | `ShadowCast / PCSS` | KeepConceptRename |

---

## 7. ImageDomain

| Semantic | Domain | 旧来源 | 旧编码位置 | Producer | Consumer | 阶段 | 精度 | DebugView | 新 RP 判定 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Image.FinalStyleLayer` | ImageDomain | Shoost layer/effect descriptor | runtime layer stack | ImagePostProcess | ImagePostProcess, DebugComposite | ImagePost | effect-specific | `ImagePost / Layer Output` | KeepConceptRename |
| `Image.PlanarReflectionWeight` | ImageDomain / MaterialDomain | `_UsePlanarReflection`, `_PlanarReflectionStrength`, smoothness fade | material property + reflection params | PlanarReflection / material producer | material forward shading | OpaqueShading | normalized float | `Reflection / Planar Weight` | KeepConceptRename |

---

## 8. 未决项

| 项 | 状态 | 后续处理 |
| --- | --- | --- |
| Motion vectors / deformation velocity | Deferred | 等 DeformationDomain 设计后再纳入正式 semantic |
| Stylized shadow / ramp / specular mask | Deferred | 等新 HoPbr/HoNpr material producer 设计 |
| Light group / receiver group | Deferred | 等 Light Capability 细化 |
| SSR / HTrace | Deferred | 当前第一阶段只确认 PlanarReflection，不把 SSR 纳入旧事实 |
