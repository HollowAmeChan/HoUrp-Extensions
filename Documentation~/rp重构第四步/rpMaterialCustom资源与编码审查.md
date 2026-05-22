# RP MaterialCustom 资源与编码审查

## 资源

| Resource | Domain | Format | Scale | Clear |
| --- | --- | --- | --- | --- |
| `Aov.MaterialCustom0_3` | Material | `R16G16B16A16_SFloat` first version | Full | zero |

## 语义映射

| Semantic | Resource | Channel |
| --- | --- | --- |
| `Material.Custom0` | `Aov.MaterialCustom0_3` | R |
| `Material.Custom1` | `Aov.MaterialCustom0_3` | G |
| `Material.Custom2` | `Aov.MaterialCustom0_3` | B |
| `Material.Custom3` | `Aov.MaterialCustom0_3` | A |

## 第一版编码

MaterialCustom channel 第一版写 normalized float 常量：

```text
0 = 默认无自定义材质语义
1 = 最大权重或启用
```

允许 0..1 中间值作为权重。第四阶段不接 texture-driven custom，也不接旧材质 shader 内的复杂计算。

## 空值与清理契约

```text
Aov.MaterialCustom0_3 clear = (0, 0, 0, 0)
```

zero 表示未写入材质自定义语义，不表示任何默认材质类别。

## Shader binding

逻辑资源名：

```text
Aov.MaterialCustom0_3
```

shader binding 名：

```text
_HoUrpAovMaterialCustom0_3Texture
_HoUrpMaterialCustom0_3
```

shader binding 是 backend detail，不是 Resource Registry 主键。

## 旧实现对照

旧全局名：

```text
_lilHoAovCustom0_3Texture
```

旧材质/MPB 名：

```text
_HoAovCustomValues0
_HoAovCustom0Tex
_HoAovCustom1Tex
_HoAovCustom2Tex
_HoAovCustom3Tex
```

第四阶段只迁移常量通道概念；贴图驱动 custom 留给新材质 producer 阶段。

