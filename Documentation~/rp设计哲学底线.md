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

它应该被拆成不同生命周期的语义集合：

- Object / Renderer 静态语义。
- Geometry 可绘制语义。
- Material 静态语义。
- Shading 派生语义。
- SSS / Lighting / Composite 等专用输入。

当前旧实现里的 `MaskId`、`NormalDepth`、`TangentNormal`、`SurfaceData`、`Custom`、`ObjectCustom`、`SssSource` 可以作为第一版资源映射参考，但新系统必须重新定义正式语义表。

不能做的事情：

- 不能把 depth / normal 混进纯对象静态语义。
- 不能把 SSS source、profile、thickness、curvature 这类材质派生语义假装成对象元数据。
- 不能为了方便继续扩张一个越来越大的 HoAOV 黑箱。

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

不能做的事情：

- 不能继续每个 Feature 自己维护一套临时 RT、copy、blur、debug 资源。
- 不能因为旧实现有 compatibility path，就让新包也长期维护双路径。
- 不能绕过资源声明直接依赖某个全局 RT “刚好已经存在”。
- 不能在 RenderGraph blit / raster pass 里采样没有通过该 pass 声明或统一资源层登记的纹理。
- 不能为了快速验证效果，临时用 `SetGlobalTexture`、静态缓存、材质属性副作用拼出长期链路；这类代码只能作为明确标注的临时诊断代码，不能进入阶段验收。

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

不能做的事情：

- 不能把所有后期都塞进一个万能 stack。
- 不能让最终画面风格栈反过来定义对象/材质语义。
- 不能让语义 mask 规则散落在各个 shader 中。

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
- `HoPbr`：未来 PBR 材质 / shader 包。
- `HoNpr`：未来 NPR 材质 / shader 包。
- `HoToon`：已有轻量 toon shader，可作为简单 shader 参考，但不是完整新材质系统。
- `lilToon-URP-Extensions`：旧 RP 扩展能力来源和迁移参照。
- `lilToon` / `lilPBR`：旧材质能力来源和迁移参照。

不能做的事情：

- 不能让 `HoUrp-Extensions` 变成旧材质包的附属适配层。
- 不能让 `HoPbr/HoNpr` 在 RP 契约未清晰前自行定义一套不兼容语义。
- 不能让旧包和新包边界长期混杂。

---

## 12. 一句话底线

新 RP 的核心是 **显式语义、显式资源、显式依赖、显式能力、显式调试**。

旧系统可以证明能力，但不能定义未来；材质系统可以接入 RP，但不能反向绑架 RP；RenderFeature 可以实现效果，但必须先声明自己在整个架构中的生产、消费和生命周期。
