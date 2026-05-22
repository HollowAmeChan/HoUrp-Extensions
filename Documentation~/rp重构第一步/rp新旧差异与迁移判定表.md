# RP 新旧差异与迁移判定表

> 本文把旧能力逐项判定为保留概念、短期旧 binding、替代、删除、延后或仅验证。视觉一致性最终由人工/截图/RenderDoc 验证决定；本文负责给出架构处理方式。

---

## 0. 判定枚举

| Decision | Meaning |
| --- | --- |
| `KeepConceptRename` | 保留概念，换新命名和新契约 |
| `KeepConceptTemporaryLegacyBinding` | 保留概念，短期可映射旧 binding 便于验证 |
| `Replace` | 旧机制被新机制替代 |
| `Remove` | 删除，不进入新系统 |
| `Defer` | 延后决策 |
| `ValidationOnly` | 仅用于旧行为对照 |

---

## 1. Feature 级迁移判定

| Old Item | Old Type | Current Behavior | New Concept | Decision | Required Work | Risk | Review Owner |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `HoAovRendererFeature` | RendererFeature | 输出多 MRT AOV，含 fallback/debug/RenderGraph/compat path | `AovOutput` | KeepConceptRename | 拆成契约、资源注册、几何/材质语义 pass | AOV 拆分后消费者读不到旧通道 | RP owner |
| `HoSubsurfaceScatteringRendererFeature` | RendererFeature | 消费 AOV，屏幕空间 SSS，transparent 前 composite | `SubsurfaceScattering` | KeepConceptRename | 声明 AOV 消费、SSS resources、debug views | transparent ordering 改动影响肤色 | RP owner |
| `WeightedOITRendererFeature` | RendererFeature | opaque copy、accumulation、revealage、composite | `WeightedOit` | KeepConceptRename | 显式资源和 material capability | transparent 背景或 forward skip 失效 | RP owner |
| `HoShadowCastRendererFeature` | RendererFeature | 自定义 atlas + 材质 forward 接收 | `ShadowCast` | KeepConceptRename | 资源化 atlas/light/slice，保留接收验证 | shadow receiver ABI 与新材质契约冲突 | RP owner |
| `HoCharacterSpecializationRendererFeature` | RendererFeature | 角色捕获、眼透、前发投影 | `CharacterSpecialization` | KeepConceptRename | 明确 composite domain 和 object custom inputs | 半透明部件排序 | RP owner |
| `HoPostProcessRendererFeature` | RendererFeature | 语义感知 layer stack，AOV rules | `SemanticPostProcess` | KeepConceptRename | 把 AOV rules 提升为 semantic query | 与 ImagePost 边界混淆 | RP owner |
| `ShoostPostProcessRendererFeature` | RendererFeature | final image style stack，部分 AOV composite | `ImagePostProcess` | KeepConceptRename | 保留 descriptor 元数据，限制 AOV composite 边界 | 变成第二套 HoPost | RP owner |
| `LILPlanarReflectionSurface` | Component-driven feature | beginCameraRendering 反射相机 + MPB | `PlanarReflection` | KeepConceptRename | 外部资源/导入资源契约 | per-surface 生命周期与 RenderGraph 不一致 | RP owner |
| old debug passes | feature-local debug | 每个 Feature 自己 debug | `DebugComposite` + `DebugRegistry` | Replace | 统一注册 DebugView/overlay/capture | 调试入口迁移遗漏 | RP owner |
| old compatibility path | non-RenderGraph path | 与 RenderGraph path 双维护 | none | ValidationOnly | 保留作行为参考，不迁移 | 误把兼容路径带入新系统 | RP owner |

---

## 2. Resource / Binding 迁移判定

| Old Item | Old Type | Current Behavior | New Concept | Decision | Required Work | Risk | Review Owner |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `_lilHoAovMaskIdTexture` | Global Texture | AOV mask/id/group/flags | `Aov.MaskId` | KeepConceptRename | 建 Resource Registry + shader binding mapping | HoPost/Character 规则错读 | RP owner |
| `_lilHoAovNormalDepthTexture` | Global Texture | normal/depth | `Aov.NormalDepth` | KeepConceptRename | 标明 normal/depth encoding | SSS/edge effect 失真 | RP owner |
| `_lilHoAovTangentNormalTexture` | Global Texture | tangent normal | `Aov.TangentNormal` | KeepConceptRename | 资源化 | debug/outline 缺输入 | RP owner |
| `_lilHoAovSurfaceDataTexture` | Global Texture | material/profile/thickness/curvature/utility | `Aov.SurfaceData` | KeepConceptRename | 重定义 MaterialDomain semantic | SSS profile/thickness 映射错 | RP owner |
| `_lilHoAovCustom0_3Texture` | Global Texture | material custom channels | `Aov.MaterialCustom0_3` | KeepConceptRename | 保留正式能力 | 自定义效果回归 | RP owner |
| `_lilHoAovObjectCustom0_3Texture` | Global Texture | object custom 0-3 | `Aov.ObjectCustom0_3` | KeepConceptRename | 保留命名 | 角色区域规则错 | RP owner |
| `_lilHoAovObjectCustom4_7Texture` | Global Texture | object custom 4-7 | `Aov.ObjectCustom4_7` | KeepConceptRename | 保留命名 | 眼透/配件规则错 | RP owner |
| `_lilHoAovSssTexture` | Global Texture | SSS source | `Aov.SssSource` | KeepConceptRename | 明确 `Shading.SssSourceColor` | SSS 源色丢失 | RP owner |
| `_lilHoAovDepthTexture` | Global Depth | AOV depth target | `Aov.Depth` | KeepConceptRename | 明确深度生命周期 | depth debug/测试错 | RP owner |
| `_lilHoAovActive` | Global Scalar | active handshake | Feature/resource status | Replace | 通过 registry 状态表达 | shader 临时分支迁移遗漏 | RP owner |
| `_lilOITActive` | Global Scalar | OIT accumulation 期间跳过 forward | pass/material phase state | Replace | 新材质 pass contract | 透明物体重复渲染 | RP owner |
| `_HoShadowCastActive` | Global Scalar | shadow active handshake | Feature/resource status | Replace | 新状态与 binding 分离 | receiver 错判 shadow | RP owner |
| `_CameraOpaqueTexture` OIT alias | Global Texture | OIT copy 同时喂 URP opaque texture path | `Oit.OpaqueColor` + optional alias | ValidationOnly | 新系统必须显式声明别名 | 隐式依赖继续蔓延 | RP owner |
| `_LILPBRPlanarReflectionTexture` | MPB Texture | per-surface reflection | `Reflection.PlanarColor` | KeepConceptRename | imported/external resource | surface 生命周期未纳管 | RP owner |

---

## 3. Material / Object Producer 迁移判定

| Old Item | Old Type | Current Behavior | New Concept | Decision | Required Work | Risk | Review Owner |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `HoAovSubject` | Component | 通过 MPB 写 AOV object/material semantic | Object Capability authoring | KeepConceptRename | UI 重命名、字段归域 | 用户已有场景迁移 | RP owner |
| `HoAovGroup` | Component | 打包 object custom + character/part/flags 到 renderer user value | Object semantic group authoring | KeepConceptRename | 文档化 pack；后续提供迁移工具 | renderer user value 丢失 | RP owner |
| `unity_RendererUserValue` pack | Renderer value ABI | byte0 custom mask, byte1 character, byte2 part, byte3 flags | `Object.CustomMask/CharacterId/PartId/Flags` | KeepConceptRename | 新 authoring 写法 | shader decode 不一致 | RP owner |
| `_HoAov*` material properties | Material ABI | 材质/对象语义输入 | Material/Object semantic producer inputs | KeepConceptTemporaryLegacyBinding | 建旧名映射表用于验证 | 误当新材质 ABI | RP owner |
| `_HoSSS*` material properties | Material ABI | SSS profile/thickness/transmission | Material SSS capability/policy | KeepConceptTemporaryLegacyBinding | 新材质 preset 声明 | SSS 不一致 | RP owner |
| `_lilOITEnabled` | Material ABI | OIT 参与开关 | `SupportsOit` | KeepConceptTemporaryLegacyBinding | 新材质 capability | OIT pass 缺失 | RP owner |
| `_UsePlanarReflection` | Material ABI | planar receiver enable | `SupportsPlanarReflection` + policy | KeepConceptTemporaryLegacyBinding | material capability | 反射不显示 | RP owner |
| old material inspector-driven structure | Editor ABI | inspector/keyword 决定 shader 行为 | material preset/generator metadata | Replace | 新材质系统阶段处理 | 旧 UI 牵引新 RP | Material owner |

---

## 4. Shader Include / Package Path 迁移判定

| Old Item | Old Type | Current Behavior | New Concept | Decision | Required Work | Risk | Review Owner |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `HoAovSampling.hlsl` | include | AOV decode helpers | new AOV decode library | KeepConceptRename | 新 include path + new resource names | old sampling decode 遗漏 | RP owner |
| `HoShadowCastSampling.hlsl` | include | material receiver samples atlas | new ShadowCast receiver library | KeepConceptTemporaryLegacyBinding | 新 receiver contract | 阴影视觉差异 | RP owner |
| `HoCharacterCaptureCommon.hlsl` | include | capture output struct/helpers | new Character capture library | KeepConceptRename | 新 include path | capture pass 不兼容 | RP owner |
| `lil_pass_hoaov.hlsl` | lilToon include | old AOV producer | validation fixture / migration reference | ValidationOnly | 不作为新材质底座 | 旧材质结构污染新契约 | Material owner |
| `lilPBR/Shaders/hoaov.hlsl` | lilPBR include | old AOV producer | validation fixture / migration reference | ValidationOnly | 不作为新材质底座 | 同上 | Material owner |
| old package id `jp.lilxyzw.liltoon.urp.extensions` | package path | include path ABI | new package id `com.hollow.hourp-extensions` | Replace | new package paths | include path 兼容包袱 | RP owner |

---

## 5. Behavior Validation Priorities

| Capability | Visual Behavior To Preserve | Validation Source | First Migration Priority |
| --- | --- | --- | --- |
| AOV mask/id/custom | HoPost/Character/Shoost masks select same regions | old HoAOV debug + HoPost masks | High |
| SSS | skin/source/thickness/profile response close to old | old HoSSS scene and debug | High |
| OIT | transparent ordering and opaque background match old | old OIT transparent scene | High |
| ShadowCast | material receiver shadow strength and PCSS behavior close to old | old shadow scenes / RenderDoc | High |
| CharacterSpecialization | eye reveal and front hair shadow match old | character test scene | High |
| HoPost | semantic edge/outline/drop shadow rules match old | HoPost layer scenes | Medium |
| Shoost | final image stack order/effects match old where migrated | Shoost test scenes | Medium |
| PlanarReflection | planar UV/matrix/fade match old | planar reflection scene | Medium |
| Debug | new debug can inspect every resource old debug could inspect | debug view checklist | High |

---

## 6. Deferred / Out-of-Scope

| Item | Decision | Reason |
| --- | --- | --- |
| SSR / HTrace integration | Defer | 当前旧实现确认的是 PlanarReflection；screen-space trace 不纳入第一阶段 |
| Motion vector semantic | Defer | 需要 DeformationDomain 和 temporal/filter contract |
| Full material system rewrite | Defer | RP contract 先于材质系统 |
| Full Shoost migration | Defer | 第二阶段不建议从完整 Shoost 开始 |
| Full HoSSS/HoShadowCast runtime migration | Defer to after minimal AOV/debug chain | 第一阶段只冻结契约 |

---

## 7. Required Next-Stage Work

第二阶段开始前必须完成：

- 新 runtime contract 类型设计：Semantic/Resource/Feature/Debug/Capability。
- 最小 Resource Registry 原型。
- 最小 DebugView Registry 原型。
- `Aov.MaskId / Aov.NormalDepth` 最小输出链路设计。
- 旧 binding 映射文档转成测试 fixture 或迁移验证工具。
