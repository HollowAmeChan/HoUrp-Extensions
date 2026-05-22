# RP 重构第六步执行计划

> 第六步目标：在第五步 `Aov.SssSource` / `Shading.SssWeight` 输入闭环之后，迁移出 **屏幕空间 SSS 最小闭环**。
>
> 本阶段只做 `SubsurfaceScattering` 的 source、diffusion、composite 最小链路：读取已注册 AOV 输入，生成 SSS 中间资源，并在透明前合成回 camera color。暂不迁移 transmission gather / blur，不迁移旧 `HoAOVSSS` LightMode，不接旧材质包。

---

## 0. 前置状态

第一到第五阶段已经完成：

- `Aov.MaskId` / `Aov.NormalDepth` 最小 AOV 输出、DebugView、SemanticPost 消费。
- `Aov.ObjectCustom0_3` / `Aov.ObjectCustom4_7` 对象语义输出、debug 和显式消费。
- `Aov.SurfaceData` / `Aov.MaterialCustom0_3` 材质语义输出、debug 和显式消费。
- `Aov.SssSource` 输出：
  - `RGB = Shading.SssSourceColor`
  - `A = Shading.SssWeight`
  - clear = `(0,0,0,0)`
- `MaterialSemanticAuthoring` 已提供迁移期 `sssSourceColor / sssWeight`。
- `AOV Debug AllRegistered` 已支持置顶、去重、红边框、无间距、tile label 和自适应标签。

第六步必须继续遵守：

- 不复制旧 `HoSubsurfaceScatteringRendererFeature.cs`。
- 不迁移旧 compatibility path。
- 不把 `_lilHoSSS*` / `_HoSSS*` 提升为新长期 ABI。
- 不修改 `lilToon` / `lilPBR`。
- 不接旧 `HoAOVSSS` LightMode。
- 不把第六步扩成完整 HDRP 级 SSS。

---

## 1. 为什么第六步只做 source / diffusion / composite

第五步已经证明 SSS 输入可生产、可 debug、可显式消费。下一步要验证的是：这些输入能否被一个独立屏幕空间 feature 读取，并产生可见的 SSS 结果。

旧 HoSSS 包含：

```text
Source
Diffusion X/Y 或 Burley disk diffusion
Transmission Gather
Transmission Blur X/Y
Composite
Profile arrays
Debug modes
```

如果一次迁完整，会同时引入过多问题：

- profile 数据结构与材质系统边界。
- diffusion kernel 选择。
- transmission 艺术增强与主 SSS 的边界。
- half/quarter resolution。
- bilateral / temporal filter。
- camera color、diffuse lighting source、AOV source 的差异。

第六步先做最小 `ScreenSss` 闭环：

```text
AOV inputs -> Sss.Source -> Sss.Diffusion -> camera color composite
```

这样能先冻结：

- `SubsurfaceScattering` feature descriptor。
- SSS 中间资源命名和生命周期。
- RenderGraph resource dependency。
- transparent 前合成时机。
- SSS debug view 的基本路径。

---

## 2. 本阶段新增契约

新增 Feature：

```text
SubsurfaceScattering
```

新增资源：

```text
Sss.Source
Sss.Diffusion
```

可选临时资源：

```text
Sss.Temp
```

本阶段消费已有资源：

```text
Aov.MaskId
Aov.NormalDepth
Aov.SurfaceData
Aov.SssSource
Camera.Color
```

本阶段消费已有语义：

```text
Object.MaskWeight
Geometry.WorldNormal
Geometry.LinearDepth
Material.SssProfile
Material.Thickness
Material.Curvature
Shading.SssSourceColor
Shading.SssWeight
```

新增 DebugView：

```text
SSS.Mask
SSS.Source
SSS.Diffusion
SSS.CompositeWeight
```

延后 DebugView：

```text
SSS.Transmission
SSS.TransmissionGate
SSS.TransmissionDirection
SSS.ProfileRadius
```

---

## 3. 资源编码建议

### `Sss.Source`

```text
RGB = source color selected from Aov.SssSource.rgb
A   = SSS participation weight
```

第一版 source 不从 camera color fallback 猜颜色。没有 `Aov.SssSource` 或 alpha 为 0 时，保持 `(0,0,0,0)`。

### `Sss.Diffusion`

```text
RGB = diffused source color
A   = composite weight
```

第一版 composite weight 至少应包含：

```text
Aov.SssSource.a
Object.MaskWeight
Material.Thickness
valid normal/depth gate
```

`Material.SssProfile` 第一版只参与 profile gate / debug，可先使用固定默认 profile 参数，不引入完整 profile asset。

---

## 4. Pass 链路

建议第一版 pass：

```text
SubsurfaceScatteringRendererFeature
  -> SSS Source
       reads:  Aov.MaskId, Aov.NormalDepth, Aov.SurfaceData, Aov.SssSource
       writes: Sss.Source

  -> SSS Diffusion
       reads:  Sss.Source, Aov.NormalDepth, Aov.SurfaceData
       writes: Sss.Diffusion

  -> SSS Composite
       reads:  Camera.Color, Sss.Diffusion, Aov.NormalDepth, Aov.SurfaceData
       writes: Camera.Color
```

第一版可以用 single full-screen material shader，多 pass index：

```text
0 = Source
1 = Diffusion
2 = Composite
```

第六步不做：

- transmission gather。
- transmission blur。
- dedicated profile arrays。
- compute shader。
- temporal filter。
- half/quarter resolution。

---

## 5. Pass 时机

默认：

```text
AovOutput               AfterRenderingOpaques
SubsurfaceScattering    BeforeRenderingTransparents
Transparent / OIT       after SSS composite
SemanticPostProcess     after transparents
DebugComposite          after post / selected debug event
```

约束：

- `SubsurfaceScattering` 必须晚于 `AovOutput`。
- `SubsurfaceScattering` composite 第一版应早于 transparents，避免皮肤扩散结果被透明顺序污染。
- 如果 camera target 是 backbuffer，composite pass 需要遵循当前 URP RenderGraph 的 active target 约束。

---

## 6. Settings 第一版

新增最小设置：

```text
enabled
renderInSceneView
strength
radius
depthTolerance
normalTolerance
sourcePreserve
debugMode
```

`debugMode` 第一版：

```text
Off
Mask
Source
Diffusion
CompositeWeight
ProfileId
Thickness
```

延后：

```text
quality presets
render scale
profile arrays
transmission settings
blend mode variants
```

---

## 7. Debug 与 AllRegistered

第六步新增的 SSS debug views 必须进入统一 registry。

要求：

- `SSS.Source` 显示 `Sss.Source.rgb`。
- `SSS.Diffusion` 显示 `Sss.Diffusion.rgb`。
- `SSS.Mask` 显示参与 mask。
- `SSS.CompositeWeight` 显示 composite alpha/weight。
- `AllRegistered` 自动包含新增 SSS 中间资源。
- AllRegistered 去重逻辑继续按 `SourceResource + SourceSemantic` 生效。
- tile 红边框、标签、自适应字号继续可用。

---

## 8. 执行顺序

```text
Step 1. 写第六阶段实现边界审查
Step 2. 写 SSS 中间资源与编码审查
Step 3. 写 SSS profile 最小运行时参数审查
Step 4. 写 Source / Diffusion / Composite 链路审查
Step 5. 写 SSS Debug 与 AllRegistered 闭环审查
Step 6. 扩展 runtime contract registry
Step 7. 新增 SSS RenderGraph resource declaration
Step 8. 新增 SubsurfaceScatteringRendererFeature 最小 RenderGraph pass
Step 9. 新增 Hidden/HoURP/SSS shader
Step 10. 接入 SSS DebugView 和 AOV Debug AllRegistered
Step 11. 补 tests、验收清单、未决项登记
```

---

## 9. 自动测试建议

- registry count / link tests。
- `SubsurfaceScattering` feature descriptor tests。
- `Sss.Source` / `Sss.Diffusion` resource descriptor tests。
- shader property mapping tests。
- DebugView source/resource mapping tests。
- `SubsurfaceScattering` consumed resource declaration tests。
- `AovDebug AllRegistered` 去重 / tile label property tests。
- `git diff --check`。

---

## 10. 手动 Unity 验收

挂载：

1. `HoURP AOV Output`
2. `HoURP Subsurface Scattering`
3. `HoURP AOV Debug`

测试对象：

1. `ObjectSemanticAuthoring`
2. `MaterialSemanticAuthoring`

设置：

```text
sssSourceColor = skin-like non-black color
sssWeight = 1
sssProfile = non-zero
thickness = non-zero
```

期望：

| 场景 | 期望 |
| --- | --- |
| SSS off | 画面与第五阶段一致 |
| SSS Source debug | 显示 `Aov.SssSource` 派生出的 `Sss.Source` |
| SSS Diffusion debug | 显示被扩散的柔化结果 |
| SSS Composite | opaque 皮肤区域出现可控柔化/染色，不影响 alpha 为 0 的区域 |
| AllRegistered | 自动出现 `SSS.Source` / `SSS.Diffusion` tile，红框和标签正常 |
| 空值区域 | 天空/未覆盖区域保持无 SSS 贡献 |

---

## 11. 本阶段不做项

- 不做旧 `HoAOVSSS` LightMode 接入。
- 不做旧材质属性迁移。
- 不做 transmission gather / blur。
- 不做 profile asset。
- 不做 HDRP Burley compute 完整迁移。
- 不做 half/quarter resolution。
- 不做 temporal / bilateral filter backend 抽象。
- 不做 transparent SSS。
- 不做 OIT / CharacterSpecialization 排序重构。

---

## 12. 第六阶段未决项预登记

| Item | 本阶段处理方式 | 后续阶段 |
| --- | --- | --- |
| 完整 Diffusion Profile asset | 不做，只保留最小 settings / 默认 profile | Profile registry 阶段 |
| HDRP Burley compute | 不做完整 compute，可先用 full-screen shader 近似 | SSS quality 阶段 |
| Transmission | 不做 | 第七阶段或 HoSSS transmission 阶段 |
| Half-resolution SSS | 不做 | Filter / SSS 性能阶段 |
| Temporal / bilateral filter backend | 只做局部 depth/normal gate，不抽象 backend | Filter backend 阶段 |
| 旧材质 SSS 属性迁移 | 不做 | 新材质系统 / migration tool |
| Diffuse lighting source | 继续使用 `Aov.SssSource`，不从 camera color 猜 | Lighting source 阶段 |
| Transparent SSS | 不做 | Transparent / OIT 阶段 |

---

## 13. 第六阶段完成定义

第六步完成时，必须能回答：

- `SubsurfaceScattering` feature 在 registry 中声明生产/消费了什么。
- `Sss.Source` 和 `Sss.Diffusion` 的格式、clear、生命周期是什么。
- SSS pass 为什么只读新 RP 注册资源，而不是旧全局纹理。
- Source / Diffusion / Composite 的 RenderGraph 依赖如何显式声明。
- DebugView 如何找到 SSS 中间资源。
- 为什么 transmission、profile asset、half-resolution 和旧材质迁移被延后。

---

## 14. 第六阶段实际完成总结

本阶段实际落地的是 `SubsurfaceScattering` 的最小 RenderGraph 闭环，而不是旧 HoSSS 的完整迁移。

已完成：

- 新增 `SubsurfaceScattering` feature descriptor，显式声明消费 AOV 输入并生产 `Sss.Source` / `Sss.Diffusion`。
- 新增 `HoUrpSssResourceDeclaration`，让 SSS 中间纹理由统一 RenderGraph resource declaration 创建和登记。
- 新增 `Hidden/HoURP/SSS/SubsurfaceScattering` shader，采用 3 个 pass：
  - `SssSource`：从 `Aov.MaskId`、`Aov.NormalDepth`、`Aov.SurfaceData`、`Aov.SssSource` 生成 `Sss.Source`。
  - `SssDiffusion`：读取 `Sss.Source`、AOV normal/surface 和 source color copy，生成 `Sss.Diffusion`。
  - `SssComposite`：读取 source color copy、`Sss.Diffusion` 和 AOV 边界信息，写回 camera color。
- 新增 SSS shader property id 与测试覆盖，包括 `SssDiffusionTexture`、`SourceColorTexture` 和 8 槽 profile 参数。
- `Sss.Source.a` 固定为参与权重，`Sss.Diffusion.a` 固定为最终 composite weight。
- `SSS.Mask`、`SSS.Source`、`SSS.Diffusion`、`SSS.CompositeWeight` 已进入统一 DebugView / AllRegistered 路径。
- 迁入旧 HoSSS 的最小 profile 分组思想：8 个运行时 profile 槽位控制 diffusion color、diffusion radius、source preserve 和 thickness scale。

没有完成，也不应算作本阶段范围：

- 未迁移 transmission gather / blur。
- 未引入 profile asset / registry。
- 未做 half-resolution、temporal、bilateral filter backend。
- 未接旧 `HoAOVSSS` LightMode。
- 未兼容旧 `_lilHoSSS*` / `_HoSSS*` ABI。

## 15. 第六阶段关键调试结论

本阶段暴露了两个 RenderGraph 使用问题。

第一，`RenderGraphUtils.BlitMaterialParameters` 只会自动声明主 source texture。shader 额外采样的 AOV normal/surface/source color 等输入，必须由同一链路显式声明。当前做法是通过独立 RenderGraph pass `UseTexture` 并 `SetGlobalTextureAfterPass` 发布这些输入，确保 shader 读取的资源进入 RenderGraph 依赖。

第二，camera color 不能作为 live render attachment 又通过全局纹理被后续 pass 间接读取。现场错误为：

```text
In pass 'DrawTransparentObjects' when trying to use resource '_CameraTargetAttachment' ...
UseTexture is called on a texture that is already used through SetRenderAttachment.
```

根因是 SSS diffusion 曾把 `resourceData.activeColorTexture` 直接发布为 `_HoUrpSourceColorTexture`。URP 的 `DrawTransparentObjects` pass 会 `UseAllGlobalTextures(true)`，于是透明绘制在写 `_CameraTargetAttachment` 的同时又声明读取同一张 texture。修正方式是：

```text
activeColorTexture -> explicit color copy -> _HoUrpSourceColorTexture
```

Diffusion 和 Composite 都读取 copy，Composite 再写回 `activeColorTexture`。后续所有读写 camera color 的 full-screen pass 都按这个规则处理。

`ZBinningJob` safety error 当前按连带错误处理：它出现在 RenderGraph 录制异常之后，优先复测资源冲突是否消失；如果资源冲突修复后仍单独复现，再作为 ForwardLights / job lifecycle 独立问题处理。
