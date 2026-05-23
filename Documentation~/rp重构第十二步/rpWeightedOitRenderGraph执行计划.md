# Weighted OIT RenderGraph 执行计划

## 目标

实现一个最小 `WeightedOitRendererFeature`，只支持 RenderGraph path。

功能链路：

```text
OpaqueCopy
Clear
Accumulation
Composite
Reset
```

第一版不维护 compatibility path，不维护 persistent RTHandle 主路径。

## 文件规划

建议新增：

```text
Runtime/OIT/
  WeightedOitRendererFeature.cs
  WeightedOitSettings.cs
  WeightedOitRenderGraphResources.cs
  WeightedOitShaderConstants.cs
```

Shader：

```text
Runtime/Shaders/Hidden/HoURP/OIT/WeightedComposite.shader
```

## Settings

第一版字段：

```csharp
[SerializeField] bool enabled = true;
[SerializeField] LayerMask layerMask = -1;
[SerializeField] int renderQueueMin = 2500;
[SerializeField] int renderQueueMax = 5000;
[SerializeField] float weight = 1.0f;
[SerializeField] float alphaClipThreshold = 0.001f;
```

不做：

```text
renderScale
custom composite material override
debug overlay selector
per camera persistent target cache
```

## RendererFeature 创建

建议显示名：

```csharp
[DisallowMultipleRendererFeature("HoURP Weighted OIT")]
```

`Create()`：

- 创建 composite material。
- 创建 pass 实例。
- 设置默认 pass event。

`AddRenderPasses()`：

- disabled 时不 enqueue。
- composite material 缺失时不 enqueue，并发 warning。
- 按顺序 enqueue：

```text
resetPass
opaqueCopyPass
clearPass
accumulationPass
compositePass
finalResetPass
```

注意：如果 `RecordRenderGraph` 能在同一个 pass 内串联部分步骤，仍建议第一版保留清晰 pass 边界，方便 RenderGraph viewer 验证。

## Pass Event 建议

第一版建议：

```text
Reset: BeforeRenderingTransparents - 2
OpaqueCopy: BeforeRenderingTransparents - 1
Clear: BeforeRenderingTransparents
Accumulation: BeforeRenderingTransparents + 1
Composite: AfterRenderingTransparents
FinalReset: AfterRenderingTransparents + 1
```

如果 URP enum 限制不允许精细偏移，使用最接近的合法 event，并在实现注释中写清楚。

核心目标：

- Opaque copy 在 transparent draw 前。
- Accumulation 在 composite 前。
- Composite 在 ScreenPost / ImagePost 前或按 renderer feature 顺序可解释。

## RenderGraph Resources

建议 `WeightedOitRenderGraphResources : ContextItem`：

```csharp
public TextureHandle OpaqueColor;
public TextureHandle Accumulation;
public TextureHandle Revealage;
public TextureHandle CompositeSource;

public override void Reset()
{
    OpaqueColor = TextureHandle.nullHandle;
    Accumulation = TextureHandle.nullHandle;
    Revealage = TextureHandle.nullHandle;
    CompositeSource = TextureHandle.nullHandle;
}
```

不要缓存跨帧 TextureHandle。

## OpaqueCopy Pass

输入：

```text
UniversalResourceData.activeColorTexture
```

输出：

```text
Oit.OpaqueColor
```

规则：

- 创建 camera color matching descriptor。
- 使用 `renderGraph.AddBlitPass` 或 raster copy。
- 复制到独立 texture。
- 可通过 `SetGlobalTextureAfterPass` 发布 `_HoUrpOitOpaqueColorTexture`，但资源生命周期仍以 RenderGraph 为准。

禁止：

- 直接把 camera color 发布为 `_HoUrpOitOpaqueColorTexture`。

## Clear Pass

输出：

```text
Oit.Accumulation
Oit.Revealage
```

Clear value：

```text
Accumulation = 0
Revealage = 1
```

实现方式：

- 创建 accumulation/revealage texture。
- raster pass 设置两个 render attachments。
- clear render target。
- 或使用 texture desc clearBuffer / clearColor，如果 URP/RenderGraph 行为可靠。

验收重点：

- RenderGraph viewer 能看到这两个资源写入。
- `Revealage` 不是默认 clear zero。

## Accumulation Pass

输入：

```text
camera depth when valid
Oit.OpaqueColor optional global binding
```

输出：

```text
Oit.Accumulation
Oit.Revealage
```

Draw：

```text
ShaderTagId("HoUrpOitAccumulation")
FilteringSettings(renderQueueRange, layerMask)
SortingCriteria.CommonTransparent
```

Global state：

```text
_HoUrpOitActive = 1
_HoUrpOitWeight = settings.weight
_HoUrpOitAlphaClipThreshold = settings.alphaClipThreshold
```

注意：

- `AllowGlobalStateModification(true)` 只用于必要 shader gate。
- 不用旧 `_lilOITActive`。
- 如果 depth target 无效，第一版可以无 depth draw，但必须记录诊断或注释。

## Composite Pass

输入：

```text
camera color -> Oit.CompositeSource copy
Oit.Accumulation
Oit.Revealage
```

输出：

```text
camera color
```

步骤：

1. copy camera color to `Oit.CompositeSource`。
2. full-screen composite from source + accumulation + revealage to camera color。
3. reset `_HoUrpOitActive = 0`。

禁止：

- composite shader 同时读写 active camera color。
- 省略 `Oit.CompositeSource` 直接采样 camera color。

## Reset Pass

Reset pass 只负责：

```text
_HoUrpOitActive = 0
```

建议 begin/end 都 reset：

- begin reset 防止上帧遗留。
- end reset 防止后续 normal transparent / post pass 误判。

## Diagnostics

第一版可以用 warning：

- composite shader missing。
- invalid camera color。
- no accumulation pass tag matched is不容易从 C# 直接判断，可暂不做。

后续可加入 debug counter：

- OIT draw count。
- active resource list。

## 实现验收

RenderGraph viewer 里应该能看到：

```text
HoURP Weighted OIT Reset
HoURP Weighted OIT Opaque Copy
HoURP Weighted OIT Clear
HoURP Weighted OIT Accumulation
HoURP Weighted OIT Composite
HoURP Weighted OIT Final Reset
```

资源依赖：

```text
OpaqueCopy -> Oit.OpaqueColor
Clear -> Oit.Accumulation / Oit.Revealage
Accumulation -> Oit.Accumulation / Oit.Revealage
CompositeSource copy -> Oit.CompositeSource
Composite -> Camera.Color
```

没有：

```text
_lilOIT*
lilToonOIT
Hidden/lilToon/URP/WeightedOITComposite
```

