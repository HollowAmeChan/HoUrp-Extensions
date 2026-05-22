# RP 第三阶段测试与验收清单

## 自动检查

| 检查 | 状态 |
| --- | --- |
| registry count / link tests | 已补充测试 |
| ObjectCustom resource descriptor tests | 已补充测试 |
| shader property mapping tests | 已补充测试 |
| `git diff --check` | 通过 |
| Unity batchmode / EditMode tests | 未运行，当前包目录没有独立 Unity project / `.csproj` |

## 手动 Unity 验收

在 Unity 中挂载：

1. `HoURP AOV Output`
2. `HoURP Semantic Post Process`
3. 可选：`HoURP AOV Debug`

给测试对象添加 `ObjectSemanticAuthoring`，设置 `Object Custom Mask = 1`。

期望：

| 场景 | 期望 |
| --- | --- |
| AOV Output | Frame Debugger 中 `HoURP AOV Output` 写入 4 个 MRT。 |
| Debug ObjectCustom0 | camera color 显示 ObjectCustom0 灰度图。 |
| Debug AllRegistered | `HoURP AOV Debug` 选择 `AllRegistered` 时，自动枚举已注册且资源存在的 AOV DebugView，并按网格平铺输出到 camera color。 |
| Debug WorldNormal 天空区域 | `Aov.NormalDepth` 未绘制/天空区域显示为空值黑，不显示默认法线颜色。 |
| AOV Clear | Frame Debugger 中不应出现额外 `HoURP AOV Clear` 绘制 pass；AOV 资源应由 TextureDesc clear-on-first-use 清到空值。 |
| SemanticPost | 只有 ObjectCustom 命中的 opaque 区域出现额外 tint 权重。 |

## 手动验收发现

- Deferred Renderer 下，`HoURP AOV Output` 已经能把 ObjectCustom 写入 MRT，但 Debug 仍全黑时，问题不在 authoring 或 AOV 输出，而在 Debug pass 读纹理绑定。
- RenderGraph pass 内用 `CommandBuffer.SetGlobalTexture(TextureHandle)` 临时绑定 AOV source，Frame Debugger 可见生产端写入，但 Debug 材质可能读不到该绑定。
- Debug 改为 `RenderGraphUtils.BlitMaterialParameters` 的 source texture 绑定路径，并让 shader 统一采样 `_HoUrpAovDebugSourceTexture` 后，`AOV.ObjectCustom0` 手动验收恢复正常。
- AllRegistered 后续改为单个 `HoURP AOV Debug All` raster pass 内逐 tile 绑定 source texture 并绘制，避开 `AddBlitPass` 多 tile 绑定/顶点参数不稳定问题。
- `Aov.NormalDepth` clear 从中性法线改为空值 `(0, 0, 0, 0)`；天空球没有几何法线，Debug 不再给天空 fallback 默认颜色。
- AOV 清理最终收敛到资源声明层：`TextureDesc.clearBuffer/clearColor` 是清理入口，AOV Output pass 用 `ReadWrite` 保留天空/未绘制区域的 clear 值。
- 手写 MRT clear pass 不作为最终设计：它会增加额外 pass，并且在多 color attachment 下容易出现只清部分目标导致拖影。

## 冻结条件

| 条件 | 状态 |
| --- | --- |
| `Aov.ObjectCustom0_3` / `Aov.ObjectCustom4_7` 已注册 | 通过 |
| `Object.Custom0..7` 已注册 | 通过 |
| DebugView 已注册并能映射到对应 resource/channel | 通过 |
| AovOutput 显式写 ObjectCustom MRT | 通过 |
| AOV 空值/天空区域契约已明确，NormalDepth 天空不伪造默认法线 | 通过 |
| AOV 清理放在资源声明/TextureDesc clear-on-first-use，而不是额外 clear draw/pass | 通过 |
| SemanticPost 显式读 ObjectCustom MRT | 通过 |
| 不依赖旧 `_lilHoAov*` 逻辑名 | 通过，旧名只在 `LegacyName` / `LegacySource` 留档 |
| 未决项已登记 | 通过 |
