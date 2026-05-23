# rp第十三阶段实现边界审查

## 阶段定位

第十三阶段处理 HoShadowCast 子系统迁移第一阶段。它位于第十二步 Weighted OIT 之后，目的不是继续扩展透明合成，而是补齐材质链路中的“多光投影/受影”契约。

本阶段产物应让后续材质重构可以稳定依赖以下事实：

- 是否投影由 `ShadowCaster` pass 和 ShadowCast feature 决定。
- 是否受影由 Forward/OIT receiver sampling 决定。
- 自定义 shadow 资源由 RenderGraph 声明、发布和清理。
- Shader ABI 由 HoURP 命名空间承载，不继续扩大旧实现名字。
- Atlas pack、spot/point、second directional atlas 和 debug view 是本阶段基线，而不是后续扩展。

## 必须完成

### RenderFeature 子系统闭环

- 新增 HoURP ShadowCast feature。
- 支持按 layer mask 选择 caster。
- 支持 Scene View/Game View 开关。
- 支持无有效 light 时跳过绘制并 reset globals。
- 支持 feature 禁用后清理上一帧全局 shadow 状态。
- 支持主 atlas row packing。
- 支持 spot light 1 slice。
- 支持 point light 6 faces。
- 支持额外方向光 second directional atlas 与 cascades。

### 资源契约

- 主 shadow atlas 由 RenderGraph 分配。
- Second directional atlas 由 RenderGraph 单独分配。
- Light/slice/world-to-shadow 数据由统一 declaration 管理。
- 资源 id 必须可被 debug 和测试查询。
- 动态资源不能被跨帧缓存。

### Shader ABI

- 新 include 提供 receiver sampling 函数。
- Receiver attenuation 至少由 punctual attenuation 与 second directional attenuation 合成。
- Forward 与 OIT 路径使用同一 receiver attenuation。
- Generated/debug material 必须拥有 `ShadowCaster` pass。
- 第一版明确透明投影限制：默认 opaque-style depth cast，不承诺 alpha/dither 精确投影。

### Debug/验收

- 提供主 atlas debug。
- 提供 second directional atlas debug。
- 提供 receiver attenuation debug 或明确延期。
- 提供测试覆盖资源命名、shader property、pass 存在性、atlas packing 和 legacy 名称隔离。
- 提供 Unity 手工场景验收说明。

## 不在本阶段处理

- CharacterSpecialization、头发/眼睛/皮肤的专用阴影策略。
- Planar Reflection 与 ShadowCast 的交互。
- 完整材质 Inspector/UI。
- PCSS 质量完全等价、EVSM、blur、contact shadow 等高质量阴影扩展。
- 更优 atlas packing、跨帧 atlas cache、动态分辨率预算。
- 透明 alpha/dither/cutout 投影的完整策略。
- 与旧 `_HoShadowCast*` ABI 的运行时兼容桥。
- 所有 lilToon shadow 参数的行为复刻。

## 风险边界

### 与 URP 内建阴影的关系

HoShadowCast 不是替换 URP 主阴影。本阶段应作为 HoURP 自定义 attenuation 输入存在，由材质决定如何混合到 final lighting。额外方向光应被视为 HoURP shadow receiver 的附加项，而不是 URP main light shadow 的替代品。

### 与 OIT 的关系

OIT 只负责透明顺序无关合成。OIT pass 参与受影采样，但不负责产生 shadow map。产生投影仍依赖 `ShadowCaster` pass。

### 与 AOV/Debug 的关系

AOV 可以读取最终材质输出或 debug state，但不应成为 ShadowCast 数据发布方。Debug 输出也不能成为生产材质依赖。

### 与旧仓库的关系

旧实现只作为数据流和行为参考。直接复制旧类名、旧 property、旧 feature 行为会扩大迁移债务。

## 阶段完成判定

满足以下条件才算完成：

- 新 feature 单独启停无残留。
- 开启后能生成主 atlas 和可选 second directional atlas。
- Spot/point/second directional 均有明确数据结构和验收。
- Debug/generated lit 材质能投影，并能在 Forward/OIT 中接收自定义阴影。
- 关闭 ShadowCast 后，第十一、十二步 post/OIT/SSS/AOV 行为不变化。
- 文档明确列出所有延期能力，避免把未完成质量误判为 bug。

