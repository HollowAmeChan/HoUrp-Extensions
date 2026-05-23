# rpShadowCastDebug与状态清理审查

## 目标

ShadowCast 的错误最容易表现为上一帧残影、feature 关闭后仍受影、Scene View/Game View 不一致、OIT 路径和 Forward 路径不一致、second directional atlas 残留。第十三步必须把 debug 和状态清理作为一等验收项。

## Debug 模式

建议 debug mode：

```text
None
Atlas
SecondDirectionalAtlas
Attenuation
LightIndex
SliceIndex
ReceiverStrength
```

第一版至少实现：

- `Atlas`
- `SecondDirectionalAtlas`

`Attenuation` 可作为第三项，但如果实现 receiver debug 成本较低，应同阶段完成。

## Debug 输出边界

Debug 输出用于观察状态，不成为生产材质 ABI。

禁止：

- 生产 shader 依赖 debug texture。
- Debug pass 反向写入 production atlas。
- AOV feature 成为 ShadowCast 资源 owner。

## 全局状态集中管理

建议在：

```text
Runtime/ShadowCast/HoShadowCastShaderConstants.cs
```

集中定义：

- property id。
- default/inactive 值。
- reset helper。

Reset helper 应覆盖：

```text
_HoUrpShadowCastActive
_HoUrpShadowCastLightCount
_HoUrpShadowCastSliceCount
_HoUrpShadowReceiverStrength
_HoUrpShadowCastAtlas
_HoUrpShadowCastWorldToShadow
_HoUrpShadowCastLightData
_HoUrpShadowCastLightAttenuation
_HoUrpShadowCastLightColor
_HoUrpShadowCastSliceData
_HoUrpShadowCastSecondDirectionalAtlas
_HoUrpShadowCastSecondDirectionalParams
_HoUrpShadowCastSecondDirectionalWorldToShadow
_HoUrpShadowCastSecondDirectionalLightData
_HoUrpShadowCastSecondDirectionalSliceData
```

## 必须 Reset 的路径

- Feature disabled。
- Camera 不参与。
- Scene View/Game View 开关排除。
- 无有效 light。
- 无 caster。
- 主 atlas descriptor 无效。
- Second directional atlas descriptor 无效。
- Shader/material 缺失导致 publish 不能完成。
- RenderGraph path 早退。

## Camera 与 View 规则

建议 settings：

```text
enabledForGameView
enabledForSceneView
```

处理原则：

- 不参与的 camera 必须 reset globals，避免 Scene View/Preview camera 继承 Game View 状态。
- Preview camera 默认不参与。
- Reflection/Planar camera 本阶段默认不参与。

## Debug 材质与 ShadowCaster

Debug 材质本身也需要接受投影能力审查：

- 需要投影时，必须有 `ShadowCaster` pass。
- 需要受影时，Forward/OIT pass 必须调用 receiver sampling。
- Debug output pass 不代替 ShadowCaster pass。

第一版透明 debug 材质如果采用 opaque-style shadow，应在 Inspector 或文档中明确。不能让用户误以为 OIT pass 自动等于透明投影。

## 验收案例

### Feature 开关

1. 开启 ShadowCast。
2. 观察 receiver attenuation。
3. 关闭 ShadowCast。
4. attenuation 应恢复为 1，不保留上一帧阴影。

### Camera 切换

1. Game View 开启。
2. Scene View 禁用。
3. 两个 view 不应共享错误状态。

### 无光源

1. 删除或禁用主光、spot、point、extra directional。
2. ShadowCast globals 应 inactive。
3. Receiver 不应变黑。

### Debug 缺失

1. 移除 debug shader。
2. 生产 ShadowCast path 仍可运行。
3. 只禁用 debug view。

### Second Directional Debug

1. 配置一个额外方向光。
2. 打开 `SecondDirectionalAtlas` debug。
3. atlas 中应显示 cascade tiles。
4. 关闭额外方向光后，second directional globals 应 inactive。

