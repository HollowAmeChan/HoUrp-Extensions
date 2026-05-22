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
| A | `Geometry.LinearDepth` | normalized depth `positionCS.z / positionCS.w` remapped through `UNITY_REVERSED_Z` |

背景 clear 为 `(0.5,0.5,1,1)`。

## Debug Decode

Debug shader 必须与本文件一致：

- `AOV / Mask`：显示 `MaskId.r`。
- `AOV / Object ID`：显示 `MaskId.g`，第一版是常量。
- `AOV / Linear Depth`：显示 `NormalDepth.a`。
- `AOV / World Normal`：显示 `NormalDepth.rgb`，第一版直接显示 encoded normal。

## 未决

线性眼空间深度在本阶段暂不强制。当前先写 normalized depth，后续扩展 SurfaceData / SSS 前复核是否改为 linear eye depth，并同步 Debug decode。
