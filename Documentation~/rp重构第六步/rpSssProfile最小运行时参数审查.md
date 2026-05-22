# RP SSS Profile 最小运行时参数审查

第六阶段不做完整 Diffusion Profile asset，只做最小运行时参数。

## 第一版 settings

```text
enabled
renderInSceneView
strength
radius
depthTolerance
normalTolerance
sourcePreserve
debugMode
```

## 第一版 profile 策略

- `Material.SssProfile` 保留为输入和 debug 语义。
- 第六阶段可以先使用默认 profile 参数。
- 不引入旧 8 槽 profile array 作为长期 ABI。
- 不读取旧 `_HoSSSProfileId` 或 `_lilHoSSSProfile*` 作为运行时契约。

## 后续

后续 profile registry 阶段再决定：

- profile asset 还是 renderer feature local list。
- profile id lookup 的稳定规则。
- diffusion radius / color / thickness remap 的正式编码。
- 是否对照 HDRP Diffusion Profile 结构。
