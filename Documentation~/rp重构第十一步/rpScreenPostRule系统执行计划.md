# RP 第十一阶段 ScreenPost Rule 系统执行计划

## 目标

把当前 `HoURP ScreenPost Prototype` 的可见性验证，推进成正式 ScreenPost rule 系统。

当前 prototype 已经证明：

- RenderGraph 中有 ScreenPost mask / composite pass。
- ScreenPost 能读取 AOV 输入。
- 屏幕上能看到 tint 结果。

接下来要移除“靠 preview fallback 证明可见”的临时状态，改为由 rule 命中区域驱动 layer blend。

## 底线

- rule language 属于 ScreenPost，不属于 ImagePost。
- rule 输入必须显式声明，并能追踪到 `PostResourceRequest`。
- shader 不能偷读旧 `_lilHoPost*` / `_lilHoAov*`。
- 每个 effect 不能各自实现一套 rule parser。
- debug 必须能看到 rule mask、rule 输入、layer influence。

## 数据模型

```text
ScreenPostRuleSet
  Enabled
  Rules[]
  DefaultValue
  Invert
  DebugName

ScreenPostRule
  Enabled
  Source
  Operator
  Combine
  Value
  Range
  Weight

ScreenPostLayer
  Enabled
  Name
  RuleSet
  BlendMode
  Opacity
  Color
  EffectId
```

第一版限制：

- 每个 layer 最多 4 条 rule。
- 每个 stack 最多 8 个 layer。
- 不支持嵌套 rule group。
- 不支持任意表达式。
- 不支持每 effect 自定义 rule。

## Rule Source

| Source | 资源 | 语义 |
| --- | --- | --- |
| `Always` | 无 | 无 |
| `MaskWeight` | `Aov.MaskId` | `Object.MaskWeight` |
| `ObjectId` | `Aov.MaskId` | `Object.Id` |
| `GroupId` | `Aov.MaskId` | `Object.GroupId` |
| `Flags` | `Aov.MaskId` | `Object.Flags` |
| `ObjectCustom0_7` | `Aov.ObjectCustom0_3` / `Aov.ObjectCustom4_7` | `Object.Custom*` |
| `MaterialClass` | `Aov.SurfaceData` | `Material.Class` |
| `Thickness` | `Aov.SurfaceData` | `Material.Thickness` |
| `Curvature` | `Aov.SurfaceData` | `Material.Curvature` |
| `LinearDepth` | `Aov.NormalDepth` | `Geometry.LinearDepth` |
| `WorldNormalFacing` | `Aov.NormalDepth` | `Geometry.WorldNormal` |

## Operator

```text
Always
Greater
Less
Range
EqualByte
FlagsAny
FlagsAll
```

## Combine

```text
Replace
Or
And
Subtract
Multiply
```

## Resource Request 规则

Planner 必须从 enabled rule 推导输入：

```text
MaskWeight/ObjectId/GroupId/Flags:
  request Aov.MaskId

ObjectCustom0_3:
  request Aov.ObjectCustom0_3

ObjectCustom4_7:
  request Aov.ObjectCustom4_7

MaterialClass/Thickness/Curvature:
  request Aov.SurfaceData

LinearDepth/WorldNormalFacing:
  request Aov.NormalDepth
```

没有 enabled rule 的 layer 不请求 AOV。

## Shader 规划

建议从 prototype shader 抽出公共 HLSL：

```text
Runtime/Shaders/ShaderLibrary/HoUrpScreenPostRules.hlsl
Runtime/Shaders/Hidden/HoURP/ScreenPost/RuleMask.shader
Runtime/Shaders/Hidden/HoURP/ScreenPost/LayerComposite.shader
```

职责：

- `HoUrpScreenPostRules.hlsl`：统一 decode AOV 与 evaluate rule。
- `RuleMask.shader`：输出 layer mask / influence。
- `LayerComposite.shader`：读取 source + mask 做 blend。

## 执行步骤

1. 新增 `ScreenPostRuleSource` / `ScreenPostRuleOperator` / `ScreenPostRuleCombine`。
2. 新增 `ScreenPostRule` / `ScreenPostRuleSet` / `ScreenPostLayerSettings`。
3. 从 enabled rules 生成 `PostResourceRequest`。
4. 把 rule 数据 pack 成 shader vector array。
5. 抽出公共 HLSL rule evaluator。
6. 改造 `ScreenPostPrototypeRendererFeature`，用 rule set 替换 preview fallback。
7. 增加 `ScreenPost.RuleMask` debug 入口。
8. 补单元测试与手动验收。

## 验收

- `MaskWeight > 0` 只影响 AOV 命中对象。
- `ObjectId EqualByte` 能命中特定对象。
- `FlagsAny` 能命中特定 capability 标记。
- 禁用 layer 后 rule mask pass 和 AOV request 消失。
- RDG 中每个 semantic texture 都有 `UseTexture`。
- 关闭 preview fallback 后，天空不再因为空 AOV 被 tint。

