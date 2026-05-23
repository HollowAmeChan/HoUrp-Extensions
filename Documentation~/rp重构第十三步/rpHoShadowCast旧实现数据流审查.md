# rpHoShadowCast旧实现数据流审查

## 旧实现参考位置

旧仓库：

```text
D:/Unity_Fork/lilToon-URP-Extensions
```

重点参考路径：

```text
Runtime/ShadowCast/HoShadowCastRendererFeature.cs
Runtime/ShadowCast/HoShadowCastController.cs
Runtime/ShadowCast/HoShadowCastRenderGraphResources.cs
Runtime/ShadowCast/HoShadowCastShaderConstants.cs
Runtime/ShadowCast/Shaders/HoShadowCastSampling.hlsl
Runtime/ShadowCast/Shaders/HoShadowCastDebug.shader
```

## 旧能力清单

旧实现不是单光原型，已经具备以下能力：

- Controller 持有 `directionalLights`、`spotLights`、`pointLights` 三组输入。
- 主 atlas 使用 row packer 分配 spot/point slices。
- Spot light 每光 1 slice。
- Point light 每光 6 slices。
- Second directional atlas 使用 grid 布局，为额外方向光写入 cascades。
- Sampling include 同时计算 punctual attenuation 与 second directional attenuation。
- Debug shader 可显示主 atlas 与 second directional atlas。
- PCF/PCSS 采样逻辑已经存在，但质量参数较多，迁移时可先保证结构，再做质量调参。

## 旧数据流概念

旧 HoShadowCast 可抽象为以下阶段：

1. Feature/Controller 读取开关、layer、atlas、spot/point/second directional 参数。
2. 为 spot/point 构建主 atlas frame data。
3. 为额外方向光构建 second directional frame data。
4. 为每个 slice 创建 shadow draw。
5. 将 shadow depth 写入对应 atlas。
6. 发布 atlas、matrix、light/slice 参数到 shader globals。
7. Forward 材质通过 sampling include 计算 punctual 与 second directional attenuation。
8. Debug shader 显示主 atlas 或 second directional atlas。

这些阶段仍然有价值，但新实现必须重新整理所有边界：

- Feature 只负责发布资源和状态，不持有材质语义。
- Receiver include 只负责采样和返回 attenuation，不决定完整 lighting 模型。
- Debug shader 只用于诊断，不作为材质生成器 ABI。

## 可保留概念

- 自定义 shadow atlas。
- 主 atlas row packing。
- Spot/point light slice 组织。
- Second directional atlas 与 cascades。
- 每帧 light/slice 参数发布。
- Receiver side attenuation 函数。
- Debug atlas/attenuation view。
- Caster layer mask 和 Scene/Game View 开关。

## 需要重建的部分

- RenderGraph 资源声明方式。
- Shader property 命名。
- Debug resource id。
- Feature 与 HoURP resource registry 的关系。
- 与 OIT receiver 的一致性。
- 全局状态 reset 规则。

## 暂缓内容

- 旧实现中依赖固定材质顺序或固定 feature 顺序的行为。
- 与 lilToon 具体 property 绑定的材质分支。
- PCSS/soft shadow 质量完全等价。
- 高级 packing 和 atlas cache。
- 透明 alpha shadow 的兼容策略。
- 多平台宏和所有历史 URP 版本兼容。

## 迁移判断表

| 旧概念 | 第十三步处理 | 说明 |
| --- | --- | --- |
| Shadow atlas | 保留概念，重建资源契约 | 使用 `ShadowCast.Atlas` 和 `_HoUrpShadowCastAtlas` |
| Atlas packer | 保留概念，重建实现 | 主 atlas row packing 是本阶段必做 |
| Spot shadows | 保留能力 | 每光 1 slice |
| Point shadows | 保留能力 | 每光 6 faces，失败整光回滚 |
| Second directional atlas | 保留能力，重命名 | 使用 `_HoUrpShadowCastSecondDirectional*` |
| Light/slice data | 保留概念，重建布局 | 覆盖 punctual 与 second directional |
| `HoShadowCastAttenuation` | 改名重建 | 建议 `HoUrpSampleShadowCastAttenuation` |
| `_HoShadowCast*` | 不进入新 ABI | 仅文档/负向测试可出现 |
| Debug shader | 重建 | Debug 输出不作为生产 ABI |
| Controller | 视需要拆分 | 只保留能降低 feature 复杂度的部分 |
| PCSS 参数 | 分层迁移 | 结构与参数位本阶段保留，质量调参后置 |

## 审查结论

旧实现给出的不是“最小单光阴影”，而是一个已经包含 atlas pack、多光源、额外方向光和 debug 的 ShadowCast 子系统。第十三步应迁移这条能力基线，同时把命名、资源生命周期和 Shader ABI 改成 HoURP 自己的契约。

