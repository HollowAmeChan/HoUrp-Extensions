# HoNpr 材质消费 HoURP 验收清单

> 本清单用于第十四步手动 Unity 验收。目标是确认 HoNpr generated 材质真的消费 HoURP runtime，而不是只通过静态 shader 文本看起来接入。

## 1. Renderer Data

开启以下 RenderFeature：

- AOV Output
- Subsurface Scattering
- Weighted OIT
- ShadowCast
- AOV / SSS / ShadowCast debug view

ShadowCast 设置：

- 开启自动收集可见场景灯。
- 场景至少有一个 Spot 或 Point light。
- 如需验证 second directional，添加一个非 main directional light。
- 打开 Inspector 的运行时参与报告，确认灯被系统收集。

## 2. 材质选择

至少验证以下 HoNpr shader：

- `HoNpr/Character_LilToon_Standard`
- 真 SSS skin preset，例如 `HoNpr/Character_LilToon_Skin_SSS`
- `HoNpr/Character_LilToon_Transparent`
- `HoNpr/Environment_LilPBR`

对照材质：

- `HoNpr/Character_LilToon_Skin_fSSS`

注意：`Skin_fSSS` 只验证 forward/fake SSS，不作为真 HoURP screen-space SSS runtime 的验收终点。

## 3. AOV 验收

步骤：

1. 给不透明角色物体使用 HoNpr generated shader。
2. 开启 AOV Debug。
3. 切换 material class / material custom / surface data / SSS source 相关 debug view。

通过标准：

- `HoUrpAovOutput` pass 被绘制。
- material class / custom 能被 AOV Debug 观察到。
- 真 SSS preset 能写入 `Aov.SssSource`。
- 关闭对应材质或切换无 SSS preset 后，SSS source 贡献消失或明显变化。

失败判定：

- 只看到 forward/fake SSS 颜色变化，但 AOV SSS source 没有变化。
- 需要旧 `HoAOV` / `HoAOVSSS` pass 才能看到输出。

## 4. SSS Runtime 验收

步骤：

1. 使用真 SSS skin preset。
2. 开启 HoURP `SubsurfaceScatteringRendererFeature`。
3. 开启 SSS Debug 或观察 SSS composite。
4. 调整 SSS source color / weight / thickness / profile 参数。

通过标准：

- HoURP SSS runtime 对参数变化有响应。
- `Aov.SssSource` 被消费后能影响 SSS source / diffusion / composite。
- 关闭 SSS runtime 后，screen-space SSS 消失；如果 preset 同时含 `ForwardThinSss`，只保留 forward/fake SSS lobe。

失败判定：

- `Character_LilToon_Skin_fSSS` 的 forward transmission 被误认为 SSS runtime。
- SSS Debug 没有任何来自 HoNpr 材质的 source。

## 5. ShadowCast Receiver 验收

步骤：

1. 使用 HoNpr generated shader 的不透明角色和环境物体。
2. 开启 ShadowCast Debug Atlas。
3. 确认 `ShadowCaster` pass 写入 atlas。
4. 切回 Forward 观察 HoCast receiver 对 HoNpr 材质的影响。
5. 关闭 ShadowCast feature，观察阴影是否清理。

通过标准：

- HoNpr 物体可进入 HoShadowCast atlas。
- Forward 结果受 HoCast attenuation 影响。
- HoCast 写入 `HoNprLightingContext.hoShadow`，不覆盖 URP main light shadow。
- 关闭 ShadowCast 后没有上一帧残留。

失败判定：

- HoNpr assembly 仍使用 `HoNprResolveHoShadowReceiver(lighting, 1.0h)`。
- HoNpr generated shader 裸采 `_HoUrpShadowCastAtlas`。
- HoCast 与 URP main light shadow 无法区分。

## 6. OIT 验收

步骤：

1. 使用 `HoNpr/Character_LilToon_Transparent`。
2. 场景放置两个以上深度交错透明物体。
3. 开启 Weighted OIT。
4. 观察 OIT accumulation / composite。

通过标准：

- `HoUrpOitAccumulation` pass 被绘制。
- 透明交错顺序比普通透明更稳定。
- 关闭 Weighted OIT 后结果发生可解释变化。

失败判定：

- 需要旧 `lilToonOIT` pass。
- generated shader 使用旧 `_lilOIT*` 属性。

## 7. 旧 ABI 扫描

必须没有：

- `HoAOV`
- `HoAOVSSS`
- `lilToonOIT`
- `_lilOIT`
- `_lilHoAov`
- `_HoAov`

允许出现：

- `LilToon` 作为来源型 block / preset 名称。
- `LilPBR` 作为来源型环境材质名称。

## 8. 完成条件

第十四步手动验收通过需要同时满足：

- 真 SSS preset 能被 HoURP SSS runtime 消费。
- `Skin_fSSS` 与真 SSS 验收路径分开。
- HoShadowReceiver 使用真实 HoURP ShadowCast sampling。
- AOV / OIT / ShadowCaster 都走新 HoURP pass tag。
- HoNpr generated shader 不回流旧 ABI。
