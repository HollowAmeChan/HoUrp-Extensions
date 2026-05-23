# RP 第十二阶段实现边界审查

## 阶段定位

第十二阶段只做 **Weighted OIT runtime 最小闭环**。

它承接第十步已经完成的 OIT-ready 材质 ABI，也承接第十一步已经稳定下来的 RenderGraph-first 资源声明思路。第十二阶段不是透明系统总重构，也不是旧 `WeightedOITRendererFeature` 的搬家。

当前前置状态：

- Generated material 已有 `HoUrpOitAccumulation` pass。
- Material contract 已有 `SupportsOit` / `ParticipatesOit` capability。
- Material semantics 已有 `OIT.AccumulationInput` / `OIT.RevealageInput`。
- `HoUrpPassStage.TransparentOit` 已存在。
- 旧 `SemanticPostProcessRendererFeature` 已删除，后处理语义入口收敛到 `ScreenPost`。

第十二阶段要补齐的是 runtime consumer：

```text
OIT-ready material pass
  -> OIT accumulation/revealage resources
  -> OIT composite
  -> camera color
```

## 本阶段必须坚持的边界

### OIT 是透明合成，不是 ImagePost

| 系统 | 定位 | 允许输入 | 禁止事项 |
| --- | --- | --- | --- |
| `TransparentOit` | 透明 accumulation / revealage / composite | camera color copy、depth、OIT-ready material pass output | 变成 image effect |
| `ImagePost` | final image style stack | primary image、original source、轻量 AOV composite | 读取 OIT 私有中间 RT |
| `ScreenPost` | 语义感知屏幕效果 | AOV、SSS、object/material/geometry semantic | 管理透明 accumulation |

判断规则：

```text
需要 draw transparent renderers:
  放 TransparentOit

只处理当前画面颜色:
  放 ImagePost

需要 object/material semantic rule:
  放 ScreenPost
```

### OIT resource 不是长期公共语义资源

`Oit.Accumulation` / `Oit.Revealage` 是 runtime composite 中间资源，不应默认成为任意后处理的公共输入。

可以注册为 resource / debug view，但消费方第一版只允许：

- `TransparentOit` 自己 composite。
- Debug view 观察。

不允许：

- ImagePost 默认读取 `Oit.Accumulation`。
- ScreenPost rule 读取 `Oit.Revealage`。
- 其它 Feature 依赖 “OIT 刚好执行过”。

## 新旧 ABI 边界

旧实现只能作为行为参考。

禁止继承：

```text
lilToonOIT
_lilOITAccumulationTexture
_lilOITRevealageTexture
_lilOITOpaqueTexture
_lilOITCompositeSourceTexture
_lilOITActive
_lilOITWeight
_lilOITAlphaClipThreshold
Hidden/lilToon/URP/WeightedOITComposite
```

新 ABI 建议：

```text
HoUrpOitAccumulation
Oit.OpaqueColor
Oit.Accumulation
Oit.Revealage
Oit.CompositeSource
_HoUrpOitOpaqueColorTexture
_HoUrpOitAccumulationTexture
_HoUrpOitRevealageTexture
_HoUrpOitCompositeSourceTexture
_HoUrpOitActive
_HoUrpOitWeight
_HoUrpOitAlphaClipThreshold
Hidden/HoURP/OIT/WeightedComposite
```

允许旧名字出现的位置：

- Legacy 审查文档。
- 测试里的 `Does.Not.Contain`。
- 注释中明确说明 “old ABI forbidden” 的小段说明。

## Feature 命名建议

建议 contract feature 名用：

```text
TransparentOit
```

建议 RendererFeature 类名用：

```text
WeightedOitRendererFeature
```

理由：

- `TransparentOit` 表达系统职责：透明 OIT runtime。
- `WeightedOit` 表达当前算法实现：weighted blended OIT。
- 资源名不绑定 `Weighted`，方便未来换算法。

## RenderGraph 边界

必须显式声明：

```text
OpaqueCopy:
  read camera color
  write Oit.OpaqueColor

Clear:
  write Oit.Accumulation
  write Oit.Revealage

Accumulation:
  draw HoUrpOitAccumulation
  write Oit.Accumulation
  write Oit.Revealage
  read depth when valid

Composite:
  read Oit.CompositeSource
  read Oit.Accumulation
  read Oit.Revealage
  write camera color
```

禁止：

- 在同一 pass 里读写同一个 `TextureHandle`。
- 将当前正在写的 camera color 直接作为全局 OIT source。
- 使用 persistent RTHandle 作为主路径。
- 用 compatibility `Execute()` 作为长期实现。

## 与普通 transparent forward 的关系

第十二阶段必须明确 “OIT accumulation” 与 “普通 transparent forward” 的相对关系。

第一版推荐规则：

```text
MaterialPhasePolicy.OitOnly:
  只走 HoUrpOitAccumulation，不走普通透明 forward。

MaterialPhasePolicy.TransparentForwardOnly:
  不参与 OIT。

MaterialPhasePolicy.TransparentForwardAndOit:
  第一版登记为 planned，不默认启用。
```

风险点：

- OIT-ready 材质如果同时走普通 forward 和 OIT accumulation，会重复显示。
- `_HoUrpOitActive` 可以作为 shader phase gate，但不能成为唯一调度依据。

## 本阶段不做

- 不做 render scale。
- 不做 per-material OIT inspector。
- 不做 transparent sorting policy UI。
- 不做 glass / refraction。
- 不做 water。
- 不做 motion vectors。
- 不做 CharacterSpecialization。
- 不做 HoShadowCast。
- 不做 Planar Reflection。
- 不做 OIT 与 ScreenPost rule 的联动。

## 进入实现前检查

实现前必须确认：

- `HoUrpDebugLitMinimal.shader` 的 `HoUrpOitAccumulation` pass 可作为最小测试材质。
- `HoUrpMaterialOit.hlsl` 已经输出 weighted color / revealage 所需数据。
- `HoUrpBuiltInContracts` 中 `GeneratedMaterial` 仍不拥有 OIT runtime resources。
- 第十二阶段新增的是 `TransparentOit` runtime owner。

