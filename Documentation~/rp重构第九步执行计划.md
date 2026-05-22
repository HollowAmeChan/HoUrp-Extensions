# RP 重构第九步执行计划

> 第九步目标：整理当前 AOV 生命周期，把已经跑通的 `Object/Material Authoring -> AOV -> SSS -> SemanticPost -> Debug` 链路拆成清楚的生命周期层级；同时把旧实现里通过 RSUV（Renderer Shader User Value / `unity_RendererUserValue`）承载的静态 renderer 语义提前到 AOV 输出之前，作为新 RP 的可选快速输入路径。
>
> 本阶段不是完整 Debug Framework，也不是迁移旧 `HoAovGroup`。重点是建立 **AOV 生命周期表、静态 renderer 语义契约、RSUV 打包/解包辅助、AOV producer 输入优先级和对应测试**。

---

## 0. 前置状态

当前新包已经具备：

- `AovOutputRendererFeature`：在 `AfterRenderingOpaques` 生成最小 AOV MRT。
- `HoUrpAovResourceDeclaration`：声明 `Aov.MaskId`、`Aov.NormalDepth`、`Aov.ObjectCustom0_3`、`Aov.ObjectCustom4_7`、`Aov.SurfaceData`、`Aov.MaterialCustom0_3`、`Aov.SssSource`。
- `ObjectSemanticAuthoring`：通过 `MaterialPropertyBlock` 写入 object mask、object id、group id、flags、object custom mask。
- `MaterialSemanticAuthoring`：通过 `MaterialPropertyBlock` 写入 material class、SSS profile、thickness、curvature、material custom、SSS source。
- `SubsurfaceScatteringRendererFeature`：消费 AOV 的 SSS 输入，生成 `Sss.Source` / `Sss.Diffusion`。
- `SemanticPostProcessRendererFeature`：消费 AOV / SSS，生成 `SemanticPost.Mask` 并做最小语义 tint。
- `AovDebugRendererFeature`：能观察 AOV、SSS、SemanticPost mask 的注册 debug view。
- 第八步已把对象/材质 authoring UI 做成可用入口。

也就是说，目前问题已经不再是“语义链路能不能通”，而是：

```text
哪些语义是静态的？
哪些语义必须通过 AOV 贴图在每帧生产？
哪些语义只是 AOV 的派生消费结果？
哪些旧 RSUV 能力应该提前到 AOV pass 之前？
```

---

## 1. 为什么第九步先整理 AOV 生命周期

原大纲里第九步曾计划直接进入完整 Debug Framework，但第八步之后更迫切的问题是 AOV 生命周期已经开始变厚：

```text
ObjectSemanticAuthoring / MaterialSemanticAuthoring
  -> AovOutput MRT
  -> SSS resources
  -> SemanticPost resources
  -> AOV Debug AllRegistered
```

如果此时直接扩展 Debug Framework，debug 只能观察“已经混在一起的 AOV 结果”，不能回答更根本的问题：

- 这个值是对象静态语义、材质静态语义、几何语义、着色派生语义，还是后续 composite 语义？
- 它的生命周期是 scene/static、per-renderer、per-camera、per-frame transient，还是 debug-only？
- 它能否在 AOV pass 之前绑定，而不是每个 pass 都通过 MPB 或材质属性重复解释？
- 它是否应该进入 RSUV 这样的 renderer 级静态输入？

因此第九步先把 AOV 生命周期梳理清楚，再让 Debug Framework 读取这张生命周期表，而不是让 Debug Framework 反过来定义资源关系。

---

## 2. 本阶段范围

### 做

- 建立 `AOV 生命周期表`，明确每个 AOV 资源、语义、producer、consumer、生命周期和 debug view。
- 把 `Object.Id`、`Object.GroupId`、`Object.Flags`、`Object.Custom0-7` 归类为 **Renderer Static Semantic**。
- 引入新包自己的 RSUV 打包/解包辅助，先只覆盖旧实现已经验证过的 32-bit 布局思想。
- 明确 RSUV 是可选 fast path，不是唯一 ABI；MPB 仍然保留为兼容和通用路径。
- 在 `ObjectSemanticAuthoring` 侧预留或实现 “prefer renderer user value” 的策略入口。
- 让 AOV fallback shader 明确按优先级读取：

```text
RSUV / renderer static semantic
  -> MaterialPropertyBlock object semantic
  -> fallback material default
```

- 补 runtime tests：
  - RSUV pack/unpack。
  - byte clamp。
  - object custom mask 映射。
  - RSUV 与 MPB 语义一致性。
  - contract registry 能查询 AOV 生命周期归属。

### 不做

- 不迁移旧 `HoAovGroup` 的全局 active group 列表。
- 不迁移旧 `HoAovGroup` 的 priority / hierarchy distance resolver。
- 不把 `unity_RendererUserValue` 变成新系统唯一数据源。
- 不修改材质系统生成逻辑。
- 不迁移 `HoCharacterSpecialization`。
- 不新增完整 Debug HUD / overlay / capture。
- 不迁移 Shoost final image stack。
- 不改变 AOV MRT 的第一版资源集合，除非生命周期表暴露出必须补的最小字段。

---

## 3. AOV 生命周期分层

第九步应把 AOV 相关数据分成五层。

### 3.1 Static Authoring

场景或 prefab 上的作者输入，不等于 GPU 资源。

代表：

- `ObjectSemanticAuthoring`
- `MaterialSemanticAuthoring`
- 后续材质 preset / generated material producer

生命周期：

```text
Scene / Prefab / Authoring-time
```

要求：

- 可以被 inspector 编辑。
- 可以被测试。
- 不直接等同 RenderGraph resource。
- 不因为临时 UI preset 名字而形成长期 ABI。

### 3.2 Renderer Static Semantic

每个 Renderer 上稳定存在的对象语义。旧实现里最有价值的 RSUV 属于这一层。

第一版字段：

| Field | Bits | Semantic |
| --- | --- | --- |
| `objectCustomMask` | 0-7 | `Object.Custom0-7` |
| `groupId` | 8-15 | `Object.GroupId` |
| `objectId` / `partId` | 16-23 | `Object.Id` |
| `flags` | 24-31 | `Object.Flags` |

说明：

- 这个布局参考旧 `HoAovGroup.PackRendererUserValue()`，但命名归新 RP 契约。
- `partId` 在新系统里先映射为 `Object.Id`，不单独创建长期字段。
- 如果后续角色系统需要 `Character.Id / Part.Id`，应新增正式语义，而不是偷偷复用 object id。

生命周期：

```text
Per Renderer, changed by authoring/editor/runtime binding
```

要求：

- 能提前写入 renderer。
- 能被 AOV pass 读取。
- 能被 debug 证明来源。
- 不能绕过 `SemanticRegistry` / `ResourceRegistry`。

### 3.3 Per-Camera AOV Resource

当前 AOV MRT 属于这一层。

资源：

- `Aov.MaskId`
- `Aov.NormalDepth`
- `Aov.ObjectCustom0_3`
- `Aov.ObjectCustom4_7`
- `Aov.SurfaceData`
- `Aov.MaterialCustom0_3`
- `Aov.SssSource`

生命周期：

```text
Per camera / frame transient, produced by AovOutput
```

要求：

- 只能由 `AovOutput` 或后续正式 producer 生产。
- 必须经 `HoUrpAovResourceDeclaration` 声明。
- 每个 consumer 必须在 RenderGraph pass 里显式 `UseTexture`。
- 全局 shader property 只作为 shader binding，不作为生命周期管理方式。

### 3.4 Derived Semantic Resource

由 AOV 派生而来的屏幕空间或 composite 资源。

当前代表：

- `Sss.Source`
- `Sss.Diffusion`
- `SemanticPost.Mask`

生命周期：

```text
Per camera / frame transient, produced after AOV
```

要求：

- producer / consumer 必须写在 `FeatureDescriptor`。
- debug view 读取它们时必须能追溯到源 AOV。
- 不允许把派生资源重新写回 AOV 当成“补字段”。

### 3.5 Debug View

Debug 不是 AOV 的一部分，而是对 AOV 生命周期中某一层的可视化。

当前代表：

- `AOV.Mask`
- `AOV.ObjectId`
- `AOV.ObjectCustom0-7`
- `AOV.MaterialClass`
- `AOV.SssSource`
- `SSS.Source`
- `SSS.Diffusion`
- `SemanticPost.Mask`

第九步只要求 debug view 能引用生命周期表，不做完整 overlay / capture。

---

## 4. 与旧 RSUV 实现的关系

旧实现可参考：

```text
D:\Unity_Fork\lilToon-URP-Extensions\Runtime\AOV\HoAovGroup.cs
D:\Unity_Fork\lilToon\Assets\lilToon\Shader\Includes\lil_pass_hoaov.hlsl
D:\Unity_Fork\lilPBR\Shaders\hoaov.hlsl
```

旧实现事实：

- `HoAovGroup.PackRendererUserValue()` 把 `objectCustomMask + characterId + partId + flags` 打包成 32-bit。
- `MeshRenderer.SetShaderUserValue()` / `SkinnedMeshRenderer.SetShaderUserValue()` 写入 renderer。
- shader 读取 `unity_RendererUserValue`。
- 如果 `unity_RendererUserValue != 0`，则优先使用 RSUV，否则回退 MPB 属性。

新系统保留的思想：

- Renderer 级静态语义可以提前写入。
- object custom / group / id / flags 可以合并成一个紧凑输入。
- shader 可以优先读取紧凑输入，降低每个 renderer 需要同步的属性数量。

新系统不继承的结构：

- 不继承旧 `HoAovGroup` 的全局列表。
- 不继承旧 priority resolver。
- 不继承旧中文角色部件 preset 作为 ABI。
- 不继承 `_HoAov*` / `_lilHoAov*` 命名。
- 不把 `SetShaderUserValue` 成功与否作为功能是否成立的唯一条件。

---

## 5. 新 RSUV 契约建议

建议新增运行时代码：

```text
Runtime/Semantic/RendererStaticSemanticValue.cs
Runtime/Semantic/RendererStaticSemanticBinding.cs
```

第一版可以只做纯数据与 helper：

```csharp
public readonly struct RendererStaticSemanticValue
{
    public readonly byte ObjectCustomMask;
    public readonly byte GroupId;
    public readonly byte ObjectId;
    public readonly byte Flags;

    public uint PackedValue { get; }

    public static RendererStaticSemanticValue FromPacked(uint packed);
    public static uint Pack(int objectCustomMask, int groupId, int objectId, int flags);
}
```

绑定策略：

```text
RendererStaticSemanticBindingMode
  Disabled
  PreferRendererUserValue
  MaterialPropertyBlockOnly
```

第一版默认值建议：

```text
PreferRendererUserValue
```

但必须满足：

- `SetShaderUserValue` 不可用时自动回退 MPB。
- Editor / Tests 能验证 pack/unpack，不依赖实际渲染。
- `ObjectSemanticAuthoring` 的现有行为不能被破坏。

---

## 6. AOV shader 读取优先级

AOV fallback shader 当前应保持“读取统一属性”最小实现。第九步若接入 RSUV，应明确优先级：

```hlsl
uint rendererSemantic = unity_RendererUserValue;
bool hasRendererSemantic = rendererSemantic != 0u;

uint objectCustomMask = hasRendererSemantic
    ? rendererSemantic & 255u
    : DecodeByte(_HoUrpObjectCustomMask);

float objectGroupId = hasRendererSemantic
    ? DecodeByte(rendererSemantic, 8u)
    : _HoUrpObjectGroupId;

float objectId = hasRendererSemantic
    ? DecodeByte(rendererSemantic, 16u)
    : _HoUrpObjectId;

float objectFlags = hasRendererSemantic
    ? DecodeByte(rendererSemantic, 24u)
    : _HoUrpObjectFlags;
```

注意：

- shader 内部名称可以使用 `_HoUrp*`，但正式资源名仍是 `Aov.*`。
- `unity_RendererUserValue == 0` 必须代表“没有 renderer static semantic”，不是合法的全零语义覆盖。
- 如果确实需要写全零语义，应走 MPB 路径。

---

## 7. AOV 生命周期表草案

第九步建议新增文档：

```text
Documentation~/rp重构第九步/rpAov生命周期审查.md
Documentation~/rp重构第九步/rpRSUV静态语义前移审查.md
Documentation~/rp重构第九步/rp第九阶段实现边界审查.md
Documentation~/rp重构第九步/rp第九阶段测试与验收清单.md
```

生命周期表模板：

| Name | Kind | Domain | Producer | Consumer | Lifetime | Source Layer | Debug View | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Object.Custom0-7` | Semantic | Object | Authoring / RSUV | AovOutput / SemanticPost | Per Renderer | Renderer Static Semantic | `AOV.ObjectCustom0-7` | 可 RSUV 前移 |
| `Object.GroupId` | Semantic | Object | Authoring / RSUV | AovOutput / SemanticPost | Per Renderer | Renderer Static Semantic | 待补 | 可 RSUV 前移 |
| `Object.Id` | Semantic | Object | Authoring / RSUV | AovOutput / SemanticPost | Per Renderer | Renderer Static Semantic | `AOV.ObjectId` | 旧 partId 暂映射 |
| `Object.Flags` | Semantic | Object | Authoring / RSUV | AovOutput / SemanticPost | Per Renderer | Renderer Static Semantic | `AOV.ObjectFlag0-7` | bit0 已用于 SemanticPost gate |
| `Material.Class` | Semantic | Material | Material authoring | AovOutput / SemanticPost | Per Renderer or Material | MPB / future material producer | `AOV.MaterialClass` | 不进 RSUV 第一版 |
| `Geometry.WorldNormal` | Semantic | Geometry | AovOutput | SSS / SemanticPost / Debug | Per Camera Frame | AOV MRT | `AOV.WorldNormal` | 不能前移 |
| `Geometry.LinearDepth` | Semantic | Geometry | AovOutput | SSS / SemanticPost / Debug | Per Camera Frame | AOV MRT | `AOV.LinearDepth` | 不能前移 |
| `Aov.SssSource` | Resource | Shading | AovOutput | SSS / SemanticPost / Debug | Per Camera Frame | AOV MRT | `AOV.SssSource` | 不属于对象静态语义 |
| `Sss.Diffusion` | Resource | Shading | SSS | SemanticPost / Debug / CameraColor | Per Camera Frame | Derived Resource | `SSS.Diffusion` | 派生资源 |
| `SemanticPost.Mask` | Resource | Composite | SemanticPost | Debug / CameraColor | Per Camera Frame | Derived Resource | `SemanticPost.Mask` | composite mask |

---

## 8. 实施步骤

### Step 1. 审查当前 AOV 生命周期

- 读取 `HoUrpBuiltInNames`。
- 读取 `HoUrpBuiltInContracts`。
- 读取 `HoUrpAovResourceDeclaration`。
- 读取 `AovOutputRendererFeature`。
- 读取 `SubsurfaceScatteringRendererFeature`。
- 读取 `SemanticPostProcessRendererFeature`。
- 输出生命周期表，标记每个资源是 static / per-renderer / per-camera / derived / debug-only。

### Step 2. 定义 Renderer Static Semantic helper

建议新增：

```text
Runtime/Semantic/RendererStaticSemanticValue.cs
```

要求：

- 不依赖 `UnityEditor`。
- pack/unpack 与旧 RSUV 事实一致，但命名归新语义。
- 输入 clamp 到 byte。
- 对 `0` 值语义有明确说明：packed zero 表示没有 RSUV 覆盖。

### Step 3. 给 ObjectSemanticAuthoring 增加绑定策略

建议最小字段：

```text
RendererStaticSemanticBindingMode rendererStaticBinding = PreferRendererUserValue
```

应用逻辑：

```text
if PreferRendererUserValue && renderer supports SetShaderUserValue:
    write packed renderer static semantic
else:
    write existing MPB fields
```

保留现有 MPB 字段写入，至少在第九步不要删除。

### Step 4. 更新 AOV fallback shader

在 `AovOutputFallback.shader` 中接入 `unity_RendererUserValue`：

- 有 RSUV 时读取 packed object custom / group / id / flags。
- 没有 RSUV 时读取 `_HoUrpObjectCustomMask`、`_HoUrpObjectGroupId`、`_HoUrpObjectId`、`_HoUrpObjectFlags`。
- material semantic 和 SSS 输入仍走 MPB / material path，不进 RSUV 第一版。

### Step 5. 更新 contract / docs

- 在 contract 说明里标记 `Object.Custom0-7`、`Object.Id`、`Object.GroupId`、`Object.Flags` 的 source layer 可为 `RendererStaticSemantic`。
- 文档说明 `RSUV` 是实现路径，不是 resource 名。
- 不把 `unity_RendererUserValue` 暴露成 public package ABI 名称。

### Step 6. 补测试

测试建议：

- `RendererStaticSemanticValue.Pack(1, 2, 3, 4)` 可解包回同样 byte。
- 超范围输入 clamp 到 `0..255`。
- `ObjectSemanticPreset.Hair` 通过 helper 得到 object custom mask `5`。
- `ReceivesSemanticPost` 对 `Flags bit0` 的有效值和 packed flags 一致。
- packed zero 被视为“无 RSUV 覆盖”的约定写入测试说明。

### Step 7. Unity 手动验收

场景配置：

1. 挂载 `HoURP AOV Output`。
2. 挂载 `HoURP Semantic Post Process`。
3. 挂载 `HoURP AOV Debug`。
4. 对同一个模型切换 `ObjectSemanticAuthoring` 的 binding mode。

验收：

- MPB-only 和 RSUV-preferred 模式下，`AOV.ObjectCustom0-7` 输出一致。
- `AOV.ObjectId` / `AOV.ObjectFlag0` 输出一致。
- `SemanticPost.Mask` 在两种模式下结果一致。
- 不支持 `SetShaderUserValue` 的 renderer 回退 MPB 后仍有输出。
- 清空/禁用 authoring 后，RSUV 与 MPB 都不会残留旧语义。

---

## 9. 成功标准

- AOV 生命周期表能回答每个 AOV 相关值的来源、生产者、消费者、生命周期和 debug view。
- `Object.Custom0-7`、`Object.Id`、`Object.GroupId`、`Object.Flags` 被正式归类为 Renderer Static Semantic。
- 新包有自己的 RSUV pack/unpack helper，不再只能引用旧 `HoAovGroup.PackRendererUserValue()`。
- `ObjectSemanticAuthoring` 仍保持第八步 UI 能力，并多一条可选的 renderer static semantic 写入路径。
- AOV fallback shader 能优先读取 RSUV，并正确回退 MPB。
- 没有引入旧 `HoAovGroup` 的全局 resolver、priority、中文 preset 或旧 ABI。
- 第十步可以在生命周期表基础上继续推进 Debug Framework，或者再进入材质系统重构。

---

## 10. 风险点

- 把 RSUV 误当成新 RP 的唯一对象语义 ABI。
- 为了复刻旧行为，把 `HoAovGroup` 的全局 priority resolver 搬进新包。
- 忘记清理 renderer user value，导致禁用 authoring 后旧语义残留。
- packed zero 的语义不清，导致“合法全零对象”和“无 RSUV 覆盖”混淆。
- 把 material semantic 也塞进第一版 RSUV，导致 32-bit 过早不够用。
- Debug Framework 在生命周期表稳定前继续扩张，结果只能观察输出，不能解释来源。

第九步的验收重点是 **生命周期清晰、静态语义可提前、RSUV 可选且可回退**，不是让 AOV 系统一次性变成完整 renderer semantic database。
