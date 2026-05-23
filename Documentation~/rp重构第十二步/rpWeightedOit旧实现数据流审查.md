# Weighted OIT 旧实现数据流审查

## 旧实现入口

参考仓库：

```text
D:\Unity_Fork\lilToon-URP-Extensions\Runtime\OIT
```

核心文件：

```text
WeightedOITRendererFeature.cs
WeightedOITSettings.cs
WeightedOITShaderConstants.cs
WeightedOIT.hlsl
WeightedOITComposite.shader
```

旧实现的价值是证明数据流可行，不是提供新实现结构。

## 旧数据流事实

旧 `WeightedOITRendererFeature` 由这些 pass 组成：

```text
ResetPass
OpaqueCopyPass
ClearPass
AccumulationPass
CompositePass
```

旧 frame flow：

```text
begin camera:
  _lilOITActive = 0

before OIT accumulation:
  copy camera color -> _lilOITOpaqueTexture
  clear _lilOITAccumulationTexture
  clear _lilOITRevealageTexture

OIT accumulation:
  _lilOITActive = 1
  draw LightMode = lilToonOIT
  write accumulation MRT
  write revealage MRT

OIT composite:
  copy camera color -> _lilOITCompositeSourceTexture
  composite source + accumulation + revealage -> camera color
  _lilOITActive = 0
```

## 旧资源

| 旧资源 | 用途 | 新资源 |
| --- | --- | --- |
| `_lilOITOpaqueTexture` | transparent accumulation 前的 opaque color copy | `Oit.OpaqueColor` |
| `_lilOITAccumulationTexture` | weighted color / alpha accumulation | `Oit.Accumulation` |
| `_lilOITRevealageTexture` | revealage / transparency product | `Oit.Revealage` |
| `_lilOITCompositeSourceTexture` | composite 前 camera color copy | `Oit.CompositeSource` |

旧资源名不得进入新 ABI。

## 旧 pass tag

旧 pass tag：

```text
lilToonOIT
```

新 pass tag：

```text
HoUrpOitAccumulation
```

旧材质侧通过 `lilToonOIT` pass 写 accumulation/revealage。新材质侧已经在 `HoUrpDebugLitMinimal.shader` 中声明 `HoUrpOitAccumulation`，第十二步 runtime 只 draw 新 pass tag。

## 旧 active 状态

旧 `_lilOITActive` 有两个用途：

- accumulation 阶段通知材质 “当前正在走 OIT pass”。
- 非 accumulation 阶段避免 OIT-only 透明对象重复走普通 forward。

新系统允许保留同类概念，但必须改名：

```text
_HoUrpOitActive
```

边界：

- 它只是 phase gate。
- 它不是资源生命周期。
- 它不能替代 renderer filtering / pass tag / material capability。

## 旧 composite 公式

旧 shader `WeightedOITComposite.shader` 读取：

```text
_lilOITAccumulationTexture
_lilOITRevealageTexture
```

第十二步实现前需要核查旧公式：

```text
accumulation = weighted color and alpha
revealage = remaining transmittance
resolved = accumulation.rgb / max(accumulation.a, epsilon)
output = lerp(resolved, sourceColor, revealage)
```

最终公式可参考旧实现，但 shader 名、texture 名、property 名必须全部换成 HoURP ABI。

## 旧实现不能照搬的点

### Compatibility path

旧实现有 RenderGraph path 与 compatibility path。新实现只保留 RenderGraph-first 路线。

禁止搬迁：

```text
RTHandle persistent render target main path
Execute() compatibility rendering
cmd.SetGlobalTexture 作为资源发布主机制
```

### 旧全局纹理命名

禁止进入新代码：

```text
_lilOIT*
```

### 旧 shader 名

禁止进入新代码：

```text
Hidden/lilToon/URP/WeightedOITComposite
```

### 旧 renderer feature 显示名

禁止进入新 Inspector：

```text
lilToon Weighted OIT
```

建议新显示名：

```text
HoURP Weighted OIT
```

## 可继承的行为

允许继承行为，不继承 ABI：

- Opaque copy 在 accumulation 前。
- Accumulation / revealage 使用 MRT。
- Composite 在 transparent 后写回 camera color。
- Begin / end reset active state。
- Alpha clip threshold / weight 作为 runtime 参数。
- layer mask / render queue range 作为 renderer filtering。

## 审查输出

实现前应补一张表：

| Old Symbol | Old Meaning | New Symbol | Decision |
| --- | --- | --- | --- |
| `_lilOITAccumulationTexture` | accumulation RT | `_HoUrpOitAccumulationTexture` | Rename |
| `_lilOITRevealageTexture` | revealage RT | `_HoUrpOitRevealageTexture` | Rename |
| `lilToonOIT` | pass tag | `HoUrpOitAccumulation` | Replace |
| `_lilOITActive` | phase gate | `_HoUrpOitActive` | Rename |
| compatibility `Execute()` | non-RDG path | none | Remove |

