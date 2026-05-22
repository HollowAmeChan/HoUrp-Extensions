# RP SemanticPost Layer 与 Rule 模型审查

## 目标

建立第一版语义后处理数据模型，让 HoPost 的规则思想进入新 RP 契约，而不是继续停留在 shader probe。

## Layer 第一版

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

## Effect 子集

```text
SemanticTint
EdgeLight 或 OutlineLite
```

第七阶段至少完成 `SemanticTint`。

## Rule Source

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

## 约束

- rule 不能用字符串表达式。
- rule source 必须能映射到 `HoUrpBuiltInNames.Semantics`。
- rule 采样资源必须能映射到 `HoUrpBuiltInNames.Resources`。
- rule evaluation 逻辑应集中在一个 shader include 或统一函数中。
- effect shader 不应各自重新定义一套 AOV 规则语言。
