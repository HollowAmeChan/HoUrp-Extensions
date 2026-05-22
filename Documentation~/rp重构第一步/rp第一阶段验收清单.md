# RP 第一阶段验收清单

> 第一阶段完成定义：能回答新 RP 第一版有哪些 Domain、Semantic、Resource、Feature、DebugView、Capability，以及旧 ABI 在新系统里如何判定。不能回答这些问题时，不进入代码迁移。

---

## 0. 文档产物

| 文档 | 状态 | 验收点 |
| --- | --- | --- |
| `rp核心契约草案.md` | Done | 定义 Domain、核心对象、生命周期、命名原则、第一批 Feature 和 pass 词汇 |
| `rp旧ABI盘点表.md` | Done | 盘点 LightMode、global texture/scalar、material property、include、RenderPassEvent、ContextItem |
| `rp新旧差异与迁移判定表.md` | Done | 每个旧能力有迁移判定和风险 |
| `rp语义注册表草案.md` | Done | 第一批 Semantic 有 Domain、Producer、Consumer、阶段和 DebugView |
| `rp资源注册表草案.md` | Done | 第一批 Resource 有 Producer、Consumer、Format、Scale、Lifetime、Clear Policy |
| `rpFeatureDescriptor草案.md` | Done | 第一批 Feature 有声明，不只描述执行逻辑 |
| `rpPass时机与依赖基线.md` | Done | 记录旧事实顺序和新候选顺序 |
| `rpDebugView注册草案.md` | Done | 第一批 DebugView 覆盖 AOV/SSS/OIT/Shadow/Character/Post/Image/Reflection/System |
| `rpCapability模型草案.md` | Done | Object/Material/Light/Feature/Debug capability 初版成表 |

---

## 1. 第一阶段冻结条件

| 条件 | 状态 | 说明 |
| --- | --- | --- |
| 每个第一批 Feature 都有 Descriptor | Pass | 见 `rpFeatureDescriptor草案.md` |
| 每个第一批 Resource 都有 Producer 和 Consumer | Pass | 见 `rp资源注册表草案.md` |
| 每个第一批 Semantic 都有 Domain | Pass | 见 `rp语义注册表草案.md` |
| 每个旧 ABI 都有迁移判定 | Pass with known scope | 主要旧 ABI 已盘点；完整细枝末节可在实现前增补 |
| 每个 pass 都有相对时机 | Pass | 见 `rpPass时机与依赖基线.md` |
| 每个 debug view 都有来源 | Pass | 见 `rpDebugView注册草案.md` |
| 每个 Capability 都有 owner | Pass | 见 `rpCapability模型草案.md` |
| 没有把旧 `lilToon/lilPBR` 作为新核心 | Pass | 旧材质只作为 validation/reference |
| 没有迁移 runtime 代码 | Pass | 本阶段只新增文档 |
| 没有把 compatibility path 纳入长期目标 | Pass | 判定为 `ValidationOnly` |

---

## 2. 可回答问题

| 问题 | 答案位置 |
| --- | --- |
| 新 RP 第一版有哪些 Domain？ | `rp核心契约草案.md` |
| 新 RP 第一版有哪些 Semantic？ | `rp语义注册表草案.md` |
| 新 RP 第一版有哪些 Resource？ | `rp资源注册表草案.md` |
| 每个 Resource 谁写、谁读？ | `rp资源注册表草案.md` |
| 每个 Feature 的输入输出是什么？ | `rpFeatureDescriptor草案.md` |
| 每个旧 ABI 是保留、替代、删除还是仅验证？ | `rp旧ABI盘点表.md`, `rp新旧差异与迁移判定表.md` |
| 哪些旧能力第一轮迁移必须视觉一致？ | `rp新旧差异与迁移判定表.md` |
| 哪些旧能力可以推迟？ | `rp新旧差异与迁移判定表.md` |
| 新材质系统以后要实现哪些 producer/consumer 接口？ | `rpCapability模型草案.md`, `rp语义注册表草案.md` |
| Debug 系统第一版要支持什么？ | `rpDebugView注册草案.md` |

---

## 3. 第一轮实现建议

第二阶段推荐从最小闭环开始：

1. 新建 runtime contract 类型，但只覆盖 `SemanticDefinition`、`ResourceDefinition`、`FeatureDescriptor`、`DebugViewDefinition` 的最小只读模型。
2. 建立 `Aov.MaskId` / `Aov.NormalDepth` 的最小 Resource Registry 条目。
3. 建立 `AOV / Mask`、`AOV / Object ID`、`AOV / Linear Depth`、`AOV / World Normal` 的最小 DebugView Registry。
4. 选择一个最小 AOV 输出链路验证 RenderGraph resource declaration。
5. 再接一个只读 AOV 的最小 SemanticPost-like effect。

不建议第二阶段一开始迁移：

- 完整 Shoost。
- 完整 HoSSS。
- 完整 HoShadowCast。
- 完整旧材质 pass。
- 旧 compatibility path。

---

## 4. 未决项登记

| Item | Status | Needed Before Runtime Migration? |
| --- | --- | --- |
| 新 shader property 命名最终定案 | Open | Yes, before shader migration |
| AOV split 后的 exact encoding | Open | Yes, before AOV runtime |
| Character capture 是否拆分透明捕获 | Open | Before Character runtime |
| PlanarReflection 如何进入 RenderGraph/imported resource | Open | Before PlanarReflection runtime |
| Light Capability 详细 UI | Open | Before ShadowCast UI |
| Motion/Deformation semantic | Deferred | No |
| SSR/HTrace 合并 | Deferred | No |

---

## 5. 第一阶段底线复核

- 新 RP 核心是显式语义、显式资源、显式依赖、显式能力、显式调试。
- 旧系统证明能力，但不定义未来。
- 材质系统接入 RP，不反向绑架 RP。
- RenderFeature 可以实现效果，但必须先声明生产、消费和生命周期。
- `LegacyInterop` 是迁移事实和验证，不是长期桥接层。
