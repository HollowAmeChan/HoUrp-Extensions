# RP 重构第七步执行计划

> 第七步目标：在第一到第六阶段已经具备 AOV / SSS 主要输入通道之后，把 `SemanticPostProcess` 从“最小 AOV 读取 probe”推进为 **语义后处理层的最小正式框架**。
>
> 本阶段迁移的是 HoPost 的“语义规则 + layer + 最小效果执行”思想，不迁移 Shoost final image stack，不迁移旧 HoPost 全量效果，不复制旧 Volume/compatibility path。

---

## 0. 前置状态

当前新包已经具备：

- `Aov.MaskId`
- `Aov.NormalDepth`
- `Aov.ObjectCustom0_3`
- `Aov.ObjectCustom4_7`
- `Aov.SurfaceData`
- `Aov.MaterialCustom0_3`
- `Aov.SssSource`
- `Sss.Source`
- `Sss.Diffusion`
- `SemanticPostProcess` feature descriptor
- `SemanticPostProcessRendererFeature` 最小 AOV read probe
- `DebugComposite` / `AOV Debug AllRegistered` 对主要资源的可视化

这些通道已经足够支持第一版语义后处理：对象 mask、object custom、material class/profile/thickness/curvature、material custom、depth/normal、SSS weight/source 都能被后处理读取。

第七步必须继续遵守：

- 不把 HoPost 和 Shoost 混成一个后处理栈。
- 不复制旧 `HoPostProcessRendererFeature.cs`。
- 不迁移 non-RenderGraph compatibility path。
- 不把 `_lilHoPost*` / `_HoPost*` 旧全局名提升为长期 ABI。
- 不让 shader 私自采样未在 RenderGraph pass 中声明的资源。
- 读 camera color 再写回 camera color 时，必须先显式 copy camera color。

---

## 1. 第七步为什么从 SemanticPost 开始

第六步之后，AOV 已经不只是 debug 输入：

```text
AOV -> SSS -> camera color
```

下一步应该证明同一套语义输入可以服务更通用的 Composite/Post 层：

```text
AOV + SSS resources + camera color copy
  -> semantic rule mask
  -> semantic layer effect
  -> camera color
```

这一步的重点不是“多做几个后处理效果”，而是冻结以下结构：

- semantic post layer 的最小数据模型。
- AOV rule 的正式输入、操作符和组合方式。
- layer mask 与 effect 执行的 RenderGraph 资源关系。
- HoPost 与 Shoost 的边界。
- Debug 如何观察 semantic mask / layer result。

---

## 2. 本阶段范围

### 做

- 将 `SemanticPostProcessRendererFeature` 从 probe 升级为正式最小 layer executor。
- 新增或整理语义后处理 layer 数据结构。
- 新增最小 AOV rule evaluator。
- 支持 1 到 4 个 layer 的固定上限，避免先引入无限 stack。
- 支持最小效果：
  - `SemanticTint`：用语义 mask 对 camera color 染色/混合。
  - `EdgeLight` 或 `OutlineLite` 二选一，优先选择实现成本低且能验证 normal/depth 边界的版本。
- 新增 semantic post 中间 mask 资源或明确声明 transient mask。
- 新增 DebugView：
  - `SemanticPost.Mask`
  - `SemanticPost.LayerResult`
- 补 registry / descriptor / shader property tests。

### 不做

- 不迁移 Shoost。
- 不做最终图像风格栈。
- 不做旧 HoPost 全量 layer UI。
- 不做 `DepthOfField`。
- 不做 `DropShadow`。
- 不做 `PostLighting`。
- 不做 `CustomMaterial`。
- 不做 temporal / bilateral / filter backend 抽象。
- 不接旧材质包。

---

## 3. HoPost 与 Shoost 边界

第七步只处理 HoPost 方向：

```text
SemanticPost / HoPost:
  读取 AOV / semantic registry
  按对象、材质、角色部件、厚度、曲率、SSS 权重等语义选择区域
  对这些区域做定向 composite
```

Shoost 继续延后：

```text
Shoost:
  最终图像风格栈
  颜色、镜头、复古、glow、blur、颗粒、CRT/VHS 等 image-space 效果
  只允许轻量 AOV composite 辅助，不定义语义规则体系
```

因此第七步不能把 Glow、IrisBlur、RGBBlur、VHS、CRT、Kuwahara 等 Shoost effect 搬进 SemanticPost。

---

## 4. 本阶段新增或固化契约

Feature：

```text
SemanticPostProcess
```

可新增资源：

```text
SemanticPost.Mask
SemanticPost.LayerResult
```

第一版也可以把这些作为 RenderGraph transient texture，不进入长期 resource registry。取舍标准：

- 如果 DebugView 需要稳定观察 `SemanticPost.Mask`，则登记为正式资源。
- 如果只作为单 pass 内部临时 mask，则保持 transient，不进入 registry。

本阶段消费资源：

```text
Aov.MaskId
Aov.NormalDepth
Aov.ObjectCustom0_3
Aov.ObjectCustom4_7
Aov.SurfaceData
Aov.MaterialCustom0_3
Aov.SssSource
Sss.Source
Sss.Diffusion
Camera.Color copy
```

本阶段消费语义：

```text
Object.MaskWeight
Object.Id
Object.GroupId
Object.Flags
Object.Custom0..7
Material.Class
Material.SssProfile
Material.Thickness
Material.Curvature
Material.Utility
Material.Custom0..3
Shading.SssWeight
Shading.SssCompositeWeight
Geometry.WorldNormal
Geometry.LinearDepth
```

新增 DebugView：

```text
SemanticPost.Mask
SemanticPost.LayerResult
```

---

## 5. AOV Rule 第一版

第一版 rule 只做可测、可解释、可映射到现有通道的子集。

Rule source：

```text
MaskWeight
ObjectId
GroupId
Flags
ObjectCustom0..7
MaterialClass
SssProfile
Thickness
Curvature
MaterialCustom0..3
SssWeight
SssCompositeWeight
LinearDepth
WorldNormalFacing
```

Operator：

```text
Always
Greater
Less
Range
EqualByte
FlagsAny
FlagsAll
```

Combine：

```text
Replace
Or
And
Subtract
Multiply
```

第一版限制：

- 每个 layer 最多 4 条 rule。
- rule 参数用明确 struct / serialized fields，不用字符串表达式。
- rule evaluation 必须集中在一个 shader include 或 C# descriptor 中，不散落在每个 effect shader。
- 所有 rule source 必须能追溯到 `HoUrpBuiltInNames.Semantics` 和具体 resource。

---

## 6. Layer 第一版

建议最小 layer：

```text
SemanticPostLayer
{
    enabled
    effect
    blendMode
    opacity
    color
    rules[4]
}
```

Effect：

```text
SemanticTint
EdgeLight 或 OutlineLite
```

BlendMode：

```text
Alpha
Add
Multiply
Screen
```

第七步不做 runtime reorder UI，但 C# 数据结构要保留顺序。

---

## 7. RenderGraph 链路

建议第一版 pass：

```text
SemanticPostProcessRendererFeature
  -> Camera Color Copy
       reads:  Camera.Color
       writes: SemanticPost.ColorCopy 或 transient copy

  -> Semantic Mask
       reads:  registered AOV / SSS resources
       writes: SemanticPost.Mask 或 transient mask

  -> Semantic Layer Composite
       reads:  color copy, semantic mask, registered AOV / SSS resources
       writes: Camera.Color
```

如果第一版为了减少 RT 数量，也可以合并 mask evaluation 与 composite：

```text
Camera Color Copy
  -> Semantic Layer Composite
       reads: color copy + AOV / SSS resources
       writes: Camera.Color
```

但只要要 debug `SemanticPost.Mask`，就必须把 mask 输出成显式资源。

硬约束：

- 不直接把 `activeColorTexture` 发布为 `_HoUrpSourceColorTexture`。
- 不让 composite pass 同时 `UseTexture(activeColorTexture)` 和 `SetRenderAttachment(activeColorTexture)`。
- 每个 shader 采样的 AOV / SSS texture 都必须在 builder 中声明 `UseTexture`，或通过明确的 resource binding pass 进入依赖。

---

## 8. Pass 时机

默认：

```text
AovOutput               AfterRenderingOpaques
SubsurfaceScattering    BeforeRenderingTransparents
Transparent / OIT       after SSS
SemanticPostProcess     AfterRenderingTransparents
DebugComposite          AfterRenderingPostProcessing 或 selected debug event
Shoost                  延后，未来 final image stack
```

第一版 `SemanticPostProcess` 放在 transparent 之后，原因：

- 它是 semantic-aware composite，不是 skin SSS 这种透明前 shading 修正。
- 它应能影响透明之后的最终画面区域。
- 它可以读取 SSS 结果作为语义输入，但不应反过来影响 SSS diffusion。

如果未来某些 semantic effect 必须透明前执行，需要拆出独立 feature 或明确 pass stage，不能把整个 SemanticPost 提前。

---

## 9. Debug 与可观察性

第七步必须让 debug 能回答：

- 当前 layer 的 mask 来自哪些 rule。
- mask 使用了哪些 resource / semantic。
- layer result 写回 camera color 前是什么。
- 某个像素为什么被 SemanticPost 影响。

第一版 DebugView：

| DebugView | Source | 显示 |
| --- | --- | --- |
| `SemanticPost.Mask` | `SemanticPost.Mask` | 当前 layer 或合并 layer mask |
| `SemanticPost.LayerResult` | `SemanticPost.LayerResult` | layer composite 后的颜色 |

如果第一版没有独立 `LayerResult` RT，则 `LayerResult` 可以延后，但 `Mask` 优先保留。

---

## 10. 旧 HoPost 迁移参照

旧实现只作为行为参照：

```text
lilToon-URP-Extensions/Runtime/HoPostProcessing
```

优先参考：

- `HoPostProcessLayer.cs`：layer、AOV rule、blend、effect 参数。
- `HoPostAovMask.hlsl`：rule source/operator/combine 思路。
- `LayerBlit.shader`：layer composite 思路。
- `EdgeLight.shader` / `Outline.shader`：第一批可迁移 effect 候选。

不直接继承：

- 旧 Volume stack 结构。
- 旧 shader property 命名。
- 旧全局纹理 ABI。
- 旧 compatibility path。
- 旧 effect enum 的全量范围。

---

## 11. 执行顺序

```text
Step 1. 写第七阶段实现边界审查
Step 2. 写 SemanticPost layer / rule 数据模型审查
Step 3. 写 SemanticPost resource 与 debug 审查
Step 4. 写旧 HoPost rule 子集迁移审查
Step 5. 扩展 built-in names / contracts / tests
Step 6. 新增或改造 SemanticPost shader include
Step 7. 改造 SemanticPostProcessRendererFeature RenderGraph 链路
Step 8. 接入 SemanticPost.Mask debug
Step 9. 新增最小效果 SemanticTint
Step 10. 评估 EdgeLight 或 OutlineLite 是否纳入本阶段
Step 11. 补验收清单和未决项登记
```

---

## 12. 自动测试建议

- registry count / link tests。
- `SemanticPostProcess` feature descriptor consumed resources tests。
- `SemanticPost.Mask` / `SemanticPost.LayerResult` resource descriptor tests。
- `SemanticPost.Mask` debug view mapping tests。
- shader property mapping tests。
- rule source -> semantic/resource mapping tests。
- rule combine deterministic tests。
- `git diff --check`。

---

## 13. 手动 Unity 验收

挂载：

1. `HoURP AOV Output`
2. `HoURP Subsurface Scattering`（可选，但建议保留）
3. `HoURP Semantic Post Process`
4. `HoURP AOV Debug`

测试对象：

```text
ObjectSemanticAuthoring:
  maskWeight = 1
  objectCustom channel = non-zero

MaterialSemanticAuthoring:
  materialClass = non-zero
  thickness / curvature / materialCustom = non-zero
  sssWeight = optional non-zero
```

期望：

| 场景 | 期望 |
| --- | --- |
| SemanticPost disabled | 画面与第六阶段一致 |
| Always rule + SemanticTint | 全屏可控 tint |
| ObjectCustom rule | 只影响指定 object custom 区域 |
| MaterialClass / Thickness rule | 只影响对应材质语义区域 |
| SSS weight rule | 可选择 SSS 参与区域 |
| Mask debug | 显示当前 rule 生成的 mask |
| 透明物体存在时 | 无 `_CameraTargetAttachment` RenderGraph 读写冲突 |
| AllRegistered | 包含 SemanticPost debug tile，标签和红框正常 |

---

## 14. 本阶段未决项预登记

| Item | 本阶段处理方式 | 后续阶段 |
| --- | --- | --- |
| 完整 HoPost layer stack UI | 不做，只保留最小 serialized layer | UI / Control Surface 阶段 |
| Volume stack | 不做完整迁移 | Runtime settings 阶段 |
| DropShadow | 不做 | Character / Composite 阶段 |
| DepthOfField | 不做 | Filter backend 阶段 |
| PostLighting | 不做 | Lighting / Composite 阶段 |
| CustomMaterial | 不做 | Plugin / custom effect ABI 阶段 |
| Shoost final stack | 不做 | Shoost 阶段 |
| 多 pass effect pipeline | 不做 | Effect pipeline 阶段 |
| Rule editor | 不做 | Editor tooling 阶段 |

---

## 15. 第七阶段完成定义

第七步完成时，必须能回答：

- `SemanticPostProcess` 与 Shoost 的边界是什么。
- SemanticPost layer 由哪些字段组成。
- AOV rule source 如何映射到 semantic/resource。
- 每个 shader 采样的资源在哪里被 RenderGraph 声明。
- 为什么 camera color 必须先 copy 再读。
- Debug 如何显示 semantic post mask。
- 哪些旧 HoPost effect 被延后，为什么延后。
