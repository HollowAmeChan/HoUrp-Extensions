# rp第十三阶段测试与验收清单

## 自动测试

### 资源契约测试

新增建议：

```text
Tests/Runtime/HoUrpShadowCastResourceContractTests.cs
```

覆盖：

- `ShadowCast.Atlas` 已注册。
- `ShadowCast.SecondDirectionalAtlas` 已注册。
- `ShadowCast.LightData` 已注册。
- `ShadowCast.LightAttenuation` 已注册。
- `ShadowCast.SliceData` 已注册。
- `ShadowCast.WorldToShadow` 已注册。
- 资源 id 唯一。
- Debug name 可追踪。
- ShadowCast 资源不复用 OIT/SSS/Post id。

### Atlas Pack 测试

新增建议：

```text
Tests/Runtime/HoUrpShadowCastAtlasPackerTests.cs
```

覆盖：

- 单 slice 分配成功。
- 换行分配成功。
- 超容量返回 false。
- point light 六 faces 任一失败时整光回滚。
- packing rect 不重叠。

### Shader ABI 测试

新增建议：

```text
Tests/Runtime/HoUrpShadowCastShaderAbiTests.cs
```

覆盖：

- `HoUrpShadowCastSampling.hlsl` 存在。
- 包含 `HoUrpSampleShadowCastAttenuation`。
- 包含 `_HoUrpShadowCastAtlas`。
- 包含 `_HoUrpShadowCastActive`。
- 包含 `_HoUrpShadowCastSecondDirectionalAtlas`。
- 包含 `_HoUrpShadowCastSecondDirectionalParams`。
- 不包含旧 `_HoShadowCast` 作为新 ABI。

### Generated/Debug Shader 测试

覆盖：

- Debug/generated lit shader 包含 `UniversalForward` pass。
- 包含 `OIT` pass。
- 包含 `ShadowCaster` pass。
- Forward path 调用 ShadowCast receiver sampling。
- OIT path 调用 ShadowCast receiver sampling。
- Receiver sampling 覆盖 spot/point punctual attenuation。
- Receiver sampling 覆盖 second directional attenuation。
- OIT path 没有退化成纯 albedo 输出。

### Feature/Constants 测试

覆盖：

- `HoShadowCastShaderConstants` 集中定义所有 property id。
- 存在 reset helper。
- reset helper 覆盖 active/count/strength/atlas。
- reset helper 覆盖 second directional params/atlas。
- Settings 默认值合理。
- Feature display name 与 HoURP 命名一致。

## Unity 编译验收

在 Unity 中确认：

- 无 C# 编译错误。
- 无 shader 编译错误。
- 新 feature 能在 Renderer Feature 面板添加。
- Feature 开启/关闭不会产生 console error。

## 手工场景验收

### 场景配置

创建最小场景：

- Plane 作为 receiver。
- Opaque debug lit 物体。
- Transparent/OIT debug lit 物体。
- Directional Light。
- Spot Light。
- Point Light。
- Extra Directional Light。
- HoURP renderer 启用 AOV、SSS、OIT、ShadowCast、Post 中的常用组合。

### 投影验收

- Opaque debug lit 物体能产生投影。
- Spot light 能在主 atlas 中产生 1 个 slice。
- Point light 能在主 atlas 中产生 6 个 faces。
- Extra directional light 能在 second directional atlas 中产生 cascades。
- Transparent/OIT debug lit 物体的投影行为符合当前策略：
  - 若第一版启用 opaque-style shadow，则能以不透明深度投影。
  - 若第一版禁用透明投影，则 UI/文档必须明确。
- 关闭 `ShadowCaster` pass 的 shader 不应产生投影。

### 受影验收

- Plane 能接收 HoShadowCast attenuation。
- Opaque debug lit 物体能接收 attenuation。
- Transparent/OIT debug lit 物体能接收 attenuation。
- Receiver attenuation 同时响应 spot/point 与 second directional。
- Forward/OIT 受影强度方向一致。

### 开关验收

- 关闭 ShadowCast feature 后，receiver 立即恢复无自定义 shadow。
- 关闭 light 后，receiver 不变黑。
- Scene View/Game View 开关互不污染。
- 反复启停 feature 不出现上一帧残影。
- 关闭 extra directional 后，second directional atlas 不残留。

### 顺序验收

- 开启 OIT 后，透明物体仍保留基础光照。
- 开启 SSS 后，ShadowCast receiver 不丢失。
- 开启 ScreenPost/ImagePost 后，只改变最终画面，不影响 ShadowCast atlas。
- AOV debug 不改变生产输出。

## RenderDoc/Frame Debugger 验收

可选但建议：

- 能看到主 ShadowCast atlas/depth pass。
- 能看到 second directional atlas/depth pass。
- ShadowCast pass render target 不是 camera color。
- Receiver draw 前 globals 已发布。
- OIT accumulation shader 绑定 ShadowCast receiver 数据。
- Post pass 不绑定 ShadowCast atlas，除非 debug mode。

## 失败判定

以下任一情况视为第十三步未完成：

- Feature 关闭后仍保留上一帧阴影。
- 主 atlas 不支持 spot/point packing。
- Point light 只写入部分 faces 但仍被 receiver 采样。
- Second directional atlas 无 debug 或无 reset。
- OIT 开启后透明物体光照或自定义阴影消失。
- Shader 只有 receiver sampling，没有 `ShadowCaster` pass，却被误认为可投影。
- 新代码默认使用旧 `_HoShadowCast*` 作为公共 ABI。
- ShadowCast pass 写入或读取 post chain ping-pong 资源。
- AOV/SSS/Post 关闭顺序改变 ShadowCast 基础结果。

## 后续延期项

- alpha/cutout/dither 透明投影策略。
- 更优 atlas packing。
- 跨帧 atlas cache。
- PCSS/soft shadow 质量调参。
- CharacterSpecialization 阴影。
- 与材质 UI 的完整参数映射。
- legacy `_HoShadowCast*` bridge。

