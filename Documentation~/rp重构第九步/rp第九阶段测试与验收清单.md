# RP 第九阶段测试与验收清单

## 自动检查

| 检查 | 期望状态 |
| --- | --- |
| AOV 生命周期文档 | 覆盖 `Aov.*`、`Sss.*`、`SemanticPost.Mask` |
| RSUV v1 pack/unpack tests | region mask、13-bit feature flags、`ObjectId 0..15`、`GroupId 0..7` 均可解回 |
| RSUV v1 overflow tests | 底层 `TryPackV1` 对 `ObjectId > 15` 或 `GroupId > 7` 返回失败，不静默截断 |
| preset -> RSUV tests | Subject / Hair 等 preset 生成预期 object custom mask |
| `ReceivesSemanticPost` tests | feature flags bit1 与 `EffectiveFlags` / packed flags 一致，bit0 固定为空 |
| object capability flag tests | bit0 固定为空；`ReceivesSss`、`ReceivesCharacterComposite` 等进入 v1 feature flags 的预期 bit |
| Inspector Object.Custom0-7 | 所有 custom bit 可全部关闭，隐藏 `ObjectCustomMask` 不反灌旧值 |
| Inspector ObjectId / GroupId | `ObjectId` UI 上限为 15，`GroupId` UI 上限为 7 |
| Inspector feature flags | 显示 bit0 空洞和 bit1-12；bit1-12 可全部关闭 |
| MPB fallback tests | `MaterialPropertyBlockOnly` 路径保持第八步行为 |
| packed zero convention | 测试或文档明确 shader 侧视为无 RSUV 覆盖 |
| contract registry tests | object semantics 仍注册到正确 domain/resource/debug view |
| shader property mapping tests | `_HoUrpObjectCustomMask` / id / group / flags 仍存在 |
| `git diff --check` | 无空白错误 |

建议新增测试文件：

```text
Tests/Runtime/HoUrpRendererStaticSemanticTests.cs
```

建议覆盖：

```csharp
RendererStaticSemanticValue.TryPackV1(mask: 1, featureFlags: 0x1fff, objectId: 15, groupId: 7, out _)
RendererStaticSemanticValue.TryPackV1(mask: 1, featureFlags: 0, objectId: 16, groupId: 0, out _)
RendererStaticSemanticValue.TryPackV1(mask: 1, featureFlags: 0, objectId: 0, groupId: 8, out _)
ObjectSemanticAuthoring.ApplyPreset(ObjectSemanticPreset.Hair)
ObjectSemanticAuthoring.ReceivesSemanticPost false/true
ObjectSemanticAuthoring Object.Custom0-7 all off
ObjectSemanticAuthoring ObjectId / GroupId clamp to 15 / 7
```

## 手动 Unity 验收

挂载：

1. `HoURP AOV Output`
2. `HoURP Semantic Post Process`
3. `HoURP AOV Debug`

可选挂载：

4. `HoURP Subsurface Scattering`

## 场景准备

创建三个对象：

| 对象 | Authoring | Preset | Binding |
| --- | --- | --- | --- |
| A | `ObjectSemanticAuthoring` | Subject | MPB-only |
| B | `ObjectSemanticAuthoring` | Subject | RSUV-preferred |
| C | `ObjectSemanticAuthoring` | Hair | RSUV-preferred |

如果 `MaterialSemanticAuthoring` 同时存在，先使用默认值，避免材质语义影响 object semantic 验收。

## AOV 验收

| 操作 | 期望 |
| --- | --- |
| A 使用 MPB-only，B 使用 RSUV-preferred | `AOV.Mask` 一致 |
| A/B 都是 Subject | `SUBJECT` tile 一致 |
| C 是 Hair | `SUBJECT` 和 `HAIR` tile 亮，`FACE` 不亮 |
| B 切换到 MPB-only | AOV 输出不变 |
| B 切换回 RSUV-preferred | AOV 输出不变 |
| B 关闭全部 `Object.Custom0-7` | `SUBJECT`、`FACE`、`HAIR` 等 object custom tile 全部变黑 |
| B 关闭 `ReceivesSemanticPost` | `POST RX` 变黑，`SUBJECT` 仍保持 |
| B 打开 `ReceivesSemanticPost` | `POST RX` 变亮 |
| B Clear preset | object custom tile 归零 |

## SemanticPost 验收

| 场景 | 期望 |
| --- | --- |
| Subject rule 命中 A/B | MPB-only 和 RSUV-preferred 都命中 |
| Hair rule 命中 C | 只命中 Hair 对象 |
| B `ReceivesSemanticPost=false` | `SUBJECT` tile 仍亮，`POST MASK` 不命中 |
| B `ReceivesSemanticPost=true` | `POST MASK` 恢复命中 |

## 清理验收

| 操作 | 期望 |
| --- | --- |
| 禁用 `ObjectSemanticAuthoring` | RSUV 清零，MPB 清理，AOV 不残留 stale object custom |
| 删除 `ObjectSemanticAuthoring` | 同上 |
| includeChildren 开启 | 子 renderer 都更新 / 清理 |
| includeChildren 关闭 | 只影响自身 renderer |
| 切换 binding mode | 不出现 stale RSUV 覆盖新 MPB 的残留 |

## Debug 验收

第九阶段不做完整 Debug Framework，但现有 AOV Debug 必须能验证：

| Debug View | 验收 |
| --- | --- |
| `AOV.Mask` | mask weight 仍由 MPB/policy 控制 |
| `AOV.ObjectId` | RSUV 和 MPB 输出一致 |
| `AOV.ObjectFlag0` / `Flag0Reserved` | bit0 保留空洞恒为 0 |
| `AOV.ObjectFlag1` / `POST RX` | `ReceivesSemanticPost` gate 一致 |
| `AOV.ObjectFlag2-7` | low feature flags 与 `Aov.MaskId.a` bit2-7 一致 |
| `AOV.ObjectFlag8-12` | high feature flags 与 `Aov.MaskId.b` bit3-7 一致 |
| `AOV.ObjectCustom0-7` | preset 与 binding mode 无关 |
| `SemanticPost.Mask` | 只受最终规则和 flag gate 影响 |

`ObjectFeatureFlags.bit8-12` 已进入 authoring / RSUV packed，并在 AOV 输出时写入 `Aov.MaskId.b` bit3-7。第九阶段验收必须确认 individual debug view 和 `AllRegistered` 都包含 `AOV.ObjectFlag8-12`。

RenderGraph 验收额外要求：

| 检查 | 期望 |
| --- | --- |
| AOV pass color attachment | 最高只使用 index 6 |
| shader target | 不出现 `SV_Target7` |
| AOV resource | 不新增 `Aov.ObjectFeatureFlagsHigh` / `Aov.ObjectFeatureMask` |
| Unity Play Mode | 不再出现 `FixedAttachmentArray can only contain 8 items` |
| SemanticPost group rule | 读取 `Aov.MaskId.b` 时先 `& 7` |

## 当前代码落地检查

第九阶段完成时，应能在代码中找到：

```text
Runtime/Semantic/RendererStaticSemanticValue.cs
Runtime/Semantic/RendererStaticSemanticBindingMode.cs
Runtime/Semantic/ObjectSemanticAuthoring.cs
Runtime/Shaders/Hidden/HoURP/AOV/AovOutputFallback.shader
Tests/Runtime/HoUrpRendererStaticSemanticTests.cs
```

如果没有实现 binding mode，只实现 helper 和 shader path，也必须在未决项说明原因。

## 未决项

| 项 | 当前处理 |
| --- | --- |
| 多个 authoring component 冲突 | 第九阶段仍不做 resolver |
| `partId` 是否成为正式语义 | 暂映射到 `Object.Id`，后续角色系统再决定 |
| `Character.Id` / `Character.PartId` | 不在第九阶段新增 |
| material semantic 是否进入 RSUV | 不进入第一版 |
| `Object.Id` / `Object.GroupId` 超出 v1 compact 范围 | Inspector 不允许输入；底层 packer 拒绝异常越界值 |
| Debug Framework | 等生命周期表稳定后推进 |

## 风险点

- RSUV-preferred 模式下忘记保留 MPB fallback。
- packed zero 语义不清，导致清理后 shader 仍误读。
- 禁用 component 只清 MPB，不清 renderer user value。
- MPB-only 和 RSUV-preferred 输出不一致。
- Debug 误把 `SUBJECT` 当成 SemanticPost 消费结果；消费结果应看 `POST MASK`。
