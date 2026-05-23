# RP 核心契约草案

> 第一阶段产物。本文定义新 RP 的第一版契约词汇、命名原则和边界。旧 `lilToon-URP-Extensions`、`lilToon`、`lilPBR` 只作为行为基线和迁移事实，不作为新核心 ABI。

---

## 0. 阶段边界

第一阶段只冻结契约，不迁移 runtime 实现。

允许：

- 盘点旧 ABI。
- 定义新语义、资源、Feature、Pass、DebugView、Capability。
- 建立旧名到新名的迁移判定。
- 记录后续实现任务。

不允许：

- 复制旧 `HoAovRendererFeature.cs`、旧 shader 或旧 compatibility path。
- 把 `_lilHoAov*`、`_lilOIT*`、`lilToonOIT` 等旧名定为新公共 ABI。
- 为旧 `lilToon/lilPBR` 建长期 Bridge。
- 先按旧材质 inspector 或旧 shader property 设计新 RP。

---

## 1. 顶层 Domain

| Domain | 职责 | 第一阶段说明 |
| --- | --- | --- |
| `ObjectDomain` | 对象、角色、部件、实例、分组、per-renderer 语义 | 承接 `HoAovSubject`、`HoAovGroup` 中的对象语义，但重命名为正式 Object semantic |
| `MaterialDomain` | 材质类型、表面模型、profile、材质自定义语义 | `Thickness`、`Curvature`、`Utility`、`Material.Custom0-3` 第一版归这里 |
| `GeometryDomain` | mesh、normal、depth、tangent、coverage | 从旧 AOV 的 normal/depth/tangent 输出反推 |
| `DeformationDomain` | skinning、morph、cloth、velocity | 第一阶段占位；旧 AOV `Velocity` 仅记录为候选 |
| `ShadingDomain` | SSS source、stylized shadow、specular mask、ramp 等着色派生结果 | 第一阶段只登记 SSS source 和 shading producer 边界 |
| `LightingDomain` | 常规光照、间接光、volumetric、light group | 保留给后续光照语义 |
| `ShadowDomain` | 自定义 shadow atlas、slice、PCSS、shadow receiver 数据 | `ShadowCast` 单独归域，不混进 LightingDomain |
| `ImageDomain` | 最终图像空间风格栈 | `ImagePostProcess` 对应旧 Shoost |
| `CompositeDomain` | 角色特化、语义合成、最终合成前的选择性处理 | `CharacterSpecialization`、`SemanticPostProcess` 所属 |
| `DebugDomain` | debug view、overlay、capture、HUD、graph/resource inspection | 第一阶段进入契约 |
| `CapabilityDomain` | 对象、材质、光、feature 可用能力 | 替代 Layer/Tag/材质开关散落规则 |

---

## 2. 核心对象

| 对象 | 定义 |
| --- | --- |
| `Semantic` | 可被 producer 写入、consumer 读取的有名语义项，例如 `Object.GroupId`、`Geometry.WorldNormal` |
| `Resource` | 帧内或跨帧存在的数据载体，例如 `Aov.MaskId`、`ShadowCast.Atlas` |
| `Feature` | 可声明生产/消费、pass、能力和 debug 的功能模块，例如 `AovOutput` |
| `Pass` | 在 frame timeline 上执行的一段工作，不等同于 Unity shader pass |
| `Producer` | 负责创建或写入语义/资源的一方 |
| `Consumer` | 依赖并读取语义/资源的一方 |
| `Capability` | 显式声明“允许参与什么”的能力 |
| `Policy` | 描述“如何参与”的参数化策略 |
| `DebugView` | 注册到 DebugDomain 的可视化入口 |
| `LegacyInterop` | 旧事实清单、差异表和验证工具所在地，不是新核心依赖 |

---

## 3. 生命周期

| 生命周期 | 用途 |
| --- | --- |
| `Static` | 项目或 asset 级静态定义 |
| `PerRenderer` | 单个 Renderer 的对象语义和能力 |
| `PerMaterial` | 单个材质或材质 preset 的语义生产能力 |
| `PerFrame` | 一帧内共享状态 |
| `PerCamera` | 单相机资源、pass 和 debug 状态 |
| `PerPass` | 单 pass 内局部数据 |
| `Transient` | RenderGraph 可别名/回收的临时资源 |
| `Persistent` | history、temporal、cache 等跨帧资源 |
| `Imported` | CameraColor、CameraDepth、外部反射纹理等导入资源 |

---

## 4. 命名原则

新 RP 命名必须表达 Domain 和用途，不表达旧实现来源。

| 类型 | 推荐形式 | 例子 | 说明 |
| --- | --- | --- | --- |
| 语义名 | `Domain.Name` | `Object.CharacterId` | Semantic Registry 内部名 |
| 资源名 | `Feature.Resource` 或 `Domain.Resource` | `Aov.MaskId` | Resource Registry 和 RenderGraph 声明名 |
| Feature 名 | PascalCase | `AovOutput` | 不保留 `HoAOV` 作为新模块名 |
| Pass 名 | 阶段 + 意图 | `GeometrySemanticAov` | 与旧 `LightMode` 分开记录 |
| DebugView 名 | `Group / Display` | `AOV / Mask ID` | 面向 UI |
| Capability 名 | 动词短语 | `WritesAov` | 表达可参与能力 |
| shader property | 可晚于逻辑名确定 | `_HoAovMaskIdTexture` 或新名 | 必须通过 binding 表映射 |

禁止：

- 新逻辑 ABI 以 `lil`、`lilToon`、`lilPBR` 为前缀。
- 新资源逻辑名沿用 `_lilHoAov*`。
- 用 shader property 名替代 Resource/Semantic 名。
- 用文件夹位置替代 Domain 归属。

---

## 5. 新旧命名隔离规则

| 旧命名类型 | 第一阶段处理 |
| --- | --- |
| 旧 LightMode：`HoAOV`、`HoAOVSSS`、`HoCharacterCapture`、`lilToonOIT` | 记录为旧 ABI；新名在 Feature/Pass 表中另定 |
| 旧全局纹理：`_lilHoAov*`、`_lilOIT*`、`_lilHoSSS*` | 记录旧资源名；新 Resource 使用逻辑名 |
| 旧材质属性：`_HoAov*`、`_HoSSS*`、`_UsePlanarReflection` | 按语义/能力重新归属；不默认成为新材质 ABI |
| 旧 renderer user value 打包 | 保留概念并文档化；新系统登记为 `Object.CustomMask/CharacterId/PartId/Flags` |
| 旧 compatibility path | 仅验证参考，不进入新长期目标 |

---

## 6. 第一版正式 Feature 名

| 新 Feature | 旧参考 | Domain | 说明 |
| --- | --- | --- | --- |
| `AovOutput` | `HoAovRendererFeature` | GeometryDomain / MaterialDomain / ShadingDomain | 负责 AOV 资源生产；内部 pass 可拆成几何语义和着色语义 |
| `SubsurfaceScattering` | `HoSubsurfaceScatteringRendererFeature` | ShadingDomain / CompositeDomain | 屏幕空间 SSS，消费 AOV |
| `WeightedOit` | `WeightedOITRendererFeature` | CompositeDomain | 透明 accumulation 和 composite |
| `ShadowCast` | `HoShadowCastRendererFeature` | ShadowDomain | 自定义 shadow atlas，仍需材质 forward 接收 |
| `CharacterSpecialization` | `HoCharacterSpecializationRendererFeature` | CompositeDomain | 眼透、前发投影、角色捕获 |
| `SemanticPostProcess` | `HoPostProcessRendererFeature` | CompositeDomain | 语义感知后处理 |
| `ImagePostProcess` | `ShoostPostProcessRendererFeature` | ImageDomain | 最终图像风格栈 |
| `PlanarReflection` | `LILPlanarReflectionSurface` | ImageDomain / MaterialDomain | 平面反射，不等同 SSR |
| `DebugComposite` | 各旧 debug pass | DebugDomain | 统一 debug view/overlay/capture |

---

## 7. 第一版 Pass 阶段词汇

| 阶段 | 含义 |
| --- | --- |
| `FrameCameraInit` | per-frame/per-camera 状态 reset 和 imported resource 注册 |
| `ObjectSemanticBinding` | Renderer/MaterialPropertyBlock/UserValue 写入对象语义 |
| `ShadowLightingPrepass` | 自定义 shadow atlas、light/slice 数据 |
| `GeometrySemanticAov` | depth、normal、tangent、coverage 等几何语义 |
| `OpaqueShading` | 不由第一阶段实现，但作为时序锚点 |
| `MaterialShadingSemanticAov` | 材质、SSS source、custom 等着色/材质语义 |
| `ScreenSss` | SSS source/diffusion/transmission/composite |
| `TransparentOit` | OIT opaque copy、accumulation、composite |
| `CharacterComposite` | 角色捕获、眼透、前发投影 |
| `SemanticPost` | HoPost-like 语义后处理 |
| `ImagePost` | Shoost-like final image stack |
| `DebugComposite` | debug replace/overlay/split/capture |
| `FinalOutput` | 输出到 camera color / backbuffer |

---

## 8. 审查结论固化

- `HoAOV` 不作为新模块名；新模块名为 `AovOutput`。
- `HoPost` / `Shoost` 新契约中分别命名为 `SemanticPostProcess` / `ImagePostProcess`。
- `CharacterSpecialization` 归 `CompositeDomain`。
- `ShadowCast` 归 `ShadowDomain`。
- `Thickness`、`Curvature`、`Utility` 第一版归 `MaterialDomain`。
- `ObjectCustom0-7` 第一版保留命名，不急于重命名成具体角色部件。
- `Material.Custom0-3` 第一版进入正式能力。
- `RSUV` 并入 Object semantic，不作为独立 Domain。
- Debug overlay 和 Debug capture 第一阶段进入契约。
> 补充命名边界：RP 公共契约不以旧实现来源命名；`HoNpr` 材质组分例外，凡仍以旧实现算法为行为基线的 Feature Block、entry、DebugView、shader property 和 UI 标签必须带来源后缀，例如 `SecondaryMatCapLilToon`、`GlitterLilToon`、`_HoNprGlitterLilToonColor`。来源后缀只标记迁移责任，不允许污染 Semantic / Resource / Feature ABI。
