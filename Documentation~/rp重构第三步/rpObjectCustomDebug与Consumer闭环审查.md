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

`AOV.LinearDepth` debug 对天空/未覆盖区域按 far depth 显示为白。`AOV.WorldNormal` debug 对 `Aov.NormalDepth.rgb == (0, 0, 0)` 按 invalid normal 处理，输出黑。天空球和未绘制区域没有几何法线，不能用中性法线或任何默认颜色伪装成有效 AOV。

`AllRegistered` 第一版用于同时验收所有已注册 AOV view：

- 单个 `HoURP AOV Debug All` raster pass 内绘制所有 tile。
- 每个 tile 显式声明 source texture read，并在执行时绑定 `_HoUrpAovDebugSourceTexture`。
- tile 保持目标画面比例，避免把 normal/depth debug 拉伸成错误比例。

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
