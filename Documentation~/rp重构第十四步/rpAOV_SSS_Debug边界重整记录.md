# AOV / SSS / Debug 边界重整记录

> 当前有效契约记录。早期阶段文档中出现的旧 SSS source 命名、`AOV Debug`、`AovDebugRendererFeature` 等说法保留为历史迁移记录，不再作为新增功能的设计依据。

## 调整原因

AOV Debug 的 AllRegistered tile 在混合 AOV、SSS runtime、post mask、object flags 等资源后，容易出现 source texture / mode / label 对不齐的问题。这不是单纯 UI 问题，而是 HoAOV、SSS、Debug 三个层级的职责被塞进同一个调试枚举。

当前边界：

- HoAOV 只拥有通用、可复用的基础语义缓存。
- SSS runtime 归 `SubsurfaceScattering`。
- Debug 是独立的 Render Cache 观察层，不是 HoAOV 子功能。

## 当前有效资源所有权

| Resource | Owner | 当前物理生产路径 | 说明 |
| --- | --- | --- | --- |
| `Aov.MaskId` | `AovOutput` | AOV MRT | object mask / id / group / flags |
| `Aov.NormalDepth` | `AovOutput` | AOV MRT | 当前仍由 AOV 生产；长期可迁到 Geometry cache |
| `Aov.ObjectCustom0_3` / `Aov.ObjectCustom4_7` | `AovOutput` | AOV MRT | object semantic / custom region |
| `Aov.SurfaceData` | `AovOutput` | AOV MRT | material class / profile / thickness / curvature |
| `Aov.MaterialCustom0_3` | `AovOutput` | AOV MRT | registered material custom channels |
| `Aov.Diffuse` | `AovOutput` | AOV MRT | reusable diffuse/source color; RGB 有效，A 保留且不表示 SSS 权重 |
| `Sss.Source` | `SubsurfaceScattering` | SSS source pass | prepared SSS source color and participation mask |
| `Sss.Diffusion` | `SubsurfaceScattering` | SSS diffusion pass | diffused SSS color and composite weight |

HoAOV 可以为真屏幕空间 SSS 提供 diffuse/base color、profile、thickness、curvature、normal/depth、mask 等基础输入。SSS 不应自行重复生产这些通用输入。

SSS 自己拥有经过 feature 策略筛选后的运行时资源。`Sss.Source.a` 是 SSS source pass 生成的 participation mask；后续若材质需要输出更细的 SSS weight / control 量，应进入 SSS 自己的 RDG/MRT 通道，而不是塞进 `Aov.Diffuse.a` 或其他 HoAOV 通道。

## Debug 命名

正式调试层命名为 Render Cache Debug：

- Shader: `Hidden/HoURP/Debug/RenderCacheDebug`
- Property: `_HoUrpRenderCacheDebug*`
- Feature display name: `HoURP Render Cache Debug`

`RenderCacheDebugRendererFeature` 和 `RenderCacheDebug.shader` 是正式入口。旧 `AovDebugRendererFeature` / `AovDebug.shader` 只作为历史迁移记录；运行时代码里如需保留 `AovDebug*`，只能作为短期兼容别名，不能作为新增概念名。

## Tile 显示策略

AllRegistered 使用稳定列数：

- 宽屏默认 5 列。
- 非宽屏默认 4 列。
- 每个 tile 有 gutter 和双层边界线。

这样可以直接看出每个 view 占用的格子范围，避免“最后几个 tile 层级混淆”的视觉误判。

## 后续禁止项

- 不再新增 `AOV.*` debug view 来显示 SSS runtime、OIT、ShadowCast 或 Post cache。
- 不再新增 `Aov.*` resource 表达某个具体 RenderFeature 的内部计算资源。
- 不再让 AOV feature descriptor 声明拥有 `Sss.Source` 或 `Sss.Diffusion`。
- 不再让 HoAOV MRT 输出 SSS 私有权重；权重/控制量归 SSS 自己的 RDG/MRT。
- 不再让 Debug shader 的 mode enum 成为资源所有权来源；所有 view 必须来自 registry。
