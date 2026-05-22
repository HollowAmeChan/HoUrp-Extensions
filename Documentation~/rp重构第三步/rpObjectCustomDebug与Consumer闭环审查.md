# RP ObjectCustom Debug 与 Consumer 闭环审查

## DebugView

新增 8 个 debug view：

```text
AOV.ObjectCustom0
AOV.ObjectCustom1
AOV.ObjectCustom2
AOV.ObjectCustom3
AOV.ObjectCustom4
AOV.ObjectCustom5
AOV.ObjectCustom6
AOV.ObjectCustom7
```

第一版显示模式：

```text
Replace
```

显示结果为对应 channel 的灰度图。

## Debug shader

`Hidden/HoURP/Debug/AovDebug` 扩展 mode：

```text
4..11 = ObjectCustom0..7
```

shader 只采样 `_HoUrpAovObjectCustom0_3Texture` / `_HoUrpAovObjectCustom4_7Texture`，不读旧全局名。

## Consumer

`SemanticPostProcessRendererFeature` 增加最小 ObjectCustom mask tint：

- 显式读取 `Aov.ObjectCustom0_3` 与 `Aov.ObjectCustom4_7`。
- 默认读取 `Object.Custom0`。
- 和已有 mask/depth tint 相乘或取 max，用于证明 consumer 可以读取新资源。

## 不做项

- 不做 HoPost rule language。
- 不做多规则组合。
- 不做 UI panel。
- 不做 overlay / HUD。

