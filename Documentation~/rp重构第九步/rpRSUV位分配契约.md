# RP RSUV 位分配契约

## 目标

RSUV 是 renderer 级静态对象语义的 32-bit 快速输入，不是 AOV 资源，也不是 MPB 的长期替代 ABI。

本阶段采用唯一正式布局：**RSUV v1 HoAOV-first Compact**。HoAOV 中已经属于 renderer/object 静态层、且能塞进 32-bit RSUV 的值，优先进入 RSUV；不能提前的材质、几何、SSS source、derived composite 仍留在 AOV pass 或后续 producer。

## v1 布局

| Bits | Width | Field | 说明 |
| --- | ---: | --- | --- |
| 0-7 | 8 | `ObjectRegionMask` | `Object.Custom0-7` |
| 8-20 | 13 | `ObjectFeatureFlags` | 对象 capability flags；feature bit0 固定为空 |
| 21-24 | 4 | `ObjectId` | `Object.Id`，范围 `0..15` |
| 25-27 | 3 | `GroupId` | `Object.GroupId`，范围 `0..7` |
| 28 | 1 | `Reserved` | 当前必须为 0 |
| 29-30 | 2 | `LayoutVersion` | v1 固定为 `01` |
| 31 | 1 | `Valid` | 必须为 1；0 表示无 RSUV 覆盖 |

```text
31      30 29      28      27 25      24 21      20 8       7 0
+--------+----------+----------+----------+----------+----------+
| Valid  | Version  | Reserved | GroupId  | ObjectId | Feature  | Region |
+--------+----------+----------+----------+----------+----------+
  1 bit    2 bits     1 bit      3 bits     4 bits     13 bits    8 bits
```

规则：

- `packed == 0` 表示没有 RSUV 覆盖。
- shader 只认 `Valid=1` 且 `LayoutVersion=1` 的 packed 值。
- 非 v1 packed 值不解码，继续使用 MPB 输入。
- `ObjectId` 和 `GroupId` 不静默截断。
- authoring / Inspector 直接把 `ObjectId` 限制为 `0..15`，`GroupId` 限制为 `0..7`。
- 底层 `TryPackV1` 遇到异常越界输入时返回 false。

## ObjectRegionMask

`ObjectRegionMask` 对应 `Object.Custom0-7`：

| Bit | 语义 |
| ---: | --- |
| 0 | Subject |
| 1 | Face |
| 2 | Hair |
| 3 | Eye |
| 4 | Accessory |
| 5 | Cloth |
| 6 | Prop |
| 7 | Reserved |

Inspector 必须允许 `Object.Custom0-7` 全部关闭。隐藏的 `ObjectCustomMask` 只能由当前 UI bool 单向生成，不能用 stale hidden mask 值反灌 UI。

## ObjectFeatureFlags

### Low 8 Flags

低 8 位会写入当前 `Aov.MaskId.a`，可被现有 AOV consumer 和 AOV Debug 消费：

| Feature Bit | RSUV Bit | 名称 | AOV 映射 |
| ---: | ---: | --- | --- |
| 0 | 8 | ReservedZero | 固定为 0 |
| 1 | 9 | `ReceivesSemanticPost` | SemanticPost 接收门控 |
| 2 | 10 | `ReceivesSss` | SSS / SemanticPost rule 可见 |
| 3 | 11 | `ReceivesCharacterComposite` | 后续角色特化 composite |
| 4 | 12 | `ReceivesOutline` | 轮廓 / 边缘类效果 |
| 5 | 13 | `ReceivesDropShadow` | 屏幕空间投影类效果 |
| 6 | 14 | `ReceivesStylizedShadow` | 风格化阴影类效果 |
| 7 | 15 | `ReceivesSelectiveImagePost` | 最终图像栈选择性遮罩 |

约束：

- feature bit0 固定为空，打包和解包都必须清零。
- `ReceivesSemanticPost` 是 feature bit1。
- `WritesAov` 不进入 feature flags，继续由 `Aov.MaskId.r` / mask weight 控制。
- AOV Debug 只暴露当前 AOV byte 可见的 low flags：`ObjectFlag0` 用于验证空洞位恒 0，`PostReceiver` 对应 bit1，`ObjectFlag2-7` 对应 bit2-7。
- 不再暴露一个与 `PostReceiver` 重复的 `Flag1` debug 入口。

### High 5 Flags

高 5 位进入 authoring / RSUV packed，并在 AOV 输出阶段写入 `Aov.MaskId.b` bit3-7。`Aov.MaskId.b` 的 bit0-2 仍保留给 compact `Object.GroupId`：

| Feature Bit | RSUV Bit | 状态 |
| ---: | ---: | --- |
| 8 | 16 | `ReceivesHoShadow` 或正式 shadow receiver capability |
| 9 | 17 | `CastsHoShadow` 或正式 shadow caster capability |
| 10 | 18 | `ParticipatesOit` |
| 11 | 19 | Reserved，需要 capability registry 登记 |
| 12 | 20 | Reserved，需要 capability registry 登记 |

不要为 high flags 新增第 8 个 color attachment。当前 AOV pass 的 7 个 color target 加 depth 已经达到 URP RenderGraph native pass attachment 上限。

## 解码规则

```hlsl
uint packed = unity_RendererUserValue;
bool hasV1 =
    (packed & 0x80000000u) != 0u &&
    (((packed >> 29u) & 3u) == 1u);

uint regionMask = DecodeByte(_HoUrpObjectCustomMask);
uint featureFlags = uint(_HoUrpObjectFlags) & 255u;
float objectId = _HoUrpObjectId;
float groupId = _HoUrpObjectGroupId;

if (hasV1)
{
    regionMask = packed & 255u;
    featureFlags = ((packed >> 8u) & 8191u) & ~1u;
    objectId = float((packed >> 21u) & 15u);
    groupId = float((packed >> 25u) & 7u);
}

uint lowFlags = featureFlags & 255u;
```

shader 输出仍写入正式 `Aov.*` 资源。Debug view 也只能读取资源化后的 `Aov.*`，不能直接读取 `unity_RendererUserValue`。

## Authoring 规则

- `Object.Custom0-7` 可全部关闭。
- feature bit0 在 UI 中只读显示为空洞位。
- feature bit1-12 可全部关闭。
- 合法 object custom 全 0 和 feature flags 全 0 都可以通过 RSUV v1 表达。
- `ObjectId` UI 上限为 15。
- `GroupId` UI 上限为 7。
- `MaskWeight` 不进入 RSUV。
- `WritesAov=false` 应通过 mask weight / AOV 输出策略表达，不占 feature flag bit。

## 验收

自动测试必须覆盖：

| 测试 | 期望 |
| --- | --- |
| `TryPackV1(mask, flags, objectId:15, groupId:7)` | 成功并可解回 compact 值 |
| `TryPackV1(... objectId:16 ...)` | 失败 |
| `TryPackV1(... groupId:8 ...)` | 失败 |
| `Object.Custom0-7` 全关 | hidden `ObjectCustomMask` 为 0 |
| `ReceivesSemanticPost=true` | feature bit1 为 1，bit0 为 0 |
| `ReceivesSemanticPost=false` | feature bit1 为 0，bit0 为 0 |
| authoring `ObjectId=512` | clamp 到 15 |
| authoring `GroupId=512` | clamp 到 7 |
| AOV Debug `Flag0Reserved` | 恒为 0 |
| AOV Debug `PostReceiver` | 跟随 feature bit1 |
| AOV Debug `Flag8-12` | 跟随 feature bit8-12，读取 `Aov.MaskId.b` bit3-7 |

## 2026-05-22 调整：high flags 复用 `Aov.MaskId.b`

第九阶段不新增 `Aov.ObjectFeatureFlagsHigh` 或 `Aov.ObjectFeatureMask` 资源。原因不是语义上不能资源化，而是 URP RenderGraph native pass 的 fixed attachment array 只能容纳 8 个 attachment；当前 AOV pass 已经是 7 个 color target，加上 depth 后再增加第 8 个 color target 会触发 `FixedAttachmentArray can only contain 8 items`。

最终布局如下：

| AOV byte | bits | 语义 |
| --- | --- | --- |
| `Aov.MaskId.a` | 0 | `ObjectFeatureFlags.bit0`，保留空洞，期望恒 0 |
| `Aov.MaskId.a` | 1 | `ReceivesSemanticPost` / `ObjectFeatureFlags.bit1` |
| `Aov.MaskId.a` | 2-7 | `ObjectFeatureFlags.bit2-7` |
| `Aov.MaskId.b` | 0-2 | compact `Object.GroupId`，范围 0-7 |
| `Aov.MaskId.b` | 3-7 | `ObjectFeatureFlags.bit8-12` |

所有 consumer 读取 group 时必须先还原 byte，再用 `& 7` 取低 3 位。所有 high flag debug view 必须从 `Aov.MaskId.b` 的 bit3-7 读取，不能直接读取 `unity_RendererUserValue`。

## 不做

- 不在 RSUV v1 中承载材质、几何、SSS source 或 derived composite。
- 不为大量 group/id 预留宽位。
- 不新增第 8 个 color attachment 来承载 high feature flags。
- 不让屏幕空间 consumer 直接依赖 renderer 是否使用 RSUV。
