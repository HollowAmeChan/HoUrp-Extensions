# RP 第十一阶段 ImagePost 最小搬迁执行

## 目标

以旧 Shoost 的 final image stack 为来源，建立新 RP 的 ImagePost 最小纯图像链路：

```text
Camera color copy
  -> ImageChain
  -> ImagePost single-pass effect 0
  -> ImagePost single-pass effect 1
  -> final copy back
```

本阶段不迁移完整旧 Shoost catalog，只验证：

- effect descriptor 是事实来源。
- single-pass effect 走 ImageChain 双缓冲。
- effect 启停会动态改变 plan / request。
- AOV composite 是显式可选能力，不是默认能力。

## 旧实现参考

旧代码定位：

```text
lilToon-URP-Extensions/Runtime/ShoostPostProcessing
  ShoostPostProcessRendererFeature.cs
  ShoostPostProcessLayer.cs
  ShoostPostProcessEffect.cs
  ShoostPostProcessEffectDescriptor.cs
  ShoostPostProcessEffectRegistry.cs
  Renderer/ShoostPostProcessPass.cs
  Renderer/ShoostPostProcessPass.Aov.cs
  Renderer/EffectPipeline/ShoostPostProcessPass.EffectDispatch.cs
  Renderer/EffectPipeline/ShoostPostProcessAovSupport.cs
  Renderer/Effects/*.cs
```

旧实现可提供：

- effect catalog。
- runtime order。
- single/multi/stateful 分类参考。
- 哪些 effect 支持 AOV composite。
- shader 行为样本。

旧实现不能提供：

- 新 descriptor ABI。
- 新资源生命周期。
- 新 shader property ABI。
- 新 temp texture 名。

## 第一版 effect 分类

先把旧 Shoost effect 分为以下状态：

| Category | 第十一阶段处理 |
| --- | --- |
| `SinglePass` | 可选 1 个做 prototype |
| `MultiPass` | 登记 planned，不迁移 |
| `Stateful` | 登记 planned，不迁移 |
| `AovComposite` | 只做一个 prototype |
| `Removed` | 保持 removed |

建议第一版 prototype：

```text
ImagePost.ColorAdjustPrototype
ImagePost.LayerBlitPrototype
ImagePost.VignettePrototype
```

选择标准：

- 只需要 `PrimaryImage`。
- 不需要 history。
- 不需要 pyramid。
- 不需要多 pass。
- shader 简单。

## Descriptor

```text
ImagePostEffectDefinition
  Id
  RuntimeOrder
  ExecutionKind
  SupportsAovComposite
  RequiredInputs
  ResourcePolicy
  DefaultShader
  LegacySource
  Status
```

示例：

```text
Id: ImagePost.ColorAdjustPrototype
RuntimeOrder: 100
ExecutionKind: SingleImagePass
SupportsAovComposite: false
RequiredInputs:
  - PrimaryImage
ResourcePolicy:
  - ImageChainWork
Status: Prototype
```

```text
Id: ImagePost.AovCompositePrototype
RuntimeOrder: 150
ExecutionKind: SemanticImagePass
SupportsAovComposite: true
RequiredInputs:
  - PrimaryImage
  - Aov.MaskId
ResourcePolicy:
  - ImageChainWork
Status: Prototype
```

## Shader

建议新增：

```text
Runtime/Shaders/Image/HoUrpImageLayerBlit.shader
Runtime/Shaders/Image/HoUrpImageColorAdjust.shader
Runtime/Shaders/Image/HoUrpImageAovComposite.shader
```

第一版也可以只用一个 `HoUrpImageLayerBlit.shader`，用参数控制 tint / brightness。

要求：

- 使用 `_HoUrp*` property。
- 不使用旧 `_lilShoost*`。
- 不读取旧 AOV 全局名。
- AOV composite shader 的 AOV texture 由 RenderGraph pass 显式绑定。

## 执行路径

### Pure image path

```text
ImageChain.Begin(cameraColorCopy)

ImagePost.ColorAdjustPrototype:
  read ImageChain.Current
  write ImageChain.Next
  swap

ImagePost.LayerBlitPrototype:
  read ImageChain.Current
  write ImageChain.Next
  swap

ImageChain.End(copy current -> camera color)
```

### Optional AOV composite path

```text
ImageChain.Current
  + Aov.MaskId
  -> ImagePost.AovCompositePrototype
  -> ImageChain.Next
```

规则：

- `SupportsAovComposite=false` 的 effect 不能声明 AOV input。
- AOV composite 只提供 mask / mix，不提供 ScreenPost rule language。
- 如果需要 material class / object custom complex rule，应使用 ScreenPost。

## 实施步骤

### Step 1. 建立 ImagePost prototype catalog

先 hard-code 1 到 3 个 prototype definition。

测试：

- catalog id 稳定。
- runtime order 稳定。
- status 可查询。
- old source 可查询。

### Step 2. 建立 Volume / settings 到 layer snapshot 的转换

第一版可以很薄：

```text
ImagePostPrototypeVolume
  Enabled
  EffectId
  Intensity
  Color
```

要求：

- Volume 只产生 `PostLayerDefinition`。
- Volume 不决定 pass / resource。

### Step 3. 接 ImageChain

把 active `SingleImagePass` 转为 `ImagePassDescriptor`。

要求：

- 多个 active effect 共用 WorkA / WorkB。
- effect 禁用后 pass 数量减少。
- pass 顺序来自 descriptor / layer order。

### Step 4. 可选 AOV composite prototype

第一版只支持一种简单 mask：

```text
mask = object/group/material rule result or Aov.MaskId channel test
output = lerp(current, effected, mask * intensity)
```

如果 ScreenPost rule mask 已完成，ImagePost AOV composite 可以只消费 `ScreenPost.RuleMask`，避免重复 rule。

### Step 5. Debug

Debug view：

```text
ImagePost.ActiveEffects
ImagePost.LayerOutput
ImagePost.AovCompositeMask
ImageChain.PassList
```

## 自动测试

建议：

```text
Tests/Runtime/HoUrpImagePostEffectDescriptorTests.cs
Tests/Runtime/HoUrpImagePostPlannerTests.cs
```

测试项：

```csharp
ImagePostPrototypeCatalogHasStableRuntimeOrder()
DisabledImagePostEffectCreatesNoImagePass()
MultipleSinglePassEffectsShareImageChain()
ImagePostEffectDoesNotUseLegacyTempTextureNames()
ImagePostAovCompositeRequestsAovOnlyWhenEnabled()
ImagePostPureImageEffectCannotDeclareMaterialSemanticInput()
VolumeSnapshotDoesNotDecideExecutionKind()
```

## 手动验收

场景：

1. 开启 ImagePost prototype feature。
2. 添加两个 pure image effects。
3. 可选开启 AOV composite prototype。

检查：

- 两个 effect 可见。
- RenderGraph 中 WorkA / WorkB 交替。
- 禁用第二个 effect 后 pass 数减少。
- 开启 AOV composite 后才出现 AOV input。
- 关闭 AOV composite 后 AOV input 消失。
- 没有旧 `_lilShoost*`。

## 成功标准

- ImagePost 有最小 descriptor catalog。
- single-pass effect 走 ImageChain。
- effect 启停动态改变 plan。
- AOV composite 显式声明输入。
- ImagePost 没有接管 ScreenPost rule language。
- 没有 per-layer 独占 full-res RT。

## 风险点

- 因为旧 Shoost effect 多，过早迁移 catalog。
- 复用旧 effect dispatch switch，绕过 descriptor。
- AOV composite 默认挂到所有 effect。
- multi-pass effect 临时创建 RT。
- Volume 字段控制 pass 结构。
