# RP SSS 输入 Debug 与 Consumer 闭环审查

## DebugView

新增 DebugView：

```text
AOV.SssSource
AOV.SssWeight
SSS.ProfileId
SSS.Thickness
SSS.Curvature
```

`AOV.SssSource` 显示 RGB。

`AOV.SssWeight` 显示 A 通道灰度。

`SSS.ProfileId / Thickness / Curvature` 可以复用 `Aov.SurfaceData`，作为 SSS 视角的 DebugView alias。

## Consumer

最小 consumer 推荐继续扩展 `SemanticPostProcessRendererFeature`：

- 显式读取 `Aov.SssSource`。
- 使用 `SssWeight` 或 `SssSourceColor` 做额外 tint / probe。
- 不做 blur。
- 不做 composite。

## 不做项

- 不做 HoSSS source/diffusion/transmission/composite stack。
- 不做 profile kernel。
- 不做 Volume stack。
- 不做 Debug overlay / HUD。

## 成功标准

- `AOV.SssSource` DebugView 能显示 source color。
- `AOV.SssWeight` DebugView 能显示参与权重。
- Consumer 的画面变化来自显式读取 `Aov.SssSource`。
- 禁止通过旧 `_lilHoAovSssTexture` 读取。

