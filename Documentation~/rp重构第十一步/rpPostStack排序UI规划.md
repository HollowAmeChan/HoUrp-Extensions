# RP 第十一阶段 Post Stack 排序与 UI 规划

## 目标

在第十一阶段继续打基础设施，让用户可以通过显式拖拽列表顺序达成不同顺序的滤镜叠加效果。

目标不是做完整产品化 Inspector，而是先建立正确的数据流：

```text
Editor reorderable list
  -> serialized settings
  -> runtime layer snapshot
  -> PostGraphPlanner
  -> ordered RenderGraph passes
```

UI 不能绕过 descriptor / planner 直接决定 pass 或 resource。

## Stack 分层

第一版保留两个 stack：

```text
ScreenPostStack
  semantic-aware layers
  each layer owns rule set + blend parameters

ImagePostStack
  pure image filters
  each item owns effect id + image parameters
```

原因：

- ScreenPost 负责 object/material/geometry 语义筛选。
- ImagePost 负责最终 image-space 滤镜。
- 两者可排序，但不能混成一个万能 stack。

后续如果需要统一 UI，可以做一个上层 `PostStackView`，但内部仍保留 domain。

## Serialized Settings

### ScreenPost

```text
ScreenPostLayerSettings
  Enabled
  Name
  EffectId
  BlendMode
  Opacity
  Color
  RuleSet
```

### ImagePost

```text
ImagePostFilterSettings
  Enabled
  Name
  EffectId
  Intensity
  Color
  FloatParams
  NeedsOriginalSource
```

列表顺序即执行顺序。不要再维护单独 `runtimeOrder` 字段，除非用于 catalog 默认排序。

## Editor UI

建议落点：

```text
Editor/PostProcess/
  PostLayerListDrawer.cs
  ScreenPostPrototypeRendererFeatureEditor.cs
  ImagePostPrototypeRendererFeatureEditor.cs
  ScreenPostRuleSetDrawer.cs
```

UI 行为：

- 支持拖拽排序。
- 支持添加、复制、删除。
- 支持 enabled toggle。
- 每一行显示 effect 名称、domain badge、resource badge、主要参数摘要。
- 展开项显示详细参数。
- planned / unsupported effect 显示为禁用态并给 warning。

## 列表项显示

ScreenPost 行：

```text
[✓] 02  Cyan Mask Tint        ScreenPost  AOV: MaskId NormalDepth
    Blend: Alpha  Opacity: 0.65  Rules: 2
```

ImagePost 行：

```text
[✓] 01  Color Adjust          ImagePost   ImageChain
    Intensity: 0.75  Tint: #FFFFFF
```

不在行内塞满所有 rule 字段，rule 放到展开区。

## Runtime Snapshot

每帧 / 每 camera 生成 snapshot：

```text
for each serialized item in list order:
  if disabled:
    skip
  resolve effect descriptor
  create PostLayerDefinition
  append to PostStackDefinition
```

Planner 保持输入顺序。

禁止：

- Editor list 直接创建 RT。
- Editor list 直接绑定 shader property。
- Editor list 的字段绕过 effect descriptor 添加 hidden pass。

## 拖拽排序验收

### ImagePost

准备两个滤镜：

```text
1. ColorAdjust Red Tint
2. Contrast / Brightness
```

拖动顺序前后，画面应有可见差异。RDG pass 顺序应对应列表顺序。

### ScreenPost

准备两个 layer：

```text
1. Rule A -> Cyan tint
2. Rule B -> Darken or additive tint
```

拖动顺序前后，重叠区域 blend 结果应变化。

## 基础设施任务

1. 把 prototype 单参数改成 layer array。
2. 新增 reorderable serialized list。
3. 新增 settings -> `PostLayerDefinition` snapshot 转换。
4. `PostGraphPlanner` 保持输入顺序并输出 pass order。
5. `ImagePostPrototypeRendererFeature` 按 list 顺序录制多个 image pass。
6. `ScreenPostPrototypeRendererFeature` 按 list 顺序录制多个 rule mask / composite pass。
7. 增加 debug：active order、skipped layer、missing effect、resource request。

## 不做

- 不做完整旧 Shoost catalog。
- 不做完整旧 HoPost catalog。
- 不做 Volume system 全量 UI。
- 不把 ScreenPost / ImagePost 混成单一万能列表。
- 不允许 ImagePost 列表项编辑 object/material rule。

