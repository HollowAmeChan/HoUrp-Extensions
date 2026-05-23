# RP 第十一阶段实现边界审查

## 阶段定位

第十一阶段只建立 ScreenPost / ImagePost 搬迁所需的后处理基础设施和最小验证链路，不迁移完整旧效果库。

```text
第十一阶段:
  PostGraph 描述与计划层
  PostResourceRequest 动态资源请求
  ImageChain 双缓冲执行器
  ScreenPost rule mask / layer blit 最小原型
  ImagePost single image pass 最小原型
  Debug / tests / docs

后续阶段:
  完整 ScreenPost rule language
  完整 ImagePost effect catalog
  multi-pass / pyramid / history effect
  CharacterSpecialization
  Weighted OIT runtime
```

本阶段可以从旧实现确认行为，但不能复制旧架构：

```text
旧 HoPost / Shoost:
  Volume stack
  RenderGraph + compatibility path
  per-layer temporary resources
  old global texture names
  old shader property names

新 HoURP:
  explicit descriptor
  frame-local plan
  dynamic request / release
  ImageChain WorkA / WorkB
  registered semantic inputs
  debug-visible resource lifetime
```

## 必须坚持的边界

### ScreenPost 与 ImagePost 分层

| 系统 | 定位 | 允许输入 | 禁止事项 |
| --- | --- | --- | --- |
| `ScreenPost` | 需要输入 RT / 语义资源的屏幕空间处理 | AOV、SSS、SemanticPost、object/material/geometry semantic、camera color copy | 变成最终画面风格大杂烩 |
| `ImagePost` | final image style stack | primary image、original source、轻量 AOV composite | 变成第二套 ScreenPost rule stack |
| `ImageChain` | 纯图像链工作区 | image source / work textures | 承载语义规则或长期公共资源 |

判断规则：

```text
需要 object id / material class / normal-depth / AOV rule:
  放 ScreenPost 或 CharacterSpecialization

只对当前画面做色彩、锐化、颗粒、简单 blur、vignette:
  放 ImagePost / ImageChain

需要多分辨率、history、original source、multi-output:
  先声明 ResourcePolicy，再决定是否仍归 ImagePost
```

### 动态资源不是全局注册表

第十一阶段要引入“动态注册 / 注销”概念，但它不是让 effect 在运行时随意写全局 registry。

正确模型：

```text
Static registry:
  knows possible effect / semantic / debug kinds

Frame-local plan:
  lists active effects for this camera/frame
  lists requested transient resources
  lists declared inputs / outputs

RenderGraph:
  creates / aliases / consumes TextureHandle for this frame
```

禁止：

- effect 保存 `TextureHandle` 到字段里跨帧复用。
- 关闭 effect 后继续绑定上帧纹理。
- debug view 继续显示已禁用 effect 的旧图。
- 用 `SetGlobalTexture` 作为资源生命周期。
- 把 `Image.WorkA` / `Image.WorkB` 发布成长期公共 resource。

### 本阶段不继承旧 ABI

禁止进入新 ABI 的旧名：

```text
_lilHoPostProcessTempA
_lilHoPostProcessTempB
_lilShoostPostProcessTempA
_lilShoostPostProcessTempB
_lilShoostPostProcessTempC
_lilHoAov*
_HoAov*
```

允许作为 LegacyInterop / 文档来源：

```text
HoPostProcessRendererFeature.cs
HoPostProcessLayer.cs
HoPostAovMask.hlsl
ShoostPostProcessEffectDescriptor.cs
ShoostPostProcessPass.cs
ShoostPostProcessPass.Aov.cs
Renderer/Effects/*.cs
```

## 建议代码落点

实际可按仓库现状调整，但职责必须可追踪。

```text
Runtime/PostProcess/
  PostEffectDefinition.cs
  PostEffectExecutionKind.cs
  PostLayerDefinition.cs
  PostStackDefinition.cs
  PostResourceRequest.cs
  PostGraphPlan.cs
  PostGraphPlanner.cs

Runtime/Image/
  ImageChain.cs
  ImageChainContext.cs
  ImagePassDescriptor.cs
  ImageResourcePolicy.cs

Runtime/Features/
  ScreenPostRendererFeature.cs
  ImagePostRendererFeature.cs

Runtime/Shaders/Post/
  HoUrpPostAovMask.hlsl
  HoUrpPostLayerBlit.shader

Runtime/Shaders/Image/
  HoUrpImageLayerBlit.shader

Tests/Runtime/
  HoUrpPostGraphPlannerTests.cs
  HoUrpImageChainTests.cs
  HoUrpPostResourceRequestTests.cs
```

如果当前实现已有相近目录，优先合并到现有命名，不为了文档强行新建重复模块。

## 分段执行建议

### Phase A. 只做数据模型和 planner

目标：

- 不写 shader。
- 不建 RenderFeature。
- 只证明 active stack 能转换成 deterministic plan。

产物：

```text
PostEffectDefinition
PostLayerDefinition
PostStackDefinition
PostResourceRequest
PostGraphPlan
PostGraphPlanner
```

验收：

- 相同输入生成相同 plan。
- disabled layer 不进入 plan。
- single image pass 自动请求 ImageChain work。
- semantic pass 自动列出 AOV / semantic 输入。
- request 有 owner / lifetime / debug name。

### Phase B. ImageChain 最小 RenderGraph 路径

目标：

- 录制 1 到 2 个纯图像 pass。
- WorkA / WorkB 双缓冲。
- 最终 copy back。

验收：

- N 个 single image pass 不创建 N 张同规格全屏 RT。
- 每个 pass 都有显式 read/write。
- 不发生 camera color 同 pass 读写冲突。

### Phase C. ScreenPost 最小语义路径

目标：

- 一个 rule mask。
- 一个 layer blend。
- 一个 semantic input request。

验收：

- layer 关闭后 AOV request 消失。
- debug 能看 rule mask / influence。
- 不读取旧 AOV 全局名。

### Phase D. ImagePost 最小图像路径

目标：

- 一个 pure image effect。
- 可选一个 AOV composite prototype。

验收：

- effect order 来自 descriptor。
- AOV composite 关闭时不请求 AOV。
- 多个 single pass 共用 ImageChain。

## 明确不做

- 不做完整 Volume inspector。
- 不迁移全部 shader。
- 不追求旧视觉完全一致。
- 不做 compatibility path。
- 不做 RenderGraph 外 fallback。
- 不做 history resource。
- 不做 depth pyramid。
- 不做 multi-resolution blur 完整实现。
- 不做 OIT。
- 不做角色特化。

## 成功标准

- 有可查询的 Post / Image descriptor。
- 有 frame-local plan。
- 有动态 request / release 测试。
- 有 ImageChain 双缓冲最小 RenderGraph 路径。
- 有 ScreenPost semantic input 最小路径。
- 有 ImagePost pure image pass 最小路径。
- Debug 能显示 active plan / request / image chain read-write。
- 旧全局名没有进入新 ABI。
- 关闭 effect 后无 stale resource / debug / global binding。

## 风险点

- 为了快，把旧 RendererFeature 直接复制过来。
- 把 Volume 当前状态当成结构事实来源。
- 每个 layer 默认分配一张全屏 RT。
- 关闭 effect 后资源还在 debug 中显示 active。
- ImagePost 读取过多语义，变成第二套 ScreenPost。
- ScreenPost rule 散落到各 effect shader。
- ImageChain WorkA / WorkB 被误当作公共资源。
- original source 没有显式 copy，触发 camera color 读写冲突。
