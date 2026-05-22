# RP SssSource 资源与编码审查

## 资源

| Resource | Domain | Format | Scale | Clear |
| --- | --- | --- | --- | --- |
| `Aov.SssSource` | Shading | `R16G16B16A16_SFloat` first version | Full | transparent black |

## 语义映射

| Semantic | Resource | Channel |
| --- | --- | --- |
| `Shading.SssSourceColor` | `Aov.SssSource` | RGB |
| `Shading.SssWeight` | `Aov.SssSource` | A |

## 空值与清理契约

```text
Aov.SssSource clear = (0, 0, 0, 0)
```

zero 表示不参与 SSS。背景、天空、未覆盖区域不参与 SSS。

## 第一版编码

```text
RGB = source color
A   = normalized SSS weight
```

第一版不承诺旧 HoSSS source 颜色完全一致，只承诺输入资源可写、可调试、可显式消费。

## Shader binding

逻辑资源名：

```text
Aov.SssSource
```

shader binding 名：

```text
_HoUrpAovSssSourceTexture
_HoUrpSssSourceColor
_HoUrpSssWeight
```

旧名 `_lilHoAovSssTexture` 只作为迁移参照。

