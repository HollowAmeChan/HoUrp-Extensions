# RP 设计哲学底线

> 每次开启新的 RP / 材质 / RenderFeature 重构对话前，先引用这份文档。它不是功能清单，而是不能被临时需求、旧项目惯性或局部实现方便性突破的设计边界。需要查旧实现时，再引用 `Documentation~/旧实现快速定位索引.md`。

---

## 0. 总原则

这套系统的目标不是“继续改一个旧 URP 扩展包”，也不是“给旧 `lilToon/lilPBR` 做更强的桥接层”，而是建立一套新的、语义驱动、RenderGraph-first、可长期扩展的 URP 渲染与合成架构。

旧系统里已经跑通的能力要尊重，但旧系统的结构、属性名、pass 名、UI 组织、keyword 习惯和隐式依赖都不能自然升级为新架构的底层规则。

---

## 1. 旧系统只能作为迁移参照，不能成为新 RP 的架构核心

`lilToon-URP-Extensions`、`lilToon`、`lilPBR` 的价值是：

- 证明 HoAOV、HoSSS、OIT、HoShadowCast、HoPost、Shoost、角色特化、Planar Reflection 等能力已经可行。
- 提供旧能力的行为基线和迁移验收标准。
- 暴露旧 ABI、pass 顺序、全局纹理、材质属性和 renderer user value 的真实依赖。

但新 RP 不以承接旧 ABI 为目标。

不能做的事情：

- 不能把 `_lilHoAov*`、`_HoAov*`、`_lilOIT*`、`lilToonOIT`、`HoAOV` 等旧名字直接视为长期公共 ABI。
- 不能为了让旧材质继续无痛运行，在新核心里建立长期 `Bridge` 层。
- 不能让旧材质系统的 inspector、keyword、include 结构反过来决定新 RP 的资源和语义模型。

允许做的事情：

- 建立 `LegacyInterop` 文档、对照表、一次性迁移工具和行为验证脚本。
- 用旧实现确认功能边界、渲染顺序和资源需求。
- 在迁移期做短期适配，但适配不能污染新核心契约。

---

## 2. 新 RP 的契约先于新材质系统

材质系统是后续接入新 RP 的 producer / consumer，不是新 RP 架构的源头。

必须先定义：

- 语义命名。
- 资源命名。
- Feature Descriptor。
- Pass 时机。
- Debug View。
- Capability 模型。
- producer / consumer 关系。

然后新材质系统再按这些契约生成 shader pass、include、属性和 UI。

不能做的事情：

- 不能先按旧材质属性和旧 shader pass 设计 RP。
- 不能让“材质里顺手输出”成为正式数据生产方式。
- 不能把材质 inspector 当成系统能力注册表。

---

## 3. 语义必须显式注册，不能隐式散落

这套 RP 的核心不是颜色输出，而是语义数据的生产、消费和生命周期管理。

每一个语义项都必须能回答：

- 它叫什么。
- 属于哪个 Domain。
- 谁生产它。
- 谁消费它。
- 在哪个阶段成立。
- 生命周期多长。
- 分辨率、格式、精度是什么。
- 如何 debug。

不能做的事情：

- 不能让 shader 隐式写全局纹理，然后由后处理偷偷读取。
- 不能让某个 effect 私有 RT 后来被别的模块绕路消费。
- 不能用 Layer/Tag/材质名字这种隐式规则替代正式 Capability。

---

## 4. HoAOV 是 Semantic Collection，不是一个万能 RT

HoAOV 不能再被理解成“一张或一组随手写的后处理辅助图”。

HoAOV 的职责是生产和缓存**跨 Feature 可复用的基础语义**，尤其是后处理、语义合成、角色特化和材质系统共同需要查询的数据。它不是所有中间 RT 的归档层，也不是 Debug 系统的资源列表来源。

HoAOV 可以承载的内容必须满足至少一个条件：

- 语义本身具有跨系统复用价值，而不是某个效果的内部计算结果。
- 数据表达的是对象、材质、几何或通用着色语义，而不是某个 RenderFeature 的私有执行状态。
- 其它系统即使不运行某个具体效果，也能独立理解这份数据的含义。
- 它能通过 Semantic / Resource Registry 说明 producer、consumer、生命周期、格式和 debug view。

HoAOV 应该被拆成不同生命周期的语义集合：

- Object / Renderer 静态语义。
- Geometry 可绘制语义。
- Material 静态语义。
- 少量确实通用的 Shading 派生语义。

当前旧实现里的 `MaskId`、`NormalDepth`、`TangentNormal`、`SurfaceData`、`Custom`、`ObjectCustom`、`SssSource` 可以作为第一版资源映射参考，但新系统必须重新定义正式语义表。旧实现里写在 HoAOV MRT 里的内容，不等于新系统里都属于 HoAOV 所有。

第一版 HoAOV 的正向职责：

- `MaskId`：对象覆盖、对象 id、分组 id、基础 flags。
- `ObjectCustom`：角色部位、区域、对象级开关等可复用对象语义。
- `SurfaceData`：材质 class、profile、厚度、曲率等材质/表面语义；其中 SSS profile 可以被 SSS 消费，但它仍然是材质语义，不是 SSS 运行时资源。
- `MaterialCustom`：可登记、可解释的通用材质自定义通道。
- `NormalDepth` / `TangentNormal`：当前阶段可由 HoAOV 生产，服务后处理和屏幕空间语义查询；长期如果 GeometryDomain 独立，应迁出为 Geometry cache，而不是继续扩大 HoAOV 概念。

HoAOV 不应该拥有的内容：

- SSS 的 diffusion、blur、composite weight、prepared source 等运行时资源。
- ShadowCast atlas、receiver attenuation、light slice 状态等 LightingDomain 资源。
- OIT accumulation / revealage 等透明合成资源。
- ScreenPost rule mask、ImagePost work texture、最终合成中间图等 Post / Image / Composite 资源。
- Debug tile、debug mode、debug overlay、debug atlas 等 DebugDomain 状态。

SSS 与 HoAOV 的正确关系是：SSS 可以消费 HoAOV 的通用材质、几何和对象语义，也可以因为当前物理 MRT 布局暂时复用 AOV pass 写入一份 SSS source input；但这份 input 的正式所有权、命名、debug view 和生命周期应归 `SubsurfaceScattering` 或 `ShadingDomain`，不能长期命名为 `Aov.SssSource`，也不能让 HoAOV Debug 代替 SSS Debug。

Debug 与 HoAOV 的正确关系是：Debug 可以观察 HoAOV 资源，但 Debug 不是 HoAOV 的子功能。跨 AOV、SSS、OIT、ShadowCast、Post cache 的调试视图应归 `DebugDomain` / Render Cache Debug 统一管理，由注册表声明每个 view 的 source resource 与 decode 方式，不能通过一个 `AovDebugMode` 枚举混合所有 Feature 的内部资源。

不能做的事情：

- 不能把 depth / normal 混进纯对象静态语义。
- 不能把 SSS source、profile、thickness、curvature 这类材质派生语义假装成对象元数据。
- 不能为了方便继续扩张一个越来越大的 HoAOV 黑箱。
- 不能让 HoAOV 成为 SSS、ShadowCast、OIT、Post、Debug 的运行时资源垃圾桶。
- 不能因为某个资源暂时由 AOV pass 的 MRT 写出，就默认它属于 AOV Domain。
- 不能把 `AovDebug` 当成全局 Render Cache Debug；AOV 调试只能显示 AOV 自己拥有的通用语义资源。

---

## 5. Domain 边界不能混

至少要保持这些 Domain 的边界：

- `ObjectDomain`：对象、角色、部件、实例、分组。
- `MaterialDomain`：材质类型、表面模型、profile、capability。
- `GeometryDomain`：mesh、normal、depth、tangent、coverage。
- `DeformationDomain`：skinning、morph、cloth、velocity。
- `ShadingDomain`：SSS weight、stylized shadow、specular mask、ramp 等着色结果。
- `LightingDomain`：shadow、light group、indirect、volumetric。
- `ImageDomain`：纯图像空间效果。
- `CompositeDomain`：角色特化、语义合成、最终合成。
- `DebugDomain`：所有 debug view 和 overlay。
- `CapabilityDomain`：对象、材质、灯光、feature 可用能力。

不能做的事情：

- 不能把对象分类、材质分类、着色结果、后处理遮罩混成同一个“mask”概念。
- 不能让一个 RenderFeature 同时偷偷定义语义、生产资源、消费资源、决定 UI，还不登记。
- 不能用实现所在文件夹替代 Domain 归属。

---

## 6. RenderGraph-first 是新包方向

`HoUrp-Extensions` 的方向是 URP-only、Unity `6000.3+`、RenderGraph-first。

旧 `ScriptableRenderPass` / compatibility path 可以作为行为参照，但不应成为新包长期双路径维护目标。

这里的 RenderGraph-first 不是“有 `RecordRenderGraph()` 就算完成”，而是必须严格按 RenderGraph 的资源声明、读写关系和生命周期模型来写。Feature 不能私自创建一条隐藏链路，也不能靠全局纹理、执行顺序或某个 RT “刚好存在”来维持跨 pass / 跨 Feature 数据流。

必须坚持：

- pass 明确声明读写资源。
- 临时 RT 生命周期交给 RenderGraph 或统一资源层。
- 跨 Feature 依赖显式化。
- 全局纹理只作为必要的 shader binding，不作为资源生命周期管理方式。
- shader 采样的每一个跨 pass 资源，都必须在对应 RenderGraph pass 中声明为输入或由明确的资源层绑定。
- 多 pass 链路必须表现为 `producer resource -> consumer resource`，不能表现为私有 RT、隐式全局状态或外部副作用。
- 如果某个全局纹理会被 URP 内置 pass 通过 `UseAllGlobalTextures(true)` 间接纳入依赖，不能把当前正在作为 render attachment 写入的 camera color 直接发布到这个全局纹理。需要先显式 copy 到独立 TextureHandle，再把 copy 作为 shader 输入发布或绑定。

不能做的事情：

- 不能继续每个 Feature 自己维护一套临时 RT、copy、blur、debug 资源。
- 不能因为旧实现有 compatibility path，就让新包也长期维护双路径。
- 不能绕过资源声明直接依赖某个全局 RT “刚好已经存在”。
- 不能在 RenderGraph blit / raster pass 里采样没有通过该 pass 声明或统一资源层登记的纹理。
- 不能为了快速验证效果，临时用 `SetGlobalTexture`、静态缓存、材质属性副作用拼出长期链路；这类代码只能作为明确标注的临时诊断代码，不能进入阶段验收。
- 不能让同一张 TextureHandle 在一个 pass 中同时通过 `SetRenderAttachment` 写入、又通过 `UseTexture` 或全局纹理依赖被读取；尤其要警惕 `activeColorTexture` / `_CameraTargetAttachment`。

---

## 7. Feature 必须声明自己，而不是只执行自己

每个 Feature 至少要声明：

- 生产哪些资源。
- 消费哪些资源。
- 生产哪些语义。
- 消费哪些语义。
- 需要哪些 shader pass / LightMode。
- 默认执行阶段。
- 是否参与 Composite。
- Debug View。
- 与 Capability 的关系。

不能做的事情：

- 不能只有 `AddRenderPasses()` 或 `RecordRenderGraph()` 里的一堆执行逻辑，却没有可查询的 Feature Descriptor。
- 不能让 Feature 之间通过名字约定、执行顺序偶然成立。
- 不能把 debug 当成临时 shader pass，而要把 debug 当成一级系统能力。

---

## 8. 后处理必须分层：语义后处理和图像后处理不能混为一谈

HoPost 和 Shoost 的边界要清楚：

- HoPost：语义感知后处理，读取 HoAOV / Semantic Registry，做角色、对象、材质、区域定向处理。
- Shoost：最终图像风格栈，以画面风格、镜头、颜色、复古、模糊、颗粒等 image-space 效果为主。

Shoost 可以轻量使用 AOV composite，但不能变成第二套 HoPost。

纯 `ImageDomain` 链路还必须有自己的资源策略：默认按统一的双缓冲 / ping-pong image chain 执行，而不是每个 image effect 或每个 layer 各自创建一张同尺寸中间 RT。线性的全屏图像 pass 应该只表达为：

```text
ImageChain.Read -> pass -> ImageChain.Write -> swap
```

这样可以让 Bloom、色彩、VHS、CRT、锐化、色差、FilmGrain、ToneMap、简单 Blur 等纯图像域效果共享少量 frame transient 资源，并由 RenderGraph / 统一资源层负责别名、生命周期和 debug 观察。

允许脱离双缓冲的情况必须显式声明，例如：

- 需要多分辨率金字塔。
- 需要历史帧或 temporal accumulation。
- 需要同时保留 original source 和 filtered result。
- 需要多 MRT、mask、depth/normal、AOV 或其它语义输入。
- 需要长期缓存或跨帧资源。

不能做的事情：

- 不能把所有后期都塞进一个万能 stack。
- 不能让最终画面风格栈反过来定义对象/材质语义。
- 不能让语义 mask 规则散落在各个 shader 中。
- 不能让纯图像域每个 pass 默认分配独占全屏 RT；只有声明过的例外链路才能申请额外资源。
- 不能把双缓冲的两个工作纹理发布成长期公共语义资源；它们只是 ImageDomain 当前链的 frame transient 工作区。

---

## 9. 材质重构不能继承旧包的结构性问题

新材质系统可以参考 `lilToon` 的 block 化思想，也可以参考 `lilPBR` 现有参数覆盖面，但不能继承它们的厚 UI、历史 keyword、反射式编辑器逻辑和隐式 pass 组合。

新材质方向应该是：

- Shader Template。
- Feature Block。
- Material Preset。
- Generated Shader。
- 有限组合。
- 可 diff、可测试、可追踪。

不能做的事情：

- 不能继续靠 inspector 动态决定 shader 结构。
- 不能让 keyword 空间无限膨胀。
- 不能让 UI 控制编译结构。
- 不能把旧材质属性名当作新材质 ABI。

---

## 10. 调试系统必须是一等公民

Debug 不是临时开关，也不是每个 Feature 自己画一张 debug 图。

Debug 系统必须能：

- 查询已注册语义。
- 查询资源生产者和消费者。
- 切换 debug view。
- 叠加 overlay。
- 可视化 pass 依赖。
- 检查资源生命周期。
- 帮助定位旧系统迁移差异。

不能做的事情：

- 不能每个模块各自定义 debug mode，互相不知道。
- 不能把 debug shader 当作最后才补的工具。
- 不能没有办法回答“这个像素为什么被这个 feature 影响”。

---

## 11. 新包边界

当前新体系的包边界应按以下方向理解：

- `HoUrp-Extensions`：URP 扩展、RenderGraph 资源、Feature、语义、调试、合成。
- `HoNpr`：未来统一 HoRP 材质 / shader 包。它承载 NPR 主方向，同时包含 `HoStandardSurface`、PBR lobe、材质语义 producer 等必要基础层；不再拆出独立 `HoPbr` 包。`HoToon` 的 URP 半调 toon shader、半调贴图和导入工具已合入这里作为小模块。
- `HoToon`：旧轻量 toon shader 仓库，可作为简单 shader 和历史验证参考，但不是完整新材质系统主线。
- `lilToon-URP-Extensions`：旧 RP 扩展能力来源和迁移参照。
- `lilToon` / `lilPBR`：旧材质能力来源和迁移参照。

不能做的事情：

- 不能让 `HoUrp-Extensions` 变成旧材质包的附属适配层。
- 不能让 `HoNpr` 在 RP 契约未清晰前自行定义一套不兼容语义。
- 不能让旧包和新包边界长期混杂。

---

## 12. 一句话底线

新 RP 的核心是 **显式语义、显式资源、显式依赖、显式能力、显式调试**。

旧系统可以证明能力，但不能定义未来；材质系统可以接入 RP，但不能反向绑架 RP；RenderFeature 可以实现效果，但必须先声明自己在整个架构中的生产、消费和生命周期。
## 新增底线：旧实现来源必须进入组分身份

迁移旧实现时，不能只把来源写在文档注释里。任何从 `lilToon`、`lilPBR` 或旧 RP 扩展迁移出来的材质组分、算法组分或 pass 原型，如果仍以旧实现作为行为参考或验收基线，名字必须显式携带来源。

要求：

- `HoNpr` Feature Block ID、entry 函数、DebugView、生成 shader 属性名/显示名和 UI 标签都要能看出来源。
- 例：`SecondaryMatCapLilToon`、`GlitterLilToon`、`HoNprEvaluateGlitterLilToon`、`Lobe.GlitterLilToon`、`_HoNprGlitterLilToonColor`。
- 来源后缀是迁移责任标记，不是兼容承诺。`LilToon` 后缀不允许带入 `_lil*`、旧 inspector、旧 include、旧 pass 名或旧属性 ABI。
- 如果一个能力已经脱离旧实现，成为 HoNpr 原生通用组分，必须另行登记并说明去来源化的原因。

底线：**旧实现可以提供能力来源，但来源必须显式；显式来源不能变成隐式 ABI。**

---
