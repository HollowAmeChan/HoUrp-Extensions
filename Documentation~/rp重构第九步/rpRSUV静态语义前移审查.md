# RP RSUV 静态语义前移审查

## 目标

把旧实现中已经验证可行的 RSUV 思路整理成新包的可选 fast path：

```text
ObjectSemanticAuthoring
  -> RendererStaticSemanticValue
  -> Renderer.SetShaderUserValue(packed)
  -> AovOutputFallback shader reads unity_RendererUserValue
  -> Aov.* object semantic output
```

RSUV 在第九阶段只服务对象静态语义，不承载材质、几何、SSS 或 composite 数据。

位分配的正式讨论见：

```text
Documentation~/rp重构第九步/rpRSUV位分配契约.md
```

## 旧实现事实

旧参考：

```text
lilToon-URP-Extensions/Runtime/AOV/HoAovGroup.cs
lilToon-URP-Extensions/Runtime/AOV/Shaders/HoAOV/HoAovFallback.shader
lilToon/Assets/lilToon/Shader/Includes/lil_pass_hoaov.hlsl
lilPBR/Shaders/hoaov.hlsl
```

旧 `HoAovGroup.PackRendererUserValue()` 布局：

| Bits | Old Field | New Semantic Mapping |
| --- | --- | --- |
| 0-7 | objectCustomMask | `Object.Custom0-7` |
| 8-15 | characterId | `Object.GroupId` |
| 16-23 | partId | `Object.Id` |
| 24-31 | flags | `Object.Flags` |

这也是当前新包 `RendererStaticSemanticValue` 已实现的 v0 布局。它是迁移期布局，不是最终建议布局。

旧 shader 行为：

```text
if unity_RendererUserValue != 0:
    use packed RSUV values
else:
    use MPB / material properties
```

保留的思想：

- renderer 静态语义可以提前写入。
- object custom / group / id / flags 可以紧凑打包。
- shader 可优先读取 packed 值，MPB 作为回退。

不保留的结构：

- 旧 `HoAovGroup` active list。
- priority / hierarchy distance resolver。
- 中文角色 preset。
- `_HoAov*` / `_lilHoAov*` 旧命名。
- RSUV 是唯一真实来源的假设。

## 新包建议实现

注意：本节描述的是当前阶段已落地的 v0 helper。下一轮应按 `rpRSUV位分配契约.md` 迁移到 v1 HoAOV-first Compact 布局：优先把 HoAOV 中已属于 renderer/object 静态层的对象区域、ID/group 和对象 capability flags 前移到 RSUV；材质、几何、SSS source、derived composite 仍不进入 RSUV。

新增：

```text
Runtime/Semantic/RendererStaticSemanticValue.cs
```

建议 API：

```csharp
public readonly struct RendererStaticSemanticValue
{
    public byte ObjectCustomMask { get; }
    public byte GroupId { get; }
    public byte ObjectId { get; }
    public byte Flags { get; }
    public uint PackedValue { get; }

    public static RendererStaticSemanticValue FromPacked(uint packed);
    public static uint Pack(int objectCustomMask, int groupId, int objectId, int flags);
}
```

v1 helper 应新增 compact 布局 API 或等价结构，不能继续把 `GroupId/ObjectId/Flags` 当成三个 byte 扩展：

```csharp
public static bool TryPackV1(
    int objectRegionMask,
    int objectFeatureFlags,
    int objectId,
    int groupId,
    out uint packed);

public static bool TryUnpackV1(uint packed, out RendererStaticSemanticValueV1 value);
```

其中 `ObjectId` 范围为 `0..15`，`GroupId` 范围为 `0..7`，超出时不截断，交给 MPB fallback。

新增：

```text
Runtime/Semantic/RendererStaticSemanticBindingMode.cs
```

建议枚举：

```csharp
public enum RendererStaticSemanticBindingMode
{
    Disabled,
    PreferRendererUserValue,
    MaterialPropertyBlockOnly
}
```

## ObjectSemanticAuthoring 接入

建议字段：

```text
rendererStaticBindingMode = PreferRendererUserValue
```

写入规则：

| Mode | 行为 |
| --- | --- |
| `Disabled` | 不写 RSUV，只使用现有 MPB 路径 |
| `PreferRendererUserValue` | 支持 `SetShaderUserValue` 时写 packed RSUV，同时保留 MPB 回退 |
| `MaterialPropertyBlockOnly` | 显式只写 MPB，用于 debug 对比 |

关键约束：

- 现有 MPB 写入不应被删除。
- 如果 renderer 不支持 `SetShaderUserValue`，必须回退 MPB。
- `OnDisable` / `OnDestroy` 必须清理 RSUV 和 MPB。
- `ReceivesSemanticPost` 仍只影响 `Object.Flags.bit0`。
- v1 中 `WritesAov / ReceivesSemanticPost / ReceivesSss / ReceivesCharacterComposite / ReceivesOutline` 等对象 capability 应进入 `ObjectFeatureFlags`，低 8 bit 继续映射到当前 `Aov.MaskId.a`。
- `ObjectId > 15` 或 `GroupId > 7` 时不写 compact RSUV，不截断，回退 MPB。
- `WritesAov=false` 应让 mask weight 为 0，但是否清 RSUV 要明确：建议仍写 identity/custom，mask 由 AOV 权重控制。

## Shader 读取优先级

`Runtime/Shaders/Hidden/HoURP/AOV/AovOutputFallback.shader` 建议接入：

```hlsl
uint rendererSemantic = unity_RendererUserValue;
bool hasRendererSemantic = rendererSemantic != 0u;

uint objectCustomMask = hasRendererSemantic
    ? (rendererSemantic & 255u)
    : DecodeByte(_HoUrpObjectCustomMask);

float groupId = hasRendererSemantic
    ? ByteToFloat(rendererSemantic, 8u)
    : _HoUrpObjectGroupId;

float objectId = hasRendererSemantic
    ? ByteToFloat(rendererSemantic, 16u)
    : _HoUrpObjectId;

float flags = hasRendererSemantic
    ? ByteToFloat(rendererSemantic, 24u)
    : _HoUrpObjectFlags;
```

这是 v0 读取示意。v1 读取必须先检查 `Valid + LayoutVersion`，再按 compact 位宽解出：

```hlsl
featureFlags = (rendererSemantic >> 8u) & 8191u;
objectId = (rendererSemantic >> 21u) & 15u;
groupId = (rendererSemantic >> 25u) & 7u;
```

注意：

- `unity_RendererUserValue == 0` 表示没有 RSUV 覆盖。
- 如果需要合法全零语义，用 MPB path 表达。
- shader 输出仍写入正式 `Aov.*` 资源。
- shader 不能直接把 RSUV 暴露成 debug resource。

## MPB 与 RSUV 一致性

`ObjectSemanticAuthoring` 同一组字段应能生成两条等价输入：

| Authoring | MPB Property | RSUV Byte |
| --- | --- | --- |
| `ObjectCustomMask` | `_HoUrpObjectCustomMask` | bits 0-7 |
| `EffectiveFlags` / object capability | `_HoUrpObjectFlags` | v1 bits 8-20 |
| `ObjectId` | `_HoUrpObjectId` | v1 bits 21-24 |
| `GroupId` | `_HoUrpObjectGroupId` | v1 bits 25-27 |

`Aov.MaskId.r` 的 `MaskWeight` 不进入 RSUV，继续走 MPB / material property。`WritesAov` 可以进入 RSUV 作为对象 capability，但最终 mask weight 仍由 AOV 输出策略写入。

原因：

- `MaskWeight` 是 participation policy，不是身份语义。
- `WritesAov=false` 可以让同一 renderer 保留静态身份但不参与当前 AOV 输出。
- 32-bit RSUV 应优先留给 HoAOV 已验证的稳定对象语义和对象 capability，而不是为 group/id 预留过宽 byte。

## 清理规则

禁用或销毁 authoring 时：

```text
SetShaderUserValue(0)
_HoUrpAovMaskWeight = 1 or 0 按现有 clear 约定
_HoUrpObjectCustomMask = 0
_HoUrpObjectId = 1
_HoUrpObjectGroupId = 0
_HoUrpObjectFlags = 0
```

需要特别验证：

- 切换 binding mode 不留下旧 RSUV。
- 禁用 component 后 `unity_RendererUserValue` 不继续影响 AOV。
- 多 renderer children 场景都被清理。

## 测试建议

自动测试：

| 测试 | 期望 |
| --- | --- |
| `Pack(1, 2, 3, 4)` | packed 后可解回 1/2/3/4 |
| 超范围输入 | clamp 到 byte |
| `TryPackV1(mask, flags, objectId:15, groupId:7)` | 成功并可解回 compact 值 |
| `TryPackV1(... objectId:16 ...)` | 失败，调用方回退 MPB |
| `TryPackV1(... groupId:8 ...)` | 失败，调用方回退 MPB |
| `ObjectSemanticPreset.Hair` | object custom mask 为 5 |
| `ReceivesSemanticPost=true` | packed flags bit0 为 1 |
| `ReceivesSemanticPost=false` | packed flags bit0 为 0 |
| `Pack(0,0,0,0)` | 文档约定为 shader 侧无 RSUV 覆盖 |

手动验证：

- MPB-only 和 RSUV-preferred 模式下 AOV Debug 输出一致。
- `POST RX` tile 在两种模式下都随 `ReceivesSemanticPost` 变化。
- 不支持 RSUV 的 renderer 仍通过 MPB 输出。

## 风险

- `SetShaderUserValue` API 可用性受 renderer 类型限制。
- packed zero 与合法全零语义容易混淆。
- RSUV 残留会造成非常难查的 AOV 误输出。
- 为了兼容旧 shader，把旧字段名泄漏到新 contract。
- 未来角色系统把 `CharacterId / PartId` 当成已存在正式字段；第九步只映射到 `Object.GroupId / Object.Id`。
