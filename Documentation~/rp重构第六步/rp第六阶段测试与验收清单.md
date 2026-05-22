# RP 第六阶段测试与验收清单

## 自动检查

| 检查 | 状态 |
| --- | --- |
| registry count / link tests | 已补测试 |
| `SubsurfaceScattering` feature descriptor tests | 已补测试 |
| `Sss.Source` / `Sss.Diffusion` descriptor tests | 已补测试 |
| shader property mapping tests | 已补测试 |
| DebugView source/resource mapping tests | 已补测试 |
| SSS consumed resource declaration tests | 已通过 feature descriptor 测试覆盖 |
| `git diff --check` | 已通过，仅有 LF/CRLF 提示 |
| Unity batchmode / EditMode tests | 当前包目录没有独立 Unity project / `.csproj` |

已新增代码入口：

```text
Runtime/Features/SubsurfaceScatteringRendererFeature.cs
Runtime/RenderGraph/HoUrpSssResourceDeclaration.cs
Runtime/Shaders/Hidden/HoURP/SSS/SubsurfaceScattering.shader
```

已新增契约：

```text
Feature: SubsurfaceScattering
Resources: Sss.Source, Sss.Diffusion
DebugView: SSS.Mask, SSS.Source, SSS.Diffusion, SSS.CompositeWeight
```

## 手动 Unity 验收

挂载：

1. `HoURP AOV Output`
2. `HoURP Subsurface Scattering`
3. `HoURP AOV Debug`

设置测试对象：

```text
sssSourceColor = non-black
sssWeight = 1
sssProfile = non-zero
thickness = non-zero
```

期望：

| 场景 | 期望 |
| --- | --- |
| SSS disabled | 画面与第五阶段一致 |
| Source debug | 显示 `Sss.Source` |
| Diffusion debug | 显示 `Sss.Diffusion` |
| Composite | opaque SSS 区域产生可控柔化结果 |
| AllRegistered | 包含 SSS tile，标签和红框正常 |
| 空值区域 | 不产生 SSS 贡献 |
| 透明绘制阶段 | 不再出现 `_CameraTargetAttachment` 同时 `UseTexture` / `SetRenderAttachment` 的 RenderGraph Execution error |

## 现场调试记录

- `SSS.Mask` / `SSS.Source` 已确认有输出，说明 authoring 与 AOV `SssSource` 链路有效。
- `SSS.Diffusion` / `SSS.CompositeWeight` 曾为黑，原因是 diffusion shader 在 `RenderGraphUtils.BlitMaterialParameters` 路径下仍读取未作为该 pass 输入声明的 AOV normal/surface 纹理。
- 当前第六阶段已改为 Source / Diffusion / Composite 三段显式 RenderGraph 链路。主输入通过 `RenderGraphUtils.BlitMaterialParameters.sourceTexturePropertyID` 绑定；额外 AOV 输入通过独立 RenderGraph pass 声明读取并 `SetGlobalTextureAfterPass` 发布，避免 shader 采样未登记资源。
- Source 读取 `Aov.MaskId`、`Aov.NormalDepth`、`Aov.SurfaceData`、`Aov.SssSource` 生成 `Sss.Source`；Diffusion 读取 `Sss.Source`、`Aov.NormalDepth`、`Aov.SurfaceData` 生成 `Sss.Diffusion`；Composite 读取 source color、`Sss.Diffusion` 和 AOV 边界信息写回 camera color。
- `Sss.Source.a` 编码参与权重，`Sss.Diffusion.a` 编码最终 composite weight；debug 的 `SSS.CompositeWeight` 应与实际合成权重一致。
- 已接入旧 HoSSS 的最小 profile 分组思想：8 个 profile 槽位控制 diffusion color、diffusion radius、source preserve 和 thickness scale。第六阶段仍不接 transmission profile。
- 曾出现 `DrawTransparentObjects` RenderGraph Execution error：`_CameraTargetAttachment` 在同一 pass 中既被 `SetRenderAttachment` 写入，又被 `UseTexture` 读取。根因是 SSS diffusion 将 `resourceData.activeColorTexture` 直接发布为 `_HoUrpSourceColorTexture`；URP 透明绘制 pass 的 `UseAllGlobalTextures(true)` 会把该全局纹理纳入读取依赖。
- 当前修正为先显式 copy camera color 到 `_HoUrpSssColorCopy`，再把 copy 发布为 `_HoUrpSourceColorTexture`。Diffusion / Composite 只读取 copy，Composite 写回 live camera color。
- `ZBinningJob` safety error 暂按 RenderGraph 录制异常后的连带错误登记。复测时先确认 `_CameraTargetAttachment` 资源冲突是否消失；如果资源冲突消失后仍复现，再单独调查 ForwardLights job handle。

## 第六阶段复测重点

| 检查 | 期望 |
| --- | --- |
| Unity 控制台 | 无 `Render Graph Execution error` |
| 透明物体存在时 | `DrawTransparentObjects` 不再报 `_CameraTargetAttachment` 使用冲突 |
| SSS diffusion | 不再因为未声明 AOV 输入导致黑图 |
| SSS composite | 只影响 `Sss.Diffusion.a > 0` 且 AOV normal/thickness 有效区域 |
| Debug AllRegistered | `SSS.Source` / `SSS.Diffusion` / `SSS.CompositeWeight` 均可见 |
| Job safety | 若仍出现 `ZBinningJob`，需在无 RenderGraph error 的前提下单独记录堆栈 |
