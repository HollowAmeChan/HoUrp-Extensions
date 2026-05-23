# rpShadowCastRenderGraph执行计划

## 目标

在 HoURP 新 RenderGraph 框架下实现 HoShadowCast 的子系统级执行路径。第一版重点是稳定迁移旧实现已有能力：主 atlas packing、spot/point 投影、second directional atlas、debug view 和 receiver globals。

## 入口文件

建议新增：

```text
Runtime/ShadowCast/HoShadowCastRendererFeature.cs
Runtime/ShadowCast/HoShadowCastSettings.cs
Runtime/ShadowCast/HoShadowCastShaderConstants.cs
Runtime/ShadowCast/HoShadowCastResources.cs
Runtime/ShadowCast/HoShadowCastAtlasPacker.cs
Runtime/RenderGraph/HoUrpShadowCastResourceDeclaration.cs
```

可选 debug：

```text
Runtime/Shaders/Hidden/HoURP/ShadowCast/Debug.shader
```

## Feature Settings

第一版设置建议：

```text
enabledForGameView
enabledForSceneView
casterLayerMask
receiverStrength
punctualShadowStrength
punctualShadowFadeSpeed
atlasSize
spotResolution
pointFaceResolution
secondDirectionalAtlasSize
secondDirectionalCascadeCount
secondDirectionalMaxDistance
secondDirectionalShadowDepth
secondDirectionalCascadeSplits
bias
normalBias
maxSpotLights
maxPointLights
maxSecondDirectionalLights
pcssEnabled
pcssQuality
punctualPcssSoftness
secondDirectionalPcssSoftness
debugMode
```

默认策略：

- Game View 开启。
- Scene View 开启，便于调试。
- Atlas size 默认 4096，允许降级。
- Max lights 对齐旧实现能力：spot 4、point 4、second directional 4。
- 点光每光 6 faces。
- Second directional 每光 1-4 cascades。
- Debug 默认关闭。

## Pass 切分

### 1. Reset/Inactive Pass

在以下情况只执行 reset：

- feature disabled。
- camera 不参与。
- 无有效 light。
- 无 caster layer。

Reset 内容：

- active/count 设零。
- receiver strength 设零。
- 主 atlas 与 second directional atlas 绑定 fallback。
- 数组或 buffer 计数清零。

### 2. Build Punctual Frame Data

Record 阶段计算：

- 当前 camera 是否参与。
- spot/point 光源候选列表。
- 主 atlas row packing。
- spot 1 slice。
- point 6 face slices。
- caster filtering settings。

若 point light 6 faces 不能完整分配，整光跳过并回滚 slice count。

### 3. Allocate Main Atlas

调用 `HoUrpShadowCastResourceDeclaration`：

- 创建主 atlas。
- 准备 light/slice/world-to-shadow 数据。
- 设置 debug name。

### 4. Draw Punctual Caster Slices

使用 RendererList 或等价 URP API 绘制 caster：

- ShaderTagId：`ShadowCaster`。
- Filtering：settings.casterLayerMask。
- Sorting：适合 shadow caster 的 sorting flags。
- Render target：主 atlas depth。
- 每个 slice 设置 viewport。
- Clear：每帧 clear。

注意：

- 不读写 camera color。
- 不依赖 OIT accumulation。
- 不依赖 SSS/AOV/Post 中间纹理。

### 5. Build Second Directional Frame Data

Record 阶段计算：

- 额外方向光候选列表。
- 排除 URP main light。
- cascade count。
- cascade split distances。
- second directional atlas grid layout。

### 6. Allocate Second Directional Atlas

创建：

- second directional atlas texture。
- second directional light data。
- second directional slice data。
- second directional world-to-shadow rows。

### 7. Draw Second Directional Caster Slices

对 second directional frame 的每个 cascade slice 绘制：

- ShaderTagId：`ShadowCaster`。
- Render target：second directional atlas depth。
- Slice viewport：grid tile。
- Clear：每帧 clear。

### 8. Publish Receiver Globals

发布：

- 主 atlas texture。
- second directional atlas texture。
- active/count。
- world-to-shadow。
- light/slice data。
- pcss params。
- receiver strength。

这个 pass 必须发生在 Forward/OIT receiver 材质被绘制前，或至少在需要采样 ShadowCast 的 pass 前。

### 9. Debug Pass

可选：

- Atlas debug。
- Second directional atlas debug。
- Receiver attenuation debug。
- Light/slice index debug。

Debug pass 不应改变生产 receiver globals。

## 与渲染顺序关系

建议位置：

```text
Before rendering transparent receivers
Before OIT accumulation
Before material forward receiver that needs HoShadowCast
Independent from post chain
```

若 URP renderer feature 注入点不能精确表达，优先保证：

- 主 atlas 与 second directional atlas 已经绘制。
- Receiver globals 已经发布。
- OIT/Forward receiver 采样时数据有效。

## 错误处理

### Atlas 分配失败

- Log warning 可选。
- reset globals。
- 跳过 draw/publish。

### 单个光源分配失败

- Spot 分配失败：跳过该 light。
- Point 任一 face 分配失败：整光回滚并跳过。
- Second directional 任一 cascade 构建失败：跳过该 second directional frame 或整组 inactive。

### Shader/Material 缺失

- Debug material 缺失不影响生产 path。
- Debug shader 缺失只关闭 debug view。

### 无 caster

- 可以仍发布 active light 数据，也可以 inactive。
- 第一版建议 inactive，降低残影风险。

## 验收点

- Frame Debugger 中能看到主 ShadowCast atlas draw。
- Frame Debugger 中能看到 second directional atlas draw。
- RenderDoc 中 ShadowCast pass 不触碰 camera color。
- 开关 feature 后 globals 不残留。
- Forward/OIT receiver 在同一帧采样到一致 attenuation。

