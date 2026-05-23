# rpShadowReceiverShaderABI与材质接入审查

## 目标

定义材质侧接收 HoShadowCast 的 ABI。第十三步要求生成材质/debug lit shader 能接收 punctual 与 second directional 自定义阴影，并能独立产生投影。

## 核心原则

投影和受影分离：

- 产生投影：依赖 `ShadowCaster` pass。
- 接收阴影：依赖 Forward/OIT pass 中的 receiver sampling。

因此，给 OIT pass 加光照不会自动让物体产生投影；给 shader 加 `ShadowCaster` pass 也不会自动让 Forward/OIT 受影。

## Include 建议

新增：

```text
Runtime/Shaders/ShaderLibrary/HoUrpShadowCastSampling.hlsl
```

最小函数：

```hlsl
half HoUrpSampleShadowCastAttenuation(float3 positionWS, half3 normalWS)
{
    if (_HoUrpShadowCastActive == 0)
    {
        return 1.0h;
    }

    half punctual = HoUrpSampleShadowCastPunctual(positionWS);
    half secondDirectional = HoUrpSampleShadowCastSecondDirectional(positionWS);
    return punctual * secondDirectional;
}
```

可选封装：

```hlsl
half3 HoUrpApplyShadowCast(half3 litColor, float3 positionWS, half3 normalWS)
{
    half attenuation = HoUrpSampleShadowCastAttenuation(positionWS, normalWS);
    return litColor * lerp(1.0h, attenuation, _HoUrpShadowReceiverStrength);
}
```

## Shader Property ABI

新 ABI 使用 HoURP 命名：

```text
_HoUrpShadowCastAtlas
_HoUrpShadowCastAtlasSize
_HoUrpShadowCastActive
_HoUrpShadowCastLightCount
_HoUrpShadowCastSliceCount
_HoUrpShadowCastWorldToShadow
_HoUrpShadowCastLightData
_HoUrpShadowCastLightAttenuation
_HoUrpShadowCastLightColor
_HoUrpShadowCastSliceData
_HoUrpShadowCastPcssParams
_HoUrpShadowCastPcssParams2
_HoUrpShadowCastSecondDirectionalAtlas
_HoUrpShadowCastSecondDirectionalParams
_HoUrpShadowCastSecondDirectionalAtlasSize
_HoUrpShadowCastSecondDirectionalWorldToShadow
_HoUrpShadowCastSecondDirectionalLightData
_HoUrpShadowCastSecondDirectionalSliceData
_HoUrpShadowCastSecondDirectionalPcssParams
_HoUrpShadowReceiverStrength
```

禁止默认使用：

```text
_HoShadowCast*
```

旧名字只可在 legacy bridge 或迁移说明中出现。

## Punctual Sampling

本阶段 receiver include 应覆盖：

- Spot range fade。
- Spot cone fade。
- Point range fade。
- Point face selection。
- Slice transform 与 atlas uv。
- 主 atlas depth compare。

## Second Directional Sampling

本阶段 receiver include 应覆盖：

- Second directional active/count 判断。
- Cascade selection。
- Second directional atlas sampling。
- 与 punctual attenuation 相乘。

## PCF/PCSS 分层

旧实现已有 manual PCF/PCSS。第十三步应迁移结构和参数位：

- PCF/PCSS 函数入口存在。
- `PcssParams`、`PcssParams2`、`SecondDirectionalPcssParams` 可发布。
- 至少有可用 PCF 或退化 PCSS 路径。

以下后置：

- 质量完全等价旧实现。
- 性能优化。
- 多平台采样差异调参。

## Generated/Debug Lit Shader 改造

### UniversalForward

Forward path 应：

1. 计算原本 debug/simple lighting。
2. 调用 `HoUrpSampleShadowCastAttenuation`。
3. 将 attenuation 混入 diffuse/direct lighting。
4. 保持第十二步已有光照行为。

### OIT

OIT path 应：

1. 使用与 Forward 同源的 lighting 计算。
2. 使用同一 receiver attenuation。
3. 写入 OIT accumulation/revealage。

不能出现：

- OIT 开启后退回纯 albedo。
- OIT 使用另一套 shadow property。
- OIT receiver 与 Forward receiver 视觉方向相反。

### ShadowCaster

Shader 必须包含：

```text
Tags { "LightMode" = "ShadowCaster" }
```

第一版接受 opaque-style depth cast。后续若要支持透明 alpha/cutout shadow，需要新增明确策略：

- alpha clip threshold。
- dither shadow。
- per-material cast mode。
- OIT transparent depth approximation。

## 材质参数边界

第一版不引入完整材质 UI。允许增加最小参数：

```text
_HoUrpShadowReceiverStrength
```

但不应一次迁入旧 lilToon 的全部 shadow 参数。旧参数映射应在材质重构阶段单独处理。

## 测试要求

- Shader include 存在。
- Debug/generated shader 引用新 include。
- Forward 和 OIT 都出现 receiver sampling 调用。
- Receiver sampling 同时覆盖 punctual 与 second directional。
- Shader 包含 `UniversalForward`、`OIT`、`ShadowCaster` pass。
- Shader 文本不包含旧 `_HoShadowCast*`，除非测试明确标注 legacy。
- OIT pass 不再出现只输出 base color 的退化路径。

## 手工验收

场景：

- 一个平面 receiver。
- 一个 debug lit opaque object。
- 一个 debug lit transparent/OIT object。
- 一个 spot light。
- 一个 point light。
- 一个 extra directional light。

检查：

- Opaque object 能投影。
- Spot/point/second directional receiver attenuation 均可观察。
- Transparent/OIT object 第一版按 opaque-style shadow policy 投影或明确不投影，不能表现为不确定状态。
- Forward 与 OIT 受影方向一致。
- 关闭 ShadowCast feature 后，所有 receiver 恢复无自定义 shadow。

