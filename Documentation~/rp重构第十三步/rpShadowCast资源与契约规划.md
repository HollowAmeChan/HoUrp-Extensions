# rpShadowCast资源与契约规划

## 目标

建立 ShadowCast 动态资源契约，让 ShadowCast feature、receiver shader、debug 工具和测试都通过统一名字识别资源。

资源契约解决三个问题：

- 谁创建主 shadow atlas 与 second directional atlas。
- 谁发布 receiver 需要的 punctual/second directional 数据。
- 什么时候资源有效，什么时候必须 reset。

## Resource Id 建议

新增资源族：

```text
ShadowCast.Atlas
ShadowCast.AtlasSize
ShadowCast.LightData
ShadowCast.LightAttenuation
ShadowCast.LightColor
ShadowCast.SliceData
ShadowCast.WorldToShadow
ShadowCast.SecondDirectionalAtlas
ShadowCast.SecondDirectionalAtlasSize
ShadowCast.SecondDirectionalLightData
ShadowCast.SecondDirectionalSliceData
ShadowCast.SecondDirectionalWorldToShadow
ShadowCast.DebugAtlas
ShadowCast.DebugSecondDirectionalAtlas
ShadowCast.DebugAttenuation
```

命名原则：

- 资源 id 描述语义，不描述实现类名。
- Debug id 与生产 id 分开。
- 不使用旧 `_HoShadowCast*` 名称作为 resource id。

## Declaration 建议

新增：

```text
Runtime/RenderGraph/HoUrpShadowCastResourceDeclaration.cs
```

职责：

- 根据 settings 创建主 atlas descriptor。
- 根据 settings 创建 second directional atlas descriptor。
- 声明 atlas texture。
- 声明 light/slice/world-to-shadow 数据 buffer 或全局数组绑定点。
- 统一设置 debug name。
- 返回一个只在当前 record 阶段有效的 resource bundle。

示意结构：

```csharp
public readonly struct HoUrpShadowCastResources
{
    public readonly TextureHandle Atlas;
    public readonly BufferHandle LightData;
    public readonly BufferHandle LightAttenuation;
    public readonly BufferHandle LightColor;
    public readonly BufferHandle SliceData;
    public readonly BufferHandle WorldToShadow;
    public readonly TextureHandle SecondDirectionalAtlas;
    public readonly BufferHandle SecondDirectionalLightData;
    public readonly BufferHandle SecondDirectionalSliceData;
    public readonly BufferHandle SecondDirectionalWorldToShadow;
    public readonly bool IsValid;
}
```

实际实现可根据当前 Unity/URP 版本选择 buffer 或 shader global array。若 RenderGraph buffer API 不稳定，第一版允许用全局数组发布，但资源命名和 reset 规则仍必须集中。

## Atlas Descriptor

主 atlas：

- Format：depth format，由 settings 提供默认值。
- Size：默认 4096，可配置。
- Dimension：2D。
- Mip：关闭。
- MSAA：1。
- Clear：每帧 clear depth。

Second directional atlas：

- 独立 descriptor。
- 默认 4096，可配置。
- Grid/cascade 布局，不参与主 atlas row packing。
- Clear：每帧 clear depth。

需要避免：

- 直接复用 camera depth。
- 直接复用 URP main shadow map。
- 跨 camera 持有同一 `TextureHandle`。

## Light/Slice 数据

主 atlas 必须支持 spot/point：

```text
LightData0: lightType, firstSlice, sliceCount, shadowStrength
LightData1: position.xyz, range
LightData2: direction.xyz, spotCosHalfAngle
LightAttenuation: rangeScale, fadeSpeed, spotScale, spotOffset
LightColor: rgb, reserved
SliceData: atlasOffset.xy, atlasScale, reserved
WorldToShadow: matrix rows
```

Second directional 必须支持 cascades：

```text
SecondDirectionalParams: active, lightCount, cascadeCount, sliceCount
SecondDirectionalLightData: firstSlice, cascadeCount, shadowStrength, reserved
SecondDirectionalSliceData: atlasOffset.xy, atlasScale, cascadeDistanceSqr
SecondDirectionalWorldToShadow: matrix rows
```

延期字段：

```text
transparent shadow params
receiver category mask
character specialization mask
advanced pcss tuning
```

## Shader Globals

建议集中在：

```text
Runtime/ShadowCast/HoShadowCastShaderConstants.cs
```

建议 property：

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

第一版必须能表达 inactive：

```text
_HoUrpShadowCastActive = 0
_HoUrpShadowCastLightCount = 0
_HoUrpShadowCastSliceCount = 0
_HoUrpShadowReceiverStrength = 0
_HoUrpShadowCastSecondDirectionalParams = 0
```

## 生命周期规则

### 有效期

ShadowCast 资源只在当前 camera/frame 的 RenderGraph record/execute 生命周期中有效。

### 禁止项

- 禁止 feature 保存 `TextureHandle` 到下一帧。
- 禁止材质保存 atlas 资源引用。
- 禁止 debug feature 反向成为资源 owner。
- 禁止在 feature disabled 时沿用上一帧 globals。

### Reset 时机

以下情况必须 reset：

- Feature disabled。
- Camera type 不参与。
- Scene View/Game View 被设置排除。
- 无有效 light。
- 无 caster 或 atlas allocation 失败。
- RenderGraph pass 被条件跳过。
- Second directional 输入为空或分配失败。

## 测试要求

- 资源 id 全部注册且唯一。
- Debug name 与 resource id 可追踪。
- Shader constants 不包含旧 `_HoShadowCast*`。
- Reset 函数覆盖 active/count/texture/array 参数。
- 主 atlas 和 second directional atlas 都能 inactive。
- Declaration 在无效 descriptor 下返回 inactive，而不是抛出不可控异常。

