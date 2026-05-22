# RP AOV Debug AllRegistered 显示策略补记

> 本文记录第五阶段后半段对 `HoURP AOV Debug` 的交互和显示修正。它属于 Debug 系统可用性修正，不改变 `Aov.SssSource` / `Shading.SssWeight` 的资源契约。

## 1. 下拉顺序

`AllRegistered` 在 `AovDebugView` 下拉中固定放在 `None` 后面。

原因：
- `AllRegistered` 是全局排查入口，使用频率高于单项 channel。
- 以后继续增加 debug view 时，不需要每次把它从列表末尾挪回来。
- 枚举值保留为 `21`，避免已有 RendererFeature 资产因为 enum 序列化值变化而错位。

## 2. 去重规则

`AllRegistered` 平铺时按以下 key 去重：

```text
SourceResource + SourceSemantic
```

这样 registry 可以继续保留 `SSS.ProfileId / SSS.Thickness / SSS.Curvature` 这类 SSS 视角 alias，但不会在 AllRegistered 中重复显示和 `AOV.SssProfile / AOV.Thickness / AOV.Curvature` 等价的 tile。

单项下拉也不暴露这些重复 alias，只保留已有 AOV 单项和新增的：

```text
SSS Source
SSS Weight
```

## 3. Tile 边界

AllRegistered 每个 tile 绘制红色边框。

原因：
- 黑色 debug 内容常见，例如空 mask、空 source、无覆盖区域。
- 只有黑背景时无法判断到底有几个 tile。
- 红色边框表示“框内是一个 debug view”，避免把黑色内容误判为空画面或少 tile。

## 4. Tile 间距

移除原先的 tile padding，不再保留黑色间距。

原因：
- 红边框已经承担 tile 边界识别。
- 继续保留黑边会浪费有效显示面积。
- 小视口或 debug view 数量增加时，tile 内容会更难观察。

## 5. Tile 标签

AllRegistered 每个 tile 在左上角绘制短标签，例如：

```text
AOV MASK
OBJECT ID
DEPTH
NORMAL
OBJ C0
MAT C0
SSS SRC
SSS WGT
```

实现策略：
- 不引入 Unity Font / TMP / 字体资产。
- `AovDebug.shader` 内置极小 5x7 位图字体。
- C# 端传入 `_HoUrpAovDebugTileGrid = (columns, rows, tileCount, 0)`。
- shader 根据网格密度自适应放大标签占 tile 的比例。

## 6. 字号自适应

字号不是固定像素，而是按 tile grid 密度调整：

```text
density = max(columns, rows)
```

网格越密，标签在单个 tile 内占比越大。这样 tile 数量变多时，文字不会因为 tile 变小而完全不可读。

当前策略是保守放大：优先保证标签可读，但限制标签背景高度，避免过度遮挡主要 debug 内容。

## 7. Shader 编译注意

`AovDebug.shader` 有两个独立 `HLSLPROGRAM`：

```text
Pass "AovDebug"
Pass "AovDebugTile"
```

tile 标签和 `_HoUrpAovDebugTileGrid` 只用于 `AovDebugTile`，但相关变量必须在第二个 `HLSLPROGRAM` 内单独声明。不能因为第一个 pass 已声明就认为第二个 pass 可见。

普通 `AovDebug` pass 的 `ResolveDebugColor` 已改成先初始化 `resolvedColor` 再统一返回，避免 d3d11 编译器报潜在未初始化 warning。

## 8. 不改变的内容

本补记不改变：
- `DebugViewRegistry` 的正式注册项。
- `Aov.SssSource` 编码。
- `SemanticPostProcess` 对 `Aov.SssSource` 的消费。
- 后续第六阶段 SSS pass 的资源契约。
