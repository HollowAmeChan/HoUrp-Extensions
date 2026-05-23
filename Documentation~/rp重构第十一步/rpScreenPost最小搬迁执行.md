# RP 第十一阶段 ScreenPost 最小搬迁执行

## 目标

把旧 HoPost 的核心价值迁移成新 RP 的最小语义后处理路径：

```text
AOV / Semantic input
  -> ScreenPost rule mask
  -> ScreenPost layer blend
  -> ImageChain or explicit output
```

本阶段不追求旧 HoPost 视觉等价，只验证：

- rule 输入显式声明。
- mask 生成可 debug。
- layer blend 可执行。
- 关闭 layer 后资源 request 消失。

当前状态（2026-05-23）：

- `HoURP ScreenPost Prototype` 已经能在 RenderGraph 中生成 SourceCopy / RuleMask / Composite，并实际影响屏幕。
- 当前 `Preview When AOV Mask Is Empty` 只是可见性验证 fallback，不是正式 rule 语义。
- 后续仍在第十一阶段内继续实现正式 rule 系统。

## 旧实现参考

旧代码定位：

```text
lilToon-URP-Extensions/Runtime/HoPostProcessing
  HoPostProcessRendererFeature.cs
  HoPostProcessLayer.cs
  HoPostProcessEffect.cs
  HoPostProcessEffectRegistry.cs
  HoPostProcessShaderConstants.cs
  Shaders/HoPost/HoPostAovMask.hlsl
  Shaders/HoPost/LayerBlit.shader
  Shaders/HoPost/SubjectMask.shader
  Shaders/HoPost/EdgeLight.shader
  Shaders/HoPost/Outline.shader
  Shaders/HoPost/DropShadow.shader
  Shaders/HoPost/DepthOfField.shader
  Shaders/HoPost/PostLighting.shader
```

旧实现只能回答行为问题：

- 哪些输入被使用。
- rule 大概有哪些 operator。
- layer blend 如何组织。
- 哪些 effect 适合后续迁移。

旧实现不能成为：

- 新 shader property ABI。
- 新全局纹理名。
- 新 effect enum ABI。
- 新资源生命周期。

## 第一版 ScreenPost 数据模型

### `ScreenPostRuleSet`

```text
ScreenPostRuleSet
  Rules
  DefaultValue
  CombineMode
  Invert
```

### `ScreenPostRule`

```text
ScreenPostRule
  SourceSemantic
  Operator
  CompareValue
  Threshold
  Weight
```

第一版支持语义：

| SourceSemantic | 输入资源 |
| --- | --- |
| `Object.Id` | `Aov.MaskId` |
| `Object.GroupId` | `Aov.MaskId` |
| `Object.Flags` | `Aov.MaskId` |
| `Object.Custom0_7` | `Aov.ObjectCustom*` 或当前等价资源 |
| `Material.Class` | `Aov.SurfaceData` |
| `Material.Thickness` | `Aov.SurfaceData` |
| `Material.Curvature` | `Aov.SurfaceData` |

第一版支持 operator：

```text
Equal
NotEqual
Greater
Less
BitAny
BitAll
```

第一版支持 combine：

```text
Replace
Or
And
Multiply
```

不支持：

- 复杂表达式。
- 嵌套规则组。
- 每 effect 自定义 rule 解析。
- shader 中散落各自 rule 代码。

## 下一步：正式 Rule 系统

当前 prototype 应改造成：

```text
ScreenPostLayerSettings[]
  -> ScreenPostRuleSet
  -> PostResourceRequest
  -> RuleMask pass
  -> LayerComposite pass
```

执行要求：

- layer 列表顺序决定 composite 顺序。
- 每个 enabled rule 推导所需 AOV resource。
- 无 enabled rule 的 layer 不请求 AOV。
- preview fallback 只保留为 debug 开关，默认正式路径不依赖它。
- rule evaluator 抽到公共 HLSL。

## Shader / HLSL 入口

建议新增：

```text
Runtime/Shaders/Post/HoUrpPostAovMask.hlsl
Runtime/Shaders/Post/HoUrpPostRuleMask.shader
Runtime/Shaders/Post/HoUrpPostLayerBlit.shader
```

HLSL 职责：

```text
HoUrpPostAovMask.hlsl:
  decode registered AOV inputs
  evaluate limited ScreenPostRule
  combine rule result

HoUrpPostRuleMask.shader:
  write mask / influence texture or direct layer influence

HoUrpPostLayerBlit.shader:
  blend layer result into current image
```

命名规则：

- 使用 `_HoUrp*`。
- 不使用 `_lilHoPost*`。
- 不使用 `_lilHoAov*`。
- 不暴露旧 HoPost property 名作为长期 ABI。

## 执行路径

### 最小路径 A：direct semantic layer

如果第一版不想创建独立 mask texture，可直接在 layer pass 中 evaluate rule。

```text
ImageChain.Current
  + AOV inputs
  -> ScreenPostLayerBlit
  -> ImageChain.Next
```

优点：

- 资源少。
- 快速验证 input declaration。

限制：

- rule mask debug 需要 shader debug path 或额外 pass。

### 最小路径 B：rule mask + layer blend

推荐最终最小闭环：

```text
AOV inputs
  -> ScreenPost.RuleMask

ImageChain.Current + ScreenPost.RuleMask
  -> ScreenPost.LayerBlit
  -> ImageChain.Next
```

优点：

- mask 可 debug。
- layer blend 与 rule evaluation 解耦。
- 后续多个 layer 可复用 rule mask 策略。

第十一阶段可以先实现 A，再补 B；但验收文档必须说明当前选择。

## Resource request

ScreenPost layer 应生成：

```text
PostResourceRequest
  ResourceKind: SemanticInput
  ReadSemantics:
    - Aov.MaskId
    - Aov.SurfaceData
    - Aov.NormalDepth optional
```

如果使用独立 mask：

```text
PostResourceRequest
  ResourceKind: OutputAlias
  WriteSemantic: ScreenPost.RuleMask
  FormatPolicy: MaskR8 or MaskR16
  Lifetime: Frame
```

## 实施步骤

### Step 1. 旧 HoPost 输入表

输出文档或注释表：

| 旧 effect | 旧输入 | 新输入分类 | 第十一阶段状态 |
| --- | --- | --- | --- |
| EdgeLight | AOV normal/depth/mask | SemanticImagePass | Prototype candidate |
| Outline | AOV normal/depth/mask | SemanticImagePass | Planned |
| DropShadow | AOV mask/depth | SemanticImagePass | Planned |
| DepthOfField | depth / subject | SemanticImagePass | Planned |
| PostLighting | material/object semantic | SemanticImagePass | Planned |
| CustomMaterial | arbitrary | Removed / Later |

### Step 2. 定义 ScreenPost prototype effect

建议：

```text
EffectId: ScreenPost.LayerBlitPrototype
ExecutionKind: SemanticImagePass
RequiredInputs:
  PrimaryImage
  Aov.MaskId
ProducedOutputs:
  ImageChain.Next
Status: Prototype
```

可选：

```text
EffectId: ScreenPost.RuleMaskPrototype
ProducedOutputs:
  ScreenPost.RuleMask
```

### Step 3. 实现 rule 数据绑定

第一版可以用固定大小数组：

```text
_HoUrpPostRuleCount
_HoUrpPostRuleSource[MaxRules]
_HoUrpPostRuleOperator[MaxRules]
_HoUrpPostRuleValue[MaxRules]
_HoUrpPostRuleThreshold[MaxRules]
```

要求：

- 名称是新 ABI。
- MaxRules 有明确限制，例如 8。
- 超出限制时 planner diagnostic。

### Step 4. 实现最小 shader

最小 shader 只需：

- sample current image。
- evaluate rule mask。
- apply tint / intensity / blend。
- output next image。

不做：

- edge light 完整视觉。
- outline 完整采样。
- drop shadow blur。

### Step 5. Debug

Debug view：

```text
ScreenPost.RuleMask
ScreenPost.LayerInfluence
ScreenPost.ActiveRules
```

关闭 layer 后：

- Debug 显示 inactive。
- 不显示上一帧 mask。

## 自动测试

建议：

```text
Tests/Runtime/HoUrpScreenPostRuleTests.cs
Tests/Runtime/HoUrpScreenPostPlannerTests.cs
```

测试项：

```csharp
ScreenPostRuleDeclaresAovMaskIdInput()
ScreenPostRuleDeclaresSurfaceDataInputForMaterialClass()
DisabledScreenPostLayerRemovesSemanticInputs()
ScreenPostRuleLimitProducesDiagnostic()
ScreenPostPrototypeDoesNotUseLegacyPropertyNames()
ScreenPostRuleMaskDebugInactiveWhenLayerDisabled()
ImagePostEffectCannotUseScreenPostRuleSet()
```

## 手动验收

场景：

1. 挂载 AOV Output。
2. 挂载 SSS / SemanticPost 可选。
3. 挂载 ScreenPost prototype feature。
4. 创建两个 object group / material class 不同的对象。
5. ScreenPost layer rule 只命中其中一个。

检查：

- 命中对象产生 layer tint / influence。
- 未命中对象不受影响。
- Debug 能显示 rule mask。
- 关闭 layer 后 rule mask inactive。
- RenderGraph 中能看到 AOV inputs 被声明。
- 没有旧 `_lilHoPost*` / `_lilHoAov*`。

## 成功标准

- ScreenPost 最小 layer 可以显式消费 AOV。
- rule mask / influence 可 debug。
- 关闭 layer 后 request 消失。
- rule 数据不散落到各 effect shader。
- ScreenPost 与 ImagePost 边界清楚。

## 风险点

- 一开始就迁移完整 edge/outline/drop shadow，导致基础 rule 不稳定。
- 直接复用旧 `HoPostAovMask.hlsl` 并带入旧全局名。
- rule limit 不清楚，后续 shader constant 膨胀。
- ScreenPost 输出绕过 ImageChain，造成后续 ImagePost source 混乱。
- Debug view 读取旧 mask。
