# RP ObjectCustom 资源与编码审查

## 资源

| Resource | Domain | Format | Scale | Clear |
| --- | --- | --- | --- | --- |
| `Aov.ObjectCustom0_3` | Object | RGBA8 UNorm | Full | zero |
| `Aov.ObjectCustom4_7` | Object | RGBA8 UNorm | Full | zero |

## 语义映射

| Semantic | Resource | Channel |
| --- | --- | --- |
| `Object.Custom0` | `Aov.ObjectCustom0_3` | R |
| `Object.Custom1` | `Aov.ObjectCustom0_3` | G |
| `Object.Custom2` | `Aov.ObjectCustom0_3` | B |
| `Object.Custom3` | `Aov.ObjectCustom0_3` | A |
| `Object.Custom4` | `Aov.ObjectCustom4_7` | R |
| `Object.Custom5` | `Aov.ObjectCustom4_7` | G |
| `Object.Custom6` | `Aov.ObjectCustom4_7` | B |
| `Object.Custom7` | `Aov.ObjectCustom4_7` | A |

## 第一版编码

ObjectCustom channel 第一版写 0 或 1：

```text
0 = 不属于该对象语义区域
1 = 属于该对象语义区域
```

后续如需要软 mask 或权重，可以扩展为 normalized float，但第三步只承诺 binary participation。

## Shader binding

逻辑资源名：

```text
Aov.ObjectCustom0_3
Aov.ObjectCustom4_7
```

shader binding 名：

```text
_HoUrpAovObjectCustom0_3Texture
_HoUrpAovObjectCustom4_7Texture
_HoUrpObjectCustomMask
```

shader binding 是 backend detail，不是 Resource Registry 主键。

## 旧实现对照

旧全局名：

```text
_lilHoAovObjectCustom0_3Texture
_lilHoAovObjectCustom4_7Texture
_HoAovObjectCustomMask
```

旧角色区域含义只作为迁移参照，不作为新长期 ABI。

