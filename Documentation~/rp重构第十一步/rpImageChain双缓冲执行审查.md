# RP 第十一阶段 ImageChain 双缓冲执行审查

## 目标

为 ImagePost / ImageDomain 的线性全屏图像 pass 建立统一双缓冲执行器，避免每个 layer 创建独占同规格中间 RT。

基础模型：

```text
ImageChain.Begin(source)
  current = WorkA
  alternate = WorkB

pass 0:
  read current
  write alternate
  swap

pass 1:
  read current
  write alternate
  swap

ImageChain.End()
  copy current -> camera color / final target
```

## ImageChain 只解决什么

解决：

- 纯图像 pass 的 read/write 交换。
- 同规格工作纹理复用。
- source copy / final copy。
- debug 显示当前 read/write。

不解决：

- ScreenPost semantic rule。
- AOV / depth / normal 输入解析。
- history。
- pyramid。
- multi-output。
- complex blur local ping-pong。
- OIT composite。

需要这些能力的 effect 必须通过 `PostResourceRequest` 显式升级。

## 数据结构

### `ImageChainContext`

```text
ImageChainContext
  Source
  WorkA
  WorkB
  Current
  Alternate
  OriginalSource
  Descriptor
  PassIndex
  DebugName
```

字段规则：

| Field | 规则 |
| --- | --- |
| `Source` | camera color copy 或明确输入 |
| `WorkA` | frame transient |
| `WorkB` | frame transient |
| `Current` | 当前 read handle |
| `Alternate` | 当前 write handle |
| `OriginalSource` | 只有 effect 请求时才创建 |
| `PassIndex` | 每录制一个 image pass 后递增 |

### `ImagePassDescriptor`

```text
ImagePassDescriptor
  PassId
  EffectId
  LayerId
  Shader
  PassIndex
  RequiredInputs
  WritesToChain
  NeedsOriginalSource
  DebugView
```

第一版只支持：

```text
RequiredInputs:
  PrimaryImage
  OriginalSource optional

WritesToChain:
  true
```

如果需要 AOV / semantic input，应转成 `SemanticImagePass`，由 PostGraph 显式 request。

## RenderGraph 录制规则

### Begin

Begin 阶段必须保证后续 pass 不直接读取 live camera color。

推荐：

```text
CameraColor -> Copy -> Image.WorkA
Create Image.WorkB
Current = WorkA
Alternate = WorkB
```

如果 source 已经是独立 `TextureHandle`，可以 import / alias，但必须避免同一 pass 读写同一 handle。

### AddPass

每个 pass：

```text
Read  = context.Current
Write = context.Alternate

renderGraph.AddRasterRenderPass(...)
builder.UseTexture(Read)
builder.SetRenderAttachment(Write)
Record fullscreen draw
context.Swap()
```

禁止：

- pass 同时读写 `Current`。
- 读取未声明的 global texture。
- 写 camera color 同时读 camera color。
- 在 pass 内临时创建 destination。

### End

End 阶段：

```text
Final = context.Current
Copy Final -> camera color
```

如果后续系统需要 `Image.Final`，只能作为本 frame 的明确 output alias，不把 WorkA / WorkB 自身发布出去。

## Original Source

某些 effect 需要同时读取原始 source 和当前 filtered result。

规则：

- 只有至少一个 active effect 声明 `NeedsOriginalSource=true` 时创建。
- `OriginalSource` 来自 Begin source 的 copy。
- 不允许 effect 自己再 copy source。
- Debug 显示 owner list。

示例：

```text
EffectA: no original
EffectB: needs original

Begin:
  Source -> WorkA
  Source -> OriginalSource
```

## 资源数量约束

第一版验收目标：

| Active pure image pass count | Full-res work texture count |
| --- | --- |
| 0 | 0 或 1 copy，取决于是否需要 write back |
| 1 | WorkA + WorkB |
| 2 | WorkA + WorkB |
| N | WorkA + WorkB |

例外：

- original source：最多额外 1 张。
- local ping-pong：必须由 specific effect request。
- pyramid：不在第十一阶段实现。
- history：不在第十一阶段实现。

## Debug

Debug view 建议：

```text
ImageChain.Active
ImageChain.Current
ImageChain.WorkA
ImageChain.WorkB
ImageChain.OriginalSource
ImageChain.PassList
```

调试信息至少包含：

- chain id。
- pass count。
- current handle name。
- alternate handle name。
- final output target。
- original source 是否存在。

## 实施步骤

### Step 1. 纯 C# 状态机

先不接 RenderGraph，实现：

```text
Begin()
GetRead()
GetWrite()
Swap()
End()
```

测试：

```csharp
SwapAlternatesWorkAAndWorkB()
PassIndexIncrementsAfterSwap()
OriginalSourceNotAllocatedByDefault()
OriginalSourceAllocatedWhenRequested()
```

### Step 2. Resource resolver

把 `ImageChainWork` request 映射到 TextureDesc：

- full resolution。
- camera color format 或 HDR color format。
- clear policy 按 request。
- debug name 稳定。

### Step 3. 最小 shader / blit pass

新增或复用：

```text
Runtime/Shaders/Image/HoUrpImageLayerBlit.shader
```

第一版只做：

- sample primary image。
- apply color tint / intensity。
- output color。

### Step 4. ImagePost prototype 接入

用 2 个 single-pass layer 验证：

```text
Layer 0: tint
Layer 1: vignette or brightness
```

验收：

- 两个 layer 只使用 WorkA / WorkB。
- pass 顺序正确。
- final copy back 可见。

## 自动测试

建议：

```text
Tests/Runtime/HoUrpImageChainTests.cs
Tests/Runtime/HoUrpImageChainResourceTests.cs
```

测试项：

```csharp
ImageChainUsesTwoWorkTexturesForMultiplePasses()
ImageChainDoesNotExposeWorkTexturesAsPublicResources()
ImageChainRequestsOriginalOnlyWhenNeeded()
ImageChainRejectsReadWriteSameHandle()
ImageChainBuildsDeterministicDebugNames()
SingleImagePassCannotDeclareAovInput()
```

## 手动验收

场景：

1. 开启一个 ImagePost prototype stack。
2. 添加两个纯图像 layer。
3. 打开 RenderGraph viewer / Frame Debugger。

检查：

- 能看到 source copy。
- 能看到 pass0 read WorkA write WorkB。
- 能看到 pass1 read WorkB write WorkA。
- 能看到 final copy。
- 没有每 layer 一张独立 full-res RT。
- 控制台无 camera color read/write conflict。

## 风险点

- 为了省 copy 直接读 camera color，再写回 camera color。
- 把 WorkA / WorkB 暴露给 ScreenPost 或 SSS。
- 每个 effect 自己创建 temp。
- OriginalSource 默认总是创建。
- 多输入 effect 仍伪装成 single image pass。
- final output alias 和 work texture lifetime 混淆。
