# OIT Shader ABI 与材质接入审查

## 当前材质侧状态

当前 generated material prototype 已有：

```text
Runtime/Shaders/Generated/HoUrpDebugLitMinimal.shader
Runtime/Shaders/ShaderLibrary/HoUrpMaterialOit.hlsl
Runtime/Shaders/ShaderLibrary/HoUrpMaterialSurface.hlsl
```

已声明：

```text
Name "HoUrpOitAccumulation"
Tags { "LightMode" = "HoUrpOitAccumulation" }
HoUrpTransparentOutputData
HoUrpOitAccumulationData
```

第十二阶段不需要重做材质系统，只需要让 runtime draw 这个 pass。

## 材质 pass ABI

必须保持：

```text
LightMode = HoUrpOitAccumulation
```

禁止：

```text
LightMode = lilToonOIT
```

材质输出建议第一版保持当前结构：

```text
SV_Target0: weighted color / alpha
SV_Target1: revealage
```

如果当前 shader output 不满足旧 weighted formula，需要在实现前微调 `HoUrpMaterialOit.hlsl`，但不能引入旧命名。

## Runtime shader property ABI

建议新增到 `HoUrpShaderPropertyIds`：

```text
WeightedOitCompositeShaderName = "Hidden/HoURP/OIT/WeightedComposite"

OitOpaqueColorTexture = Shader.PropertyToID("_HoUrpOitOpaqueColorTexture")
OitAccumulationTexture = Shader.PropertyToID("_HoUrpOitAccumulationTexture")
OitRevealageTexture = Shader.PropertyToID("_HoUrpOitRevealageTexture")
OitCompositeSourceTexture = Shader.PropertyToID("_HoUrpOitCompositeSourceTexture")
OitActive = Shader.PropertyToID("_HoUrpOitActive")
OitWeight = Shader.PropertyToID("_HoUrpOitWeight")
OitAlphaClipThreshold = Shader.PropertyToID("_HoUrpOitAlphaClipThreshold")
```

## Composite shader

新增：

```text
Runtime/Shaders/Hidden/HoURP/OIT/WeightedComposite.shader
```

Shader name：

```text
Hidden/HoURP/OIT/WeightedComposite
```

Shader inputs：

```hlsl
TEXTURE2D_X(_HoUrpOitCompositeSourceTexture);
TEXTURE2D_X(_HoUrpOitAccumulationTexture);
TEXTURE2D_X(_HoUrpOitRevealageTexture);
```

最低 composite 逻辑：

```hlsl
float4 source = SAMPLE_TEXTURE2D_X(_HoUrpOitCompositeSourceTexture, sampler_LinearClamp, uv);
float4 accum = SAMPLE_TEXTURE2D_X(_HoUrpOitAccumulationTexture, sampler_LinearClamp, uv);
float revealage = SAMPLE_TEXTURE2D_X(_HoUrpOitRevealageTexture, sampler_LinearClamp, uv).r;

float alpha = max(accum.a, 1.0e-5);
float3 transparentColor = accum.rgb / alpha;
float3 color = lerp(transparentColor, source.rgb, saturate(revealage));
return float4(color, source.a);
```

实际公式可以按旧实现核对，但 shader ABI 不能变。

## OIT weight / alpha clip

材质 accumulation pass 可读取：

```text
_HoUrpOitWeight
_HoUrpOitAlphaClipThreshold
```

如果当前 `HoUrpMaterialOit.hlsl` 还没有使用这两个全局参数，可先在 runtime 中预留 property id，后续在材质 HLSL 中接入。

规则：

- weight / alpha clip 是 runtime OIT 参数。
- 不使用 `_lilOITWeight`。
- 不使用 `_lilOITAlphaClipThreshold`。

## Phase Gate

新增：

```text
_HoUrpOitActive
```

用途：

- accumulation 阶段为 1。
- 其它阶段为 0。

第一版可只由 runtime 设置，不要求所有 generated shader forward pass 都消费它。

后续如果需要避免 OIT-only 材质走普通 transparent forward，应该通过：

```text
MaterialPhasePolicy
renderer filtering
pass tag
```

共同控制，而不是只靠全局 float。

## ABI 测试

新增 / 扩展测试：

```text
GeneratedDebugLitShaderDeclaresAovAndOitReadyPasses
GeneratedDebugLitShaderDoesNotExposeOldOitAbi
WeightedOitCompositeShaderUsesHoUrpNames
WeightedOitCompositeShaderDoesNotUseLegacyLilNames
OitShaderPropertyIdsUseHoUrpNames
```

必须断言：

```text
Contains "HoUrpOitAccumulation"
Contains "Hidden/HoURP/OIT/WeightedComposite"
Contains "_HoUrpOitAccumulationTexture"
Contains "_HoUrpOitRevealageTexture"
Does.Not.Contain "lilToonOIT"
Does.Not.Contain "_lilOIT"
Does.Not.Contain "Hidden/lilToon/URP/WeightedOITComposite"
```

## 材质接入验收

手动场景：

- 使用 `HoUrpDebugLitMinimal`。
- 设置 transparent queue。
- 两个透明物体深度交错。
- 开启 `HoURP Weighted OIT`。

期望：

- Frame Debugger / RenderGraph 中出现 `HoUrpOitAccumulation` draw。
- Accumulation / revealage 非空。
- Composite 后透明对象对 camera color 有影响。

