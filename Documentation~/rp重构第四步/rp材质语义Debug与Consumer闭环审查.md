# RP 材质语义 Debug 与 Consumer 闭环审查

## DebugView

新增 DebugView：

```text
AOV.MaterialClass
AOV.SssProfile
AOV.Thickness
AOV.Curvature
AOV.MaterialCustom0
AOV.MaterialCustom1
AOV.MaterialCustom2
AOV.MaterialCustom3
```

`Material.Utility` 如果本阶段不生产，不新增可选中的有效 DebugView；可以登记为 unavailable/status view，但不能显示不存在的 source channel。

## Debug shader

`Hidden/HoURP/Debug/AovDebug` 扩展 mode：

```text
MaterialClass
SssProfile
Thickness
Curvature
MaterialCustom0
MaterialCustom1
MaterialCustom2
MaterialCustom3
```

显示模式第一版仍为：

```text
Replace
```

显示结果为对应 channel 灰度图。ID/profile 类通道可先按 normalized grayscale 显示，后续 Debug Framework 再加 palette / label。

## AllRegistered

`AllRegistered` 必须自动枚举新增且 source resource 存在的材质语义 DebugView。

如果 `Material.Utility` 只登记未生产，`AllRegistered` 不应把它作为有效 tile 输出，除非 tile 明确显示 unavailable/status。

## Consumer

`SemanticPostProcessRendererFeature` 增加最小材质语义读取：

- 显式读取 `Aov.SurfaceData`。
- 显式读取 `Aov.MaterialCustom0_3`。
- 默认使用 `Material.Thickness` 或 `Material.Custom0` 作为额外 tint 权重。
- 与现有 MaskId / ObjectCustom tint 组合时，组合规则必须简单可读，例如 `max` 或乘法，不能引入 HoPost rule language。

## 不做项

- 不做 HoPost rule source/operator/combine。
- 不做多 layer。
- 不做 Volume stack。
- 不做 CustomMaterial effect。
- 不做 Debug overlay / HUD。
- 不做 SSS composite。

## 成功标准

- `AOV.Thickness` DebugView 能显示非零 authoring 值。
- `AOV.MaterialCustom0` DebugView 能显示非零 authoring 值。
- SemanticPost 的画面变化来自显式读取 `Aov.SurfaceData` / `Aov.MaterialCustom0_3`。
- 禁止通过旧 `_lilHoAovCustom0_3Texture` 或 `_lilHoAovSurfaceDataTexture` 读取。

