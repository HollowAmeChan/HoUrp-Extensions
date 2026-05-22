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
