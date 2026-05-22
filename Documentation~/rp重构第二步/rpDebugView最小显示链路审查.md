# RP DebugView 最小显示链路审查

## 目标

让已注册 DebugView 能替换画面输出，而不是由某个 Feature 私有 debug mode 临时显示。

## 最小链路

```text
DebugViewRegistry
  -> AovDebugRendererFeature.selectedDebugView
  -> DebugViewDefinition.SourceResource
  -> HoUrpRenderGraphResources.TryGetTexture
  -> AovDebug shader
  -> active camera color replace
```

## 本阶段 DebugView

| Display | Registry ID | Source Resource | Shader mode |
| --- | --- | --- | --- |
| `AOV / Mask` | `AOV.Mask` | `Aov.MaskId` | 0 |
| `AOV / Object ID` | `AOV.ObjectId` | `Aov.MaskId` | 1 |
| `AOV / Linear Depth` | `AOV.LinearDepth` | `Aov.NormalDepth` | 2 |
| `AOV / World Normal` | `AOV.WorldNormal` | `Aov.NormalDepth` | 3 |

## 设置位置

第一版 debug selection 放在 `AovDebugRendererFeature` serialized field。Volume、Debug Panel、HUD、overlay、capture 延后。

## 无源资源策略

如果 `HoUrpRenderGraphResources` 里找不到 source resource，pass 不执行。避免 debug pass 自己创建临时占位资源，防止掩盖 producer 缺失问题。

## 输出模式

第一版只做 Replace。Overlay / split / capture 是后续 Debug Framework 范围。

`AOV / Linear Depth` 的资源值是 linear 0..1 depth。Debug 显示允许做 `sqrt(depth)` 可视化拉伸，避免常见场景在远裁剪很大时几乎全黑；这不改变资源编码。天空/未覆盖区域 clear 为 depth 1，所以显示为白。

ID 类 DebugView 允许做稳定 hash 伪随机色显示，优先覆盖 `AOV / Object ID`、后续材质类 ID / profile ID 等同类视图。随机色只影响 Debug 显示，不改变 AOV 编码。
