# RP SurfaceData 资源与编码审查

## 资源

| Resource | Domain | Format | Scale | Clear |
| --- | --- | --- | --- | --- |
| `Aov.SurfaceData` | Material | `R16G16B16A16_SFloat` first version | Full | zero |

## 语义映射

第一版推荐编码：

| Semantic | Resource | Channel |
| --- | --- | --- |
| `Material.Class` | `Aov.SurfaceData` | R |
| `Material.SssProfile` | `Aov.SurfaceData` | G |
| `Material.Thickness` | `Aov.SurfaceData` | B |
| `Material.Curvature` | `Aov.SurfaceData` | A |

`Material.Utility` 先注册但不进入 `Aov.SurfaceData` 第一版编码。它不能临时挤占 `Curvature` 通道，也不能把 `Aov.MaterialCustom0_3` 的某个通道偷换成长期 utility 位置。

## 空值与清理契约

```text
Aov.SurfaceData clear = (0, 0, 0, 0)
```

zero 表示 no material semantic / no geometry / undefined，不表示合法 profile 或合法材质分类。

清理入口继续放在资源声明层：`HoUrpRenderGraphTextureDescFactory` 根据 `ResourceClearPolicy` 设置 `TextureDesc.clearBuffer` / `TextureDesc.clearColor`。AOV Output pass 不新增手写 clear pass。

## 第一版编码约定

```text
Material.Class      = normalized or integer-like float, first version 0..255 mapped by authoring
Material.SssProfile = normalized or integer-like float, first version 0..7 meaningful for future SSS
Material.Thickness  = normalized float 0..1
Material.Curvature  = normalized/signed authoring value, first version written directly
```

第四阶段不承诺旧 HoSSS 对这些值的最终解释，只承诺资源可写、可调试、可显式消费。

## Shader binding

逻辑资源名：

```text
Aov.SurfaceData
```

shader binding 名：

```text
_HoUrpAovSurfaceDataTexture
_HoUrpMaterialClass
_HoUrpMaterialSssProfile
_HoUrpMaterialThickness
_HoUrpMaterialCurvature
_HoUrpMaterialUtility
```

`_HoUrpMaterialUtility` 可以作为 authoring/property binding 预留；若本阶段不生产 `Material.Utility`，DebugView 与 Resource mapping 不应声称它已经有稳定 source channel。

## 旧实现对照

旧全局名：

```text
_lilHoAovSurfaceDataTexture
```

旧材质/MPB 名：

```text
_HoAovMaterialClass
_HoSSSProfileId
_HoAovThickness
_HoAovCurvature
_HoAovUtility
```

这些旧名只作为迁移参照，不作为新 RP 逻辑名。

