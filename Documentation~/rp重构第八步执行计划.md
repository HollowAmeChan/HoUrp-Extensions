# RP 重构第八步执行计划

> 第八步目标：在 AOV / SSS / SemanticPost 最小闭环成立之后，把现有 `ObjectSemanticAuthoring` 和 `MaterialSemanticAuthoring` 从“临时填语义数值的测试组件”推进为 **Capability UI 的第一版可用入口**。
>
> 本阶段不做完整 Capability 调度器，不做新材质系统，不做 Light Capability，也不做 Debug Framework。重点是让对象和材质的语义参与关系可以被显式配置、复用、检查和验收。

---

## 0. 前置状态

当前新包已经具备：

- `CapabilityDefinition` / `CapabilityRegistry`。
- `HoUrpBuiltInContracts` 内的第一批 capability 登记。
- `ObjectSemanticAuthoring`，能通过 `MaterialPropertyBlock` 写对象语义。
- `MaterialSemanticAuthoring`，能通过 `MaterialPropertyBlock` 写材质语义和 SSS 输入。
- `AovOutput` 能把对象 / 材质 / SSS authoring 数据写入 AOV。
- `SubsurfaceScattering` 能消费 `Aov.SssSource` 并生产 `Sss.Source` / `Sss.Diffusion`。
- `SemanticPostProcess` 能消费 AOV / SSS 资源生成 `SemanticPost.Mask` 并做 `SemanticTint`。
- `AOV Debug AllRegistered` 能观察 AOV、SSS 和 SemanticPost mask。

这些条件说明语义链路已经通了；第八步要解决的是“谁来可靠、可理解地声明这些语义和能力”。

---

## 1. 为什么第八步做 Capability UI

第七步之后，SemanticPost 已经证明：

```text
Object/Material authoring
  -> AOV resources
  -> SSS resources
  -> SemanticPost.Mask
  -> camera color
```

接下来继续加后处理效果收益不高，因为输入侧仍然是临时字段和手动数值。第八步应该把旧系统里 `HoAovSubject` / `HoAovGroup` 的“对象可声明能力”思想迁移成新包第一版 UI：

```text
Object Capability UI / Material Semantic UI
  -> explicit authoring data
  -> MaterialPropertyBlock / future material producer
  -> AOV / SSS / SemanticPost
```

这一步的重点不是做漂亮 inspector，而是冻结三条边界：

- Capability 表示“允许参与什么”。
- Policy 表示“如何参与、参数是多少”。
- Authoring UI 只能写入已登记或已规划的语义，不允许发明隐式规则。

---

## 2. 本阶段范围

### 做

- 为 `ObjectSemanticAuthoring` 建立第一版 Object Capability inspector。
- 为 `MaterialSemanticAuthoring` 建立第一版 Material Semantic inspector。
- 提供对象常用 preset：
  - `Subject`
  - `Face`
  - `Hair`
  - `Eye`
  - `Accessory`
  - `Prop`
- 提供材质常用 preset：
  - `DefaultOpaque`
  - `SkinSss`
  - `Hair`
  - `Eye`
  - `Cloth`
  - `Metal`
- 明确 object custom 0-7 的第一版推荐语义。
- 明确 material custom 0-3 的第一版推荐语义。
- 将 `ReceivesSemanticPost` 映射到 object custom / mask policy 的可验证输出。
- 保持 runtime authoring 仍通过 `MaterialPropertyBlock` 写入，不引入旧 renderer user value 作为唯一数据源。
- 补 Editor / Runtime tests：
  - authoring clamp。
  - preset 映射。
  - capability registry 链接。
  - shader property id 映射。

### 不做

- 不迁移旧 `HoAovGroup` 完整优先级系统。
- 不实现全局 group resolver。
- 不把 `unity_RendererUserValue` 作为新系统核心 ABI。
- 不做新材质 preset/generator。
- 不做 Light Capability。
- 不做 Feature Capability 调度器。
- 不做完整 Debug Framework。
- 不新增 SemanticPost effect。
- 不迁移 CharacterSpecialization。
- 不接旧 lilToon / lilPBR inspector。

---

## 3. Capability 与 Authoring 的边界

第八步只处理 authoring / UI 层，不改变 RenderGraph 资源链路。

```text
Capability:
  允许参与什么
  例：WritesAov, ReceivesSemanticPost, WritesObjectCustom

Policy:
  如何参与
  例：maskWeight, objectCustom bits, objectId, materialClass, thickness

Authoring:
  用户在哪里配置
  例：ObjectSemanticAuthoring inspector, MaterialSemanticAuthoring inspector

Execution:
  谁消费
  例：AovOutput, SubsurfaceScattering, SemanticPostProcess
```

本阶段不能让 UI 直接决定 RenderFeature 行为；UI 只能写入已定义语义，RenderFeature 继续只读资源 / 语义。

---

## 4. Object Capability UI 第一版

推荐把 `ObjectSemanticAuthoring` inspector 分成以下区域：

| Section | 字段 | 说明 |
| --- | --- | --- |
| Participation | `WritesAov`, `ReceivesSemanticPost`, `ReceivesCharacterComposite` | 第一版只让前两项生效，角色项占位 |
| Identity | `Object.Id`, `Object.GroupId`, `Object.Flags` | 0-255 / flags byte |
| Object Custom | `Custom0-7` | 8 个对象区域位 |
| Semantic Policy | `MaskWeight` | AOV / SemanticPost 基础参与权重 |
| Apply Target | self / children renderers | 沿用现有 authoring 的 renderer 写入范围 |
| Debug | apply now / clear / ping affected renderers | Editor 辅助，不进入 runtime 契约 |

第一版 object custom 建议含义：

| Bit | 建议名 | 用途 |
| --- | --- | --- |
| Custom0 | Subject | 主要可语义处理对象 |
| Custom1 | Face | 角色脸部 |
| Custom2 | Hair | 头发 / 前发 |
| Custom3 | Eye | 眼睛 |
| Custom4 | Accessory | 配件 |
| Custom5 | Cloth | 布料 / 衣物 |
| Custom6 | Prop | 道具 |
| Custom7 | Reserved | 预留 |

这些名字是 UI preset，不是资源名；正式资源仍是 `Aov.ObjectCustom0_3` / `Aov.ObjectCustom4_7`。

---

## 5. Material Semantic UI 第一版

`MaterialSemanticAuthoring` 仍是迁移期 authoring，不是最终材质系统。它的 inspector 应明确标注为“Material Semantic Authoring / Transitional Producer”。

推荐分区：

| Section | 字段 | 说明 |
| --- | --- | --- |
| Material Class | class id | `Material.Class` |
| SSS | profile / thickness / curvature / source color / weight | 驱动 `Aov.SssSource` 和 SSS |
| Material Custom | custom0-3 | 暂定给 SemanticPost / debug 使用 |
| Preset | Skin / Hair / Eye / Cloth / Metal / Default | 写入一组建议值 |
| Debug | apply now / clear | Editor 辅助 |

第一版 material custom 建议含义：

| Channel | 建议名 | 用途 |
| --- | --- | --- |
| Custom0 | StylizeWeight | 语义后处理强度 |
| Custom1 | EdgeWeight | 后续边缘 / rim 输入 |
| Custom2 | RegionMask | 材质区域选择 |
| Custom3 | Reserved | 预留 |

这些名字同样只属于 UI 和文档，不改变资源契约。

---

## 6. 与旧 HoAovSubject / HoAovGroup 的关系

旧实现可参考：

```text
D:\Unity_Fork\lilToon-URP-Extensions\Runtime\AOV\HoAovSubject.cs
D:\Unity_Fork\lilToon-URP-Extensions\Runtime\AOV\HoAovGroup.cs
```

保留的思想：

- 对象能显式声明 AOV 参与。
- 对象能显式声明 object custom bits。
- 角色 / 部件 / flags 是对象语义，不是材质语义。
- `MaterialPropertyBlock` 是合理的迁移期写入方式。

不继承的结构：

- 不继承旧全局 group priority resolver。
- 不继承旧中文角色 preset 作为新 ABI。
- 不要求 `SetShaderUserValue` 成为唯一数据路径。
- 不把 `_HoAov*` / `_lilHoAov*` 旧名作为新长期公共 ABI。

---

## 7. 实施步骤

### Step 1. 审查当前 authoring 字段

- 读取 `Runtime/Semantic/ObjectSemanticAuthoring.cs`。
- 读取 `Runtime/Semantic/MaterialSemanticAuthoring.cs`。
- 列出字段到语义 / capability / policy 的映射。
- 标记哪些字段只是调试辅助。

### Step 2. 设计 preset 数据

- Object preset：写 object custom bits、mask weight、flags。
- Material preset：写 material class、SSS policy、material custom。
- preset 只在 Editor 使用，不新增 runtime dependency。

### Step 3. 新增 Editor inspector

建议文件：

```text
Editor/Semantic/ObjectSemanticAuthoringEditor.cs
Editor/Semantic/MaterialSemanticAuthoringEditor.cs
Editor/Semantic/HoUrpCapabilityPresetNames.cs
```

Inspector 只编辑现有 runtime component 字段，避免引入新的运行时依赖。

### Step 4. 补 authoring runtime helper

如果现有字段缺少清晰方法，可以补最小 public/internal 方法：

```text
ApplyObjectPreset(...)
ApplyMaterialPreset(...)
ResetSemanticValues()
```

这些方法必须保持可测试，不依赖 `UnityEditor`。

### Step 5. 补测试

测试建议：

- Object preset -> expected custom bits。
- Object flags / id clamp。
- Material preset -> expected class / SSS values。
- Capability registry 中 `WritesAov` / `ReceivesSemanticPost` 存在并指向 Object owner。
- `git diff --check`。

### Step 6. Unity 手动验收

挂载：

1. `HoURP AOV Output`
2. `HoURP Subsurface Scattering`
3. `HoURP Semantic Post Process`
4. `HoURP AOV Debug`

验收：

- Object preset 切换后，AOV Object Custom tile 变化。
- `ReceivesSemanticPost` 打开后，`POST MASK` 有输出。
- `SkinSss` material preset 后，`SSS SRC / SSS WGT / SSS DIFF` 有输出。
- 清空 preset 后，对应 debug tile 归零。

---

## 8. 成功标准

- 用户不需要手填 object custom 数字，就能让对象进入 SemanticPost。
- 用户不需要记住 material custom 通道，就能让材质参与 SSS / SemanticPost 测试。
- Capability / Policy / Authoring 三者边界清楚。
- 旧 `HoAovSubject` / `HoAovGroup` 只作为行为参照，没有污染新 runtime 契约。
- 不新增 RenderGraph 资源冲突。
- 第九步可以自然转向 AOV 生命周期整理与 RSUV 静态语义前移，而不是继续修 authoring 入口。

---

## 9. 风险点

- UI 过早变厚，重复旧材质 inspector 问题。
- preset 名字被误认为长期 ABI。
- Capability 和 policy 混在一个字段里。
- 为了方便把 Light / Material Generator / CharacterSpecialization 都塞进第八步。
- Editor 代码直接依赖 runtime 内部实现细节，导致后续材质系统难迁。

第八步的验收重点是“显式、可复用、可 debug 的 authoring 入口”，不是“完整工具链”。
