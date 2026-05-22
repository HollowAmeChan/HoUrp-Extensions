# RP 重构第四步执行计划

> 第四步目标：在第三步 `ObjectCustom` 对象语义纵切之后，扩展出 **材质派生语义纵切**。  
> 本阶段只迁移 `Aov.SurfaceData` 与 `Aov.MaterialCustom0_3` 的最小生产、调试和显式消费链路，不进入完整 SSS、`Aov.SssSource`、HoPost rule stack 或新材质生成系统。

---

## 0. 前置状态

第一到第三阶段已经完成：

- 第一阶段冻结新 RP 的 Domain、Semantic、Resource、Feature、DebugView、Capability 和旧 ABI 判定。
- 第二阶段完成 `Aov.MaskId` / `Aov.NormalDepth` 最小 AOV 绘制、DebugView 和 SemanticPost 只读消费闭环。
- 第三阶段完成 `Aov.ObjectCustom0_3` / `Aov.ObjectCustom4_7` 对象语义生产、DebugView、AllRegistered debug 和 SemanticPost 显式消费闭环。
- AOV 资源清理已收敛到资源声明层 `TextureDesc.clearBuffer/clearColor`，未覆盖区域统一为空值 zero。

第四步必须继续遵守：

- 不复制旧 `HoAovRendererFeature.cs`。
- 不迁移旧 compatibility path。
- 不把旧 `_lilHoAov*` / `_HoAov*` 命名提升为长期 ABI。
- 不修改 `lilToon` / `lilPBR`。
- 不把第四步扩成完整 HoAOV / HoSSS / HoPost 迁移。
- 不让旧材质 inspector 或旧材质属性反向定义新 RP 契约。

---

## 1. 为什么第四步做 SurfaceData / MaterialCustom

第三步之后，当前新包已经能生产和消费对象级语义，但仍缺少材质派生语义。下一步如果直接迁 HoSSS 或 HoPost rule，会把“材质数据怎么来”这个问题压到具体效果里，重新走向旧系统的隐式依赖。

第四步优先做 `Aov.SurfaceData` 和 `Aov.MaterialCustom0_3`，理由是：

- 它们是 SSS、SemanticPost rule、ImagePost AOV composite 的共同输入地基。
- 它们属于 `MaterialDomain`，可以先用最小 authoring / override material 生产，不需要完整新材质系统。
- 它们能验证 `MaterialShadingSemanticAov` 阶段，而不是继续把所有 AOV 都塞在 `GeometrySemanticAov`。
- 它们把 `Material.Class`、`Material.SssProfile`、`Material.Thickness`、`Material.Curvature`、`Material.Utility` 从旧 `SurfaceData` 黑盒里正式拆出来。
- 它们为第五步做 HoSSS 或 HoPost rule 提供可检查输入，但本阶段不实现这些完整消费者。

---

## 2. 本阶段新增契约

新增资源：

```text
Aov.SurfaceData
Aov.MaterialCustom0_3
```

新增或激活语义：

```text
Material.Class
Material.SssProfile
Material.Thickness
Material.Curvature
Material.Utility
Material.Custom0
Material.Custom1
Material.Custom2
Material.Custom3
```

新增 debug view：

```text
AOV.MaterialClass
AOV.SssProfile
AOV.Thickness
AOV.Curvature
AOV.Utility
AOV.MaterialCustom0
AOV.MaterialCustom1
AOV.MaterialCustom2
AOV.MaterialCustom3
```

新增 shader binding 建议：

```text
_HoUrpAovSurfaceDataTexture
_HoUrpAovMaterialCustom0_3Texture
_HoUrpMaterialClass
_HoUrpMaterialSssProfile
_HoUrpMaterialThickness
_HoUrpMaterialCurvature
_HoUrpMaterialUtility
_HoUrpMaterialCustom0_3
```

shader binding 是 backend detail，不是 Resource Registry 主键。最终命名在本阶段审查文档中冻结。

---

## 3. 本阶段不做项

- 不做 `Aov.SssSource`。
- 不做 `HoAOVSSS` 等价迁移。
- 不做完整 `SubsurfaceScattering`。
- 不做 HoPost AOV rule language。
- 不做 Shoost AOV composite 迁移。
- 不做新材质 template / generated shader 系统。
- 不接旧 `lilToon` / `lilPBR` 原生 `HoAOV` pass。
- 不支持旧材质属性的完整自动迁移。
- 不做材质 inspector UI；如果需要 authoring，先做最小组件或测试材质参数。
- 不做透明材质 SurfaceData 一致性。

---

## 4. 建议新增文档产物

```text
Documentation~/rp重构第四步/
├── rp第四阶段实现边界审查.md
├── rpSurfaceData资源与编码审查.md
├── rpMaterialCustom资源与编码审查.md
├── rp材质语义Authoring审查.md
├── rpMaterialShadingSemanticAov链路审查.md
├── rp材质语义Debug与Consumer闭环审查.md
├── rp第四阶段测试与验收清单.md
└── rp第四阶段未决项登记.md
```

---

## 5. 执行顺序

```text
Step 1. 写第四阶段实现边界审查
Step 2. 写 SurfaceData 资源与编码审查
Step 3. 写 MaterialCustom 资源与编码审查
Step 4. 写材质语义 authoring 审查
Step 5. 写 MaterialShadingSemanticAov 绘制链路审查
Step 6. 写 DebugView / SemanticPost 闭环审查
Step 7. 扩展 runtime contract registry
Step 8. 扩展 RenderGraph AOV resource declaration
Step 9. 新增最小材质语义 authoring 输入
Step 10. 扩展 AOV fallback shader 和 AovOutput MRT
Step 11. 扩展 Debug shader / feature
Step 12. 扩展 SemanticPost probe
Step 13. 补测试、验收清单、未决项登记
```

---

## 6. SurfaceData 第一版编码建议

`Aov.SurfaceData` 第一版承载：

```text
R = Material.Class
G = Material.SssProfile
B = Material.Thickness
A = Material.Curvature
```

`Material.Utility` 暂时有两个候选：

| 候选 | 说明 | 建议 |
| --- | --- | --- |
| 放入 `Aov.SurfaceData.a`，延后 `Curvature` | 节省资源，但会牺牲旧 SSS 输入完整性 | 不推荐 |
| 暂不写入，只登记为未决 | SurfaceData 第一版只覆盖 SSS 最关键输入 | 可接受 |
| 增加第二张 SurfaceData 扩展图 | 扩展性强，但第四步范围变大 | 延后 |

建议第四步先不写 `Material.Utility`，但必须在文档和 registry 中保持语义登记，标记为 `RegisteredNotProducedInPhase4` 或等价状态。若实现成本很低，也可以把 `Material.Utility` 放入 `Aov.MaterialCustom0_3.a` 的测试路径，但不能把这个临时复用定为长期契约。

Clear policy：

```text
Aov.SurfaceData = (0, 0, 0, 0)
```

zero 表示 no material semantic / no geometry，不表示合法 profile。

---

## 7. MaterialCustom 第一版编码建议

`Aov.MaterialCustom0_3` 第一版承载：

```text
R = Material.Custom0
G = Material.Custom1
B = Material.Custom2
A = Material.Custom3
```

默认值：

```text
Aov.MaterialCustom0_3 = (0, 0, 0, 0)
```

第一版只承诺 normalized float 常量输入。旧实现里的 texture-driven custom 或材质侧复杂计算只作为后续新材质 producer 的职责，不进入第四步。

---

## 8. Authoring 策略

第四步不能让旧材质系统定义新契约，因此第一版 authoring 应从以下方案中选择：

| 方案 | 说明 | 建议 |
| --- | --- | --- |
| `MaterialSemanticAuthoring` 组件通过 `MaterialPropertyBlock` 写最小材质语义 | 不改材质资源，适合验证链路 | 推荐 |
| AOV fallback shader serialized defaults | 最小但不够 per-renderer | 只作为兜底 |
| 修改测试材质 shader property | 接近材质 producer，但容易滑向旧 inspector 依赖 | 暂不作为主路径 |
| 接旧 `lilToon/lilPBR` 属性 | 能快速验证旧视觉，但污染本阶段边界 | 不做 |

建议新增最小组件：

```text
MaterialSemanticAuthoring
  - materialClass
  - sssProfile
  - thickness
  - curvature
  - materialCustom0_3
```

它的定位是迁移期 authoring / test producer，不是最终新材质系统。后续 HoPbr / HoNpr / HoToon 应通过正式 material producer 接口写入这些语义。

---

## 9. AovOutput 绘制链路

第四步需要把 `AovOutput` 从对象语义 MRT 扩展到材质语义 MRT：

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
```

实现上可以仍由同一个 RenderGraph pass 写多 MRT，但文档和 descriptor 必须区分 `GeometrySemanticAov` 与 `MaterialShadingSemanticAov` 的逻辑阶段。若 Unity/URP pass 结构更适合拆成两个 pass，本阶段允许拆分，但必须保证：

- 资源生产者登记清楚。
- DebugView 能找到对应 source resource。
- SemanticPost 明确声明读取依赖。
- 未覆盖区域保持资源声明层 zero clear。

---

## 10. Debug 与 Consumer 闭环

第四步至少要让以下 DebugView 可用：

- `AOV.MaterialClass`
- `AOV.SssProfile`
- `AOV.Thickness`
- `AOV.Curvature`
- `AOV.MaterialCustom0`

`AllRegistered` debug 必须自动包含新增且 source resource 存在的材质语义 DebugView。

SemanticPost 最小消费者不迁 HoPost rule，但要证明正式 consumer 能显式读取材质语义：

```text
SemanticPostProcessRendererFeature
  -> read Aov.SurfaceData
  -> read Aov.MaterialCustom0_3
  -> apply small tint/weight based on Thickness or MaterialCustom0
```

不允许只补 DebugView 而没有正式 consumer 读取链路。

---

## 11. 测试建议

自动检查：

- registry count / link tests。
- `Aov.SurfaceData` resource descriptor tests。
- `Aov.MaterialCustom0_3` resource descriptor tests。
- shader property mapping tests。
- DebugView source/channel mapping tests。
- SemanticPost consumed resource declaration tests。
- `git diff --check`。

手动 Unity 验收：

1. 挂载 `HoURP AOV Output`。
2. 挂载 `HoURP Semantic Post Process`。
3. 可选挂载 `HoURP AOV Debug`。
4. 给测试对象添加 `ObjectSemanticAuthoring` 和 `MaterialSemanticAuthoring`。
5. 设置非零 `thickness` / `curvature` / `materialCustom0`。

期望：

| 场景 | 期望 |
| --- | --- |
| AOV Output | Frame Debugger 中 `HoURP AOV Output` 写入新增材质语义 MRT。 |
| Debug Thickness | camera color 显示 thickness 灰度。 |
| Debug MaterialCustom0 | camera color 显示 custom0 灰度。 |
| Debug AllRegistered | 新增材质语义 DebugView 自动进入平铺输出。 |
| SemanticPost | 只有材质语义命中的 opaque 区域出现额外 tint/weight。 |
| 空值区域 | 天空/未覆盖区域保持 zero，不被解释成合法材质 profile。 |

---

## 12. 冻结条件

第四步完成时必须满足：

- `Aov.SurfaceData` 已注册。
- `Aov.MaterialCustom0_3` 已注册。
- `Material.Class`、`Material.SssProfile`、`Material.Thickness`、`Material.Curvature`、`Material.Custom0-3` 已注册并有 DebugView。
- `Material.Utility` 有明确处理方式：本阶段生产或登记为未生产未决项。
- AOV 资源 clear policy 仍由资源声明层负责。
- `AovOutput` 显式写入新增材质语义 MRT。
- DebugView 能读取并显示新增资源/channel。
- `SemanticPostProcess` 显式读取新增材质语义资源。
- 不依赖旧 `_lilHoAov*` / `_HoAov*` 逻辑名。
- 不修改 `lilToon` / `lilPBR`。
- 未决项已登记。

---

## 13. 第四阶段未决项预登记

| Item | 本阶段处理方式 | 后续阶段 |
| --- | --- | --- |
| `Material.Utility` 放在哪里 | 优先登记，视实现成本决定是否生产 | SurfaceData 扩展或 SSS 阶段 |
| `Aov.SssSource` | 不做 | HoSSS 输入纵切 |
| `Shading.SssWeight` | 不做完整生产 | HoSSS 输入纵切 |
| 旧材质原生 `HoAOV` pass | 不接 | 新材质 producer 或 legacy validation 阶段 |
| texture-driven MaterialCustom | 不做 | 新材质系统阶段 |
| SurfaceData 旧编码视觉一致 | 只保留旧实现参照 | HoSSS 迁移对比阶段 |
| 材质 inspector UI | 不做 | HoPbr/HoNpr 材质系统 |
| transparent material semantic | 不做 | Transparent / OIT 阶段 |

---

## 14. 第四阶段完成定义

第四步完成时，应该能回答：

- `Aov.SurfaceData` 在哪里注册、谁写、谁读。
- `Aov.MaterialCustom0_3` 在哪里注册、谁写、谁读。
- `Material.Class / SssProfile / Thickness / Curvature / Custom0-3` 的编码和 clear 值是什么。
- 材质语义第一版 authoring 从哪里来，为什么它不是最终材质系统。
- DebugView 如何按注册表找到 SurfaceData / MaterialCustom source 和 channel。
- SemanticPost-like consumer 如何显式声明和读取材质语义。
- 哪些旧 SurfaceData / SSS 行为只是迁移参照，尚未迁移。

如果这些问题答不上来，就不能进入完整 HoSSS 或 HoPost rule stack 迁移。
