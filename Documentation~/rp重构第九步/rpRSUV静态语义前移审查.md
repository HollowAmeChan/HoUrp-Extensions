# RP RSUV 静态语义前移审查

## 目标

RSUV v1 把对象静态语义提前绑定到 renderer，用于减少 AOV pass 对 per-renderer MPB 的依赖。它只服务 object/static semantic，不承载材质、几何、SSS 或 composite 数据。

当前链路：

```text
ObjectSemanticAuthoring
  -> RendererStaticSemanticValue.TryPackV1
  -> Renderer.SetShaderUserValue(packed)
  -> AovOutputFallback shader checks Valid + LayoutVersion
  -> Aov.* object semantic output
  -> SemanticPost / AOV Debug consume Aov.* resources
```

正式位分配见：

```text
Documentation~/rp重构第九步/rpRSUV位分配契约.md
```

## 实现范围

新增或变更的核心文件：

```text
Runtime/Semantic/RendererStaticSemanticValue.cs
Runtime/Semantic/RendererStaticSemanticBindingMode.cs
Runtime/Semantic/ObjectSemanticAuthoring.cs
Editor/Semantic/ObjectSemanticAuthoringEditor.cs
Runtime/Shaders/Hidden/HoURP/AOV/AovOutputFallback.shader
Runtime/Shaders/Hidden/HoURP/SemanticPost/AovReadProbe.shader
Runtime/Debug/AovDebugRendererFeature.cs
Runtime/Core/HoUrpBuiltInContracts.cs
```

## Authoring

`ObjectSemanticAuthoring` 是 RSUV v1 的唯一 authoring 入口。

字段规则：

| Authoring 字段 | RSUV v1 | MPB / AOV |
| --- | --- | --- |
| `Object.Custom0-7` | bits 0-7 | `Aov.ObjectCustom0_3` / `Aov.ObjectCustom4_7` |
| `ObjectFeatureFlags` | bits 8-20 | low 8 bits 写入 `Aov.MaskId.a` |
| `ObjectId` | bits 21-24 | `Aov.MaskId.g` |
| `GroupId` | bits 25-27 | `Aov.MaskId.b` |
| `MaskWeight` | 不进入 RSUV | `Aov.MaskId.r` |
| `WritesAov` | 不进入 RSUV | 通过 mask weight / AOV 输出策略表达 |

Inspector 规则：

- `Object.Custom0-7` 可全部关闭。
- 隐藏 `ObjectCustomMask` 只能由当前 UI bool 单向生成。
- feature bit0 显示为空洞位，只读且恒为 0。
- feature bit1-12 可全部关闭。
- `ObjectId` 上限为 15。
- `GroupId` 上限为 7。
- `ReceivesSemanticPost` 对应 feature bit1。

## Binding Mode

| Mode | 行为 |
| --- | --- |
| `Disabled` | 清 RSUV，只保留 MPB 清理 / 默认值 |
| `PreferRendererUserValue` | 支持 `SetShaderUserValue` 时写 RSUV v1，同时保留 MPB 输出 |
| `MaterialPropertyBlockOnly` | 不写 RSUV，只使用 MPB，用于 debug 对比 |

清理规则：

```text
SetShaderUserValue(0)
_HoUrpAovMaskWeight = 1 or 0 按当前 authoring 状态
_HoUrpObjectCustomMask = 0
_HoUrpObjectId = 1
_HoUrpObjectGroupId = 0
_HoUrpObjectFlags = 0
```

## Shader

`AovOutputFallback.shader` 必须只解码 v1：

```hlsl
uint rendererStaticSemantic = unity_RendererUserValue;
bool hasRendererSemanticV1 =
    (rendererStaticSemantic & 0x80000000u) != 0u &&
    (((rendererStaticSemantic >> 29u) & 3u) == 1u);
```

当 `hasRendererSemanticV1` 为 false 时，shader 使用 `_HoUrpObjectCustomMask`、`_HoUrpObjectId`、`_HoUrpObjectGroupId`、`_HoUrpObjectFlags`。

当 `hasRendererSemanticV1` 为 true 时：

```hlsl
objectCustomMask = rendererStaticSemantic & 255u;
featureFlags = ((rendererStaticSemantic >> 8u) & 8191u) & ~1u;
objectFlags = featureFlags & 255u;
objectId = (rendererStaticSemantic >> 21u) & 15u;
groupId = (rendererStaticSemantic >> 25u) & 7u;
```

`AovReadProbe.shader` 的 SemanticPost gate 读取 `Aov.MaskId.a` bit1。

## Debug Consumer

当前已有下游消费端是 AOV Debug 和 SemanticPost probe。Debug view 读取的是 `Aov.MaskId`，不是 `unity_RendererUserValue`。

| Debug View | AOV bit | 语义 |
| --- | ---: | --- |
| `Flag0Reserved` / `AOV.ObjectFlag0` | `MaskId.a bit0` | 保留空洞，期望恒 0 |
| `PostReceiver` / `AOV.ObjectFlag1` | `MaskId.a bit1` | `ReceivesSemanticPost` gate |
| `AOV.ObjectFlag2-7` | `MaskId.a bit2-7` | low feature flags |
| `AOV.ObjectFlag8-12` | `MaskId.b bit3-7` | high feature flags |

`Aov.MaskId.b` 的低 3 位仍是 compact `Object.GroupId`。任何按 group 做规则匹配的 consumer 都必须用 `round(maskId.b * 255) & 7` 解码，避免 high flag 位污染 group 判断。

`AllRegistered` 必须包含 `AOV.ObjectFlag0-12`。`Flag8-12` 属于已经落到 AOV resource 的语义，不再允许 debug view 直接回读 RSUV。

## 验收

自动测试：

| 测试 | 期望 |
| --- | --- |
| `TryPackV1(mask, flags, 15, 7)` | 成功 |
| `TryPackV1(... objectId:16 ...)` | 失败 |
| `TryPackV1(... groupId:8 ...)` | 失败 |
| `Object.Custom0-7` 全关 | hidden mask 为 0 |
| feature flags 全关 | `ObjectFeatureFlags == 0` |
| `ReceivesSemanticPost` 开关 | feature bit1 / AOV bit1 同步 |
| authoring ID / Group 赋越界值 | clamp 到 15 / 7 |
| Debug mapping | `Flag0Reserved -> mode27`，`PostReceiver -> mode28`，`Flag8 -> mode35`，`Flag12 -> mode39` |

手动验证：

- MPB-only 和 RSUV-preferred 模式下 AOV Debug 输出一致。
- `POST RX` tile 在两种模式下都随 `ReceivesSemanticPost` 变化。
- `AOV.ObjectFlag8-12` 在 individual view 和 `AllRegistered` 中都随 high feature flags 变化。
- 关闭全部 `Object.Custom0-7` 后 object custom debug tile 全黑。
- 禁用 / 删除 component 后不残留 renderer user value。

## 风险

- `SetShaderUserValue` API 可用性受 renderer 类型限制。
- packed zero 与合法全零语义容易混淆，shader 必须通过 valid/version 判断。
- RSUV 残留会造成 AOV 误输出。
- `Aov.MaskId.b` 同时承载 group 和 high flags，consumer 忘记 mask 低 3 位会误判 group。
