# RP 渲染架构对话总结（详细版）

> 这是对我们前面关于 HOAOV、RSUV、后处理、语义域划分、几何/形变/着色/合成边界、UI/交互设计、Debug 层设计等讨论的系统整理。

> 开始新的 RP / 材质 / RenderFeature 重构对话前，先引用 `Documentation~/rp设计哲学底线.md`。需要查旧代码入口时，再引用 `Documentation~/旧实现快速定位索引.md`。第一阶段执行按 `Documentation~/rp重构第一步执行计划.md` 审查后推进。

---

## 0. 这次讨论的核心结论

你现在做的已经不只是“在 URP 里加效果”，而是在搭一套 **语义驱动的混合式实时渲染与合成架构**。它的关键特征不是单纯画出颜色，而是把渲染过程中可复用、可被后续系统消费的“信息”持续输出出来，并且把这些信息按语义和阶段分层管理。

整套系统里最重要的变化是：

1. **从颜色驱动转向语义驱动**。
2. **从单一 HOAOV 转向分层语义系统**。
3. **从“效果各写各的 RT”转向统一的中间资源与滤波框架**。
4. **从“材质里顺手写点东西”转向“显式声明谁生产、谁消费、在哪个阶段成立”**。
5. **从传统渲染管线思维，逐步走向 RenderGraph / FrameGraph + Composite 的思维方式**。
6. **从 Layer/Tag 这种隐式分类，转向显式能力（Capability）附加**。
7. **从分散的 feature debug，转向统一的 Debug Domain / Debug Framework**。

这里需要补一条硬底线：新 RP 里的 RenderGraph 不是可选写法，也不是只把代码放进 `RecordRenderGraph()`。每条资源链路都必须严格按 RenderGraph 的读写声明、生命周期和 producer/consumer 关系表达；不能私自创建隐藏 RT 链路，不能依赖全局纹理“刚好已经被前面某个 pass 设置过”，也不能让 shader 采样没有被当前 pass 显式声明或统一资源层登记的输入。

第六阶段 SSS 调试补充了一条更具体的工程约束：全局纹理发布本身也会进入 RenderGraph 依赖。URP 的部分内置 pass 会通过 `UseAllGlobalTextures(true)` 读取当前已发布的全局纹理；如果我们把 `activeColorTexture` 直接发布为 source color，后续透明绘制在把同一张 `_CameraTargetAttachment` 当 render attachment 写入时，会同时形成 texture read 依赖并触发 RenderGraph 校验。后续所有“读 camera color 再写回 camera color”的 pass，都必须先显式 copy camera color，再读取 copy。

---

## 1. 当前已经存在的系统（按 2026-05-22 仓库核查修正版）

本节不再只按概念推断，而是按 `D:\Unity_Fork\lilToon-URP-Extensions`、`lilToon` 和 `lilPBR` 里已经落地的能力来描述。结论是：当前旧工作流已经不是“几个 RenderFeature 的集合”，而是一个由管线扩展、shader pass 契约、全局纹理/状态、Volume/Inspector UI 和材质属性共同组成的临时协作体系。

但这只是现状盘点，不是新 RP 的设计约束。新的 RP 重构不准备直接承接旧 `lilToon/lilPBR` 材质系统，也不应在核心架构里建立“旧材质桥”。旧项目的价值主要是证明哪些能力已经跑通过、哪些资源流/时序/语义确实有用，以及迁移时有哪些行为需要重新定义。

### 1.1 lilToon-URP-Extensions 的真实定位

`lilToon-URP-Extensions` 目前是旧工作流里的 **URP 管线扩展与能力验证层**：

- `lilToon` 和 `lilPBR` 提供 shader pass、材质属性和 HLSL include。
- `lilToon-URP-Extensions` 负责分配 RT、调度 pass、发布全局纹理/缓冲、提供 Volume/Inspector 控制面。
- 它通过 `LightMode`、全局 shader property、MaterialPropertyBlock、`SetShaderUserValue` 等契约与旧项目对接。
- 这些契约应该被记录为 **Legacy Interop / Current State**，用来理解现有能力与迁移风险；新 RP 不应把它们原样提升成长期 ABI。

已经存在的公开 RendererFeature / runtime 入口包括：

- `HoAovRendererFeature`
- `HoSubsurfaceScatteringRendererFeature`
- `HoCharacterSpecializationRendererFeature`
- `HoPostProcessRendererFeature`
- `ShoostPostProcessRendererFeature`
- `WeightedOITRendererFeature`
- `HoShadowCastRendererFeature`
- `LILPlanarReflectionSurface`（不是 RendererFeature，而是场景组件驱动）

这些模块多数已经同时保留 RenderGraph 路径和非 RenderGraph 兼容路径。对新 `HoUrp-Extensions` 来说，非 RenderGraph 路径只应作为旧实现对照和行为验证来源；新实现方向应保持 RenderGraph-first。

### 1.2 新仓库骨架的当前状态

当前 `D:\Unity_Fork\HoUrp-Extensions` 已经是新的目标包骨架：

- 包名是 `com.hollow.hourp-extensions`。
- 定位是 URP-only、Unity `6000.3+`、RenderGraph-first。
- `Runtime/RenderGraph/`、`Runtime/Features/`、`Runtime/Resources/`、`Editor/`、`Tests/` 已经建好目录和 asmdef。
- 当前还没有迁移旧实现代码，旧实现来源仍是 `D:\Unity_Fork\lilToon-URP-Extensions`。

材质侧也已经有新包边界：

- `HoNpr`：未来统一 HoRP 材质 / shader 包；NPR 是主方向，PBR 只作为 `HoStandardSurface` 和 PBR lobe 子集存在，不再拆独立 `HoPbr` 包。`HoToon` 的 URP 半调 toon shader、半调贴图和导入工具已合入这里作为小模块。
- `HoNpr`：未来 NPR shader 包，目前主要是包骨架。
- `HoToon`：旧轻量 toon shader 仓库，URP 小模块已迁入 `HoNpr`；独立仓库保留为 Built-in/历史参考。

因此，新 RP 大纲应该服务于这些新包：`HoUrp-Extensions` 先定义 RenderGraph 资源、Feature、语义与调试契约；`HoNpr` 作为统一材质系统按新契约接入；`HoToon` 保留为轻量历史参考包。旧 `lilToon/lilPBR` 只作为能力样本和迁移参照。

### 1.3 HoAOV：已经是多 MRT 语义集合，而不是单张图

当前 HoAOV 默认在 `AfterRenderingOpaques` 写入，并通过 `LightMode = "HoAOV"` 与 `LightMode = "HoAOVSSS"` 绘制材质原生 pass；在材质没有原生 HoAOV pass 时，可用 fallback material 写入基础 AOV。

当前全局输出包括：

- `_lilHoAovMaskIdTexture`：mask、group/object/material/flags 等 ID 类信息。
- `_lilHoAovNormalDepthTexture`：世界法线/视图相关法线与线性深度。
- `_lilHoAovTangentNormalTexture`：切线空间法线。
- `_lilHoAovSurfaceDataTexture`：厚度、曲率、材质/profile、utility 等材质/SSS 输入。
- `_lilHoAovCustom0_3Texture`：材质自定义通道 0-3。
- `_lilHoAovObjectCustom0_3Texture` 与 `_lilHoAovObjectCustom4_7Texture`：对象/角色部件自定义位。
- `_lilHoAovSssTexture`：HoSSS 专用源色 MRT。
- `_lilHoAovDepthTexture`：HoAOV 自己的 depth target。

这说明文档里“HOAOV 是一个 Semantic Collection”的判断是对的，但需要修正一点：它现在已经不是等待未来拆分的单一 HOAOV；当前代码层已经先形成了 **Mask/ID、Normal/Depth、SurfaceData、Custom、ObjectCustom、SSS Source** 这些子资源。

### 1.4 HoAOV 的对象语义组件已经存在

当前已经有两个很重要的早期语义/能力配置组件：

- `HoAovSubject`：通过 MaterialPropertyBlock 写入 mask weight、system write mask、custom write mask、group id、object id、material class、flags、thickness、curvature、utility、debug color、自定义通道值。
- `HoAovGroup`：按角色组 ID、部件 ID、flags、主体/脸/前发/眼睛/眼透区域/配件等列表给 Renderer 打包语义；优先使用 `MeshRenderer/SkinnedMeshRenderer.SetShaderUserValue()`，失败时回退到 MaterialPropertyBlock。

`HoAovGroup.PackRendererUserValue()` 当前把 `objectCustomMask + characterId + partId + flags` 打成 32-bit renderer user value。也就是说，原文里说的 RSUV/角色部位语义并不只是设想，已经有一版面向 Renderer 的显式语义写入机制。

### 1.5 HoPost：语义感知后处理栈已落地

`HoPostProcessRendererFeature` 当前是 Volume 驱动的语义后处理栈，旧实现支持 RenderGraph/兼容路径。现有 effect 包括：

- `EdgeLight`
- `Outline`
- `DropShadow`
- `DepthOfField`
- `PostLighting`
- `CustomMaterial`

每个 HoPost layer 可启用 HoAOV mask，并且已经有规则系统：

- source 可读 `Mask / GroupId / ObjectId / Flags / Thickness / Curvature / Material / Utility / Custom0-3 / ObjectCustom0-7`。
- operator 支持 direct、threshold、比较、范围、颜色匹配、flags any/all。
- combine 支持 replace、or、and、subtract、add、multiply。
- runtime 最多评估 4 条规则。

所以 HoPost 不只是“吃 HOAOV 的后处理”，它已经有一个初步的语义查询语言雏形。

### 1.6 Shoost：主要是最终图像栈，但并非完全不碰 AOV

原文把 SHOPost/Shoost 说成纯图像后处理，这个方向基本对，但需要补充边界：当前 `ShoostPostProcessing` 是 final-stack 风格的后处理系统，包含大量画面风格效果，例如色彩、CRT、VHS、Glow、IrisBlur、RGBBlur、Kuwahara、Weather、LogoOverlay 等。

它的工程结构已经比较清晰：

- `ShoostPostProcessEffectDescriptor` 是 effect metadata 的集中事实来源。
- descriptor 维护默认 shader、运行顺序、是否支持 AOV composite、执行类型（SinglePass/MultiPass/Stateful/Removed）。
- 正式 effect 通过 executor 注册，而不是散落 switch。

但它并非绝对“纯图像”：部分效果支持 AOV composite。更准确的边界应是：

- 需要角色 mask、主体捕获、depth/normal/ID 精确控制的效果放 HoPost 或 CharacterSpecialization。
- Shoost 保持最终风格栈为主，只允许轻量 AOV composite 作为局部遮罩/混合辅助。

### 1.7 HoSSS：独立屏幕空间 SSS，不属于 HoPost 调试输出

当前已经有独立的 `HoSubsurfaceScatteringRendererFeature`。它读取 HoAOV，而不是 HoPost 的一个子效果。

当前输入包括：

- HoAOV mask/id
- HoAOV normal/depth
- HoAOV surface data
- HoAOV SSS 专用源色 `_lilHoAovSssTexture`

当前结构包括 Source、横向扩散、纵向扩散、Transmission gather/blur、Composite。设置里有 8 个 profile 槽位，质量档控制 Burley-like disk gather 的采样预算。默认 source/composite 时机约束在 opaque 之后、transparent 之前，避免皮肤散射结果被透明顺序污染。

所以原文“未来要接入 SSS/SSS 只是后处理”的表述需要修正：HoSSS 已经是一个独立的 HoAOV 数据消费者，同时也证明了材质派生语义可以在屏幕空间被专门模块消费。

### 1.8 OIT：Weighted Blended OIT 已经形成完整数据流

`WeightedOITRendererFeature` 当前提供：

- per-camera reset：每个相机开始时重置 `_lilOITActive = 0`。
- opaque copy：skybox 之后复制 camera color 到 `_lilOITOpaqueTexture`，同时发布给 `_CameraOpaqueTexture` 路径。
- accumulation：绘制 `LightMode = "lilToonOIT"` 的对象到 accumulation/revealage MRT。
- composite：透明阶段后把 OIT 结果合成回 camera color。

材质侧通过 `_lilOITEnabled` 与 `_lilOITActive` 握手，避免启用 OIT 的透明材质在 accumulation 阶段又走普通 forward。这个系统的关键不是“透明也能画”，而是它已经形成了 **pass tag + 全局状态 + 背景拷贝 + 独立合成** 的完整数据流。

### 1.9 HoShadowCast：独立 shadow atlas + 材质 forward 接收

`HoShadowCastRendererFeature` 当前在 `BeforeRenderingPrePasses` 默认执行，使用 `ShadowCaster` pass 生成自己的 shadow atlas，并发布：

- `_HoShadowCastAtlas`
- `_HoShadowCastSecondDirectionalAtlas`
- light/slice/worldToShadow 数组
- PCSS 参数与 debug 参数

它支持点光、聚光、方向光，以及第二方向光级联 atlas。采样侧已经从硬件 compare 转向 raw depth + manual compare / PCSS，atlas tile 会 clamp，避免串采样。

需要修正的一点是：当前 HoShadowCast 的主消费路径仍然是 `lilToon/lilPBR` 的材质 forward 阶段调用 `HoShadowCastAttenuation(positionWS)`。HoAOV depth 可以服务 debug、receiver guard 或未来屏幕空间版本，但不能替代 shadow atlas 的 blocker search。

### 1.10 角色特化：眼透和前发投影是独立角色合成域

`HoCharacterSpecializationRendererFeature` 当前使用 `LightMode = "HoCharacterCapture"` 做角色捕获，然后合成：

- 眼睛透过（Eye Reveal）
- 前发向脸部投影（Hair Drop Shadow）
- 相关 debug view

它读取 HoAOV 的 mask/id/normal-depth/objectCustom，并通过 Volume 覆盖参数。角色部件语义依赖 `HoAovGroup` 写入的主体、脸、前发、眼睛、眼透区域等 object custom 位。

所以“角色”在当前系统里已经不是普通对象分类，而是有自己的 capture pass、语义输入、合成 RT 和屏幕空间规则。

### 1.11 平面反射：当前扩展仓库可确认的是 Planar Reflection，不是 SSR

原文写“SSR（水面）”需要谨慎。按当前 `lilToon-URP-Extensions` 仓库核查，已落地的是 `LILPlanarReflectionSurface`：

- 由场景组件注册 `RenderPipelineManager.beginCameraRendering`。
- 创建镜像相机和反射 RenderTexture。
- 通过 MaterialPropertyBlock 写入 `_LILPBRPlanarReflectionTexture`、矩阵和参数。
- 可自动设置 `_UsePlanarReflection`。

因此在这份 RP 大纲里应把它归为 **Reflection/PlanarReflection 子系统**。如果水体 SSR 存在于其它仓库，需要另行核对，不应把它当成当前 URP 扩展包已经确认的能力。

### 1.12 lilToon / lilPBR 旧项目的实际对接方式（迁移参照，不是新设计边界）

旧项目并不是“等待未来接入”，它们已经通过 shader pass 契约接入了当前 RP 扩展。

`lilToon` 当前可确认的对接点：

- URP block 中有 `HoAOV`、`HoAOVSSS`、`HoCharacterCapture`、`UniversalGBuffer`、`MotionVectors` 等 pass。
- 透明模板中有 `LightMode = "lilToonOIT"`。
- forward include 通过 `_lilOITEnabled / _lilOITActive` 跳过 OIT accumulation 期间的普通 forward。
- light attenuation 宏里乘 `HoShadowCastAttenuation(positionWS)`。
- `lil_pass_hoaov.hlsl` 写入 HoAOV 的 mask/id/surface/custom/objectCustom/SSS 源。
- `lil_pass_hocharacter_capture.hlsl` 接入角色捕获输出。

`lilPBR` 当前可确认的对接点：

- `lilPBR.shader` / `lilPBR_Tessellation.shader` 有 `UniversalForward`、`UniversalGBuffer`、`ShadowCaster`、`DepthOnly`、`DepthNormals`、`Meta`、`MotionVectors`、`XRMotionVectors`、`HoAOV`、`HoAOVSSS`、`HoCharacterCapture` pass。
- `hoaov.hlsl` 写入与 lilToon 一致的 HoAOV 数据。
- `hocharacter_capture.hlsl` 接入角色捕获。
- `unity_urp.hlsl` 采样 `_LILPBRPlanarReflectionTexture`，并通过 `_HoShadowStrength` 控制 HoShadowCast 对材质阴影的影响。

这说明旧系统已经形成了一个临时但可工作的 ABI：`LightMode` 名称、全局纹理名、材质属性名、renderer user value 打包格式、HLSL include 路径。

这里需要明确修正：**新 RP 不以承接这个旧 ABI 为目标**。这些内容应作为迁移参照和能力清单，而不是未来核心架构的一部分。真正需要固化的是新 RP 自己的 Semantic / Feature / Resource 契约；材质系统会在后续重构中按新契约接入，而不是通过一个长期存在的旧材质桥接层接入。

---

## 2. 我们对当前架构的判断

我们对你现有系统的判断是：它已经从普通的“效果堆叠”进化成了一个 **Hybrid Film/Game RP**。这个系统的关键不是堆更多效果，而是把“谁是什么、谁属于谁、谁能被谁消费、谁在哪个阶段成立”弄清楚。

也就是说，真正重要的不再是：

- 再加一个特效
- 再加一张 RT
- 再加一个 shader keyword

而是：

- 建立统一的语义体系
- 明确数据生命周期
- 明确各个域的责任边界
- 让后处理、角色特化、几何预处理、材质输出都在一个结构里协调工作

---

## 3. 为什么你会遇到“HOAOV 不能提前”的问题

你后来发现：有些材质在真正渲染的时候，会“顺手”输出一些信息到 HOAOV，这导致 HOAOV 无法整体前移到物体真实渲染之前。

这个问题的本质是：

# HOAOV 里混了两类完全不同的数据

---

### 3.1 静态语义（Static Semantic）

这类数据本质上描述的是：

> “这个东西是什么”

它们通常不依赖光照、不依赖真正的 fragment shading、不依赖材质的计算结果。

例如：

- ObjectID
- MaterialID
- CharacterPart
- LightingGroup
- RSUV Group
- Render Layer
- Feature Flags

这些信息大多可以在较早阶段输出，甚至可以从对象、实例、材质元数据中直接得到。

---

### 3.2 动态着色语义（Dynamic Shading Semantic）

这类数据描述的是：

> “材质真正算出来了什么”

例如：

- SSS Mask
- Toon Shadow Factor
- Specular Mask
- Wetness Result
- Final Stylized Ramp
- Eye Highlight
- Emission Contribution
- Custom Light Term

这些数据必须经过材质、纹理采样、光照计算、屏幕空间逻辑后才能得到，所以它们天然晚于真正的着色过程。

---

### 3.3 结论

所以问题不是“HOAOV 可不可以前移”，而是：

# 你现在的 HOAOV 把静态语义和动态着色语义混在一起了

这导致：

- 有些信息可以前置
- 有些信息必须后置
- 统一放在一个层里会让它们的生命周期互相冲突

---

### 3.4 按当前仓库实现修正这个判断

当前代码里已经有一个很重要的事实：`HoAovSubject` / `HoAovGroup` 写入的对象语义，已经可以在渲染前通过 MaterialPropertyBlock 或 renderer user value 绑定到 Renderer；而 `HoAOV` / `HoAOVSSS` pass 仍然需要在绘制阶段把深度、法线、surface data、SSS source 等写入 MRT。

所以“HOAOV 不能提前”不应该理解成所有 AOV 都被困在 late pass，而应该拆成三类：

1. **Renderer/对象级静态语义**：例如 characterId、partId、objectCustomMask、flags、groupId。当前已经能提前绑定，只是还没有统一注册表。
2. **几何可得语义**：例如 depth、normal、tangent normal、coverage。它们需要一次绘制，但不一定需要完整 forward lighting。
3. **材质/着色派生语义**：例如 SSS thinness/source、profile、曲率增强、custom channel 采样、alpha clip 后覆盖。这类必须走材质 pass。

当前 HoAOV 的真实问题不是“还没拆成资源”，而是 **资源已经拆出雏形，但语义定义、生产者、消费者和生命周期还没有被正式登记**。

## 4. 我们建议的核心拆分：ObjectDomain / MaterialDomain / ShadingDomain

你后来问到“是不是 ObjectDomain 跟 MaterialDomain 这样吗”。答案是：**对，而且不止这两个。**

更准确地说，应该按“语义在哪个阶段成立”来划分 Domain。

---

### 4.1 Object Domain

描述对象本身是什么。

典型内容包括：

- ObjectID
- CharacterID
- PartID
- RenderLayer
- VisibilityGroup
- LightGroup
- ShadowGroup
- DecalReceiver
- OutlineGroup

这个域里的东西通常来自场景对象、Renderer、Skeleton、Instance 等，不依赖最终 shading。

---

### 4.2 Material Domain

描述这个表面材质本身是什么。

典型内容包括：

- MaterialID
- SurfaceType
- ShadingModel
- SSSProfile
- HairType
- WetnessCapability
- TransparencyType
- ToonProfile

它更像材质元数据，而不是最后算出来的光照结果。

---

### 4.3 Shading Domain

描述真正着色后的结果。

典型内容包括：

- SSSWeight
- StylizedShadow
- SpecularMask
- FinalRamp
- FoamFactor
- RimLightWeight
- AnisotropyTerm
- MatcapContribution

这类信息必须经过 fragment、纹理采样、光照计算等步骤之后才成立。

---

### 4.4 Lighting Domain

描述光照系统产生的中间结果。

例如：

- ShadowFactor
- LightAccumulation
- IndirectDiffuse
- IndirectSpecular
- VolumetricFog
- CausticIntensity

它可能来自 deferred、clustered、shadow、volumetric 等子系统。

---

### 4.5 Image Domain

描述纯屏幕空间、纯图像处理的结果。

例如：

- Bloom
- LensDistortion
- Chromatic Aberration
- FilmGrain
- Sharpen
- ToneMap

它不关心对象是谁，只关心图像如何被处理。

这一域需要特别补一条资源设计：**纯图像域默认不应该按 effect / layer 数量线性增长 RT 数量，而应该由统一的 ImageChain 以双缓冲方式执行**。

典型形式是：

```text
ImageChain.Read  = 当前图像
ImageChain.Write = 另一张同规格工作纹理

pass 0: Read -> Write
swap
pass 1: Read -> Write
swap
pass 2: Read -> Write
swap
```

这和 RenderGraph-first 不冲突。RenderGraph 仍然看到每个 pass 的显式读写关系；区别只是资源声明层把一组线性 image-space pass 约束到少量可复用工作纹理，而不是让每个 pass 都创建一张新的全屏 transient texture。这样能显著降低 Shoost final image stack、简单色彩链路、锐化、VHS/CRT、色差、颗粒、tone map、简单 blur 等链路的内存峰值和资源 churn。

需要额外资源的 image effect 必须显式升级资源类型，而不是绕开 ImageChain：

- 多分辨率 bloom / pyramid：申请 pyramid pool。
- 迭代 blur / separable blur：申请局部 ping-pong pair，可按分辨率缩放。
- temporal / history：申请 persistent history。
- 需要原图参与最终合成：声明 `OriginalSource` pin。
- 需要 AOV / depth / normal：声明为 Semantic / Geometry 输入，不能继续伪装成纯 ImageDomain。

---

### 4.6 Composite Domain

这是你已经在做的 HOPost 所在的地方。

它负责利用语义做最终合成，比如：

- 角色局部 Bloom
- 头发描边
- 皮肤 SSS 后处理
- Anime Shadow Composite
- 光照组局部调整
- 材质层级定向修正

它已经非常接近实时合成层了。

---

## 5. 为什么 Domain 不是 Buffer

一个很重要的结论是：

> Domain 不是 Buffer。

它们是两个不同维度的概念。

- **Domain**：这个语义在哪个阶段成立、属于什么层次。
- **Buffer**：这个数据最后存放在哪个资源里。

同一个 RT 里可能混合多个 Domain 的信息；同一个 Domain 的信息也可能拆分到多个 Buffer 里。

真正重要的是：

# “这个语义属于哪个阶段，而不是它最终塞在哪张图里”

---

## 6. 为什么“顺手输出”会成为风险点

你提到材质在渲染时会“顺手输出”一些信息到 HOAOV，这件事很危险，因为它会让系统变成：

- shader 里悄悄写 buffer
- 依赖关系隐式存在
- 谁写了谁、谁消费了谁，不透明
- 后续维护和调试越来越难

现代渲染器更倾向于：

- 显式注册输出
- 声明生产者与消费者
- 让系统知道每个语义项的生命周期和归属

也就是说，你需要的不是“顺手写”，而是一个正式的 **Semantic Registry**。

---

## 7. 一个更合理的 HOAOV 拆法

为了让 HOAOV 能提前，我们建议把它拆成至少两层。按当前仓库实现，这个拆分首先应该是 **语义生命周期拆分**，不一定立刻等于重命名/重建所有 RT。现有 `MaskId / NormalDepth / SurfaceData / Custom / ObjectCustom / SSS` 已经是物理资源雏形，下一步要补的是正式语义表。

### 7.1 HOAOV_Base / PreSemantic

负责在较早阶段就能获得的内容：

- ObjectID
- MaterialID
- CharacterPart
- RSUV Group
- FeatureFlags
- RendererUserValue / ObjectCustomMask
- GroupID / CharacterID / PartID
- Render Layer

这层本质上是“对象与材质的静态语义层”。

当前已有对应雏形：

- `HoAovSubject` 通过 MaterialPropertyBlock 写入对象/材质语义。
- `HoAovGroup` 通过 renderer user value 或 MaterialPropertyBlock 写入角色组、部件和 object custom 位。
- 旧 `lilToon/lilPBR` 的 HoAOV pass 会优先读取 renderer user value，再回退到材质属性；新材质系统应重新定义这条读取规则，而不是原样继承旧属性名。

Depth / normal 虽然可以早于后处理生成，但它们仍然需要绘制，建议归到 `GeometrySemantic` 或 `AOV_Geometry`，不要混进纯对象静态语义。

### 7.2 HOAOV_Shading / ShadingSemantic

负责真正着色以后才有的内容：

- SSSMask
- SSS Source Color
- SSS Profile
- Thickness / Curvature / Utility 的材质派生值
- AnimeRamp
- StylizedShadow
- SpecMask
- Wetness
- Foam
- CustomLightTerm

这层本质上是“材质和光照算出来的结果层”。

这样拆分以后，你就可以：

- 提前执行一部分 HOAOV
- 保留必须晚出的着色结果
- 让后处理、角色特化、语义提取各取所需

### 7.3 当前 HoAOV 资源到语义域的建议映射

```text
_lilHoAovMaskIdTexture
  -> Object / Material / Capability / Coverage

_lilHoAovNormalDepthTexture
  -> Geometry / View / Coverage

_lilHoAovTangentNormalTexture
  -> MaterialGeometry / Shading Input

_lilHoAovSurfaceDataTexture
  -> Material / ShadingSemantic / SSS Input

_lilHoAovCustom0_3Texture
  -> Material Custom Semantic

_lilHoAovObjectCustom0_3Texture
_lilHoAovObjectCustom4_7Texture
  -> Object / Character Part / RSUV-like Semantic

_lilHoAovSssTexture
  -> ShadingSemantic / HoSSS Source
```

这张映射表应该成为后续 Semantic Registry 的初始数据，而不是另起一套与现有 shader 名称脱节的概念。

---

## 8. RSUV 的定位

你提到 RSUV 的各种 ID 组（光照分组、物体分组、角色部位分组），我们对它的判断是：

# RSUV 不应该仅仅被理解成“特殊 UV”

它更像一种 **Runtime Semantic Coordinate System**，也就是运行时语义坐标系统。

它可以支持：

- 角色局部控制
- 风格化遮罩
- 动态污渍、湿润、血迹、灰尘
- 局部后期和局部阴影
- 光照组控制
- 材质层控制

RSUV 最好和 Object Domain / Material Domain / Shading Domain 一起进入统一语义注册，而不是零散塞进 shader 宏里。

---

## 9. 统一滤波系统的必要性

你前面提到很多后处理和中间效果会涉及大量抗锯齿、模糊、降采样、重建、历史融合等操作。这里有一个非常重要的结论：

# 这些滤波过程不应该被每个效果单独重复实现

你未来会经常遇到：

- Gaussian Blur
- Kawase Blur
- Bilateral Blur
- Atrous Filter
- Temporal Accumulation
- Mipmap Pyramid
- Edge-aware Filter
- Downsample / Upsample

所以很适合建立一个统一的 **Filter Backend** 或 **FilterGraph**。

这样：

- Bloom、SSS、SSR、DoF、AO、Volumetric 都能复用同一套滤波基础设施
- 资源和算法更统一
- 调试和替换更容易

这里还应该拆出一个比 FilterGraph 更基础的概念：**ImageChain 双缓冲执行器**。

FilterGraph 解决的是“如何滤波”：blur、downsample、upsample、temporal、edge-aware、pyramid 等算法。ImageChain 解决的是“纯图像 pass 如何连续写下去而不浪费资源”。大量 Shoost / ImagePost layer 并不需要独立输出资源，它们只是把当前图像变成下一张图像。对这类 pass，默认执行模型应该是：

```text
ImageChain.Begin(cameraColorCopy or imported source)
ImageChain.AddPass(effect0)
ImageChain.AddPass(effect1)
ImageChain.AddPass(effect2)
ImageChain.End(write back to camera color or final output)
```

内部只需要 `Image.WorkA` / `Image.WorkB` 两张同规格工作纹理，必要时加一个 `Image.Original` copy。每个 pass 仍然在 RenderGraph 中显式声明读 `WorkA` 写 `WorkB`，然后交换读写角色。

这能避免旧实现里常见的两种浪费：

- RenderGraph 路径中每个 layer 都 `CreateTexture`，导致同尺寸全屏 transient 数量随 layer 增长。
- Feature 自己维护 `TempA/TempB/TempC`，但资源命名、debug、生命周期和跨 Feature 复用无法统一。

原则上，纯图像域先走 ImageChain；只有当 effect 需要多分辨率、历史帧、分支合成、original source、AOV/depth/normal 或多输出时，才向 FilterGraph / ResourceRegistry 申请额外资源。

---

## 10. RenderGraph / FrameGraph 的意义

随着你后面中间 buffer 和阶段越来越多，手写 RT 分配、释放、切换、Blit、pass 排序会越来越混乱。

RenderGraph / FrameGraph 的意义是：

- 自动管理资源生命周期
- 自动 alias transient RT
- 自动排序 pass 依赖
- 更容易接入并行处理
- 更容易加调试可视化

你现在已经具备了非常适合迁移到 RenderGraph 的系统规模。但按当前仓库看，重点不是“从零引入 RenderGraph”：AOV、OIT、HoSSS、HoPost、Shoost、HoShadowCast、角色特化都已经有 `RecordRenderGraph` 或 RenderGraph path。

因此后续重构的审查标准不能停留在“是否有 RenderGraph path”，而要检查它是否真的按 RenderGraph 写：

- 每个 pass 的所有输入纹理、输出纹理、buffer 和状态依赖都要显式声明。
- 每个中间资源都要通过统一资源声明层或 RenderGraph 创建，不能由 Feature 私有维护长期链路。
- shader 中采样的跨 pass 纹理必须能在 C# pass 声明里找到对应 `UseTexture` / blit source / resource declaration。
- 允许用全局 shader property 做最终绑定，但不能用全局 property 代替资源生命周期和依赖声明。
- 临时诊断代码如果用到了绕路绑定，必须在验收前删除或改成正式 RenderGraph 依赖。

真正需要推进的是：

- 统一各 Feature 的资源命名和生命周期规则。
- 减少每个 Feature 自己维护临时 RT、拷贝、blur、debug 的重复逻辑。
- 让跨 Feature 依赖显式化，例如 HoSSS 消费 HoAOV，角色特化消费 HoAOV 与 HoCharacterCapture，Shoost 可选消费 AOV composite。
- 把旧 compatibility path 作为行为对照，避免迁移时遗漏功能；新 `HoUrp-Extensions` 不应再以双路径长期维护为目标。

---

## 11. 关于“后处理吃 HOAOV 但写得机械”的问题

你说目前两个后处理系统已经能吃到 HOAOV，但写得比较机械。这个问题的本质是：

- 语义已经有了
- 但数据组织和消费方式还没有体系化

也就是说，系统现在能用，但还没形成“可声明、可查询、可复用”的架构。

后续你应该考虑：

- 每个语义项的注册表
- 每个后处理模块声明自己消费什么
- 每个渲染阶段声明自己生产什么
- 让系统自己做依赖链接，而不是手动拼接

这样后处理就不再只是“吃一个图”，而是“消费一组带语义的输入”。

---

## 12. Geometry / Deformation 相关的进一步分层

后面我们又讨论到了一个更细的层次：

> 如果在 SkinnedMeshRender 之前，就用顶点缓存、线长、线宽、mask 之类的数据先输出出去，然后后面再做形变计算，这些东西分在哪儿？

这个问题的答案是：这已经进入 **Geometry Processing Domain**，或者更准确地说是 **Deformation Domain**。

---

### 12.1 为什么它不属于 ObjectDomain

ObjectDomain 说的是：

> “这个东西是谁”

而你这里的线长、线宽、顶点缓存、rest distance、curve data 这些，不是对象身份，而是几何结构和变形输入。

---

### 12.2 为什么它也不属于 MaterialDomain

MaterialDomain 说的是：

> “这个表面是什么材质”

而这些线长、线宽、顶点缓存、曲线拓扑、骨骼权重之类，明显属于几何结构和运动形变，而不是材质属性。

---

### 12.3 更合理的划分：Geometry Domain / Deformation Domain

你可以把这部分单独提出来：

#### Geometry Domain

负责：

- Mesh topology
- Vertex attributes
- Rest state
- Curve data
- Bone weights
- Strand data
- Static structural information

#### Deformation Domain

负责：

- Skinned position
- Morph result
- Cloth result
- Hair simulation
- Velocity
- Stretch
- Compression
- Corrective deformation

也就是说：

# “哪些数据在形变前成立，哪些数据要等形变后才有”

这就是这一层最重要的边界。

---

## 13. 角色与场景的功能设计：从 Layer/Tag 到 Capability

你后面又提出了一个非常关键的问题：UI 与用户交互怎么设计。

你观察到 HDRP / URP 在这块做得不够理想，常见做法是：

- tag
- layer
- rendering layer
- light layer
- shadow layer
- 各种散落在不同地方的分类

这会让功能语义分散、用户难以直观控制，也会让系统行为越来越隐式。

你的思路更偏向于：

# 显式地给某个东西挂功能，而不是通过继承或隐式分类去猜

这非常重要。

---

### 13.1 为什么 Layer/Tag 不够用

Layer/Tag 的问题是：

- 它们主要是分类，而不是能力
- 往往是排他的，不能很好表达“一个对象同时具备多种能力”
- 容量有限，语义容易膨胀
- 在复杂管线里会变得分散、隐式、难以维护

例如，一个角色可能需要同时：

- 接收 SSS
- 参与 Outline
- 参与 Stylized Shadow
- 不参与 Fog
- 使用自定义 SSR
- 对某些后处理开放

如果靠 Layer 去表达，这个系统会很快失控。

---

### 13.2 你的思路：Capability System

更合理的方式是：

# 功能是附加的，是可声明的，是显式组合的

例如，对一个对象挂一个组件，维护一张能力列表：

- ReceiveSSS
- CharacterOutline
- AnimeShadow
- IgnoreFog
- CustomSSR
- HairSpecular
- ReceiveDropShadow

这说明：

- 对象是什么是一回事
- 它具有什么渲染能力又是另一回事

这就是 **Capability-based Design**。

---

### 13.3 Capability + Policy

还可以继续细化：

- **Capability**：它允许什么
- **Policy**：它以什么规则参与

例如：

- Capability: ReceiveSSS
- Policy: SSSQuality = High / Low

这样，系统会同时兼顾可控性与灵活度。

---

### 13.4 为什么这比继承更好

继承型设计容易导致：

- 类型爆炸
- 子类越来越多
- 规则交叉越来越复杂
- 很难组合出新的行为

而 capability 是正交的、可组合的，更适合你的 RP。

---

## 14. Debug 层：为什么它是核心系统，而不是附带功能

你提到一个非常关键的现实问题：

- 你所有 RenderFeature 上都有 debug mode
- 可以直接绘制到视图里
- 但经常忘记谁开着、谁没关
- fallback 叠在下面会产生误导

这说明你已经到达了一个阶段：

# Debug 自身必须成为一个正式的系统层

而不是每个 feature 各自画一下。

---

### 14.1 现有问题的本质

当前问题不是“debug 画不出来”，而是：

- debug 状态分散在各个 feature 里
- 没有统一的激活管理
- 没有统一优先级
- 没有统一生命周期
- 没有统一可视化层级
- 没有统一的查询入口

所以一旦功能多了，就会出现：

- 叠加误导
- 残留状态
- 难以排查
- 忘关某个 debug

---

### 14.2 Debug 应该被看成一个 Domain

更合理的方式是把它提升成：

# Debug Domain

它不属于某个单独 RenderFeature，而属于整个 RP 的基础系统。

也就是说：

- 生产功能是一回事
- Debug 观察是另一回事
- Debug 的生命周期、优先级、输出方式都应该统一管理

---

### 14.3 建议的 Debug Framework

你可以考虑以下几个部分：

#### 1. Debug Manager

统一维护：

- 当前激活的 debug 项
- 它们的优先级
- 它们的显示模式
- 是否允许叠加
- 是否只显示一个

#### 2. Debug Source Registry

每个 RenderFeature 声明它能提供哪些 debug 视图，例如：

- SSRRay
- SSRMask
- ShadowCascade
- HOAOV_Normal
- SSSWeight
- MotionVector

#### 3. Debug Overlay / Debug Composite

不要让每个 feature 直接抢屏幕，而是把所有调试结果纳入一个统一的 DebugGraph 或 DebugComposite 流程里。

#### 4. Debug HUD

在 UI 上可视化：

- 当前激活了哪些 debug
- 哪个正在显示
- 当前谁在覆盖谁
- 当前 debug 的来源是什么

#### 5. Debug 生命周期管理

避免“忘记关”。

例如：

- 一旦切换场景，自动重置
- 一旦切换预设，自动重置
- 一旦调试对象变化，自动重置

---

### 14.4 为什么 Debug 层极其关键

因为你现在已经有：

- 多 Pass
- 多 Domain
- 多 Feature
- 多 History
- 多后处理栈
- 多语义输出

这时如果没有统一调试，系统复杂度会迅速失控。成熟的 RP 竞争力不只在“能做多少效果”，还在：

- 能不能看懂自己在干什么
- 能不能快速定位问题
- 能不能稳定复现和验证

---

## 15. UI 与交互设计：你真正要做的是“能力配置界面”

你提到想在 UI 上直接拉一个组件，维护一堆列表，让用户显式指定某个东西具有什么功能。

这个方向非常正确，因为它和你的 Capability System 是一致的。

---

### 15.1 UI 不应该只暴露分类

传统 UI 常常暴露的是：

- 层
- 标签
- 渲染队列
- 一些零散的开关

问题是这些东西本身不是最终目标，用户真正想要的是：

- 这个角色要不要接收 SSS
- 这个灯要不要 cast
- 这个物体参与不参与某类阴影
- 这个对象属于哪个语义组
- 这个材质允许哪些附加能力

也就是说，UI 要展示的是 **能力和策略**，不是孤立的分类项。

---

### 15.2 建议的 UI 模型

你可以考虑：

#### 对象级组件

对象上挂一个“渲染能力组件”，里面是列表式配置：

- 功能开关
- 所属组
- 接收规则
- 输出规则
- Debug 选项

#### 材质级组件

材质上可以定义：

- 支持哪些 shading 扩展
- 输出哪些 shading semantic
- 支持哪些过滤参与

#### 光照级组件

灯光上显式配置：

- 是否 cast shadow
- 是否 soft shadow
- 是否参与某组对象
- 是否输出额外信息到特定通道

这样，所有能力都变成显式附加，而不是通过隐式继承或层级自动推断。

---

### 15.3 为什么这种 UI 结构更适合你

因为你的系统目标不是做一个简单的场景编辑器，而是做一个可以持续扩展的 RP。显式能力配置有几个好处：

- 用户知道自己在开关什么
- 系统行为更可预测
- 功能组合更灵活
- 不容易被 Layer/Tag 限死
- 更适合语义驱动和角色特化

---

## 16. 一个更完整的分层图景

综合上面的讨论，你的系统可以被理解成如下层次：

### 16.1 Scene Domain

- Object
- Instance
- Visibility
- Render Layer
- Culling 信息

### 16.2 Geometry Domain

- Mesh topology
- Vertex attributes
- Rest data
- Bone weights
- Curve / Strand 信息

### 16.3 Deformation Domain

- Skinning
- Morph
- Cloth
- Hair
- Velocity
- 其它动态形变结果

### 16.4 Semantic Domain

- ObjectID
- MaterialID
- PartID
- LightingGroup
- RSUV Group
- Feature Flags

### 16.5 Shading Domain

- Surface response
- Lighting response
- Stylized terms
- SSS / Spec / Rim / Ramp 等结果

### 16.6 Image Domain

- Bloom
- SSR
- TAA
- DOF
- ToneMap
- 其它图像空间效果

### 16.7 Composite Domain

- 语义感知合成
- 角色特化后期
- 图像与语义共同驱动的最终合成

### 16.8 Debug Domain

- 调试视图
- 调试优先级
- 调试叠加
- 调试生命周期
- 调试 HUD

### 16.9 Capability Domain

- 对象/材质/灯光/角色可附加的功能集合
- 显式配置
- 可组合策略

---

## 17. 为什么这套思路特别适合你现在的项目

你现在的系统已经天然长成了“角色优先、语义优先、合成优先”的形态，因此非常适合用这种分层方式来继续扩展。

这套方式的好处是：

- 不容易被 shader keyword 爆炸拖死
- 不容易被中间 RT 失控拖死
- 角色和场景可以分开治理
- 纯图像后期和语义后期可以分开治理
- 几何、形变、着色、合成都可以独立演化
- UI 与用户交互可以围绕显式能力展开
- Debug 可以从一开始就成为一级系统

---

## 18. 我们最终对系统方向的判断

你现在的 RP 方向，不像传统游戏渲染管线，更像：

- 一个带语义层的实时合成器
- 一个角色优先的混合式渲染系统
- 一个介于游戏引擎和离线合成之间的渲染框架
- 一个拥有显式能力配置与统一 Debug 层的工具化渲染平台

它更像：

- 语义驱动的渲染器
- 角色特化的实时电影化管线
- 可扩展的 AOV + Composite 架构
- 能让用户显式配置功能的能力型系统

而不是单纯的：

- Forward
- Deferred
- 一个个孤立的 RenderFeature

---

## 19. 下一步最值得推进的事情

### 第一优先级：定义新 RP 的核心契约，并盘点旧 ABI 作为迁移参照

当前最先要做的不是重写模块，也不是给旧材质系统补一个更厚的桥，而是先定义新 RP 自己的核心契约。旧系统接口需要被记录下来，但它们的定位是迁移参照、能力验收清单和断点说明，不是未来必须长期兼容的公共 ABI。

至少包括：

- 新 RP 的语义命名、资源命名、Feature 声明、Pass 时机和 Debug View 命名。
- 新材质未来需要实现的 producer/consumer 接口，例如写入哪些语义、读取哪些管线资源、声明哪些能力。
- 旧系统现状清单：`HoAOV`、`HoAOVSSS`、`HoCharacterCapture`、`lilToonOIT`、`_lilHoAov*`、`_lilOIT*`、`_HoShadowCast*`、`_LILPBRPlanarReflectionTexture`、`_HoAov*`、`_HoSSS*`、`_HoShadowStrength`、`_lilOITEnabled`、`_UsePlanarReflection`、renderer user value 打包格式。
- 新旧差异表：哪些行为保留，哪些重命名，哪些删除，哪些必须由新材质系统重新生产。
- pass 时机基线：HoShadowCast、HoAOV、HoSSS、OIT、HoCharacter、HoPost、Shoost 当前相对顺序可作为迁移验证，不作为最终顺序锁死。

### 第二优先级：把语义分层写清楚

至少要明确：

- ObjectDomain
- MaterialDomain
- GeometryDomain
- DeformationDomain
- ShadingDomain
- LightingDomain
- ImageDomain
- CompositeDomain
- DebugDomain
- CapabilityDomain

当前 HoAOV 资源应该先映射到这些 Domain，而不是先大规模改名。

### 第三优先级：建立统一的语义注册机制

- 每个语义项的名字
- 属于哪个 Domain
- 谁生产
- 谁消费
- 生命周期多长
- 分辨率/精度/格式如何

第一版可以直接从现有 HoAOV texture、HoPost AOV rule、HoSSS input、HoCharacter input 反推。

### 第四优先级：整理 Feature Descriptor 和依赖关系

每个 feature 至少声明：

- 需要哪些 `LightMode`
- 生产哪些资源/语义
- 消费哪些资源/语义
- 依赖哪个 pass 时机
- 旧实现是否有 RenderGraph/compatibility 双路径，以及新实现是否只需要 RenderGraph 路径
- 提供哪些 debug view

### 第五优先级：整理滤波后端

- 统一 Blur / Downsample / Upsample / Temporal / Bilateral
- 让 HoSSS、Shoost Glow/IrisBlur/RGBBlur、HoPost DoF、未来 AO/SSR/水体共享底层能力
- 同步定义 ImageDomain 的 ImageChain 双缓冲执行模型：纯图像 pass 默认复用 `Image.WorkA` / `Image.WorkB`，只有声明过的多分辨率、历史帧、original source、AOV/depth/normal 或多输出需求才能额外申请资源

### 第六优先级：收敛 RenderGraph 资源管理

把资源生命周期、依赖顺序、临时 RT 复用统一起来。当前不是“有没有 RenderGraph”的问题，而是多个 Feature 的 RenderGraph path 还缺统一资源目录、统一 debug 和统一依赖声明。

这一步必须把“禁止私自创建链路”作为验收项：Feature 之间只能通过登记过的资源和声明过的 pass 依赖连接；不能靠私有 RT、全局纹理副作用、固定执行顺序或 shader 里偷偷采样未声明纹理来完成数据传递。

对纯图像域还要增加一个验收项：线性 image stack 的中间全屏资源数量不能随 layer 数量增长。RenderGraph 中可以有多个 pass，但它们应通过统一 ImageChain 的读写交换来表达，而不是每层创建独占同规格 RT。

### 第七优先级：迁移语义后处理层

在 AOV、对象语义、材质语义、SSS 输入和 SSS 最小闭环都成立之后，可以开始把 `SemanticPostProcess` 从“读取 AOV 的 probe”推进为正式 HoPost 方向的语义后处理层。

这一阶段不应迁移 Shoost final image stack，也不应把旧 HoPost 全量效果搬进来。重点是先建立：

- `SemanticPostLayer` 数据模型。
- AOV rule source / operator / combine 子集。
- `SemanticPost.Mask` debug 观察路径。
- `SemanticTint` 等最小 layer composite。
- 读 camera color 再写回 camera color 的 RenderGraph copy 规则。

旧 HoPost 的价值是提供 rule/layer/effect 行为参照，不是定义新 ABI。第七阶段执行按 `Documentation~/rp重构第七步执行计划.md` 推进。

### 第八优先级：建立 Capability UI

让对象、材质、灯光、角色的功能都显式可配，而不是靠隐式层和 tag 猜测。`HoAovSubject` / `HoAovGroup` 可以作为第一版对象语义 UI 的基础，而不是推倒重做。

第八阶段先做 **Capability UI / Authoring 最小闭环**，不做完整 Capability 调度器，不做 Light Capability，不做新材质系统，也不做 Debug Framework。重点是把现有 `ObjectSemanticAuthoring` / `MaterialSemanticAuthoring` 从临时测试组件整理为可复用的对象/材质语义入口：

- `ObjectSemanticAuthoring` inspector：对象参与、object custom 0-7、id/group/flags、常用对象 preset。
- `MaterialSemanticAuthoring` inspector：material class、SSS policy、material custom 0-3、常用材质 preset。
- Capability 表示“允许参与什么”，Policy 表示“如何参与”，UI 只能写入已定义语义和策略值。
- 旧 `HoAovSubject` / `HoAovGroup` 只作为行为参照，不继承旧全局优先级系统，也不把 renderer user value 作为唯一核心路径。

第八阶段执行按 `Documentation~/rp重构第八步执行计划.md` 推进。

### 第九优先级：整理 AOV 生命周期与 RSUV 静态语义前移

第八阶段之后，AOV 已经从单纯输出贴图推进为对象语义、材质语义、SSS 输入、语义后处理和 debug 共同消费的中间语义集合。继续扩展 Debug Framework 前，必须先回答每个 AOV 值的来源、生命周期、producer / consumer 和是否可提前绑定。

第九阶段先做 **AOV 生命周期整理 / Renderer Static Semantic 前移**：

- 把 `Object.Custom0-7`、`Object.Id`、`Object.GroupId`、`Object.Flags` 明确归类为 per-renderer 静态语义。
- 参考旧 `HoAovGroup.PackRendererUserValue()`，建立新包自己的 RSUV pack/unpack helper。
- 让 RSUV 成为 `ObjectSemanticAuthoring` 的可选 fast path，而不是唯一 ABI。
- 保留 MPB 作为通用回退路径。
- 明确 AOV fallback shader 的读取优先级：RSUV -> MPB -> fallback default。
- 输出 AOV 生命周期表，供后续 Debug Framework 查询。

旧 RSUV 的价值是证明 renderer 级静态语义可以提前到 AOV pass 之前，不是要求迁移旧 `HoAovGroup` 的全局 priority resolver。第九阶段执行按 `Documentation~/rp重构第九步执行计划.md` 推进。

### 第十优先级：材质系统重构

材质重构应排在新 RP 契约定义之后。`lilToon/lilPBR` 现在已经能对接旧 RP 扩展，说明现有能力链路可行；但新材质系统不应继承它们的厚 UI、历史 keyword 和旧属性体系。下一步应按材质重构大纲推进模板、Feature Block、Preset、Generated Shader，并让它们直接实现新 RP 的语义/资源契约。

第十阶段先做 **材质系统接入新 RP 的契约准备**：冻结 SurfaceData / MaterialSemanticData / AOV 输出 ABI，建立 Feature Block / Preset 最小描述模型，并用一个 generated shader 原型证明材质可以直接生产新 `Aov.*` / `Sss.*` 语义。第十阶段执行按 `Documentation~/rp重构第十步执行计划.md` 推进。

### 第十一优先级：HoPost / Shoost 搬迁前置契约与最小 Stack

HoNpr 侧已经开始把材质系统重构为更显式、可管理的声明系统。HoUrp-Extensions 侧因此可以暂时跳过原本排在第十步后的 Weighted OIT runtime 视觉验证，优先搬迁旧 `HoPost` 与 `Shoost` stack。但这里的“跳过验证”只表示不把 OIT runtime 作为第十一步优先项，不表示 HoPost / Shoost 可以绕过契约、注册、RenderGraph 声明和测试。

第十一阶段改为建立后处理搬迁的基础设施：

- 区分 `HoPost` 语义感知后处理和 `Shoost` 最终图像风格栈。
- 定义 post effect / layer / stack descriptor，禁止直接复制旧 enum / shader property 作为新 ABI。
- 建立 frame-local PostGraph / ImageChain 计划层，让启用的 layer/effect 在当前 frame 动态声明需要的资源、输入和 debug view。
- 引入可动态注册/注销的 RenderGraph 资源请求模型：启用的 effect 才请求 transient resources，关闭后不保留 stale handle、全局纹理或 debug active 状态。
- 让纯图像链默认走 `ImageChain.Read -> pass -> ImageChain.Write -> swap` 双缓冲；多输入、多输出、多分辨率、history、AOV/depth/normal/original source 等需求必须显式升级资源类型。
- 先迁移最小可验证集合：HoPost rule mask / layer blit 与 Shoost single-pass image effect，不急于搬完整 effect catalog。

第十一阶段执行按 `Documentation~/rp重构第十一步执行计划.md` 推进。验收重点是 **Post stack 的显式声明、动态资源生命周期、多输入约束和双缓冲资源复用**，不是一次性完成旧 HoPost / Shoost 的视觉等价。

### 第十二优先级：Weighted OIT runtime 验证或 Post Stack 扩展

第十二阶段根据第十一步结果再决定优先方向：

- 如果透明链路需要先闭环，则回到 Weighted OIT runtime 最小验证：`Oit.OpaqueColor`、`Oit.Accumulation`、`Oit.Revealage`、`Oit.CompositeSource`、clear、draw `HoUrpOitAccumulation`、composite 和 debug view。
- 如果后处理搬迁成为主线，则扩展 PostGraph：HoPost rule language 完整化、Shoost effect catalog 分批迁移、AOV composite 约束、多 pass effect、history / pyramid / original source 资源策略。

旧 `WeightedOITRendererFeature`、`WeightedOITSettings`、`WeightedOIT.hlsl`、`WeightedOITComposite.shader`、旧 `HoPostProcessRendererFeature` 和旧 `ShoostPostProcessRendererFeature` 都只作为行为参照。新实现必须使用新资源名、RenderGraph 声明、Feature Descriptor 和 frame-local resolve，不继承旧全局名、compatibility path 或隐式执行顺序。

---

## 20. 工程化结构设计（第一版）

这一部分开始不再只讨论理念，而是尝试把整个 RP 转化成真正可落地的工程结构。

目标不是立即实现所有东西，而是：

- 明确模块边界
- 明确系统职责
- 明确资源流向
- 明确注册与依赖关系
- 让未来扩展不至于失控

---

# 20.1 推荐的顶层模块结构

建议整个 RP 按以下一级模块组织。

```text
Runtime/
├── Core/
├── RenderGraph/
├── Resource/
├── Semantic/
├── Capability/
├── LegacyInterop/
├── Geometry/
├── Deformation/
├── Lighting/
├── Shading/
├── Composite/
├── PostProcess/
├── Filter/
├── Debug/
├── Feature/
├── UI/
└── Tools/
```

其中 `LegacyInterop/` 不是新 RP 的长期桥接层，而是迁移期的事实记录与验证工具。它可以保存当前 `lilToon/lilPBR` 的旧 ABI 清单、对照表、调试验证脚本和一次性迁移适配；但核心 runtime 不应该通过它来设计新材质系统。新材质后续要直接面向 `Semantic/`、`Resource/`、`Feature/` 和 `Capability/` 的正式契约。

---

## 20.2 Core

核心运行时基础。

负责：

- RP 生命周期
- Frame Context
- Render Context
- Pass 调度
- 全局状态
- 配置加载
- Feature 启停
- 全局事件

建议内容：

```text
Core/
├── RPContext
├── FrameContext
├── CameraContext
├── RenderSettings
├── RPBootstrap
├── FrameScheduler
└── GlobalState
```

---

## 20.3 RenderGraph

负责真正的帧依赖管理。

这里不建议另造一套完全替代 URP RenderGraph 的系统。当前项目已经在 URP 17.x 上实现多条 `RecordRenderGraph` 路径，合理做法是先建立项目自己的 **Feature/Resource 声明层**，再落到 URP RenderGraph。

这个声明层的职责不是包装出第二套私有图，而是约束所有 Feature 严格落到 URP RenderGraph：资源由声明层登记，pass 在 URP RenderGraph 中显式读写，debug 也从同一套登记资源里取数。任何绕开声明层的私有 RT 链路、全局纹理链路或 shader 隐式采样，都应被视为架构违规。

建议职责：

- Pass 注册
- Resource 生命周期
- Pass Dependency
- Transient RT Alias
- Async Compute
- Barrier
- Pass Culling
- Graph Debug View

推荐结构：

```text
RenderGraph/
├── GraphBuilder
├── GraphPass
├── GraphResource
├── ResourceHandle
├── PassDependency
├── PassCompiler
├── ResourceLifetime
└── GraphDebugger
```

---

## 20.4 Semantic System

这是整个 RP 的核心之一。

负责：

- Semantic 注册
- Domain 分类
- Semantic Producer
- Semantic Consumer
- Semantic 生命周期
- Buffer 映射
- Debug 映射

推荐结构：

```text
Semantic/
├── SemanticRegistry
├── SemanticDefinition
├── SemanticDomain
├── SemanticProducer
├── SemanticConsumer
├── SemanticBufferBinding
├── SemanticFormat
└── SemanticDebugInfo
```

---

## 20.5 Capability System

负责“功能附加”而不是“Layer 分类”。

核心思想：

- 功能显式声明
- 功能正交组合
- 不依赖继承
- 不依赖 Layer

推荐结构：

```text
Capability/
├── CapabilityRegistry
├── CapabilityDefinition
├── CapabilityComponent
├── CapabilityMask
├── CapabilityPolicy
├── CapabilityResolver
└── CapabilityUIBinding
```

---

## 20.6 推荐的 Capability 类型

### Object Capability

例如：

```text
ReceiveSSS
ReceiveOutline
ReceiveFog
ReceiveDropShadow
ParticipateSSR
ParticipateStylizedShadow
```

---

### Material Capability

例如：

```text
HairLighting
SkinLighting
StylizedRamp
MatcapSupport
WetnessSupport
```

---

### Light Capability

例如：

```text
CastShadow
SoftShadow
StylizedShadow
CharacterOnly
SceneOnly
```

---

### Composite Capability

例如：

```text
CharacterBloom
SelectiveColorGrade
OutlineComposite
```

---

## 20.7 Capability 与 Policy 的区别

推荐正式拆开：

### Capability

说明：

```text
允许参与什么
```

例如：

```text
ReceiveSSS
```

---

### Policy

说明：

```text
如何参与
```

例如：

```text
SSSQuality=High
ShadowSoftness=Medium
OutlineMode=Anime
```

---

## 20.8 Geometry / Deformation 模块

### Geometry

负责：

- 静态 mesh
- 顶点属性
- 拓扑
- RSUV
- Strand
- Curve

推荐结构：

```text
Geometry/
├── MeshCache
├── VertexLayout
├── RSUVData
├── CurveData
├── StrandData
└── GeometrySemantic
```

---

### Deformation

负责：

- Skinning
- Morph
- Cloth
- Hair Sim
- Velocity
- Corrective

推荐结构：

```text
Deformation/
├── SkinningPass
├── MorphPass
├── ClothPass
├── HairSimPass
├── VelocityBuilder
└── DeformationCache
```

---

## 20.9 HOAOV 工程化拆分

建议不要再把 HOAOV 看成单一 RT。

而应该：

# HOAOV 是一个 Semantic Collection

---

推荐：

```text
HOAOV/
├── HOAOV_Base
├── HOAOV_Shading
├── HOAOV_Lighting
├── HOAOV_Composite
└── HOAOV_Debug
```

按当前代码，更贴近实际的第一版目录可以是：

```text
HoAOV/
├── Contract/
│   ├── HoAovSemanticNames
│   ├── HoAovTextureBindings
│   └── HoAovRendererUserValue
├── Producer/
│   ├── HoAovSubject
│   ├── HoAovGroup
│   ├── HoAovPassContract
│   └── HoAovFallback
├── Resources/
│   ├── MaskId
│   ├── NormalDepth
│   ├── TangentNormal
│   ├── SurfaceData
│   ├── CustomChannels
│   ├── ObjectCustomChannels
│   └── SssSource
├── Consumer/
│   ├── HoPostAovRules
│   ├── HoSSSInput
│   ├── HoCharacterInput
│   └── ShoostAovComposite
└── Debug/
```

这样能直接覆盖现有实现，而不是用一个过早理想化的目录把已存在资源打散。

---

### HOAOV_Base

前置语义：

- ObjectID
- MaterialID
- GroupID
- RSUVGroup
- FeatureFlags

---

### HOAOV_Shading

着色结果：

- SSSWeight
- StylizedShadow
- SpecularMask
- Ramp

---

### HOAOV_Lighting

光照结果：

- ShadowFactor
- Indirect
- Volumetric
- Caustic

---

### HOAOV_Composite

后合成输入：

- CharacterMask
- CompositeRegion
- SpecialBlend

---

## 20.10 Filter Backend

这是未来非常关键的基础设施。

建议所有滤波统一进入 FilterGraph。

推荐结构：

```text
Filter/
├── Blur/
├── Temporal/
├── Reconstruction/
├── Pyramid/
├── EdgeAware/
└── FilterGraph/
```

---

## 20.10.1 ImageChain / 纯图像域双缓冲

`ImageDomain` 需要一个比具体滤波算法更底层的执行器，用来承载纯图像空间的线性 pass 链。建议称为 `ImageChain` 或 `ImagePostChain`。

职责：

- 管理当前图像的 read/write 工作纹理。
- 提供 `WorkA` / `WorkB` 双缓冲。
- 在每个 RenderGraph pass 后交换 read/write。
- 在需要时提供一次显式 `OriginalSource` copy。
- 把最终结果交还给 camera color / final output。
- 向 Debug Framework 暴露当前 image pass、read/write 资源和 swap 状态。

推荐结构：

```text
Image/
├── ImageChain
├── ImageChainContext
├── ImagePassDescriptor
├── ImageWorkTexturePool
├── ImageOriginalSource
└── ImageDebugView
```

基础执行模型：

```text
Begin(source)
  WorkA = copy/import source
  WorkB = same descriptor transient

For each pure image pass:
  Read  = current
  Write = alternate
  Record pass(Read -> Write)
  Swap()

End()
  Publish current as Image.Final
  Copy/alias to camera color when required
```

这套机制主要服务 Shoost / ImagePost 这类 final image stack。它不替代 HoPost、HoSSS、OIT、角色特化或任何语义合成模块；这些模块如果读取 AOV、depth、normal、object id、material id 或 profile，就必须声明对应 Semantic / Geometry 输入。

允许申请额外资源的例外：

- `Pyramid`：多分辨率 bloom、Kawase chain、mipmap-like down/up sample。
- `History`：TAA、temporal accumulation、motion trail。
- `Branch`：一个 pass 同时需要 original source 和 filtered result。
- `MultiOutput`：MRT 或多个逻辑输出。
- `SemanticInput`：AOV / depth / normal / mask 参与决策，此时不再是纯 ImageDomain。

验收标准：

- 线性纯图像 pass 数量增加时，全分辨率工作 RT 不应按 pass 数量增长。
- 每个 pass 的 source / destination 仍必须在 RenderGraph 中显式声明。
- `WorkA` / `WorkB` 不能作为长期公共资源发布，只能作为 frame transient 工作区。
- Debug view 应能显示 ImageChain 当前 pass 的 read/write handle 和最终输出来源。

---

## 20.11 推荐统一的 Filter API

例如：

```cpp
FilterRequest
{
    Source
    Destination
    FilterType
    Radius
    Iteration
    SemanticMask
    TemporalMode
}
```

纯图像 pass 不应该直接从 `FilterRequest` 开始。更合理的是先声明 `ImagePassDescriptor`：

```cpp
ImagePassDescriptor
{
    Name
    Input = ImageChain.Current
    Output = ImageChain.Next
    Shader
    PassIndex
    NeedsOriginalSource
    ExtraInputs
    DebugView
}
```

当 `ImagePassDescriptor` 发现自己需要 blur、pyramid、history 或 edge-aware 采样时，再向 FilterGraph 发出 `FilterRequest`。这样可以把“普通线性图像变换”和“真正需要额外滤波资源的算法”分开，避免每个简单 layer 都变成一次资源分配。

这样：

- SSR
- SSS
- Bloom
- AO
- Volumetric

都可以复用。

---

## 20.12 Debug Framework（工程版）

这是你未来最重要的系统之一。

建议直接做成一级模块。

推荐结构：

```text
Debug/
├── DebugManager
├── DebugRegistry
├── DebugView
├── DebugOverlay
├── DebugComposite
├── DebugHUD
├── DebugHistory
└── DebugCapture
```

---

## 20.13 Debug Registry

每个 feature 显式注册：

```cpp
RegisterDebugView(
    name,
    source,
    mode,
    output
)
```

例如：

```text
SSRMask
SSRRay
SSRResolve
ShadowCascade
SSSWeight
MotionVector
```

---

## 20.14 Debug 显示模式

建议支持：

```text
Replace
Overlay
Split
PictureInPicture
ChannelInspect
Heatmap
```

---

## 20.15 Debug 生命周期管理

推荐统一管理：

```text
FrameOnly
Sticky
Persistent
SceneLocal
Temporary
```

避免：

```text
忘记关闭某个debug
```

---

## 20.16 Global Debug HUD

建议始终显示：

```text
当前激活debug
当前覆盖来源
当前feature状态
当前graph pass
当前semantic输入
```

这样可以极大减少误判。

---

## 20.17 Feature System

你现在的 RenderFeature 已经不再只是 URP 那种简单 pass。

建议正式做成：

# Feature Module System

推荐结构：

```text
Feature/
├── FeatureRegistry
├── FeatureDescriptor
├── FeatureDependency
├── FeatureCapability
├── FeaturePassBuilder
└── FeatureDebugBinding
```

---

## 20.18 Feature 的推荐声明结构

例如：

```cpp
FeatureDescriptor
{
    Name
    Dependencies
    ProducedSemantic
    ConsumedSemantic
    RequiredCapability
    DebugViews
    Passes
}
```

---

## 20.19 UI Framework

建议不要把 UI 只当 Inspector。

而是：

# RP Control Surface

推荐结构：

```text
UI/
├── CapabilityPanel
├── SemanticViewer
├── GraphViewer
├── DebugPanel
├── ResourceViewer
├── LightingPanel
├── CompositePanel
└── FeatureInspector
```

---

## 20.20 推荐的对象 Inspector 结构

### Object Semantic

```text
ObjectID
CharacterID
GroupID
```

---

### Capability

```text
ReceiveSSS
ReceiveOutline
ParticipateFog
```

---

### Policy

```text
SSSQuality
OutlineType
ShadowMode
```

---

### Debug

```text
ShowSemantic
ShowMask
ShowLighting
```

---

## 20.21 推荐的灯光 Inspector

不要只有：

```text
Cast Shadow
```

而应该：

```text
Shadow Capability
Shadow Policy
Semantic Group
Receiver Group
Stylized Mode
Composite Participation
```

---

## 20.22 推荐的资源生命周期

建议资源分：

### Persistent

长期存在：

- History
- Temporal
- Cache

---

### FrameTransient

仅本帧存在：

- Blur RT
- Temp Semantic
- Intermediate Lighting
- ImageChain WorkA / WorkB
- ImageChain OriginalSource copy

---

### Imported

外部资源：

- CameraColor
- Depth
- External Texture

---

## 20.23 推荐的系统执行顺序

抽象顺序可以保持 Scene -> Geometry -> Deformation -> Semantic -> Shading -> Composite。按当前旧仓库核查，现有事实基线顺序是：

```text
Per-camera reset
  - _lilHoAovActive = 0
  - _lilOITActive = 0
  - _HoShadowCastActive / light counts reset

Object semantic binding
  - HoAovSubject / HoAovGroup
  - renderer user value / MaterialPropertyBlock

HoShadowCast
  - ShadowCaster pass -> _HoShadowCastAtlas
  - second directional atlas when enabled

Opaque / forward / gbuffer shading
  - old lilToon / lilPBR consume HoShadowCastAttenuation(positionWS)

HoAOV
  - LightMode = HoAOV
  - fallback material when native pass is missing
  - outputs mask/id, normal/depth, tangent normal, surface data, custom, object custom
  - LightMode = HoAOVSSS -> _lilHoAovSssTexture

HoSSS
  - consumes HoAOV mask/normal-depth/surfaceData/SSS source
  - source/diffusion/transmission/composite before transparents

Transparent / OIT
  - OIT opaque copy after skybox
  - LightMode = lilToonOIT accumulation before transparents
  - OIT composite after transparents

HoCharacterSpecialization
  - LightMode = HoCharacterCapture
  - eye reveal / hair drop shadow composite

HoPost
  - semantic-aware layer stack
  - HoAOV rule masks

Shoost
  - final image style stack
  - optional AOV composite for supported effects

Debug composite / overlays

Final Output
```

这个顺序不是最终设计，也不是新 RP 必须长期兼容的顺序。它只是迁移时用来确认“旧能力为什么能工作”的事实基线。未来如果把 HoShadowCast 接收改成屏幕空间 composite、把 HoAOV 拆出 pre-semantic pass，或者引入更完整的 FrameGraph，都应以新契约为准，并用这张表检查哪些旧行为需要被替代或删除。

---

## 20.24 推荐的长期方向

你这个 RP 的长期方向已经非常明确：

不是做：

```text
更多shader
```

而是做：

# 语义驱动 + 能力驱动 + 合成驱动 + 可调试驱动 的实时渲染平台

它更像：

- 实时合成器
- 实时电影化 renderer
- 风格化角色渲染平台
- Hybrid Film/Game Pipeline

---

## 20. 结尾总结

这次讨论的最大收获是：

# 你已经不再是在“堆效果”，而是在搭一套语义驱动、能力驱动、可调试、可组合的渲染系统。

而且你已经自然碰到了几个现代渲染器最核心的议题：

- 语义分层
- 数据生命周期
- 几何 / 形变 / 着色 / 合成的边界
- 中间 RT 的统一治理
- 滤波框架的统一
- 角色特化路径的独立性
- 显式能力配置
- Debug 体系化

如果后续继续推进，最值得优先完善的不是某个具体效果，而是这套系统的 **术语、分层、注册表、能力模型、调试体系和资源流向**。

一旦这些基础打稳，后面再加体积雾、水体、粒子、SSS、透明、风格化效果，都会顺很多。
## 新增原则：组分来源可追踪

新 RP / HoNpr 迁移旧能力时，来源必须作为正式元数据和命名的一部分。凡是仍以 `lilToon`、`lilPBR` 或旧 `lilToon-URP-Extensions` 行为作为验收基线的组分，都必须带来源后缀，例如 `GlitterLilToon`、`SecondaryMatCapLilToon`。来源后缀只说明迁移参考，不说明 ABI 继承。

这条规则服务于 RenderGraph-first 和显式语义原则：人和工具必须能从表格、preset、generated shader、debug view 看出某个组分是 HoNpr 原生能力，还是旧实现迁移能力。

---
