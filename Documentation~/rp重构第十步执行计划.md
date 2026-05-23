# RP 重构第十步执行计划

> 第十步目标：在 AOV 生命周期、RSUV 静态对象语义、Capability UI 和 Debug view 已经形成最小闭环之后，开始准备 **新材质系统接入 RP 的第一版契约**。
>
> 本阶段不是完整 `HoNpr` 统一材质包，也不是迁移旧 `lilToon` / `lilPBR`。重点是冻结 **材质语义 ABI、shader pass 契约、SurfaceData / AOV 输出接口、OIT-ready 透明输出接口、Feature Block / Preset 的最小数据模型，以及一个可验证的 generated shader 原型路径**。

---

## 0. 前置状态

当前新包已经具备：

- `HoUrpBuiltInContracts`：登记资源、语义、feature、debug view。
- `AovOutputRendererFeature`：生成 `Aov.MaskId`、`Aov.NormalDepth`、`Aov.SurfaceData`、`Aov.MaterialCustom0_3`、`Aov.SssSource` 等 AOV 资源。
- `ObjectSemanticAuthoring`：能通过 RSUV / MPB 写入对象静态语义。
- `MaterialSemanticAuthoring`：能通过 MPB 写入材质语义和 SSS 输入。
- `SubsurfaceScatteringRendererFeature`：消费 AOV SSS 输入并生成 `Sss.Source` / `Sss.Diffusion`。
- `SemanticPostProcessRendererFeature`：消费 AOV / SSS，生成 `SemanticPost.Mask`。
- `AovDebugRendererFeature`：能观察 `AOV.ObjectFlag0-12`、object custom、material custom、SSS 和 SemanticPost 结果。
- 第九步已经明确 `Aov.MaskId.b` 同时承载 `Object.GroupId` 低 3 位和 `ObjectFeatureFlags.bit8-12`。

也就是说，RP 侧已经有了材质系统需要对接的目标：

```text
Generated / authored material
  -> material semantic
  -> AOV resources
  -> SSS / SemanticPost / Debug
```

第十步还必须给后续 Weighted OIT 铺路：最小 shader 侧要先具备透明/OIT accumulation pass、alpha / weight / coverage 输出和 `SupportsOit` / `ParticipatesOit` 元数据。后续透明阶段才能直接测试 OIT runtime，而不是回头补材质 pass。

第十步要解决的是：新材质系统应该怎样稳定地产生这些语义和透明输出，而不是继续依赖临时 `MaterialSemanticAuthoring` 或旧材质包的属性/keyword。

---

## 1. 为什么第十步进入材质契约

前九步已经把新 RP 的核心链路做成了可查询、可调试的资源系统：

```text
Object / Material Authoring
  -> AOV Output
  -> SSS
  -> SemanticPost
  -> Debug AllRegistered
```

如果继续只扩展 RenderFeature，会出现一个新的瓶颈：

- AOV 中的材质语义仍主要来自 `MaterialSemanticAuthoring` 这种过渡组件。
- SSS source、material class、profile、thickness、curvature、material custom 还没有正式 shader 生产者。
- 新材质系统还没有明确应实现哪些 pass、include、数据结构和输出函数。
- 旧 `lilToon/lilPBR` 已经证明能力可行，但它们的 UI、keyword、pass 名和属性名不能成为新 ABI。

因此第十步先做材质系统的 **接入契约和最小原型**，再进入完整材质生成器、UI 和大规模 shader feature。

---

## 2. 本阶段范围

### 做

- 定义第一版材质侧 shader ABI：
  - `ObjectSemanticData`
  - `SurfaceData`
  - `MaterialSemanticData`
  - `AovOutputData`
  - `TransparentOutputData`
  - `OitAccumulationData`
  - pass 输入 / 输出边界
- 明确材质 shader 需要生产哪些 RP 语义：
  - `Material.Class`
  - `Material.SssProfile`
  - `Material.Thickness`
  - `Material.Curvature`
  - `Material.Custom0-3`
  - `Sss.Source`
  - `Sss.Weight`
- 明确对象静态语义仍来自 `ObjectSemanticAuthoring` / RSUV / MPB，不由材质 shader 私自定义。
- 对象静态语义应通过独立 object semantic shader ABI 被材质 pass 消费；generated material 的 Properties 面板不暴露对象/RSUV 字段。
- 固化第一版 pass 契约：
  - AOV output pass
  - forward pass 占位契约
  - OIT accumulation pass 占位契约
  - depth / shadow pass 只做边界说明，不做完整迁移
- 给 prototype preset 增加 OIT-ready 元数据：
  - `SupportsOit`
  - `ParticipatesOit`
  - OIT accumulation pass name
  - normal forward skip / phase policy
- 建立 Feature Block / Preset 的最小描述模型。
- 建一个完全独立的最小 generated shader 原型，用来证明：
  - pass、属性、include、AOV 编码都由新 HoURP ABI 控制。
  - 不依赖旧 `lilToon/lilPBR` include。
  - 不继承 URP Lit 的 keyword、Inspector 和属性体系。
  - 能写入新 `Aov.*` 资源。
  - 能被 SSS / SemanticPost / Debug 正确消费。
  - 能提供独立 OIT accumulation pass，供后续 Weighted OIT runtime 直接绘制。
- 更新 contract / docs，标记材质语义可以来自 `GeneratedMaterial` producer，而不是只能来自 `MaterialSemanticAuthoring`。
- 补测试：
  - ABI 名称稳定。
  - preset / feature block 描述可查询。
  - generated shader 不引用旧 ABI。
- AOV / SSS / debug 绑定名仍使用 `_HoUrp*` 新命名。
- AOV runtime 必须同时支持 fallback overrideMaterial 路径和 generated material 自己的 explicit `HoUrpAovOutput` pass，后者不能只限 opaque queue。

### 不做

- 不迁移旧 `lilToon` shader。
- 不迁移旧 `lilPBR` shader。
- 不接旧 inspector。
- 不建立长期 legacy material bridge。
- 不实现完整 PBR / NPR 光照模型。
- 不做 Weighted OIT RenderGraph runtime。
- 不做 OIT composite。
- 不做完整透明排序 / 折射 / 透明 SSS。
- 不做 HoShadow receiver 的完整材质对接。
- 不做完整 ShaderGraph 集成。
- 不做运行时动态 shader 组装。
- 不做厚材质 UI。
- 不让 UI 决定 shader pass 结构。
- 不把旧 `_lilHoAov*`、`_HoAov*`、`_lilOIT*`、`_HoShadowStrength` 等旧属性提升为新 ABI。

---

## 3. 包边界

`HoUrp-Extensions` 负责定义 RP 契约：

```text
Runtime/
  Core/
  Resources/
  Semantic/
  Capability/
  Features/
  Shaders/
```

第十步中，`HoUrp-Extensions` 可以提供：

- 公共 HLSL ABI include。
- AOV / SSS / SemanticPost 的 shader binding 名。
- Feature / Resource / Semantic registry。
- 测试用最小材质 shader。
- generated shader 原型需要遵守的模板规则。

但长期完整材质包应属于：

```text
HoNpr unified material system
HoToon as lightweight legacy reference
```

不能让 `HoUrp-Extensions` 变成旧材质包的附属适配层，也不能让材质包反过来定义 RP 资源名。

---

## 4. 材质语义 ABI 第一版

第十步建议先定义材质 shader 内部统一数据，而不是先做 UI。

### 4.1 `SurfaceData`

`SurfaceData` 描述材质本身和 forward shading 所需的基础表面信息。

第一版建议字段：

| Field | 说明 | AOV 关系 |
| --- | --- | --- |
| `baseColor` | 基础色 | 可进入 `Sss.Source` |
| `alpha` | 透明 / coverage | 后续透明与 depth 使用 |
| `normalTS` | 切线空间法线 | 可写 `Aov.TangentNormal` |
| `normalWS` | 世界法线 | 可写 `Aov.NormalDepth` |
| `roughness` | 粗糙度 | 第一版不强制进入 AOV |
| `metallic` | 金属度 | 第一版不强制进入 AOV |
| `emission` | 自发光 | 后续 composite 使用 |
| `occlusion` | AO | 后续 shading / composite 使用 |

第十步不要求一次性使用全部字段，但结构必须先稳定，避免 feature block 之间各自发明数据。`alpha` 在第十步必须能被 forward、AOV 和 OIT accumulation pass 读取，避免后续透明阶段再改 SurfaceData ABI。

### 4.2 `MaterialSemanticData`

`MaterialSemanticData` 描述材质参与 RP 语义系统的部分。

第一版建议字段：

| Field | 对应语义 | AOV 映射 |
| --- | --- | --- |
| `materialClass` | `Material.Class` | `Aov.SurfaceData.b` 或当前 contract 指定位置 |
| `sssProfile` | `Material.SssProfile` | `Aov.SurfaceData.g` 或当前 contract 指定位置 |
| `thickness` | `Material.Thickness` | `Aov.SurfaceData.r` |
| `curvature` | `Material.Curvature` | `Aov.SurfaceData.a` |
| `materialCustom0_3` | `Material.Custom0-3` | `Aov.MaterialCustom0_3` |
| `sssSourceColor` | `Sss.SourceColor` | `Aov.SssSource.rgb` |
| `sssWeight` | `Sss.Weight` | `Aov.SssSource.a` |

这些字段是材质 producer 应写入的语义，不等同旧材质属性名。

### 4.3 `AovOutputData`

`AovOutputData` 是材质 pass 输出到 RP AOV 的最终结构。

原则：

- Object 静态语义由 AOV pass 统一从 RSUV / MPB 解码。
- Material 语义由材质 shader / generated shader 生产。
- Geometry 语义由当前 pass 的顶点 / fragment 输入生产。
- Shading 派生语义只写入已经登记的 AOV 通道，不临时加私有 RT。
- `AOV Mask Weight` 表示对象参与/覆盖，不应把 material class、profile 这类 byte-like 材质语义按 fractional coverage 压低到不可解码。SSS source / weight 这类实际贡献量可以继续按 coverage 缩放。

### 4.3.1 `ObjectSemanticData`

`ObjectSemanticData` 是对象静态语义进入材质 AOV pass 的统一入口。它属于 renderer-owned ABI，不属于材质 asset 属性。

第一版应覆盖：

| Field | 来源 | 说明 |
| --- | --- | --- |
| `maskWeight` | `ObjectSemanticAuthoring` / MPB | AOV participation / coverage gate |
| `objectId` | RSUV 优先，MPB fallback | `Aov.MaskId.g` |
| `objectGroupAndHighFlags` | RSUV 优先，MPB fallback | group id 与高位 feature flags 打包 |
| `objectFlags` | RSUV 优先，MPB fallback | 低 8 位 object feature flags |
| `objectCustomMask` | RSUV 优先，MPB fallback | object custom 0-7 |

原则：

- `unity_RendererUserValue` 是首选对象语义来源。
- MPB uniform 只是 fallback，用于无法写 RSUV 或测试 authoring 场景。
- generated material 可以 consume 解析后的对象语义，但不能在材质 Properties 面板暴露这些字段。
- `MaterialSemanticAuthoring` 与 generated material 是两种 producer 路径；不要让它们共享同名材质语义属性导致 MPB 覆盖材质 asset 参数。

### 4.4 `TransparentOutputData`

`TransparentOutputData` 描述透明材质在普通 forward 和 OIT pass 之间共享的最小输出。

第一版建议字段：

| Field | 说明 |
| --- | --- |
| `color` | premultiply 前的材质颜色 |
| `alpha` | 透明度 / coverage |
| `coverage` | alpha clip 或 dither 后的覆盖率，第一版可等于 `alpha` |
| `depthWeight` | OIT 权重深度项输入，第一版可由 runtime policy 计算 |
| `supportsOit` | preset / material capability，编译期或材质元数据 |
| `participatesOit` | 当前材质实例是否进入 OIT accumulation |

这些字段不等同旧 `_lilOITEnabled`。旧开关只是迁移事实，新材质侧应通过 capability / preset 声明。

### 4.5 `OitAccumulationData`

`OitAccumulationData` 是 OIT accumulation pass 写入 `Oit.Accumulation` / `Oit.Revealage` 的输入结构。

第一版建议字段：

| Field | 说明 |
| --- | --- |
| `weightedColor` | `color * alpha * weight` |
| `weightedAlpha` | `alpha * weight` |
| `revealage` | 透明遮挡项，第一版可用 `alpha` 推导 |
| `weight` | weighted blended OIT 权重，初版可由统一函数计算 |

第十步只定义 shader 输出和最小 pass，不创建 `Oit.*` RenderGraph resource。后续 Weighted OIT runtime 由透明阶段接管 resource、clear、draw 和 composite。

---

## 5. Pass 契约

第十步先固化 pass 语义，不急着实现所有 pass。

| Pass | LightMode / 名称 | 第十步状态 | 说明 |
| --- | --- | --- | --- |
| AOV Output | `HoUrpAovOutput` | 必做 | generated shader 必须能生产 AOV |
| Forward | `UniversalForward` | 独立最小实现 | 只做可见性和基础漫反射调试，不接完整 URP Lit |
| OIT Accumulation | `HoUrpOitAccumulation` | 必做占位 | 第十步只提供材质 pass；后续 OIT runtime 绘制此 pass |
| DepthOnly | `DepthOnly` | 只定义边界 | 保持 URP 兼容语义 |
| ShadowCaster | `ShadowCaster` | 只定义边界 | 不接 HoShadow receiver |
| Meta | `Meta` | 不做 | 材质烘焙后续再处理 |
| MotionVectors | `MotionVectors` | 不做 | 形变 / motion 阶段再处理 |

第十步重点是 AOV output 和 OIT-ready material pass：AOV 是 SSS、SemanticPost 和 Debug 的共同入口；OIT accumulation pass 是后续直接测试 Weighted OIT 的前置条件。

AOV runtime 应区分两条路径：

- fallback path：对没有 explicit AOV pass 的普通材质使用 `AovOutputFallback` overrideMaterial，继续服务 `MaterialSemanticAuthoring` / MPB 对照路径。
- explicit path：绘制材质自己的 `LightMode = HoUrpAovOutput` pass，覆盖 opaque 与 transparent queue，不使用 overrideMaterial。generated material prototype 必须走这条路径。

---

## 6. Feature Block / Preset 模型

第十步不做完整生成器，但需要定义最小描述模型，避免后续 UI 直接控制 shader 结构。

### 6.1 Feature Block

Feature Block 表示编译期固定模块。

第一版建议：

```text
MaterialFeatureBlock
  Id
  DisplayName
  Domain
  RequiredInputs
  ProducedFields
  RequiredIncludes
  CompatibleTemplates
```

示例 block：

| Block | Domain | 生产内容 |
| --- | --- | --- |
| `BaseColorTexture` | Material | `SurfaceData.baseColor` |
| `NormalMap` | Geometry / Material | `SurfaceData.normalTS` |
| `SkinSss` | Shading | `MaterialSemanticData.sss*` |
| `MaterialCustom` | Material | `Material.Custom0-3` |
| `AovOutputStandard` | AOV | `AovOutputData` |
| `OitTransparent` | Composite / Transparent | `TransparentOutputData` / `OitAccumulationData` |

### 6.2 Material Preset

Preset 表示有限组合，不是任意开关集合。

第一版建议：

```text
MaterialPreset
  Id
  DisplayName
  Template
  FeatureBlocks
  DefaultProperties
  ProducedSemantics
  RequiredCapabilities
  SupportedPasses
  PhasePolicy
```

首批 preset 建议只做文档和数据模型：

| Preset | 用途 |
| --- | --- |
| `Character_Toon_Minimal` | 角色 toon 最小 AOV 输出 |
| `Character_Toon_SSS` | 角色 toon + SSS source |
| `Character_Toon_SSS_OITReady` | 角色 toon + SSS source + OIT accumulation pass |
| `Character_PBR_Minimal` | PBR 基础表面 + AOV 输出 |
| `Hair_Toon_Minimal` | 头发材质占位 |
| `Environment_PBR_Minimal` | 环境材质占位 |

第十步只需要一个 prototype preset 真正生成或落地，其余可以先登记为规划项。

prototype preset 应优先选择 OIT-ready 版本，保证后续透明阶段能直接进入 Weighted OIT runtime 验证。

---

## 7. Shader 生成策略

第十步的生成策略应保持保守。

允许：

- 用模板 + include 生成一个可读 `.shader`。
- 生成结果提交到仓库，便于 diff。
- 生成器只处理固定 preset，不开放任意组合。
- 生成器可先是 Editor 工具或测试辅助，不进入 runtime。

不允许：

- 在 inspector 中实时拼 shader。
- 通过反射扫描所有字段决定 keyword。
- 让材质 UI 添加 / 删除 pass。
- 生成旧 `lilToon/lilPBR` 风格的大量 compatibility keyword。
- 生成依赖旧 include 的新 shader。

第一版原型应完全独立实现，避免调试时被 URP Lit 的隐藏 keyword、属性兼容层和 include 依赖干扰。

允许依赖：

- URP 的 `Core.hlsl`、基础矩阵变换和必要 shader library。
- 新建的 HoURP shader ABI include。
- 自己定义的最小 vertex / fragment / surface 结构。

不允许依赖：

- URP Lit shader 文件或 LitInput / LitForwardPass 这类完整 Lit include。
- URP Lit material inspector。
- URP Lit keyword 矩阵。
- URP Lit 的属性集作为新 ABI。
- ClearCoat / Detail / Parallax / DBUFFER / Lightmap / Meta 等非第十步必要路径。
- 任何旧 `lilToon/lilPBR` include 或 property。

建议原型命名：

```text
Runtime/Shaders/ShaderLibrary/HoUrpMaterialSurface.hlsl
Runtime/Shaders/ShaderLibrary/HoUrpMaterialAov.hlsl
Runtime/Shaders/ShaderLibrary/HoUrpMaterialOit.hlsl
Runtime/Shaders/Generated/HoUrpDebugLitMinimal.shader
```

实际路径可按 Unity package 导入规则调整，但命名应体现这是新 ABI，不是旧材质桥。

---

## 8. 与现有 `MaterialSemanticAuthoring` 的关系

`MaterialSemanticAuthoring` 在第十步之后仍然保留，但定位要降级为：

```text
Transitional / diagnostic material semantic producer
```

它的价值是：

- 给没有新材质 shader 的 renderer 提供 MPB 语义。
- 让测试场景快速设置 SSS / material class / material custom。
- 作为 generated material 输出的对照组。

它不能成为长期材质系统核心，也不能继续承担“材质 UI”的职责。

第十步验收时应至少有一组对照：

```text
MaterialSemanticAuthoring MPB
Generated material shader
```

两者在 AOV Debug 中输出一致或差异可解释。

---

## 9. 与旧材质系统的关系

旧 `lilToon/lilPBR` 的价值：

- 查明旧能力如何生产 HoAOV / SSS source / character capture。
- 提供行为验收样本。
- 帮助确认 pass 时机和材质参数覆盖范围。

第十步可以审查旧实现，但不能把旧实现升级为新契约。

明确不继承：

- 旧 inspector 组织。
- 旧 property name。
- 旧 keyword 体系。
- 旧 include 层级。
- 旧 `LightMode = HoAOV / HoAOVSSS` 命名。
- 旧 `_lilHoAov*` / `_HoAov*` 全局名。
- 旧材质包对 RP 扩展的隐式依赖。

如果需要对照，应写进 `LegacyInterop` 文档或测试备注，不进入新 runtime ABI。

---

## 10. 实施步骤

### Step 1. 审查当前材质语义入口

- 读取 `MaterialSemanticAuthoring`。
- 读取 `AovOutputFallback.shader`。
- 读取 `HoUrpBuiltInContracts` 中 Material / Shading / SSS 相关登记。
- 读取第九步 AOV 生命周期文档。
- 输出当前材质语义字段到 AOV channel 的映射表。

### Step 2. 定义 HLSL ABI include

建议新增：

```text
Runtime/Shaders/ShaderLibrary/HoUrpMaterialSurface.hlsl
Runtime/Shaders/ShaderLibrary/HoUrpMaterialAov.hlsl
```

要求：

- 不引用旧 `lilToon/lilPBR` include。
- 只暴露新 `HoUrp` 命名。
- 明确 `SurfaceData`、`MaterialSemanticData`、`AovOutputData`。
- 提供 material semantic 到 AOV channel 的编码函数。
- 明确 `TransparentOutputData`、`OitAccumulationData`。
- 提供 alpha / coverage / OIT weight 的最小统一函数。

### Step 3. 定义 C# 描述模型

可新增：

```text
Runtime/Semantic/MaterialFeatureBlockDefinition.cs
Runtime/Semantic/MaterialPresetDefinition.cs
```

或先放在 `Editor` / `Tests` 侧，取决于是否需要 runtime 查询。

要求：

- 能描述 preset 生产哪些语义。
- 能声明需要哪些 feature block。
- 能声明 supported passes，例如 `HoUrpAovOutput`、`UniversalForward`、`HoUrpOitAccumulation`。
- 能声明 `SupportsOit` / `ParticipatesOit` capability 和 phase policy。
- 能被测试查询。
- 不直接触发 shader 编译。

### Step 4. 建立一个最小 generated shader 原型

建议原型：

```text
HoUrpDebugLitMinimal
```

最低要求：

- 有 AOV output pass。
- 有独立 `UniversalForward` pass，能用基础 base color + simple light/debug normal 方式显示。
- 有独立 `HoUrpOitAccumulation` pass，能输出 weighted color / revealage 所需数据。
- 使用新 HLSL ABI。
- 写出 material class、SSS profile、thickness、curvature、SSS source、SSS weight。
- 写出 transparent color、alpha、coverage、OIT weight 输入。
- object id / group / flags 仍由 AOV pass 从 RSUV / MPB 统一处理。
- 不引用旧 `lilToon/lilPBR` property / include。
- 不引用 URP Lit shader / full Lit include。

### Step 5. 接入 registry / descriptor

- 给 `HoUrpBuiltInContracts` 标记 `GeneratedMaterial` producer。
- 给对应 feature descriptor 说明 generated material 能生产 material / shading semantic。
- 给材质 preset / capability registry 标记 `SupportsOit` / `ParticipatesOit`，但不注册 Weighted OIT runtime 为已实现。
- 记录新 OIT material pass 名 `HoUrpOitAccumulation`，旧 `lilToonOIT` 只作为 legacy mapping。
- 不新增未落地资源名。
- 不让 material preset 直接成为 RenderFeature。

### Step 6. 补测试

测试建议：

- `MaterialFeatureBlockDefinition` / `MaterialPresetDefinition` 基础字段稳定。
- prototype preset 声明包含 `Material.Class`、`Sss.SourceColor`、`Sss.Weight`。
- prototype preset 声明包含 `SupportsOit`、`ParticipatesOit` 和 `HoUrpOitAccumulation` pass。
- 新 HLSL / shader 文件不包含 `_lilHoAov`、`_HoAov`、`lilToon`、`lilPBR`。
- 新 HLSL / shader 文件不包含 `_lilOITEnabled`、`_lilOITActive`、`lilToonOIT`。
- shader property ids 继续使用 `_HoUrp*`。
- registry 能查询某个 material semantic 允许由 generated material 生产。
- registry / descriptor 能查询 prototype material 是 OIT-ready，但 Weighted OIT runtime 未在第十步启用。

### Step 7. Unity 手动验收

场景配置：

1. 挂载 `HoURP AOV Output`。
2. 挂载 `HoURP Subsurface Scattering`。
3. 挂载 `HoURP Semantic Post Process`。
4. 挂载 `HoURP AOV Debug`。
5. 同一模型分别使用：
   - `MaterialSemanticAuthoring` + 简单材质。
   - generated shader 原型材质。

验收：

- `AOV.MaterialClass` 输出可见。
- `AOV.SssSource` 输出可见。
- `SSS.Source` / `SSS.Diffusion` 能读取 generated material 的 SSS 输入。
- `SemanticPost.Mask` 能按材质语义规则响应。
- `AllRegistered` 中相关 debug view 不缺失。
- generated shader 的透明材质实例能被 Frame Debugger / RenderDoc 识别出 `HoUrpOitAccumulation` pass。
- 第十步不要求画出 OIT composite，但要求材质 pass 已经能被后续 OIT runtime 直接按 pass 名绘制。
- 控制台无 RenderGraph attachment / 未声明 texture 读写错误。

---

## 11. 成功标准

- 新 RP 有第一版材质 shader ABI 文档和 HLSL include。
- 材质语义字段和 AOV channel 映射不再只存在于 `MaterialSemanticAuthoring` 和 fallback shader 里。
- 至少一个 generated shader 原型能生产 AOV material / SSS 语义。
- 至少一个 generated shader 原型带 `HoUrpOitAccumulation` pass，并声明 `SupportsOit` / `ParticipatesOit`。
- `MaterialSemanticAuthoring` 被明确降级为过渡 producer / 对照工具。
- Feature Block / Preset 的最小描述模型能表达固定组合。
- 没有把旧 `lilToon/lilPBR` property、keyword、include、inspector 结构提升为新 ABI。
- 第十一步可以继续推进：
  - HoPost / Shoost 搬迁前置契约与最小 stack。
  - Weighted OIT runtime 最小验证。
  - 完整材质生成器。
  - HoNpr 统一材质包接入。
  - 材质 inspector 轻量化。
  - 或者回到 Debug Framework 读取 material preset / producer 信息。

第十一步的当前推荐方向改为 **HoPost / Shoost 搬迁前置契约与最小 stack**。原因是 HoNpr 侧已经开始把材质生产者变得更显式、可管理，HoUrp-Extensions 可以暂时跳过原本排队的 OIT runtime 视觉验证，优先把旧 `HoPost` 与 `Shoost` 搬迁所需的 PostGraph、ImageChain、动态资源请求、多输入声明和双缓冲策略定下来。

这不取消第十步已经准备好的 OIT-ready 材质契约。`SupportsOit` / `ParticipatesOit` capability、新 OIT pass 名和透明材质 phase 规则继续保留，后续回到 Weighted OIT runtime 时仍应直接接 `Oit.*` RenderGraph resources、clear、draw accumulation 和 debug/composite 验收。

---

## 12. 风险点

- 太早做完整 shader generator，导致模板、ABI 和 preset 同时变化，难以测试。
- 为了快速看效果，直接复制旧 `lilToon/lilPBR` include。
- 让材质 UI 重新控制 shader 结构，回到厚 inspector / keyword 地狱。
- 把 material semantic 塞进 RSUV，破坏第九步的对象静态语义边界。
- 让 generated material 私自写全局纹理或私有 RT，绕过 RenderGraph / resource registry。
- 忘记把 generated material producer 登记进 contract，导致 Debug 只能看到资源，不能解释来源。
- AOV output pass 继续膨胀 MRT 数量，重新触发 RenderGraph attachment 上限问题。
- 第十步没有提前落地 OIT accumulation pass，导致后续迁移 OIT runtime 时又反向修改材质 ABI。
- 把旧 `_lilOITEnabled` / `_lilOITActive` / `lilToonOIT` 当成新 ABI，而不是 legacy mapping。

第十步的验收重点是 **材质生产者契约清晰、shader ABI 稳定、AOV/SSS/SemanticPost 可消费、旧材质系统不污染新核心**，不是一次性完成全新的 PBR/NPR 材质体系。
