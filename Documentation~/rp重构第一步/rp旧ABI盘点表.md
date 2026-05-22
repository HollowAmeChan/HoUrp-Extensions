# RP 旧 ABI 盘点表

> 旧 ABI 事实清单。本文只记录 `lilToon-URP-Extensions`、`lilToon`、`lilPBR` 当前如何协作，不代表新 RP 要继承这些名称或结构。

---

## 0. 判定枚举

| 判定 | 含义 |
| --- | --- |
| `KeepConceptRename` | 保留概念，换成新命名和新契约 |
| `KeepConceptTemporaryLegacyBinding` | 保留概念，并短期映射旧 binding 便于验证 |
| `Replace` | 用新机制替代 |
| `Remove` | 删除，不进入新系统 |
| `Defer` | 延后决策 |
| `ValidationOnly` | 仅用于迁移验证 |

---

## 1. LightMode / ShaderTag ABI

| 旧 ABI | 类型 | 定义位置 | 写入者 | 读取者 | 用途 | 生命周期 | 新 RP 判定 | 备注 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `HoAOV` | LightMode / ShaderTagId | `HoAovShaderConstants.ShaderPassName`, lilToon/lilPBR shader pass | lilToon/lilPBR material pass | `HoAovRendererFeature` | 输出 AOV 主 MRT | PerCamera pass | KeepConceptRename | 新 pass 归 `AovOutput`，旧名不进新 ABI |
| `HoAOVSSS` | LightMode / ShaderTagId | `HoAovShaderConstants.SssShaderPassName`, lilToon/lilPBR shader pass | lilToon/lilPBR material pass | `HoAovRendererFeature` | 输出 SSS source | PerCamera pass | KeepConceptRename | 新语义为 `Shading.SssSourceColor` |
| `lilToonOIT` | LightMode / ShaderTagId | `WeightedOITShaderConstants.ShaderPassName`, lilToon transparent template | transparent material pass | `WeightedOITRendererFeature` | weighted blended OIT accumulation | PerCamera transparent pass | KeepConceptRename | 新名应表达 transparent accumulation |
| `HoCharacterCapture` | LightMode / ShaderTagId | `HoCharacterSpecializationShaderConstants.CapturePassName`, lilToon/lilPBR pass | material capture pass | `HoCharacterSpecializationRendererFeature` | 角色眼透/前发数据捕获 | PerCamera pass | KeepConceptRename | 新 capture pass 待定 |
| `ShadowCaster` | LightMode / ShaderTagId | URP/lilPBR/lilToon shader pass | shadow caster material pass | `HoShadowCastRendererFeature` | 自定义 shadow atlas 生成 | PerLight/PerCamera | KeepConceptTemporaryLegacyBinding | 仍需材质接收 shadow |
| `UniversalForward` | LightMode | lilToon/lilPBR shader pass | material forward pass | URP, HoPost subject mask | 常规前向渲染/subject mask | PerCamera pass | ValidationOnly | 不作为 HoUrp 新自定义 ABI |
| `UniversalForwardOnly` | LightMode | URP-compatible shader pass | material forward pass | HoPost subject mask | subject mask fallback | PerCamera pass | ValidationOnly | 仅旧 HoPost 查找 |
| `SRPDefaultUnlit` | LightMode | Unity fallback pass | material pass | HoPost subject mask | subject mask fallback | PerCamera pass | ValidationOnly | 仅旧 HoPost 查找 |
| `UniversalGBuffer` | LightMode | lilToon/lilPBR shader pass | material GBuffer pass | URP deferred | deferred compatibility | PerCamera pass | ValidationOnly | 不定义新 RP contract |
| `DepthOnly` / `DepthNormals` | LightMode | lilPBR shader pass | material depth passes | URP | depth/normal prepass | PerCamera pass | ValidationOnly | 可作为几何语义参考 |
| `MotionVectors` / `XRMotionVectors` | LightMode | lilPBR shader pass | material motion vector pass | URP | motion vectors | PerCamera pass | Defer | DeformationDomain 后续处理 |

---

## 2. AOV ABI

| 旧 ABI | 类型 | 定义位置 | 写入者 | 读取者 | 用途 | 生命周期 | 新 RP 判定 | 备注 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `_lilHoAovActive` | global scalar | `HoAovShaderConstants.ActiveName` | HoAOV reset/pass | material/debug shaders | 标记 AOV 是否有效 | PerCamera | Replace | 新系统用 Feature/Resource 状态 |
| `_lilHoAovSystemChannelMask` | global scalar | `HoAovShaderConstants.SystemChannelMaskName` | HoAOV pass | AOV shaders | 控制系统通道写入 | PerCamera | KeepConceptRename | 变为 AovOutput pass config |
| `_lilHoAovMaskIdTexture` | global texture | `HoAovShaderConstants.MaskIdTextureName` | HoAOV | HoSSS, HoPost, Shoost, Character, Debug | mask/id/flags/group | PerCamera | KeepConceptRename | 新资源 `Aov.MaskId` |
| `_lilHoAovNormalDepthTexture` | global texture | `HoAovShaderConstants.NormalDepthTextureName` | HoAOV | HoSSS, HoPost, Character, Debug | normal/depth | PerCamera | KeepConceptRename | 新资源 `Aov.NormalDepth` |
| `_lilHoAovTangentNormalTexture` | global texture | `HoAovShaderConstants.TangentNormalTextureName` | HoAOV | HoPost, Debug | tangent normal | PerCamera | KeepConceptRename | 新资源 `Aov.TangentNormal` |
| `_lilHoAovSurfaceDataTexture` | global texture | `HoAovShaderConstants.SurfaceDataTextureName` | HoAOV | HoSSS, HoPost, Debug | material/profile/thickness/curvature/utility | PerCamera | KeepConceptRename | 新资源 `Aov.SurfaceData` |
| `_lilHoAovCustom0_3Texture` | global texture | `HoAovShaderConstants.Custom0TextureName` | HoAOV | HoPost, Shoost, Debug | material custom channels | PerCamera | KeepConceptRename | 新资源 `Aov.MaterialCustom0_3` |
| `_lilHoAovObjectCustom0_3Texture` | global texture | `HoAovShaderConstants.ObjectCustom0TextureName` | HoAOV | Character, HoPost, Debug | object custom 0-3 | PerCamera | KeepConceptRename | 新资源 `Aov.ObjectCustom0_3` |
| `_lilHoAovObjectCustom4_7Texture` | global texture | `HoAovShaderConstants.ObjectCustom1TextureName` | HoAOV | Character, HoPost, Debug | object custom 4-7 | PerCamera | KeepConceptRename | 新资源 `Aov.ObjectCustom4_7` |
| `_lilHoAovSssTexture` | global texture | `HoAovShaderConstants.SssTextureName` | HoAOVSSS pass | HoSSS, Debug | SSS source color | PerCamera | KeepConceptRename | 新资源 `Aov.SssSource` |
| `_lilHoAovDepthTexture` | global depth texture | `HoAovShaderConstants.DepthTextureName` | HoAOV | Debug / depth tests | AOV depth target | PerCamera | KeepConceptRename | 新资源 `Aov.Depth` |
| `_HoAovDebugMode` | shader property/global | `HoAovShaderConstants.DebugModeId` | HoAOV debug | debug shader | AOV debug mode | PerCamera | Replace | 新 DebugRegistry |
| `_HoAovDebugDepthParams` | shader property/global | `HoAovShaderConstants.DebugDepthParamsId` | HoAOV debug | debug shader | depth remap | PerCamera | KeepConceptRename | DebugView 参数 |

---

## 3. Object / Material Property ABI

| 旧 ABI | 类型 | 定义位置 | 写入者 | 读取者 | 用途 | 生命周期 | 新 RP 判定 | 备注 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `_HoAovMaskWeight` | MPB/material property | `HoAovSubject`, lilToon/lilPBR shader props | HoAovSubject / material | AOV pass | subject coverage | PerRenderer/PerMaterial | KeepConceptRename | `Object.MaskWeight` |
| `_HoAovSystemWriteMask` | MPB/material property | `HoAovSubject`, material props | HoAovSubject / material | AOV pass | channel write mask | PerRenderer/PerMaterial | KeepConceptRename | new authoring policy |
| `_HoAovCustomWriteMask` | MPB/material property | `HoAovSubject`, material props | HoAovSubject / material | AOV pass | material custom write mask | PerRenderer/PerMaterial | KeepConceptRename | `Material.Custom0-3` write mask |
| `_HoAovGroupId` | MPB/material property | `HoAovSubject`, `HoAovGroup` fallback | HoAovSubject/Group | AOV pass | group id | PerRenderer | KeepConceptRename | `Object.GroupId` |
| `_HoAovObjectId` | MPB/material property | `HoAovSubject` | HoAovSubject | AOV pass | object id | PerRenderer | KeepConceptRename | `Object.Id` |
| `_HoAovMaterialClass` | MPB/material property | `HoAovSubject`, material props | HoAovSubject/material | AOV pass | material class | PerRenderer/PerMaterial | KeepConceptRename | `Material.Class` |
| `_HoAovFlags` | MPB/material property | `HoAovSubject`, `HoAovGroup` fallback | HoAovSubject/Group | AOV pass | flags | PerRenderer | KeepConceptRename | `Object.Flags` |
| `_HoAovThickness` | MPB/material property | `HoAovSubject`, material props | HoAovSubject/material | AOV pass, HoSSS | material thickness | PerRenderer/PerMaterial | KeepConceptRename | `Material.Thickness` |
| `_HoAovCurvature` | MPB/material property | `HoAovSubject`, material props | HoAovSubject/material | AOV pass, HoSSS | material curvature | PerRenderer/PerMaterial | KeepConceptRename | `Material.Curvature` |
| `_HoAovUtility` | MPB/material property | `HoAovSubject`, material props | HoAovSubject/material | AOV pass, HoPost | utility channel | PerRenderer/PerMaterial | KeepConceptRename | `Material.Utility` |
| `_HoAovDebugColor` | MPB property | `HoAovSubject` | HoAovSubject | AOV/debug | debug color | PerRenderer | Replace | Debug authoring field, not core semantic |
| `_HoAovCustomValues0` | MPB/material property | `HoAovSubject`, material props | HoAovSubject/material | AOV pass | material custom scalar values | PerRenderer/PerMaterial | KeepConceptRename | `Material.Custom0-3` |
| `_HoAovObjectCustomMask` | MPB property | `HoAovSubject`, `HoAovGroup` fallback | HoAovGroup/Subject | AOV pass | object custom bitmask | PerRenderer | KeepConceptRename | `Object.CustomMask` |
| `_HoAovCustom0Tex` ... `_HoAovCustom3Tex` | material texture | lilToon/lilPBR shaders | material | AOV pass | material custom source textures | PerMaterial | KeepConceptRename | material producer contract |
| `unity_RendererUserValue` | renderer user value | `HoAovGroup.PackRendererUserValue()` | HoAovGroup | AOV pass | packed custom mask + character/part/flags | PerRenderer | KeepConceptRename | packed bytes: mask, characterId, partId, flags |

---

## 4. SSS ABI

| 旧 ABI | 类型 | 定义位置 | 写入者 | 读取者 | 用途 | 生命周期 | 新 RP 判定 | 备注 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `_lilHoSSSSourceTexture` | global texture | `HoSubsurfaceScatteringShaderConstants` | HoSSS source pass | HoSSS blur/composite | SSS source working texture | PerCamera | KeepConceptRename | `Sss.Source` |
| `_lilHoSSSDiffusedTexture` | global texture | same | HoSSS blur | HoSSS composite | diffusion result | PerCamera | KeepConceptRename | `Sss.Diffusion` |
| `_lilHoSSSTransmissionTexture` | global texture | same | HoSSS transmission | HoSSS composite | transmission result | PerCamera | KeepConceptRename | `Sss.Transmission` |
| `_lilHoSSSTransmissionTempTexture` | global texture | same | HoSSS transmission blur | HoSSS transmission blur | intermediate | PerCamera | KeepConceptRename | `Sss.TransmissionTemp` |
| `_lilHoSSSCompositeSourceTexture` | global texture | same | HoSSS composite | HoSSS composite | camera color copy | PerCamera | KeepConceptRename | `Sss.CompositeSource` |
| `_lilHoSSSParams` | global vector | same | HoSSS | HoSSS shader | main SSS params | PerCamera | KeepConceptRename | SSS policy |
| `_lilHoSSSGateParams` | global vector | same | HoSSS | HoSSS shader | gating | PerCamera | KeepConceptRename | SSS policy |
| `_lilHoSSSProfileIds` | global vector array | same | HoSSS | HoSSS shader | profile mapping | PerCamera | KeepConceptRename | first version mirrors 8 slots |
| `_HoSSSProfileId` | material property | lilToon/lilPBR material props | material | AOV pass / HoSSS | SSS profile id | PerMaterial | KeepConceptRename | `Material.SssProfile` |
| `_HoSSSThicknessScale` | material property | lilToon/lilPBR material props | material | AOV pass / HoSSS | thickness scale | PerMaterial | KeepConceptRename | material SSS policy |
| `_HoSSSTransmissionStrength` | material property | lilToon/lilPBR material props | material | AOV pass / HoSSS | transmission strength | PerMaterial | KeepConceptRename | `Shading.TransmissionStrength` |
| `_HoSSSTransmissionRadius` | material property | lilToon/lilPBR material props | material | AOV pass / HoSSS | transmission radius | PerMaterial | KeepConceptRename | `Shading.TransmissionRadius` |

---

## 5. OIT ABI

| 旧 ABI | 类型 | 定义位置 | 写入者 | 读取者 | 用途 | 生命周期 | 新 RP 判定 | 备注 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `_lilOITAccumulationTexture` | global texture | `WeightedOITShaderConstants` | OIT accumulation | OIT composite | weighted color/alpha | PerCamera | KeepConceptRename | `Oit.Accumulation` |
| `_lilOITRevealageTexture` | global texture | same | OIT accumulation | OIT composite | revealage/alpha | PerCamera | KeepConceptRename | `Oit.Revealage` |
| `_lilOITOpaqueTexture` | global texture | same | OIT opaque copy | transparent shaders / composite | opaque scene color | PerCamera | KeepConceptRename | `Oit.OpaqueColor` |
| `_lilOITCompositeSourceTexture` | global texture | same | OIT composite | OIT composite | camera color copy | PerCamera | KeepConceptRename | `Oit.CompositeSource` |
| `_CameraOpaqueTexture` | global texture alias | `WeightedOITShaderConstants.CameraOpaqueTextureId` | OIT opaque copy / URP | transparent shaders | URP-compatible opaque texture | PerCamera | ValidationOnly | new resource should be explicit |
| `_lilOITActive` | global scalar | `WeightedOITShaderConstants.OITActiveName` | OIT reset/accumulation | material forward pass | skip normal forward while accumulating | PerCamera | Replace | new material capability/phase state |
| `_lilOITEnabled` | material property | lilToon input | material | material forward/OIT pass | material participates OIT | PerMaterial | KeepConceptRename | `SupportsOit` |
| `_lilOITWeight` | global scalar | `WeightedOITRendererFeature` | OIT feature | OIT shader | accumulation weight | PerCamera | KeepConceptRename | OIT policy |
| `_lilOITAlphaClipThreshold` | global scalar | same | OIT feature | OIT shader | reject low alpha | PerCamera | KeepConceptRename | OIT policy |

---

## 6. ShadowCast ABI

| 旧 ABI | 类型 | 定义位置 | 写入者 | 读取者 | 用途 | 生命周期 | 新 RP 判定 | 备注 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `_HoShadowCastAtlas` | global texture | `HoShadowCastShaderConstants.AtlasTextureName` | ShadowCast | material receiver/debug | main shadow atlas | PerCamera | KeepConceptRename | `ShadowCast.Atlas` |
| `_HoShadowCastSecondDirectionalAtlas` | global texture | same | ShadowCast | material receiver/debug | second directional atlas | PerCamera | KeepConceptRename | `ShadowCast.SecondDirectionalAtlas` |
| `_HoShadowCastActive` | global scalar | same | ShadowCast reset/pass | material receiver | receiver enable state | PerCamera | Replace | Feature/resource status |
| `_HoShadowCastLightCount` | global scalar | same | ShadowCast | material receiver | light count | PerCamera | KeepConceptRename | `ShadowCast.LightCount` |
| `_HoShadowCastSliceCount` | global scalar | same | ShadowCast | material receiver | atlas slice count | PerCamera | KeepConceptRename | `ShadowCast.SliceCount` |
| `_HoShadowCastWorldToShadowRow0-3` | global array | same | ShadowCast | material receiver | matrix rows | PerCamera | KeepConceptRename | `ShadowCast.WorldToShadow` |
| `_HoShadowCastLightData0-2` | global array | same | ShadowCast | material receiver | light data | PerCamera | KeepConceptRename | `ShadowCast.LightData` |
| `_HoShadowCastLightAttenuation` | global array | same | ShadowCast | material receiver | attenuation | PerCamera | KeepConceptRename | `ShadowCast.LightData` |
| `_HoShadowCastSliceData` | global array | same | ShadowCast | material receiver | slice rect/depth | PerCamera | KeepConceptRename | `ShadowCast.SliceData` |
| `_HoShadowCastPcssParams*` | global vector | same | ShadowCast | material receiver | PCSS params | PerCamera | KeepConceptRename | Shadow policy |
| `_CASTING_PUNCTUAL_LIGHT_SHADOW` | GlobalKeyword | `HoShadowCastShaderConstants` | ShadowCast | shadow caster shader | punctual casting mode | PerPass | Replace | avoid global keyword leakage if possible |
| `_HoShadowStrength` | material property | lilPBR/lilToon material integration | material | forward shading | strength blending HoShadowCast | PerMaterial | KeepConceptRename | material shadow policy |
| `HoShadowCastAttenuation(positionWS)` | include function | `HoShadowCastSampling.hlsl`, material includes | ShadowCast include | material forward shading | sample custom atlas | PerMaterial pass | KeepConceptTemporaryLegacyBinding | old behavior validation |

---

## 7. Character ABI

| 旧 ABI | 类型 | 定义位置 | 写入者 | 读取者 | 用途 | 生命周期 | 新 RP 判定 | 备注 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `_lilHoCharacterEyeColorTexture` | global texture | `HoCharacterSpecializationShaderConstants` | Character capture | Character composite | eye/color capture | PerCamera | KeepConceptRename | `Character.EyeColor` |
| `_lilHoCharacterEyeDataTexture` | global texture | same | Character capture | Character composite | eye/hair data | PerCamera | KeepConceptRename | `Character.EyeData` |
| `_lilHoCharacterCaptureDepthTexture` | global depth texture | same | Character capture | Character composite | capture depth | PerCamera | KeepConceptRename | `Character.CaptureDepth` |
| `_lilHoCharacterCompositeSource` | global texture | same | Character composite | Character composite | camera color copy | PerCamera | KeepConceptRename | `Character.CompositeSource` |
| `_HoCharacterCaptureMode` | shader property | same | Character feature | capture shader | capture output mode | PerPass | KeepConceptRename | Character pass policy |
| `_HoCharacterEyeRevealParams` | shader property | same | Character feature/volume | composite shader | eye reveal control | PerCamera | KeepConceptRename | Character policy |
| `_HoCharacterHairShadowParams*` | shader property | same | Character feature/volume | composite shader | hair drop shadow | PerCamera | KeepConceptRename | Character policy |
| `_HoCharacterCaptureOpacity` | material property | lilToon/lilPBR capture includes | material | capture pass | capture opacity | PerMaterial | KeepConceptRename | material capability/policy |

---

## 8. HoPost / Shoost ABI

| 旧 ABI | 类型 | 定义位置 | 写入者 | 读取者 | 用途 | 生命周期 | 新 RP 判定 | 备注 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `_lilHoPostProcessTempA/B` | global texture | `HoPostProcessShaderConstants` | HoPost | HoPost | layer ping-pong | PerCamera | KeepConceptRename | `SemanticPost.LayerTempA/B` |
| `_lilHoPostSubjectMaskTexture` | global texture | same | HoPost subject mask pass | HoPost layers | subject mask | PerCamera | KeepConceptRename | `SemanticPost.SubjectMask` |
| `_LayerAovMaskEnabled` | shader property | HoPost/Shoost constants | layer runtime | layer shaders | enable AOV mask | PerLayer | KeepConceptRename | semantic query control |
| `_LayerAovSource` | shader property | HoPost/Shoost constants | layer runtime | AOV mask shader | source selection | PerLayer | KeepConceptRename | maps to registered semantic |
| `_LayerAovRuleCount` / `_LayerAovRuleData*` | shader arrays | HoPost/Shoost constants | runtime rule packing | AOV mask shader | up to 4 AOV rules | PerLayer | KeepConceptRename | semantic query model |
| `HoPostAovSource.Mask..ObjectCustom7` | enum | `HoPostProcessLayer.cs` | layer authoring | HoPost/Shoost AOV mask | semantic source choices | Static/PerLayer | KeepConceptRename | becomes semantic source registry |
| `_lilShoostPostProcessTempA/B/C` | global texture | `ShoostPostProcessShaderConstants` | Shoost | Shoost | image stack temps | PerCamera | KeepConceptRename | `ImagePost.LayerTempA/B/C` |
| `SupportsAovComposite` | metadata field | `ShoostPostProcessEffectDescriptor` | Shoost descriptor | Shoost runtime | per-effect AOV composite support | Static | KeepConceptRename | keep as ImagePost metadata |

---

## 9. Planar Reflection ABI

| 旧 ABI | 类型 | 定义位置 | 写入者 | 读取者 | 用途 | 生命周期 | 新 RP 判定 | 备注 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `_UsePlanarReflection` | material/MPB scalar | `LILPlanarReflectionSurface`, lilToon/lilPBR shaders | component/material | material forward shading | enable receiver | PerRenderer/PerMaterial | KeepConceptRename | material capability |
| `_LILPBRPlanarReflectionTexture` | material/MPB texture | same | component | material forward shading | reflection color | PerSurface persistent | KeepConceptRename | `Reflection.PlanarColor` |
| `_LILPBRPlanarReflectionTextureMatrix` | material/MPB matrix | same | component | material forward shading | UV projection | PerSurface/PerFrame | KeepConceptRename | `Reflection.PlanarMatrix` |
| `_LILPBRPlanarReflectionParams` | material/MPB vector | same | component | material forward shading | enabled/params | PerSurface/PerFrame | KeepConceptRename | `Reflection.PlanarParams` |
| `RenderPipelineManager.beginCameraRendering` | pipeline event hook | `LILPlanarReflectionSurface` | component | Unity render loop | render reflection camera | PerCamera external | Replace | needs formal imported/external resource contract |

---

## 10. Include / Package Path ABI

| 旧 ABI | 类型 | 定义位置 | 写入者 | 读取者 | 用途 | 生命周期 | 新 RP 判定 | 备注 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Packages/jp.lilxyzw.liltoon.urp.extensions/Runtime/CharacterSpecialization/Shaders/HoCharacterCaptureCommon.hlsl` | include path | lilToon/lilPBR capture includes | old package | material shaders | capture output struct/helpers | Compile time | ValidationOnly | new package path must not preserve old package id |
| `Runtime/AOV/Shaders/HoAOV/HoAovSampling.hlsl` | include path | old package | HoSSS/HoPost/Shoost shaders | AOV decode helpers | Compile time | KeepConceptRename | new decode library should use new names |
| `Runtime/ShadowCast/Shaders/HoShadowCastSampling.hlsl` | include path | old package | material shaders | shadow atlas sampling | Compile time | KeepConceptTemporaryLegacyBinding | keep for validation only |
| lilToon include modifications | shader include ABI | `Assets/lilToon/Shader/Includes/*` | old material package | old RP extensions | OIT, HoShadow, AOV, planar reflection | Compile time | ValidationOnly | new material system must target new contract |
| lilPBR shader passes/properties | shader ABI | `lilPBR/Shaders/*` | old material package | old RP extensions | AOV, character, SSS, planar, shadow | Compile time | ValidationOnly | useful behavior baseline only |

---

## 11. RenderPassEvent / RenderGraph ABI

| 旧 ABI | 类型 | 定义位置 | 写入者 | 读取者 | 用途 | 生命周期 | 新 RP 判定 | 备注 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `HoAOV AfterRenderingOpaques` | RenderPassEvent default | `HoAovSettings.aovPassEvent` | settings | HoAOV | AOV output timing | PerCamera | ValidationOnly | new timeline splits geometry/material AOV |
| `HoAOV Debug AfterRenderingPostProcessing` | RenderPassEvent default | `HoAovSettings.debugPassEvent` | settings | HoAOV debug | debug preview | PerCamera | Replace | unified DebugComposite |
| `HoSSS Source AfterRenderingSkybox` | RenderPassEvent default | `HoSubsurfaceScatteringSettings.sourcePassEvent` | settings | HoSSS | source/gather start | PerCamera | ValidationOnly | first version keeps before transparents requirement |
| `HoSSS Composite BeforeRenderingTransparents` | RenderPassEvent default | `HoSubsurfaceScatteringSettings.compositePassEvent` | settings | HoSSS | composite before transparents | PerCamera | KeepConceptRename | timing contract |
| `OIT Accumulation BeforeRenderingTransparents` | RenderPassEvent default | `WeightedOITSettings.accumulationPassEvent` | settings | OIT | transparent accumulation | PerCamera | KeepConceptRename | first version follows old timing |
| `OIT Composite AfterRenderingTransparents` | RenderPassEvent default | `WeightedOITSettings.compositePassEvent` | settings | OIT | transparent composite | PerCamera | KeepConceptRename | first version follows old timing |
| `ShadowCast BeforeRenderingPrePasses` | RenderPassEvent default | `HoShadowCastRendererFeature.Settings.passEvent` | settings | ShadowCast | shadow atlas | PerCamera | KeepConceptRename | clamp to prepasses or later |
| `Character AfterRenderingTransparents` | RenderPassEvent default | `HoCharacterSpecializationSettings.passEvent` | settings | Character | capture/composite | PerCamera | KeepConceptRename | may split later |
| `HoPost AfterRenderingPostProcessing` | RenderPassEvent const | `HoPostProcessRenderPassEvents.HoPostStack` | HoPost | HoPost | semantic post stack | PerCamera | KeepConceptRename | `SemanticPost` |
| `Shoost AfterRenderingPostProcessing + 1` | RenderPassEvent const | `HoPostProcessRenderPassEvents.ShoostFinalStack` | Shoost | Shoost | final image stack | PerCamera | KeepConceptRename | `ImagePost` |
| `HoAovRenderGraphResources` | ContextItem | `HoAovRendererFeature.cs` | HoAOV | HoSSS/HoPost/Character/Shoost | AOV handles | PerCamera | KeepConceptRename | new Resource Registry equivalent |
| `WeightedOITRenderGraphResources` | ContextItem | `WeightedOITRendererFeature.cs` | OIT | OIT composite | OIT handles | PerCamera | KeepConceptRename | new Resource Registry equivalent |
| `HoSubsurfaceScatteringRenderGraphResources` | ContextItem | `HoSubsurfaceScatteringRendererFeature.cs` | HoSSS | HoSSS composite/debug | SSS handles | PerCamera | KeepConceptRename | new Resource Registry equivalent |
| `HoShadowCastRenderGraphResources` | ContextItem | `HoShadowCastRenderGraphResources.cs` | ShadowCast | debug/material binding | shadow handles | PerCamera | KeepConceptRename | new Resource Registry equivalent |

---

## 12. Review Notes

- Most old global textures are real resource concepts and should be renamed, not deleted.
- Most old scalar globals named `Active` are execution-state handshakes and should be replaced by feature/resource state.
- Old material properties are evidence of useful producer inputs, but new material ABI must be designed after RP contract freeze.
- Old compatibility path is validation-only. New implementation direction remains URP 6000.3+ and RenderGraph-first.
