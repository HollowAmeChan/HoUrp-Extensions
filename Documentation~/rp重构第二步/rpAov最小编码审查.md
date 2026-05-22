# RP AOV 最小编码审查

## 目标

冻结第二阶段 `Aov.MaskId` / `Aov.NormalDepth` 的最小编码，避免实现时临时决定。

## `Aov.MaskId`

| Channel | Semantic | 本阶段写入 |
| --- | --- | --- |
| R | `Object.MaskWeight` / coverage | `1` |
| G | `Object.Id` | 常量 `1 / 255` |
| B | `Object.GroupId` | 常量 `0` |
| A | `Object.Flags` | 常量 `0` |

背景 clear 为 `(0,0,0,0)`。

## `Aov.NormalDepth`

| Channel | Semantic | 本阶段写入 |
| --- | --- | --- |
| RGB | `Geometry.WorldNormal` | world normal encoded to `normal * 0.5 + 0.5` |
| A | `Geometry.LinearDepth` | `Linear01Depth(positionCS.z / positionCS.w, _ZBufferParams)` |

背景 clear 为 `(0,0,0,1)`，表示 no geometry / sky / undefined：

- RGB = `(0,0,0)` 表示 invalid normal，不表示合法世界法线。
- A = `1` 表示 far depth / infinite background。

`NormalDepth.a` 的有效几何范围是 near -> 0、far -> 1。它不是 no geometry 标记；没有几何覆盖时，深度按无限远处理为 1，法线按 invalid normal 处理为 zero。

## Debug Decode

Debug shader 必须与本文件一致：

- `AOV / Mask`：显示 `MaskId.r`。
- `AOV / Object ID`：对 `MaskId.g` 的 8-bit ID 做稳定 hash 伪随机色显示；资源编码不变。
- `AOV / Linear Depth`：显示 `sqrt(NormalDepth.a)` 用于 Debug 可视化；背景/天空显示为白。
- `AOV / World Normal`：`NormalDepth.rgb == (0,0,0)` 时显示 invalid normal / 空值黑，否则显示 `NormalDepth.rgb`，第一版直接显示 encoded normal。

## 未决

当前写入的是 linear 0..1 depth，不是线性眼空间距离。后续如果 SSS / Character / depth-aware post 需要真实 eye depth，应新增明确编码或 `Aov.Depth`，并同步 Debug decode。
