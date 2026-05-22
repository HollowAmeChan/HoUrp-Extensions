# RP 渲染架构对话总结（详细版）

> 这是对我们前面关于 HOAOV、RSUV、后处理、语义域划分、几何/形变/着色/合成边界、UI/交互设计、Debug 层设计等讨论的系统整理。

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

---

## 1. 当前已经存在的系统

你现在的系统其实已经非常有雏形了，而且远比一般自定义 RP 走得更深。

### 1.1 HOAOV

你定义了一个 HOAOV，它位于物体真实渲染之后，负责输出各种辅助数据与语义数据。它不是单纯的颜色缓冲，而是一个更宽泛的“场景辅助输出层”。

当前 HOAOV 里已经包含：

- 颜色
- 法线
- 世界法线
- 深度
- 物体 ID
- 材质 ID
- 遮罩
- 材质层面的 mask
- RSUV 相关 ID 组
- 光照分组
- 物体分组
- 角色部位分组

这说明 HOAOV 已经不是传统意义上的 GBuffer，也不只是普通 AOV，而是一个包含“语义”的多用途信息层。

### 1.2 HOPost

你有一个吃 HOAOV 的后处理栈。它的特点是：

- 可以直接读取语义信息
- 能做角色、材质、区域、光照组等定向后处理
- 不是完全图像空间的后期，而是“语义感知后期”

这非常接近离线合成里的 AOV 合成思路，只不过它发生在实时渲染里。

### 1.3 SHOPost

你还有一个纯图像空间的后处理栈，负责不依赖语义的信息处理，例如：

- Bloom
- 镜头畸变
- 色彩调整
- 颗粒
- 锐化
- 其它不需要知道对象是谁的效果

这说明你已经把后处理分成了两类：

- **语义感知后处理**（HOPost）
- **纯图像后处理**（SHOPost）

这是很合理、也很先进的划分。

### 1.4 OIT

你已经有 OIT 路径，说明透明物体已经不是简单地混进主流程，而是有独立的处理思路。

### 1.5 SSR（水面）

你已经做了水面的屏幕空间反射，说明水面已经不是一个单独的小特效，而是有自己的反射逻辑和后续扩展空间。

### 1.6 ShadowCast 多光源阴影

你还有额外的多光源阴影路径。这说明阴影系统与主渲染路径之间也已经开始分离。

### 1.7 角色特化 RenderFeature

你有角色特化的 RenderFeature，负责：

- 眼透
- DropShadow
- 角色特化的 HOAOV 消费

这说明“角色”已经不是普通场景物体，而是一个独立的渲染域。

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

为了让 HOAOV 能提前，我们建议把它拆成至少两层：

### 7.1 HOAOV_Base / PreSemantic

负责在较早阶段就能获得的内容：

- ObjectID
- MaterialID
- CharacterPart
- RSUV Group
- FeatureFlags
- Depth
- Normal
- Render Layer

这层本质上是“对象与材质的静态语义层”。

### 7.2 HOAOV_Shading / ShadingSemantic

负责真正着色以后才有的内容：

- SSSMask
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

---

## 10. RenderGraph / FrameGraph 的意义

随着你后面中间 buffer 和阶段越来越多，手写 RT 分配、释放、切换、Blit、pass 排序会越来越混乱。

RenderGraph / FrameGraph 的意义是：

- 自动管理资源生命周期
- 自动 alias transient RT
- 自动排序 pass 依赖
- 更容易接入并行处理
- 更容易加调试可视化

你现在已经具备了非常适合迁移到 RenderGraph 的系统规模。

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

### 第一优先级：把语义分层写清楚

至少要明确：

- ObjectDomain
- MaterialDomain
- GeometryDomain
- DeformationDomain
- ShadingDomain
- ImageDomain
- CompositeDomain
- DebugDomain
- CapabilityDomain

### 第二优先级：把 HOAOV 拆成可前置与不可前置两层

- PreSemantic / HOAOV_Base
- ShadingSemantic / HOAOV_Shading

### 第三优先级：建立统一的语义注册机制

- 每个语义项的名字
- 属于哪个 Domain
- 谁生产
- 谁消费
- 生命周期多长
- 分辨率/精度/格式如何

### 第四优先级：整理滤波后端

- 统一 Blur / Downsample / Upsample / Temporal / Bilateral
- 让 SSR、SSS、Bloom、AO 等共享底层能力

### 第五优先级：引入 RenderGraph

把资源生命周期、依赖顺序、临时 RT 复用统一起来。

### 第六优先级：建立 Capability UI

让对象、材质、灯光、角色的功能都显式可配，而不是靠隐式层和 tag 猜测。

### 第七优先级：建立 Debug Framework

把 debug 提升成一级系统，让它能查询、能切换、能重置、能叠加控制、能可视化。

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

---

### Imported

外部资源：

- CameraColor
- Depth
- External Texture

---

## 20.23 推荐的系统执行顺序

推荐：

```text
Scene
↓
Geometry
↓
Deformation
↓
PreSemantic
↓
Shadow
↓
Lighting
↓
Shading
↓
HOAOV_Shading
↓
Composite
↓
Image Post
↓
DebugComposite
↓
Final Output
```

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

