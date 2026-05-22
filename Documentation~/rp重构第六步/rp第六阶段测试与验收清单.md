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

## 现场调试记录

- `SSS.Mask` / `SSS.Source` 已确认有输出，说明 authoring 与 AOV `SssSource` 链路有效。
- `SSS.Diffusion` / `SSS.CompositeWeight` 曾为黑，原因是 diffusion shader 在 `RenderGraphUtils.BlitMaterialParameters` 路径下仍读取未作为该 pass 输入声明的 AOV normal/surface 纹理。
- 当前第六阶段先将 diffusion 收敛为 `Sss.Source -> Sss.Diffusion` 的闭合 RenderGraph blit；边缘保护/法线深度约束留到后续增强版 pass 中以显式 RenderGraph 读依赖实现。
