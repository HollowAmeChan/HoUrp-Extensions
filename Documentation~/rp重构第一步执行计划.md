# RP 重构第一步执行计划

> 第一步目标：定义新 RP 的核心契约，并盘点旧 ABI 作为迁移参照。  
> 本文是执行大纲，供审查。审查通过后，再按本文拆出的表格和产物逐项落地。

---

## 0. 必读前置

执行本计划前，先读：

- `Documentation~/rp设计哲学底线.md`
- `Documentation~/rp重构初步大纲.md`
- `Documentation~/旧实现快速定位索引.md`

这一步必须遵守的核心边界：

- 新 RP 不承接旧 `lilToon/lilPBR` 材质系统。
- 旧 `lilToon-URP-Extensions` 只作为能力来源、行为基线和迁移参照。
- 不建立长期旧材质 `Bridge`。
- 新契约先于新材质系统。
- 先定义语义、资源、Feature、Pass、Debug、Capability，再迁移代码。
- 当前阶段不写 runtime 实现代码。

---

## 1. 第一阶段的真实目标

第一步不是“先把 HoAOV 搬过来”，也不是“先把旧 RendererFeature 复制到新包”。

第一步要产出一套可审查、可执行、可约束后续代码迁移的 **契约包**：

1. 新 RP 的语义命名草案。
2. 新 RP 的资源命名草案。
3. Feature Descriptor 草案。
4. Pass 时机和依赖顺序草案。
5. Debug View 草案。
6. Capability 模型草案。
7. 旧 ABI 盘点表。
8. 新旧差异表。
9. 第一轮迁移验收标准。

这些产物是后续实现的“合同”。后续任何代码迁移、材质接入、RenderGraph pass 编写，都应该先对齐这些合同。

---

## 2. 本阶段不做什么

明确不做：

- 不迁移旧 `HoAovRendererFeature.cs`。
- 不复制旧 shader。
- 不新增运行时 RendererFeature。
- 不设计新材质 inspector。
- 不改 `lilToon` / `lilPBR`。
- 不把旧 `_lilHoAov*`、`_HoAov*`、`lilToonOIT` 等命名直接定为新 ABI。
- 不把旧 compatibility path 纳入新包长期目标。

允许做：

- 读旧代码。
- 建表。
- 命名。
- 定义新契约。
- 写文档。
- 写后续实现任务拆解。
- 标记旧行为：保留、替代、删除、延后。

---

## 3. 建议新增的第一阶段文档产物

第一阶段建议最终形成以下文档。可以先集中写在一个大文件夹里，审查后再拆分。

```text
Documentation~/rp重构第一步/
├── rp重构第一步执行计划.md                 # 本文
├── rp核心契约草案.md                       # 新 RP 正式契约草案
├── rp旧ABI盘点表.md                        # 旧系统接口事实清单
├── rp新旧差异与迁移判定表.md               # 保留/替代/删除/延后
├── rp语义注册表草案.md                     # Semantic Registry 草案
├── rp资源注册表草案.md                     # Resource Registry 草案
├── rpFeatureDescriptor草案.md              # Feature 声明草案
├── rpPass时机与依赖基线.md                 # Pass timeline / dependency
├── rpDebugView注册草案.md                  # Debug 视图草案
└── rpCapability模型草案.md                 # Capability 归属和 UI 输入草案
```

如果文档过多，可以第一轮合并成四份：

```text
rp核心契约草案.md
rp旧ABI盘点表.md
rp新旧差异与迁移判定表.md
rp第一阶段验收清单.md
```

推荐先用四份，避免文档数量过早膨胀。

---

## 4. 执行顺序总览

```text
Step 1. 固定术语和命名原则
Step 2. 盘点旧 ABI
Step 3. 反推新语义注册表
Step 4. 反推新资源注册表
Step 5. 建立 Feature Descriptor 草案
Step 6. 建立 Pass 时机与依赖基线
Step 7. 建立 Debug View 草案
Step 8. 建立 Capability 草案
Step 9. 写新旧差异与迁移判定
Step 10. 审查与冻结第一版契约
```

每一步都只产出文档，不写运行时代码。

---

## 5. Step 1：固定术语和命名原则

### 5.1 目标

先把后续所有文档使用的术语固定下来，避免同一个概念在不同文档里叫不同名字。

### 5.2 必须固定的术语

Domain：

- `ObjectDomain`
- `MaterialDomain`
- `GeometryDomain`
- `DeformationDomain`
- `ShadingDomain`
- `LightingDomain`
- `ImageDomain`
- `CompositeDomain`
- `DebugDomain`
- `CapabilityDomain`

核心对象：

- `Semantic`
- `Resource`
- `Feature`
- `Pass`
- `Producer`
- `Consumer`
- `Capability`
- `DebugView`
- `LegacyInterop`

数据生命周期：

- `Static`
- `PerRenderer`
- `PerMaterial`
- `PerFrame`
- `PerCamera`
- `PerPass`
- `Transient`
- `Persistent`

### 5.3 命名原则

新命名原则：

- 新 RP 命名不以 `lil`、`lilToon`、`lilPBR` 为前缀。
- 新 RP 不沿用旧 `_lilHoAov*` 作为正式资源名。
- 新 RP 的命名应该表达 Domain 和用途，而不是旧实现来源。
- shader binding 名可以晚于资源逻辑名确定。
- 逻辑资源名、shader property 名、debug display name 要分开记录。

示例：

| 类型 | 推荐风格 | 说明 |
| --- | --- | --- |
| 逻辑语义名 | `Object.CharacterId` | 注册表内部名 |
| 逻辑资源名 | `Aov.MaskId` | RenderGraph / Resource Registry 名 |
| shader property | `_HoAovMaskIdTexture` 或新名待定 | 可以和逻辑资源分离 |
| debug name | `AOV / Mask ID` | 面向 UI |

### 5.4 产物

产物写入：

- `rp核心契约草案.md`

最少要包含：

- 术语表。
- 命名原则。
- 新旧命名隔离规则。
- 旧命名是否允许临时映射。

### 5.5 审查问题

审查时需要回答：

- Domain 是否足够。
足够
- 是否有 Domain 重叠。
没有
- 是否允许保留 `HoAOV` 作为模块名，还是改成 `AOV` / `SemanticAOV`。
改名为SemanticAOV
- shader property 是否第一版就重命名，还是先建立逻辑名和旧 binding 的映射。
直接重命名，映射可以写进md文档

---

## 6. Step 2：盘点旧 ABI

### 6.1 目标

把旧系统的事实接口列清楚。这里是事实盘点，不是未来设计。

### 6.2 盘点范围

旧 RP 扩展：

- HoAOV
- HoSSS
- Weighted OIT
- HoShadowCast
- HoCharacterSpecialization
- HoPost
- Shoost
- Planar Reflection

旧材质：

- `lilToon`
- `lilPBR`

### 6.3 旧 ABI 分类

至少分成这些类别：

- `LightMode`
- 全局 texture
- 全局 buffer / array
- 全局 scalar/vector/matrix
- 材质 property
- renderer user value
- MaterialPropertyBlock
- include 路径
- shader pass include
- Volume component
- RenderPassEvent
- RenderGraph ContextItem
- debug mode

### 6.4 表格模板

```text
| 旧 ABI | 类型 | 定义位置 | 写入者 | 读取者 | 用途 | 生命周期 | 新 RP 判定 | 备注 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
```

`新 RP 判定` 只能填：

- `保留概念，重命名`
- `保留概念，旧名临时映射`
- `替代`
- `删除`
- `延后`
- `仅迁移验证`

### 6.5 必查入口

从这些文件开始：

- `D:\Unity_Fork\lilToon-URP-Extensions\Runtime\AOV\HoAovShaderConstants.cs`
- `D:\Unity_Fork\lilToon-URP-Extensions\Runtime\OIT\WeightedOITShaderConstants.cs`
- `D:\Unity_Fork\lilToon-URP-Extensions\Runtime\ShadowCast\HoShadowCastShaderConstants.cs`
- `D:\Unity_Fork\lilToon-URP-Extensions\Runtime\CharacterSpecialization\HoCharacterSpecializationShaderConstants.cs`
- `D:\Unity_Fork\lilToon-URP-Extensions\Runtime\SubsurfaceScattering\HoSubsurfaceScatteringShaderConstants.cs`
- `D:\Unity_Fork\lilToon\Assets\lilToon\Shader\Includes\lil_pass_hoaov.hlsl`
- `D:\Unity_Fork\lilPBR\Shaders\hoaov.hlsl`
- `D:\Unity_Fork\lilPBR\Shaders\unity_urp.hlsl`

### 6.6 建议查询

```powershell
rg -n "public const string|Shader.PropertyToID|ShaderTagId|GlobalKeyword" D:\Unity_Fork\lilToon-URP-Extensions\Runtime
rg -n "LightMode|HoAOV|HoAOVSSS|HoCharacterCapture|lilToonOIT" D:\Unity_Fork\lilToon D:\Unity_Fork\lilPBR
rg -n "_lilHoAov|_HoAov|_HoSSS|_lilOIT|_HoShadowCast|_LILPBRPlanarReflection" D:\Unity_Fork\lilToon-URP-Extensions D:\Unity_Fork\lilToon D:\Unity_Fork\lilPBR
```

### 6.7 产物

产物写入：

- `rp旧ABI盘点表.md`

### 6.8 审查问题

审查时需要回答：

- 哪些旧 ABI 只是 shader binding。
- 哪些旧 ABI 是真正架构概念。
- 哪些旧 ABI 是为了旧材质服务，未来应该删除。
- 哪些旧 ABI 需要短期迁移映射。

---

## 7. Step 3：反推新语义注册表

### 7.1 目标

把旧 HoAOV、HoPost rule、HoSSS input、HoCharacter input 里实际用到的语义抽出来，整理成新 `Semantic Registry` 草案。

### 7.2 初始语义来源

从这些旧资源反推：

- `_lilHoAovMaskIdTexture`
- `_lilHoAovNormalDepthTexture`
- `_lilHoAovTangentNormalTexture`
- `_lilHoAovSurfaceDataTexture`
- `_lilHoAovCustom0_3Texture`
- `_lilHoAovObjectCustom0_3Texture`
- `_lilHoAovObjectCustom4_7Texture`
- `_lilHoAovSssTexture`

从这些消费者反推：

- HoPost AOV rule source。
- HoSSS mask / profile / thickness / normal-depth。
- HoCharacter eye / face / front hair / object custom 位。
- Shoost AOV composite。

### 7.3 表格模板

```text
| Semantic | Domain | 旧来源 | 旧编码位置 | Producer | Consumer | 阶段 | 精度 | DebugView | 新 RP 判定 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
```

### 7.4 第一批候选语义

ObjectDomain：

- `Object.Id`
- `Object.GroupId`
- `Object.Flags`
- `Object.CharacterId`
- `Object.PartId`
- `Object.CustomMask`
- `Object.Custom0`
- `Object.Custom1`
- `Object.Custom2`
- `Object.Custom3`
- `Object.Custom4`
- `Object.Custom5`
- `Object.Custom6`
- `Object.Custom7`

MaterialDomain：

- `Material.Class`
- `Material.SssProfile`
- `Material.Custom0`
- `Material.Custom1`
- `Material.Custom2`
- `Material.Custom3`

GeometryDomain：

- `Geometry.LinearDepth`
- `Geometry.WorldNormal`
- `Geometry.ViewNormal`
- `Geometry.TangentNormal`
- `Geometry.Coverage`

ShadingDomain：

- `Shading.SssWeight`
- `Shading.SssSourceColor`
- `Shading.Thickness`
- `Shading.Curvature`
- `Shading.Utility`

CompositeDomain：

- `Composite.CharacterEye`
- `Composite.CharacterFace`
- `Composite.CharacterFrontHair`
- `Composite.EyeRevealArea`

### 7.5 产物

产物写入：

- `rp语义注册表草案.md`

### 7.6 审查问题

审查时需要回答：

- `Thickness / Curvature / Utility` 应归 `MaterialDomain` 还是 `ShadingDomain`。
归属于MaterialDomain
- `ObjectCustom0-7` 是否保留这个命名，还是改成角色语义名。
保留命名
- `Material.Custom0-3` 是否属于第一版正式能力，还是仅迁移验证。
属于，因为shader的hoaovpass已经有能力直接写入了，后面接入会很快，并且他是有用的
- `RSUV` 是独立概念，还是并入 Object / Geometry semantic。
并入Object semantic，因为这个是perOBj的，没有到perVertex的程度

---

## 8. Step 4：反推新资源注册表

### 8.1 目标

把旧 RT / buffer / atlas / composite source 整理成新 `Resource Registry` 草案。

资源注册表描述的是“帧内实际存在的数据载体”，不是语义本身。

### 8.2 表格模板

```text
| Resource | ResourceKind | Semantic | Producer Feature | Consumer Feature | Format | Scale | Lifetime | Clear Policy | DebugView | 旧资源名 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
```

### 8.3 第一批候选资源

AOV：

- `Aov.MaskId`
- `Aov.NormalDepth`
- `Aov.TangentNormal`
- `Aov.SurfaceData`
- `Aov.MaterialCustom0_3`
- `Aov.ObjectCustom0_3`
- `Aov.ObjectCustom4_7`
- `Aov.SssSource`
- `Aov.Depth`

SSS：

- `Sss.Source`
- `Sss.Diffusion`
- `Sss.Transmission`
- `Sss.TransmissionTemp`
- `Sss.CompositeSource`

OIT：

- `Oit.Accumulation`
- `Oit.Revealage`
- `Oit.OpaqueColor`
- `Oit.CompositeSource`

Shadow：

- `ShadowCast.Atlas`
- `ShadowCast.SecondDirectionalAtlas`
- `ShadowCast.LightData`
- `ShadowCast.SliceData`

Character：

- `Character.EyeColor`
- `Character.EyeData`
- `Character.CaptureDepth`
- `Character.CompositeSource`

Post / Image：

- `HoPost.LayerTempA`
- `HoPost.LayerTempB`
- `Shoost.LayerTempA`
- `Shoost.LayerTempB`
- `Shoost.AovCompositeMask`

Reflection：

- `Reflection.PlanarColor`
- `Reflection.PlanarMatrix`
- `Reflection.PlanarParams`

### 8.4 产物

产物写入：

- `rp资源注册表草案.md`

### 8.5 审查问题

审查时需要回答：

- 哪些资源应该是 RenderGraph transient。
具体情况具体分析，可以先自行决定，只要留档写进文档就好
- 哪些资源需要跨 pass 暴露为 global shader binding。
具体情况具体分析，可以先自行决定，只要留档写进文档就好
- 哪些旧全局 RT 在新系统里应改成明确的 Resource Handle。
具体情况具体分析，可以先自行决定，只要留档写进文档就好
- 哪些资源需要支持 half/quarter render scale。
具体情况具体分析，可以先自行决定，只要留档写进文档就好
- 哪些资源需要 Debug View。
几乎所有的东西都得有view的能力

---

## 9. Step 5：Feature Descriptor 草案

### 9.1 目标

每个 Feature 必须能被查询，而不是只有执行逻辑。

### 9.2 Descriptor 模板

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

### 9.3 第一批 Feature

- `AovOutput`
- `SubsurfaceScattering`
- `WeightedOit`
- `ShadowCast`
- `CharacterSpecialization`
- `SemanticPostProcess`
- `ImagePostProcess`
- `PlanarReflection`
- `DebugComposite`

### 9.4 表格模板

```text
| Feature | Domain | Stage | Produces | Consumes | Shader Pass | Resources | Debug | Legacy File | Migration Decision |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
```

### 9.5 产物

产物写入：

- `rpFeatureDescriptor草案.md`

### 9.6 审查问题

审查时需要回答：

- HoPost / Shoost 是否分别命名为 `SemanticPostProcess` / `ImagePostProcess`。
对
- HoAOV 是否命名为 `AovOutput`、`SemanticAov` 还是保留 `HoAOV` 作为模块名。
命名为AovOutput
- CharacterSpecialization 是否属于 CompositeDomain。
属于
- ShadowCast 是 LightingDomain 还是独立 ShadowDomain。
独立的ShadowDomain

---

## 10. Step 6：Pass 时机与依赖基线

### 10.1 目标

把旧系统事实顺序写清楚，再定义新 RP 第一版目标顺序。

旧顺序是迁移基线，不是最终锁死。

### 10.2 旧事实基线

按当前旧仓库：

```text
Per-camera reset
Object semantic binding
HoShadowCast
Opaque / Forward / GBuffer
HoAOV
HoSSS
Transparent / OIT
HoCharacterSpecialization
HoPost
Shoost
Debug
Final Output
```

### 10.3 新 RP 第一版候选顺序

第一版可先定义为：

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

### 10.4 依赖表模板

```text
| Pass | Before | After | Produces | Consumes | Can Move Earlier | Can Move Later | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
```

### 10.5 产物

产物写入：

- `rpPass时机与依赖基线.md`

### 10.6 审查问题

审查时需要回答：

- HoAOV 是否拆成 `GeometrySemanticAov` 与 `ShadingSemanticAov`。
拆
- HoSSS 是否固定在 transparent 前。
你自己判读吧我不好说
- HoCharacterSpecialization 是否应该在 transparents 后，还是部分捕获需要提前。
因为眉毛可能半透，你看怎么搞吧
- OIT opaque copy 和 accumulation 的相对时机是否沿用旧实现。
暂时先沿用
- ShadowCast 是否继续材质 forward 接收，还是未来改屏幕空间 composite。
屏幕空间已经有Htrace了（这个暂时不属于我们的体系，属于锦上添花，以后要合并的时候我还需要重新开仓库），ShadowCast还是需要材质去接收

---

## 11. Step 7：Debug View 草案

### 11.1 目标

把 debug 提升成注册系统，而不是各 Feature 自己做临时 debug mode。

### 11.2 DebugView 模板

```text
| DebugView | Domain | Source Resource | Source Semantic | Producer | Display Mode | Range | Owner Feature | Legacy Reference |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
```

### 11.3 第一批 DebugView

AOV：

- `AOV.Mask`
- `AOV.Id`
- `AOV.Flags`
- `AOV.LinearDepth`
- `AOV.WorldNormal`
- `AOV.ViewNormal`
- `AOV.TangentNormal`
- `AOV.Thickness`
- `AOV.Curvature`
- `AOV.Material`
- `AOV.Utility`
- `AOV.Custom0`
- `AOV.Custom1`
- `AOV.Custom2`
- `AOV.Custom3`
- `AOV.ObjectCustom0-7`
- `AOV.SssSource`

SSS：

- `SSS.Mask`
- `SSS.Source`
- `SSS.Diffusion`
- `SSS.Transmission`
- `SSS.CompositeWeight`
- `SSS.ProfileId`
- `SSS.Thickness`

Shadow：

- `ShadowCast.Atlas`
- `ShadowCast.SecondDirectionalAtlas`
- `ShadowCast.LightSlices`

Character：

- `Character.EyeColor`
- `Character.EyeData`
- `Character.HairShadow`
- `Character.EyeReveal`

Post：

- `HoPost.AovMask`
- `HoPost.LayerOutput`
- `Shoost.AovCompositeMask`
- `Shoost.LayerOutput`

### 11.4 产物

产物写入：

- `rpDebugView注册草案.md`

### 11.5 审查问题

审查时需要回答：

- DebugView 是否统一由 DebugDomain 管。
对
- 每个 Feature 是否必须声明至少一个 debug view。
对，至少有一个全白的告诉用户他启用了
- Debug overlay 是否第一阶段就进入契约。
直接进
- Debug capture 是否需要作为独立功能。
需要

---

## 12. Step 8：Capability 模型草案

### 12.1 目标

替代旧 Layer/Tag/材质开关散落的隐式分类方式。

### 12.2 Capability 分类

Object Capability：

- `ReceivesSemanticPost`
- `ReceivesCharacterComposite`
- `WritesAov`
- `WritesObjectCustom`
- `CastsHoShadow`
- `ReceivesHoShadow`
- `ParticipatesOit`

Material Capability：

- `WritesMaterialSemantic`
- `WritesSssSource`
- `SupportsSss`
- `SupportsOit`
- `SupportsPlanarReflection`
- `SupportsCharacterCapture`

Light Capability：

- `CastsHoShadow`
- `UsesSecondDirectionalAtlas`
- `SupportsPcss`

Feature Capability：

- `RequiresAov`
- `RequiresDepth`
- `RequiresNormal`
- `RequiresMotion`
- `SupportsHalfResolution`
- `SupportsDebugView`

### 12.3 表格模板

```text
| Capability | Domain | Owner | Data Source | Affects | Default | UI Surface | Legacy Reference | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
```

### 12.4 产物

产物写入：

- `rpCapability模型草案.md`

### 12.5 审查问题

审查时需要回答：

- `HoAovSubject` / `HoAovGroup` 的能力是否转成 Object Capability UI。
转吧
- 材质 Capability 是否由新材质 preset 声明，而不是 inspector 临时决定。
由材质决定，inspector这边只接受物体这种列表，材质不要做
- OIT / SSS / PlanarReflection 是 Material Capability 还是 Feature Capability。
 Material Capability
- Light Capability 是否第一版就纳入。
先占位，具体内容可以不做

---

## 13. Step 9：新旧差异与迁移判定

### 13.1 目标

对每个旧能力给出新 RP 的处理方式。

### 13.2 判定枚举

只允许使用以下判定：

- `KeepConceptRename`
- `KeepConceptTemporaryLegacyBinding`
- `Replace`
- `Remove`
- `Defer`
- `ValidationOnly`

### 13.3 表格模板

```text
| Old Item | Old Type | Current Behavior | New Concept | Decision | Required Work | Risk | Review Owner |
| --- | --- | --- | --- | --- | --- | --- | --- |
```

### 13.4 示例

| Old Item | Old Type | Current Behavior | New Concept | Decision |
| --- | --- | --- | --- | --- |
| `_lilHoAovMaskIdTexture` | Global Texture | HoAOV mask/id output | `Aov.MaskId` resource | `KeepConceptRename` |
| `_HoAovMaskWeight` | Material Property | material/object AOV weight | semantic write weight | `Replace` |
| `lilToonOIT` | LightMode | OIT accumulation pass | transparent accumulation pass | `KeepConceptRename` |
| `HoAOV` | LightMode | AOV material pass | semantic AOV producer pass | `KeepConceptRename` |
| `HoAovSubject` | Component | writes MPB semantics | object capability authoring | `KeepConceptRename` |
| old compatibility path | Runtime path | non-RenderGraph execution | migration reference | `ValidationOnly` |

### 13.5 产物

产物写入：

- `rp新旧差异与迁移判定表.md`

### 13.6 审查问题

审查时需要回答：

- 哪些旧能力必须保持视觉行为一致。
旧的都是验证过的，都尽量搬过来，视觉一致不是你决定的而是我看了对比出来的
- 哪些旧能力只保留概念。
- 哪些旧命名完全删除。
命名都可以改，但是要留档
- 哪些旧材质属性不再进入新系统。
具体情况具体分析

---

## 14. Step 10：第一版契约审查与冻结

### 14.1 冻结条件

第一版契约冻结前，必须满足：

- 每个第一批 Feature 都有 Descriptor。
- 每个第一批 Resource 都有 Producer 和 Consumer。
- 每个第一批 Semantic 都有 Domain。
- 每个旧 ABI 都有迁移判定。
- 每个 pass 都有相对时机。
- 每个 debug view 都有来源。
- 每个 Capability 都有 owner。

### 14.2 冻结产物

冻结后应产生：

```text
Documentation~/rp第一阶段验收清单.md
```

验收清单包含：

- 文档是否完整。
- 是否有未判定旧 ABI。
- 是否有无 Producer 的 Resource。
- 是否有无 Consumer 的 Resource。
- 是否有跨 Domain 归属不清的 Semantic。
- 是否有仍然依赖旧材质属性的设计。
- 是否有违反 `rp设计哲学底线.md` 的项。

### 14.3 冻结后的下一步

冻结后才进入第二阶段：

- 创建新 runtime contract 类型。
- 建 Resource Registry 基础结构。
- 建 Feature Descriptor 基础结构。
- 建 RenderGraph resource declaration 工具。
- 选择第一个最小 Feature 做迁移试点。

推荐第二阶段试点：

1. `Aov.MaskId / NormalDepth` 的最小输出链路。
2. `DebugView` 最小显示链路。
3. 一个只读 AOV 的最小 HoPost-like effect。

不推荐第二阶段一开始就迁移：

- 完整 Shoost。
- 完整 HoSSS。
- 完整 HoShadowCast。
- 完整旧材质 pass。

---

## 15. 第一阶段审查清单

审查本文时，请重点看这些问题：

- 第一阶段是否足够聚焦，没有提前进入实现。
- 新契约是否真的先于材质系统。
- 旧 ABI 是否被放在迁移参照位置，而不是新核心位置。
- 文档产物数量是否合适。
- 表格字段是否足够支撑后续实现。
- 第一批 Semantic / Resource / Feature 是否过多或过少。
- 哪些审查问题必须在第一阶段解决，哪些可以延后。

---

## 16. 推荐执行节奏

建议按 5 个小批次推进，每批都可以单独审查。

### Batch A：基础定义

产物：

- `rp核心契约草案.md`
- Domain 术语。
- 命名原则。
- 旧名隔离规则。

### Batch B：旧 ABI 事实盘点

产物：

- `rp旧ABI盘点表.md`

范围：

- HoAOV
- HoSSS
- OIT
- HoShadowCast
- HoCharacter
- HoPost
- Shoost
- PlanarReflection
- lilToon / lilPBR 接入点

### Batch C：新注册表草案

产物：

- `rp语义注册表草案.md`
- `rp资源注册表草案.md`

范围：

- AOV 相关先做。
- SSS / OIT / Shadow / Character 先填候选。

### Batch D：Feature / Pass / Debug / Capability

产物：

- `rpFeatureDescriptor草案.md`
- `rpPass时机与依赖基线.md`
- `rpDebugView注册草案.md`
- `rpCapability模型草案.md`

### Batch E：差异判定和冻结

产物：

- `rp新旧差异与迁移判定表.md`
- `rp第一阶段验收清单.md`

---

## 17. 第一阶段完成定义

第一阶段完成时，应该能回答：

- 新 RP 第一版有哪些 Domain。
- 新 RP 第一版有哪些 Semantic。
- 新 RP 第一版有哪些 Resource。
- 每个 Resource 谁写、谁读。
- 每个 Feature 的输入输出是什么。
- 每个旧 ABI 在新系统里是保留、替代、删除还是仅验证。
- 哪些旧能力第一轮迁移必须视觉一致。
- 哪些旧能力可以推迟。
- 新材质系统以后需要实现哪些 producer/consumer 接口。

如果这些问题答不上来，就不能进入代码迁移。

---

## 18. 最重要的底线

第一阶段做的是 **契约冻结**，不是 **代码搬家**。

只有当契约足够清楚，后续 RenderGraph pass、资源系统、Debug 系统和新材质系统才不会再次被旧项目结构牵着走。
> 补充命名边界：RP 的 Semantic / Resource / Feature 公共 ABI 仍然按 Domain 和用途命名，不表达旧实现来源；但 `HoNpr` 材质 Feature Block、entry、DebugView、shader property 和 UI 标签如果仍以旧实现算法为行为基线，必须带来源后缀，例如 `GlitterLilToon`、`_HoNprGlitterLilToonColor`。这是迁移责任标记，不是 ABI 继承。
