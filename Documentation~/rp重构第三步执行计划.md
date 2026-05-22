# RP 重构第三步执行计划

> 第三步目标：在第二步 `Aov.MaskId` / `Aov.NormalDepth` 最小闭环之上，扩展出 **ObjectCustom 对象语义纵切**。  
> 本阶段只验证对象/角色区域语义的生产、调试和显式消费，不进入 `SurfaceData` / SSS / HoPost rule stack。

---

## 0. 前置状态

第二步已经完成：

- `Aov.MaskId` / `Aov.NormalDepth` 注册。
- RenderGraph AOV resource declaration。
- `AovOutputRendererFeature` override material 最小绘制。
- `AovDebugRendererFeature` replace debug。
- `SemanticPostProcessRendererFeature` mask tint 最小消费者。

第三步必须继续遵守：

- 不复制旧 `HoAovRendererFeature.cs`。
- 不迁移旧 compatibility path。
- 不把旧 `_lilHoAov*` 命名提升为长期 ABI。
- 不修改 `lilToon` / `lilPBR`。
- 不把第三步扩成完整 HoAOV 迁移。

---

## 1. 为什么第三步先做 ObjectCustom

第二阶段未决项给出两个候选方向：

- `Aov.ObjectCustom0_3` / `Aov.ObjectCustom4_7`
- `Aov.SurfaceData`

第三步优先 ObjectCustom，理由是：

- ObjectCustom 仍属于 `ObjectDomain`，可以用对象组件或 renderer property 明确生产。
- 它能验证角色/区域选择、DebugView 和 semantic-aware consumer，而不要求新材质系统参与。
- 它是后续 `HoCharacterSpecialization`、HoPost AOV rule、区域选择 UI 的基础。
- `SurfaceData` 属于材质/着色派生语义，应该等材质 producer 契约更清楚后再进入。

---

## 2. 本阶段新增契约

新增资源：

```text
Aov.ObjectCustom0_3
Aov.ObjectCustom4_7
```

新增语义：

```text
Object.Custom0
Object.Custom1
Object.Custom2
Object.Custom3
Object.Custom4
Object.Custom5
Object.Custom6
Object.Custom7
```

新增 debug view：

```text
AOV.ObjectCustom0
AOV.ObjectCustom1
AOV.ObjectCustom2
AOV.ObjectCustom3
AOV.ObjectCustom4
AOV.ObjectCustom5
AOV.ObjectCustom6
AOV.ObjectCustom7
```

旧实现里可以临时用以下角色含义做迁移参照，但不作为新 RP 长期 ABI：

```text
Object.Custom0 = 主体
Object.Custom1 = 脸
Object.Custom2 = 前发
Object.Custom3 = 眼睛
Object.Custom4 = 眼透区域
Object.Custom5 = 配件
Object.Custom6 = 预留
Object.Custom7 = 预留
```

---

## 3. 执行顺序

```text
Step 1. 写第三阶段边界审查
Step 2. 写 ObjectCustom 资源与编码审查
Step 3. 写 ObjectCustom authoring 审查
Step 4. 写 DebugView / SemanticPost 闭环审查
Step 5. 扩展 runtime contract registry
Step 6. 扩展 RenderGraph AOV resource declaration
Step 7. 新增最小 ObjectCustom authoring component
Step 8. 扩展 AOV fallback shader 和 AovOutput MRT
Step 9. 扩展 Debug shader / feature
Step 10. 扩展 SemanticPost probe
Step 11. 补测试、验收清单、未决项登记
```

---

## 4. 建议新增文档产物

```text
Documentation~/rp重构第三步/
├── rp第三阶段实现边界审查.md
├── rpObjectCustom资源与编码审查.md
├── rpObjectCustomAuthoring审查.md
├── rpObjectCustomDebug与Consumer闭环审查.md
├── rp第三阶段测试与验收清单.md
└── rp第三阶段未决项登记.md
```

---

## 5. 第三阶段不做项

- 不做 `Aov.SurfaceData`。
- 不做 `Aov.SssSource`。
- 不做 `Aov.MaterialCustom0_3`。
- 不做完整 HoCharacter capture/composite。
- 不做 HoPost AOV rule language。
- 不做 Capability UI 面板。
- 不接旧 `HoAovGroup` 完整优先级系统。
- 不依赖 `unity_RendererUserValue` 作为唯一数据来源。

---

## 6. 完成定义

第三步完成时必须能回答：

- `Aov.ObjectCustom0_3` / `Aov.ObjectCustom4_7` 在哪里注册。
- 谁写这两个资源。
- 谁读这两个资源。
- ObjectCustom channel 的编码和 clear 值是什么。
- DebugView 如何按注册表找到 ObjectCustom source。
- SemanticPost-like consumer 如何显式声明和读取 ObjectCustom。
- 哪些旧角色区域语义只是迁移参照，不是新 ABI。

