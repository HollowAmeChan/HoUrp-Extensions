# rpShadowCastAtlasPack与多光源执行规划

## 目标

第十三步不能只做单光硬阴影。旧 HoShadowCast 已经具备主 atlas packing、spot/point 投影、额外方向光 second directional atlas 和 debug 能力。新实现应把这些能力作为本阶段基线，但重新整理命名、资源契约和 RenderGraph 边界。

## 旧实现能力基线

旧常量：

```text
MaxDirectionalLights = 4
MaxSpotLights = 4
MaxPointLights = 4
MaxLights = 12
MaxShadowSlices = 4 + 4 + 4 * 6
MaxSecondDirectionalCascades = 4
MaxSecondDirectionalSlices = 4 * 4
```

旧布局：

- 主 atlas：主要承载 spot/point shadows。
- Spot：每个 light 1 slice。
- Point：每个 light 6 slices，对应 cubemap 六面。
- Second directional atlas：额外方向光，每光 1-4 cascades。
- Debug：可显示主 atlas 和 second directional atlas。

## 新实现目标

第十三步必做：

- 主 atlas row packer。
- Spot light slice 构建。
- Point light six-face slice 构建。
- Punctual light data 发布。
- Second directional atlas grid/cascade 布局。
- Second directional receiver sampling。
- Atlas 与 second directional atlas debug view。

第十三步不必做到：

- 最优 packing。
- 动态 atlas defragmentation。
- 跨帧 atlas cache。
- 所有软阴影质量完全等价旧实现。
- 透明 alpha/dither 精确投影。

## 主 Atlas Packing

第一版可沿用旧实现的 row packing 思路，但实现应归入 HoURP 命名空间：

```text
cursorX
cursorY
rowHeight
TryAllocate(size)
```

规则：

- `size` clamp 到 `[1, atlasSize]`。
- 当前行放不下则换行。
- 下一行放不下则该 light 分配失败。
- point light 必须 6 faces 全部分配成功，否则回滚该 light 的 slices。

建议新增：

```text
Runtime/ShadowCast/HoShadowCastAtlasPacker.cs
```

也可以先作为 feature 内部 struct，但后续测试会更不方便。

## Slice 类型

### Spot Slice

每个 spot light：

- 1 个 slice。
- 使用 light view/projection。
- 写入 `WorldToShadow`。
- 写入 atlas rect。
- 写入 light range、spot direction、spot attenuation。

### Point Slice

每个 point light：

- 6 个 slices。
- 每个 face 独立 view/projection。
- receiver 侧根据 `positionWS - lightPositionWS` 选择 face。
- 必须保证六面分配一致，不允许只写入部分 faces。

### Second Directional Cascade Slice

每个额外方向光：

- `cascadeCount` 个 slices。
- 根据 camera near/far 与 split ratios 计算 cascade。
- 使用 second directional atlas。
- receiver 侧根据 camera distance 选择 cascade。

## Light Data 布局建议

主 atlas light data：

```text
LightData0: lightType, firstSlice, sliceCount, shadowStrength
LightData1: position.xyz, range
LightData2: direction.xyz, spotCosHalfAngle
LightAttenuation: rangeScale, fadeSpeed, spotScale, spotOffset
LightColor: rgb, reserved
```

Second directional light data：

```text
SecondDirectionalLightData: firstSlice, cascadeCount, shadowStrength, reserved
SecondDirectionalSliceData: atlasOffset.xy, atlasScale, cascadeDistanceSqr
```

Slice data：

```text
SliceData: atlasOffset.xy, atlasScale, reservedOrDistance
WorldToShadowRow0-3: matrix rows
```

## Resolution 策略

主 atlas：

- `atlasSize` 默认 4096，允许 settings 调整。
- `spotResolution` 默认 512。
- `pointFaceResolution` 默认 512。
- 若请求 slice 太多，按 atlas 容量限制最大 slice resolution。

Second directional atlas：

- `secondDirectionalAtlasSize` 默认 4096。
- `gridSize = ceil(sqrt(requestedSliceCount))`。
- `resolution = atlasSize / gridSize`。

## RenderGraph Pass 建议

```text
BuildPunctualFrameData
AllocateMainAtlas
DrawPunctualAtlasSlices
BuildSecondDirectionalFrameData
AllocateSecondDirectionalAtlas
DrawSecondDirectionalAtlasSlices
PublishReceiverGlobals
DebugAtlasPass
```

可以合并 publish pass，但数据结构必须能分清主 atlas 和 second directional atlas。

## 验收

- 1 个 spot light：主 atlas 出现 1 个 slice。
- 1 个 point light：主 atlas 出现 6 个 faces。
- 2 个 point lights + 2 个 spot lights：packing 无重叠，超容量时整光跳过。
- 1 个 second directional light，4 cascades：second directional atlas 出现 4 个 tiles。
- Debug mode 可分别显示两个 atlas。
- Receiver attenuation 同时乘上 punctual 与 second directional attenuation。

