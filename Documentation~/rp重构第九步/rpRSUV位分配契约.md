# RP RSUV 位分配契约

## 目标

RSUV 是 renderer 级静态语义的 32-bit 快速输入，不是 AOV 资源，也不是长期替代 MPB 的唯一 ABI。

这份文档专门记录：

- 当前已经实现的 RSUV v0 位布局。
- 为什么 v0 位利用率不够。
- 推荐的 RSUV v1 HoAOV-first Compact 位布局。
- ID 位宽削减后的溢出策略。
- shader / authoring 后续迁移规则。

本轮决策：**HoAOV 中已经属于 renderer/object 静态层、且能塞进 32-bit RSUV 的值，优先进入 RSUV**。不能提前的仍然留在 AOV pass 或 MPB/material producer 中，不因为“HoAOV”这个旧名字而混进 RSUV。

---

## 1. 当前已实现布局：RSUV v0

当前代码位置：

```text
Runtime/Semantic/RendererStaticSemanticValue.cs
Runtime/Semantic/ObjectSemanticAuthoring.cs
Runtime/Shaders/Hidden/HoURP/AOV/AovOutputFallback.shader
```

当前 v0 是旧 `HoAovGroup.PackRendererUserValue()` 的等价迁移：

| Bits | Width | Field | 当前语义 |
| --- | ---: | --- | --- |
| 0-7 | 8 | `ObjectCustomMask` | `Object.Custom0-7` |
| 8-15 | 8 | `GroupId` | `Object.GroupId` |
| 16-23 | 8 | `ObjectId` | `Object.Id` |
| 24-31 | 8 | `Flags` | `Object.Flags` |

当前 shader 判定：

```text
unity_RendererUserValue != 0 -> 视为 RSUV 覆盖
unity_RendererUserValue == 0 -> 回退 MPB
```

当前 v0 的优点：

- 与旧实现事实一致。
- 实现简单。
- `Object.Id / GroupId / Flags` 能直接写回 `Aov.MaskId.gba`。
- `Object.Custom0-7` 能直接写回 `Aov.ObjectCustom0_3 / Aov.ObjectCustom4_7`。

当前 v0 的问题：

- `ObjectId` 和 `GroupId` 各占 8 bit，但当前对象/部件/组通常不需要 256 档。
- 没有 layout/version bit，后续改布局时无法可靠区分旧值和新值。
- 只有 `Flags` 这 8 个 bool 开关，且 bit0 已被 `ReceivesSemanticPost` 占用。
- `packed == 0` 被保留为“无 RSUV 覆盖”，因此合法全零语义只能走 MPB。

结论：

```text
v0 是迁移期临时布局，不应继续扩展。
```

---

## 2. 位宽需求判断

### 2.1 对象语义位

`Object.Custom0-7` 当前已经形成第一批对象/区域语义：

| Bit | 语义 |
| --- | --- |
| 0 | Subject |
| 1 | Face |
| 2 | Hair |
| 3 | Eye |
| 4 | Accessory |
| 5 | Cloth |
| 6 | Prop |
| 7 | Reserved |

这 8 bit 应保留。它们已经被 AOV Debug、SemanticPost rule 和 authoring preset 使用。

### 2.2 Feature / Capability bool

比 8-bit ID / Group 更值得进入 RSUV 的，是 HoAOV 已经在消费链路里证明有价值的对象区域与对象参与能力开关。

第一批建议：

| Switch | 语义 |
| --- | --- |
| `WritesAov` | 是否允许写 AOV |
| `ReceivesSemanticPost` | 是否允许语义后处理消费 |
| `ReceivesSss` | 是否允许屏幕空间 SSS 消费 |
| `ReceivesCharacterComposite` | 是否允许角色特化 composite 消费 |
| `ReceivesOutline` | 是否允许轮廓/边缘类效果消费 |
| `ReceivesDropShadow` | 是否允许屏幕空间投影类效果消费 |
| `ReceivesStylizedShadow` | 是否允许风格化阴影消费 |
| `ReceivesSelectiveImagePost` | 是否允许最终图像栈的选择性遮罩消费 |
| `ReceivesHoShadow` | 是否允许 HoShadow / stylized shadow receiver 类路径消费 |
| `CastsHoShadow` | 是否允许对象级 HoShadow caster 类路径消费 |
| `ParticipatesOit` | 是否允许对象级 OIT 透明路径消费 |
| `ReservedObjectCapability0-1` | 留给下一批对象 capability，必须登记后才能启用 |

这些开关是 bool，放在 RSUV 比放在单独 MPB float 更合理。

### 2.3 ID / Group

`Object.Id` 和 `Object.GroupId` 第一版不应该继续各占 8 bit。

建议目标：

| Field | 建议位宽 | 范围 | 理由 |
| --- | ---: | --- | --- |
| `Object.Id` | 4 bit | 0-15 | 第一版优先表达角色部件/局部对象索引；超出可回退 MPB |
| `Object.GroupId` | 3 bit | 0-7 | 分组位默认激进收紧；大量分组应走更正式的 group table 或 MPB |

原则：

```text
RSUV 是 fast path，不承诺覆盖所有大 ID。
大 ID 不应被截断，应自动回退 MPB。
RSUV v1 不为“未来可能很多组”预留宽位，未来真的需要大量分组时新增表或资源。
```

---

## 3. 推荐目标布局：RSUV v1 HoAOV-first Compact

推荐采用 v1 HoAOV-first Compact 布局：

| Bits | Width | Field | 说明 |
| --- | ---: | --- | --- |
| 0-7 | 8 | `ObjectRegionMask` | `Object.Custom0-7` |
| 8-20 | 13 | `ObjectFeatureFlags` | 低 8 bit 可写回 `Aov.MaskId.a`，高 5 bit 为对象 capability 扩展 |
| 21-24 | 4 | `ObjectId` | `Object.Id`，范围 0-15 |
| 25-27 | 3 | `GroupId` | `Object.GroupId`，范围 0-7 |
| 28 | 1 | `Reserved` | 当前必须为 0，后续只能用于版本内小扩展 |
| 29-30 | 2 | `LayoutVersion` | v1 固定为 `01` |
| 31 | 1 | `Valid` | 必须为 1；0 表示按旧规则或 MPB 回退 |

位图：

```text
31      30 29      28      27 25      24 21      20 8       7 0
+--------+----------+----------+----------+----------+----------+
| Valid  | Version  | Reserved | GroupId  | ObjectId | Feature  | Region |
+--------+----------+----------+----------+----------+----------+
  1 bit    2 bits     1 bit      3 bits     4 bits     13 bits    8 bits
```

推荐常量：

```text
Valid = bit31
LayoutVersion = bits29-30, v1 = 1
```

这样可以同时满足：

- `packed == 0` 仍然表示没有 RSUV 覆盖。
- shader 可以区分 v1 与旧 v0。
- ID / Group 位宽从 16 bit 降到 7 bit。
- 释放出 13 个连续对象 capability bit 和 4 个布局管理位。
- 低 8 个 feature flags 仍能写入当前 `Aov.MaskId.a`。
- 分组不为未知未来过度预留；超出 0-7 时走 MPB 或正式 group table。

---

## 4. ObjectFeatureFlags 分配

### 4.1 Low 8 flags：会写入 `Aov.MaskId.a`

这些 flag 能被当前 AOV consumer 和 debug view 消费：

| RSUV Bit | LowFlag Bit | 名称 | AOV 映射 |
| ---: | ---: | --- | --- |
| 8 | 0 | `WritesAov` | 可影响 `Aov.MaskId.r` / AOV 输出门控 |
| 9 | 1 | `ReceivesSemanticPost` | 建议取代当前 `Object.Flags.bit0` 的语义位置 |
| 10 | 2 | `ReceivesSss` | 可供 SSS / SemanticPost rule 消费 |
| 11 | 3 | `ReceivesCharacterComposite` | 后续角色特化消费 |
| 12 | 4 | `ReceivesOutline` | 轮廓/边缘类效果 |
| 13 | 5 | `ReceivesDropShadow` | 屏幕空间投影类效果 |
| 14 | 6 | `ReceivesStylizedShadow` | 风格化阴影类效果 |
| 15 | 7 | `ReceivesSelectiveImagePost` | 最终图像栈选择性遮罩 |

注意：

- 当前代码的 `ReceivesSemanticPost` 在 v0 中是 `Flags bit0`。
- v1 迁移时应把它移到 `LowFlag bit1`，或者重新定义 `LowFlag bit0` 为 `ReceivesSemanticPost`。
- 建议 `LowFlag bit0 = WritesAov`，因为它是 producer 门控，不是 consumer 门控。

### 4.2 High 5 flags：暂不写入当前 AOV

这些 bit 是 HoAOV-first compact 布局释放出来的对象 capability 位。当前 `Aov.MaskId.a` 只有 8 bit，无法承载全部 13 个 feature flags。

| RSUV Bit | HighFlag Bit | 建议状态 |
| ---: | ---: | --- |
| 16 | 8 | `ReceivesHoShadow` 或正式 shadow receiver capability |
| 17 | 9 | `CastsHoShadow` 或正式 shadow caster capability |
| 18 | 10 | `ParticipatesOit` |
| 19 | 11 | Reserved，需要 capability registry 登记 |
| 20 | 12 | Reserved，需要 capability registry 登记 |

如果未来需要让 high flags 被屏幕空间 consumer 使用，应新增显式资源，例如：

```text
Aov.ObjectFeatureMask
```

不要把 high flags 偷塞进 `Aov.ObjectCustom*` 或 `Aov.SurfaceData`。RSUV 可以提前携带这些静态对象能力，但 AOV consumer 仍只能消费 `Aov.*` 或派生资源。

---

## 5. v1 解码规则

建议 shader 后续按三段处理：

```hlsl
uint packed = unity_RendererUserValue;
bool hasPacked = packed != 0u;
bool hasV1 = (packed & 0x80000000u) != 0u && (((packed >> 29u) & 3u) == 1u);

if (hasV1)
{
    regionMask = packed & 255u;
    featureFlags = (packed >> 8u) & 8191u;
    lowFlags = featureFlags & 255u;
    highFlags = (featureFlags >> 8u) & 31u;
    objectId = (packed >> 21u) & 15u;
    groupId = (packed >> 25u) & 7u;
}
else if (hasPacked)
{
    // legacy v0 decode for migration window
    regionMask = packed & 255u;
    groupId = (packed >> 8u) & 255u;
    objectId = (packed >> 16u) & 255u;
    lowFlags = (packed >> 24u) & 255u;
}
else
{
    // MPB fallback
}
```

迁移窗口结束后，可以删除 v0 decode，但不要在当前阶段直接删除。

---

## 6. v1 溢出策略

v1 不允许静默截断 ID。

| 条件 | 行为 |
| --- | --- |
| `ObjectId <= 15` 且 `GroupId <= 7` | 可写 RSUV v1 |
| `ObjectId > 15` | 自动走 MPB-only，Inspector 显示 ID 超出 RSUV 范围 |
| `GroupId > 7` | 自动走 MPB-only，Inspector 显示 Group 超出 RSUV 范围 |
| high flags 需要被 AOV consumer 读取 | 不能靠当前 AOV，必须新增资源 |

原因：

- RSUV 是优化路径，不是完整表达路径。
- MPB 当前仍能表达 8-bit ID / Group。
- 如果某个项目确实需要 8 个以上对象组，这已经不是 RSUV v1 fast path 的职责，应上 group table / registry。
- 截断 ID 会制造最难查的错误：debug 中看起来有值，但语义错了。

---

## 7. 当前代码与 v1 的差异

当前代码仍是 v0：

```text
ObjectCustomMask: 8 bit
GroupId: 8 bit
ObjectId: 8 bit
Flags: 8 bit
```

需要迁移的代码点：

```text
Runtime/Semantic/RendererStaticSemanticValue.cs
Runtime/Semantic/ObjectSemanticAuthoring.cs
Runtime/Shaders/Hidden/HoURP/AOV/AovOutputFallback.shader
Tests/Runtime/HoUrpRendererStaticSemanticTests.cs
Editor/Semantic/ObjectSemanticAuthoringEditor.cs
```

迁移步骤建议：

1. 在 `RendererStaticSemanticValue` 中新增 v1 pack/unpack，不删除 v0。
2. 在 shader 中新增 v1 decode，保留 v0 decode。
3. `ObjectSemanticAuthoring` 默认写 v1。
4. 如果 ID 溢出，自动 `MaterialPropertyBlockOnly` 或在本次 apply 中跳过 RSUV。
5. Editor 显示当前写入布局：`RSUV v1` / `RSUV v0` / `MPB`。
6. 更新 tests：v0 兼容、v1 pack/unpack、ID 溢出回退。

---

## 8. 不采用的备选布局：v1 Balanced

之前讨论过较保守的 balanced 布局：

| Bits | Width | Field |
| --- | ---: | --- |
| 0-7 | 8 | `ObjectRegionMask` |
| 8-15 | 8 | `ObjectFeatureFlagsLow` |
| 16-20 | 5 | `ObjectId` |
| 21-24 | 4 | `GroupId` |
| 25-28 | 4 | `ObjectFeatureFlagsHigh` |
| 29-30 | 2 | `LayoutVersion` |
| 31 | 1 | `Valid` |

优点：

- `ObjectId` 和 `GroupId` 范围更宽。
- 对还没决定 group table 的项目更保守。

缺点：

- 为 `ObjectId/GroupId` 预留过多，挤压了 HoAOV-first capability bit。
- `GroupId 0-15` 仍不足以作为长期“大量分组”方案，却占掉 1 个可用 capability bit。
- 不符合本轮“HoAOV 已验证对象语义优先塞 RSUV”的取向。

结论：

```text
默认不采用 Balanced。v1 目标改为 HoAOV-first Compact。
```

---

## 9. 决策建议

短期：

- 保留当前 v0 实现，不马上改位宽。
- 把 v0 明确标成迁移期布局。
- UI 显示 RSUV / MPB 是否生效。

下一次实现：

- 改 `RendererStaticSemanticValue` 为 v0+v1 双解码。
- 新写入统一用 v1 HoAOV-first Compact。
- ID / group 超出范围时回退 MPB。
- shader 支持 v1 优先、v0 兼容、MPB fallback。

不要做：

- 不要继续在 v0 的 8-bit `Flags` 里随意塞新语义。
- 不要把 high flags 偷塞进 ObjectCustom 或 SurfaceData。
- 不要截断 ID。
- 不要让材质语义进入 RSUV v1。
