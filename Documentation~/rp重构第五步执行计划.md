# RP 重构第五步执行计划

> 第五步目标：在第四步 `SurfaceData / MaterialCustom` 材质语义纵切之后，扩展出 **SSS 输入语义纵切**。  
> 本阶段只迁移 `Aov.SssSource` 与 `Shading.SssWeight` 的最小生产、调试和显式消费链路，不进入完整 HoSSS diffusion / transmission / composite。

---

## 0. 前置状态

第一到第四阶段已经完成：

- 第一阶段冻结新 RP 的 Domain、Semantic、Resource、Feature、DebugView、Capability 和旧 ABI 判定。
- 第二阶段完成 `Aov.MaskId` / `Aov.NormalDepth` 最小 AOV 绘制、DebugView 和 SemanticPost 只读消费闭环。
- 第三阶段完成 `Aov.ObjectCustom0_3` / `Aov.ObjectCustom4_7` 对象语义生产、DebugView、AllRegistered debug 和 SemanticPost 显式消费闭环。
- 第四阶段完成 `Aov.SurfaceData` / `Aov.MaterialCustom0_3` 材质语义生产、DebugView 和 SemanticPost 显式消费闭环。
- `Aov.NormalDepth` 背景语义已调整为 invalid normal + far depth `(0,0,0,1)`，LinearDepth debug 显示无限远为白。
- ID 类 DebugView 已支持稳定 hash 伪随机色显示。

第五步必须继续遵守：

- 不复制旧 `HoAovRendererFeature.cs`。
- 不迁移旧 compatibility path。
- 不把旧 `_lilHoAov*` / `_HoSSS*` 命名提升为长期 ABI。
- 不修改 `lilToon` / `lilPBR`。
- 不把第五步扩成完整 HoSSS 迁移。
- 不让旧材质 inspector 或旧材质属性反向定义新 RP 契约。

---

## 1. 为什么第五步先做 SSS 输入纵切

第四阶段之后，`SurfaceData` 已经能提供 `Material.SssProfile`、`Material.Thickness`、`Material.Curvature`。旧 HoSSS 真正进入屏幕空间扩散前，还缺少两个关键输入：

- `Aov.SssSource`：可被扩散的源颜色 / 源能量。
- `Shading.SssWeight`：当前像素是否参与 SSS，以及参与强度。

如果直接迁完整 `HoSubsurfaceScatteringRendererFeature`，会同时引入 source copy、diffusion、transmission、profile 参数、blur、composite 和时机问题，范围过大。第五步先把 SSS 输入变成可注册、可调试、可显式消费的数据，后续第六步再迁屏幕空间 SSS pass。

---

## 2. 本阶段新增契约

新增资源：

```text
Aov.SssSource
```

新增或激活语义：

```text
Shading.SssSourceColor
Shading.SssWeight
```

依赖已有语义：

```text
Material.SssProfile
Material.Thickness
Material.Curvature
Geometry.WorldNormal
Geometry.LinearDepth
Object.MaskWeight
```

新增 DebugView：

```text
AOV.SssSource
AOV.SssWeight
SSS.ProfileId
SSS.Thickness
SSS.Curvature
```

其中 `SSS.ProfileId / Thickness / Curvature` 可以复用 `Aov.SurfaceData` source resource，只是在 Debug registry 中作为 SSS 视角别名登记，便于后续 HoSSS 调试面板聚合。

新增 shader binding 建议：

```text
_HoUrpAovSssSourceTexture
_HoUrpSssSourceColor
_HoUrpSssWeight
```

shader binding 是 backend detail，不是 Resource Registry 主键。最终命名在本阶段审查文档中冻结。

---

## 3. 本阶段不做项

- 不做完整 `SubsurfaceScatteringRendererFeature`。
- 不做 SSS diffusion blur。
- 不做 transmission gather / transmission blur。
- 不做 SSS composite 回 camera color。
- 不做 profile kernel / Burley disk / per-profile radius。
- 不做 half-resolution SSS。
- 不做 temporal / bilateral filter。
- 不做旧 `HoAOVSSS` LightMode 接入。
- 不接旧 `lilToon` / `lilPBR` 原生 SSS pass。
- 不修改旧材质包。
- 不做材质 inspector UI。
- 不做透明 SSS。

---

## 4. 建议新增文档产物

```text
Documentation~/rp重构第五步/
├── rp第五阶段实现边界审查.md
├── rpSssSource资源与编码审查.md
├── rpSss输入Authoring审查.md
├── rpSssInputAov链路审查.md
├── rpSss输入Debug与Consumer闭环审查.md
├── rp第五阶段测试与验收清单.md
└── rp第五阶段未决项登记.md
```

---

## 5. 执行顺序

```text
Step 1. 写第五阶段实现边界审查
Step 2. 写 SssSource 资源与编码审查
Step 3. 写 SSS 输入 authoring 审查
Step 4. 写 SssInput AOV 绘制链路审查
Step 5. 写 DebugView / Consumer 闭环审查
Step 6. 扩展 runtime contract registry
Step 7. 扩展 RenderGraph AOV resource declaration
Step 8. 扩展材质语义 authoring 或新增 SSS 输入 authoring
Step 9. 扩展 AOV fallback shader 和 AovOutput MRT
Step 10. 扩展 Debug shader / feature
Step 11. 扩展 SemanticPost probe 或新增 SssInputProbe
Step 12. 补测试、验收清单、未决项登记
```

---

## 6. `Aov.SssSource` 第一版编码建议

`Aov.SssSource` 第一版承载：

```text
RGB = Shading.SssSourceColor
A   = Shading.SssWeight
```

默认值：

```text
Aov.SssSource = (0, 0, 0, 0)
```

语义：

- RGB 为可供后续 SSS diffusion 使用的源颜色。
- A 为 SSS 参与权重。
- A 为 0 表示不参与 SSS。
- 本阶段不承诺旧 HoSSS 的最终扩散视觉一致性。

第一版 source color 可来自 authoring 常量或 fallback 策略。推荐默认：

```text
SssSourceColor = material semantic authoring color
SssWeight      = authoring weight * Object.MaskWeight
```

如果没有显式 authoring，则默认不参与 SSS，即 `(0,0,0,0)`。

---

## 7. Authoring 策略

第五步不能让旧材质系统定义新契约，因此第一版 authoring 从以下方案中选择：

| 方案 | 说明 | 建议 |
| --- | --- | --- |
| 扩展 `MaterialSemanticAuthoring`，增加 SSS source color / weight | 最少新增组件，和材质语义保持同源 | 推荐 |
| 新增 `SssInputAuthoring` 组件 | 边界清晰，但对象组件数量增加 | 可接受 |
| AOV fallback shader serialized defaults | 最小但不够 per-renderer | 只作为兜底 |
| 接旧 `lilToon/lilPBR` SSS 属性 | 快速但污染边界 | 不做 |

建议优先扩展：

```text
MaterialSemanticAuthoring
  - sssSourceColor
  - sssWeight
```

它仍然是迁移期 authoring / test producer，不是最终材质系统。后续 HoPbr / HoNpr / HoToon 应通过正式 material producer 接口写入这些语义。

---

## 8. AovOutput 绘制链路

第五步需要把 `AovOutput` 扩展到 SSS 输入 MRT：

```text
AovOutputRendererFeature
  -> GeometrySemanticAov writes:
       Aov.MaskId
       Aov.NormalDepth
       Aov.ObjectCustom0_3
       Aov.ObjectCustom4_7
  -> MaterialShadingSemanticAov writes:
       Aov.SurfaceData
       Aov.MaterialCustom0_3
       Aov.SssSource
```

实现上可以仍由同一个 RenderGraph pass 写多 MRT，但 descriptor、文档和 DebugView mapping 必须把 `Aov.SssSource` 归入 `MaterialShadingSemanticAov` / SSS input 范围。

如果后续发现 SSS source 需要专门材质 pass 或 lighting-aware shading，允许在第六步拆成独立 pass；第五步只承诺最小输入资源可写、可调试、可被显式消费。

---

## 9. Debug 与 Consumer 闭环

第五步至少要让以下 DebugView 可用：

- `AOV.SssSource`
- `AOV.SssWeight`
- `SSS.ProfileId`
- `SSS.Thickness`

`AllRegistered` debug 必须自动包含新增且 source resource 存在的 DebugView。

Consumer 最小闭环有两个候选：

| 候选 | 说明 | 建议 |
| --- | --- | --- |
| 扩展 `SemanticPostProcessRendererFeature`，读取 `Aov.SssSource.a` 做 tint | 最小，沿用现有 probe | 推荐 |
| 新增 `SssInputProbeRendererFeature` | 边界更清晰，但代码稍多 | 后续如果 SemanticPost 变臃肿再拆 |

不允许只补 DebugView 而没有正式 consumer 读取链路。

---

## 10. 测试建议

自动检查：

- registry count / link tests。
- `Aov.SssSource` resource descriptor tests。
- shader property mapping tests。
- DebugView source/channel mapping tests。
- SemanticPost or SssInputProbe consumed resource declaration tests。
- `git diff --check`。

手动 Unity 验收：

1. 挂载 `HoURP AOV Output`。
2. 挂载 `HoURP Semantic Post Process`。
3. 可选挂载 `HoURP AOV Debug`。
4. 给测试对象添加 `ObjectSemanticAuthoring` 和 `MaterialSemanticAuthoring`。
5. 设置非零 `sssSourceColor` / `sssWeight` / `sssProfile` / `thickness`。

期望：

| 场景 | 期望 |
| --- | --- |
| AOV Output | Frame Debugger 中 `HoURP AOV Output` 写入新增 `Aov.SssSource` MRT。 |
| Debug SssSource | camera color 显示 SSS source color。 |
| Debug SssWeight | camera color 显示 SSS weight 灰度图。 |
| Debug AllRegistered | 新增 SSS 输入 DebugView 自动进入平铺输出。 |
| Consumer probe | SSS 输入命中的 opaque 区域出现额外 tint / weight。 |
| 空值区域 | 天空/未覆盖区域保持 `(0,0,0,0)`，不参与 SSS。 |

---

## 11. 冻结条件

第五步完成时必须满足：

- `Aov.SssSource` 已注册。
- `Shading.SssSourceColor` 已注册。
- `Shading.SssWeight` 已注册。
- `AovOutput` 显式写入 `Aov.SssSource`。
- DebugView 能读取并显示 SSS source color 和 SSS weight。
- `AllRegistered` 能包含新增 SSS 输入 DebugView。
- 一个正式 consumer 显式读取 `Aov.SssSource`。
- 不依赖旧 `_lilHoAovSssTexture` / `_HoSSS*` 逻辑名。
- 不修改 `lilToon` / `lilPBR`。
- 未决项已登记。

---

## 12. 第五阶段未决项预登记

| Item | 本阶段处理方式 | 后续阶段 |
| --- | --- | --- |
| 完整 HoSSS diffusion | 不做 | 第六阶段或 HoSSS 迁移阶段 |
| transmission gather / blur | 不做 | HoSSS 迁移阶段 |
| profile kernel / radius | 只登记 profile id，不计算 kernel | HoSSS profile registry 阶段 |
| SSS composite | 不做 | HoSSS composite 阶段 |
| half-resolution SSS | 不做 | Filter / SSS 性能阶段 |
| bilateral / depth-aware blur | 不做 | Filter backend / HoSSS 阶段 |
| 旧 `HoAOVSSS` pass | 不接 | 新材质 producer 或 legacy validation 阶段 |
| 旧材质 SSS 属性迁移 | 不做 | 新材质系统 / migration tool 阶段 |
| transparent SSS | 不做 | Transparent / OIT / Character 阶段 |

---

## 13. 第五阶段完成定义

第五步完成时，应该能回答：

- `Aov.SssSource` 在哪里注册、谁写、谁读。
- `Shading.SssSourceColor` 和 `Shading.SssWeight` 的编码和 clear 值是什么。
- SSS 输入第一版 authoring 从哪里来，为什么它不是最终材质系统。
- DebugView 如何按注册表找到 `Aov.SssSource` source 和 channel。
- Consumer 如何显式声明和读取 SSS 输入。
- 哪些旧 HoSSS 行为只是迁移参照，尚未迁移。

如果这些问题答不上来，就不能进入完整 HoSSS diffusion / transmission / composite 迁移。

