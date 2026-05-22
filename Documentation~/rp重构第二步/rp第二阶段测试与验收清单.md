# RP 第二阶段测试与验收清单

## 完成范围

| 项 | 状态 | 证据 |
| --- | --- | --- |
| `Aov.MaskId` / `Aov.NormalDepth` 注册 | 完成 | `HoUrpBuiltInContracts.CreateMinimalAovRegistry()` |
| AOV RenderGraph resource declaration | 完成 | `HoUrpAovResourceDeclaration.DeclareMinimalAovTextures()` |
| `AovOutput` 最小绘制 | 完成 | `AovOutputRendererFeature` renderer list + override material |
| AOV fallback shader | 完成 | `Hidden/HoURP/AOV/AovOutputFallback` |
| DebugView replace pass | 完成 | `AovDebugRendererFeature` + `Hidden/HoURP/Debug/AovDebug` |
| SemanticPost 最小消费者 | 完成 | `SemanticPostProcessRendererFeature` mask tint |
| 新 shader binding 留档 | 完成 | `rpAov资源声明与绑定审查.md` |

## 自动检查

| 检查 | 状态 |
| --- | --- |
| `git diff --check` | 通过 |
| registry count / link tests | 已补充测试 |
| shader property mapping tests | 已补充测试 |
| resource descriptor tests | 沿用第一阶段测试 |

## 手动 Unity 验收

在 Unity 中挂载以下 RendererFeature：

1. `HoURP AOV Output`
2. `HoURP Semantic Post Process`
3. 可选：`HoURP AOV Debug`

验收观察：

| 场景 | 期望 |
| --- | --- |
| 仅启用 `AovOutput` | Frame Debugger 中有 `HoURP AOV Output` pass，写入两个 MRT。 |
| 启用 `SemanticPostProcess` | `HoURP Semantic Post AOV Read` 显式读取两个 AOV，画面中 opaque 区域出现轻微 tint。 |
| `AOV / Mask` Debug | camera color 被替换为 mask 灰度图。 |
| `AOV / World Normal` Debug | camera color 被替换为 encoded world normal。 |

## 本轮已知限制

- 未运行 Unity batchmode 编译；当前包目录没有独立 Unity project / `.csproj` 可直接构建。
- 没有 RenderDoc 自动截帧。
- 没有最小测试场景资产。
- 没有 alpha clip、transparent、旧材质原生 `HoAOV` pass 一致性验收。

## 冻结条件核对

| 条件 | 状态 |
| --- | --- |
| `AovOutput` 有 Descriptor / Resource / Semantic / DebugView 对齐 | 通过 |
| `Aov.MaskId` / `Aov.NormalDepth` 由 RenderGraph pass 写入 | 通过 |
| `SemanticPostProcess` 显式读取两个 AOV 资源 | 通过 |
| 至少一个 DebugView 能显示 AOV 内容 | 通过 |
| 不依赖旧 compatibility path | 通过 |
| 不修改 `lilToon` / `lilPBR` | 通过 |
| 不把 `_lilHoAov*` 作为逻辑资源名 | 通过 |
| 新增 shader binding 有登记 | 通过 |
