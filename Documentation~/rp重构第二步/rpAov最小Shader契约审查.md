# RP AOV 最小 Shader 契约审查

## 目标

定义第二阶段新增 shader 的职责，确保它们只服务最小闭环，不成为旧材质系统入口。

## 新增 Shader

```text
Runtime/Shaders/Hidden/HoURP/AOV/AovOutputFallback.shader
Runtime/Shaders/Hidden/HoURP/Debug/AovDebug.shader
Runtime/Shaders/Hidden/HoURP/SemanticPost/AovReadProbe.shader
```

## `Hidden/HoURP/AOV/AovOutputFallback`

职责：

- 使用 scene renderer list + override material 绘制 opaque objects。
- 写入 MRT：`Aov.MaskId` 和 `Aov.NormalDepth`。
- 输出最小常量 mask/id/group/flags 和 encoded world normal/depth。

禁止：

- 不 include 旧 `lil_pass_hoaov.hlsl`。
- 不读取旧 `_lilHoAov*` property。
- 不实现旧材质 custom / surface / SSS 编码。

## `Hidden/HoURP/Debug/AovDebug`

职责：

- 从注册 DebugView 的 source resource 读取 AOV。
- replace camera color。
- 第一版支持 Mask、Object ID、Linear Depth、World Normal。

## `Hidden/HoURP/SemanticPost/AovReadProbe`

职责：

- 显式读取 `Aov.MaskId` / `Aov.NormalDepth`。
- 对 camera color 做最小 mask tint，证明正式 consumer 能读取 AOV。

## LightMode

新 LightMode `HoUrpAovOutput` 在本阶段留档，但因为第一版使用 override material，renderer list 不依赖场景材质提供该 pass。后续新材质系统接入时再决定是否使用 material pass 路径。
