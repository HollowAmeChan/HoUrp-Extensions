# OIT 与 Post 顺序回归审查

## 目标

第十二阶段新增 OIT runtime 后，必须确认它不会破坏第十一步已经建立的 ScreenPost / ImagePost 链路。

关键问题：

```text
OIT composite 的 camera color 写回
  是否发生在 ScreenPost / ImagePost 之前？

ScreenPost / ImagePost
  是否能看到透明 composite 后的 camera color？

OIT 中间资源
  是否被 Post 错误当作普通输入？
```

## 推荐顺序

第一版建议：

```text
Opaque + skybox
SubsurfaceScattering source/composite
TransparentOit opaque copy / clear / accumulation
URP transparent or OIT transparent phase
TransparentOit composite
ScreenPost
ImagePost
DebugComposite
FinalOutput
```

如果项目已有 renderer feature 顺序不同，以实际 RenderGraph pass 顺序为准，但必须满足：

- OIT composite 在 final ImagePost 前。
- ImagePost 只处理 OIT composite 后的 camera color。
- ScreenPost 不读取 OIT accumulation/revealage。

## Camera Color 读写规则

OIT composite 需要读取当前 camera color 并写回 camera color。

必须使用：

```text
Oit.CompositeSource
```

禁止：

```text
Composite pass 同时采样 activeColorTexture 并 SetRenderAttachment(activeColorTexture)
```

这和第十一步 ImagePost / ScreenPost 对 camera color copy 的要求一致。

## 与 ScreenPost 的关系

ScreenPost 是语义感知屏幕后处理。

允许：

- ScreenPost 在 OIT composite 后读取 camera color。
- ScreenPost 继续读取 AOV / SSS。

不允许：

- ScreenPost rule 读取 `Oit.Accumulation`。
- ScreenPost 把 `Oit.Revealage` 当 object/material mask。
- OIT 修改 ScreenPost rule language。

## 与 ImagePost 的关系

ImagePost 是 final image stack。

允许：

- ImagePost 在 OIT 后做 color adjust、vignette、film grain 等。

不允许：

- ImagePost effect 默认声明 OIT resource input。
- ImagePost AOV composite 顺手读取 OIT resources。
- ImageChain WorkA/WorkB 与 OIT CompositeSource 混用。

## 与 AOV Debug 的关系

Debug view 可以观察 OIT resources，但 debug feature 不应改变 OIT / Post 顺序。

建议：

```text
OIT debug views in DebugComposite
DebugComposite after OIT composite and post, or as explicit debug override
```

第一版如果继续使用 `AovDebugRendererFeature`，要在文档中注明它已经是 “registered debug view composite”，名字后续可再统一。

## 手动回归场景

Renderer Features：

```text
HoURP AOV Output
HoURP Subsurface Scattering
HoURP Weighted OIT
HoURP ScreenPost Prototype
HoURP ImagePost Prototype
HoURP AOV Debug
```

场景：

- opaque background。
- two transparent OIT objects。
- one AOV object affected by ScreenPost。
- ImagePost color adjust enabled。

检查：

| 操作 | 期望 |
| --- | --- |
| 只开 OIT | 透明 composite 正常 |
| OIT + ScreenPost | ScreenPost 叠加在 OIT composite 后 |
| OIT + ImagePost | ImagePost 影响最终包含透明的画面 |
| 关闭 OIT | Post 仍正常 |
| 关闭 ScreenPost | OIT 仍正常 |
| 关闭 ImagePost | OIT 仍正常 |

## 自动测试建议

如果无法直接跑 RenderGraph，可先用 contract/plan 级测试：

```text
TransparentOitStagePrecedesScreenPostAndImagePost
ImagePostDoesNotConsumeOitRuntimeResourcesByDefault
ScreenPostDoesNotConsumeOitRuntimeResourcesByDefault
OitCompositeSourceIsPrivateToTransparentOit
```

## 风险

- RendererFeature inspector 顺序让 ImagePost 先于 OIT composite 执行。
- OIT composite 没有 source copy，导致 camera color 读写冲突。
- Debug pass 修改 active color 后影响后续 post 验收。
- OIT resource 被错误纳入 PostGraph dynamic requests，造成职责混乱。

