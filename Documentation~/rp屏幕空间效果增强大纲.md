# RP 屏幕空间效果增强大纲

> 本大纲独立于第六步 SSS 最小闭环。
>
> 第六步仍优先完成 `Aov.SssSource -> Sss.Source -> Sss.Diffusion -> Camera.Color` 的基础链路；本增强大纲记录后续如何把 SSS、SSR、AO、透明前后处理等屏幕空间能力做强。

---

## 0. 参考仓库

新增参考仓库：

```text
D:\Unity_Fork\Unity-ScreenSpaceReflections-URP
https://github.com/JoshuaLim007/Unity-ScreenSpaceReflections-URP
commit: e86e0be
license: MIT
```

定位：

- 只作为屏幕空间技术参考。
- 暂不作为 HoURP package dependency。
- 暂不复制实现代码。
- 后续若改写 substantial code，必须补第三方来源说明。

核心文件：

```text
Runtime/LimSSR.cs
Runtime/DepthPyramid.cs
Shaders/ssr_shader.shader
Shaders/HiZ_shader.compute
Shaders/Common.hlsl
Shaders/NormalSample.hlsl
```

---

## 1. 为什么另开增强大纲

SSS 第六步需要证明的是新 RP 契约是否闭环，而不是一次做完整高质量屏幕空间库。

如果把以下能力塞进第六步，会拖慢基础迁移：

- half / quarter resolution。
- bilateral upsample。
- shared depth pyramid。
- Hi-Z trace。
- temporal accumulation。
- quality preset。
- shared screen-space debug。
- SSR / SSS / AO 之间的公共基础设施。

这些能力应该作为增强路线独立规划，等基础资源、debug 和 RenderGraph 依赖稳定后再接入。

---

## 2. 增强目标

长期目标不是“给 SSS 加一个技巧”，而是形成一套可复用的屏幕空间基础设施：

```text
Shared.DepthPyramid
Shared.HalfResolutionColor
Shared.HalfResolutionDepthNormal
Shared.BilateralUpsample
Shared.TemporalHistory
Shared.ScreenSpaceDebug
```

可复用消费者：

```text
SubsurfaceScattering
ScreenSpaceReflection
ScreenSpaceAmbientOcclusion
ImagePost AOV rules
Stylized blur / glow / iris effects
Transparent pre/post composite
```

---

## 3. 可从 SSR 参考仓库借鉴的内容

### Renderer Feature 分层

参考仓库把主 SSR 和 Depth Pyramid 拆成两个 feature。

HoURP 后续可以采用类似边界：

```text
SharedScreenSpaceResourcesRendererFeature
  -> Depth Pyramid
  -> Half-resolution depth/normal
  -> common history resources

SubsurfaceScatteringRendererFeature
  -> consumes shared resources when enabled

ScreenSpaceReflectionRendererFeature
  -> consumes shared resources when enabled
```

### 工作分辨率

参考仓库通过 global scale / inverse scale 管理工作分辨率。

HoURP 后续应改为显式 settings + shader property：

```text
_HoUrpScreenSpaceScale
_HoUrpScreenSpaceInvScale
```

并禁止各 feature 私自定义互不兼容的 downsample 规则。

### Depth Pyramid

参考仓库使用 power-of-two padding 和 texture array slice 保存 pyramid。

HoURP 可借鉴：

- 每个 mip/slice 的实际尺寸与 padded 尺寸分开记录。
- Debug 能指定 slice 可视化。
- depth pyramid 作为共享资源，而不是 SSR 私有资源。

### Debug

参考仓库提供 depth pyramid debug slice。

HoURP 后续应统一进入 DebugView registry：

```text
Shared.DepthPyramidSlice0
Shared.DepthPyramidSliceN
SSS.HalfSource
SSS.BilateralWeight
SSR.HitMask
SSR.RaySteps
```

---

## 4. 不直接借鉴的内容

- 不把 SSR ray marching 逻辑塞进 SSS。
- 不沿用 `_GBuffer2` / `_CameraDepthTexture` 这类隐式全局读取作为新契约。
- 不沿用旧式 `CommandBuffer.Blit` 组织方式。
- 不把参考仓库 shader property 名变成 HoURP ABI。
- 不用 Hi-Z trace 替代 SSS diffusion。
- 不在第六步引入 compute depth pyramid。

---

## 5. 对 SSS 的增强路线

基础 SSS 完成后，增强顺序建议如下：

```text
Phase A. Half-resolution SSS
Phase B. Depth/normal-aware bilateral upsample
Phase C. Shared depth pyramid
Phase D. Profile-driven diffusion quality
Phase E. Temporal stabilization
Phase F. Transmission / thickness enhancement
```

### Phase A. Half-resolution SSS

目标：

- `Sss.Source` 或 `Sss.Diffusion` 支持 half resolution。
- 降低 diffusion 成本。
- 保持 full-resolution composite。

不做：

- temporal。
- transmission。
- profile asset。

### Phase B. Bilateral Upsample

目标：

- 使用 `Aov.NormalDepth` 或 shared half depth/normal 做边缘保护。
- 防止皮肤边缘和背景互相污染。

关键参数：

```text
depthTolerance
normalTolerance
edgeStrength
```

### Phase C. Shared Depth Pyramid

目标：

- 让 SSS、SSR、AO 等共享 depth pyramid。
- 避免每个 feature 重复构建深度层级。

第一版只服务 debug / coarse rejection，不急于绑定具体算法。

### Phase D. Profile-driven Diffusion

目标：

- 从固定 radius 走向 profile-driven radius / tint / falloff。
- `Material.SssProfile` 不再只是 debug 值，而是能索引运行时 profile 参数。

### Phase E. Temporal Stabilization

目标：

- 减少低分辨率 diffusion 和 screen-space 采样造成的闪烁。
- 引入 history resource 之前，必须先明确 motion vector / invalidation 契约。

### Phase F. Transmission

目标：

- 把旧 HoSSS transmission 作为独立增强项迁移。
- 不和基础 SSS diffusion 混成一个不可拆的 pass。

---

## 6. 对 SSR 的长期位置

SSR 不应被当成 SSS 的一部分。

它可以作为未来独立 feature：

```text
ScreenSpaceReflection
```

可能消费：

```text
Camera.Color
Camera.Depth
Aov.NormalDepth
Material.Smoothness or SurfaceData-derived reflection mask
Shared.DepthPyramid
```

可能输出：

```text
Ssr.Reflection
Ssr.HitMask
Ssr.CompositeWeight
```

但这属于后续增强阶段，不进入第六步。

---

## 7. 文档与实现规则

- 第六步文档只保留 SSS 最小闭环，不塞增强算法。
- 本大纲记录所有“可强化但暂不实现”的屏幕空间能力。
- 增强实现前必须先补资源契约文档，再写代码。
- 每个新增共享资源都必须进入 registry、RenderGraph declaration、DebugView 和测试。
- 引入参考仓库代码片段前必须补 license / notice。

---

## 8. 当前结论

`Unity-ScreenSpaceReflections-URP` 可以强化 HoURP 的屏幕空间基础设施思路，尤其是：

- depth pyramid。
- downsample / render scale。
- debug slice。
- transparent 前 composite。
- 屏幕空间 quality settings。

但它不改变第六步目标。第六步仍只做可验证的 SSS 基础闭环，增强项在本大纲中排队。
