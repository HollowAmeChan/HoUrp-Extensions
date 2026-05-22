# RP 第六阶段测试与验收清单

## 自动检查

| 检查 | 状态 |
| --- | --- |
| registry count / link tests | 待实现 |
| `SubsurfaceScattering` feature descriptor tests | 待实现 |
| `Sss.Source` / `Sss.Diffusion` descriptor tests | 待实现 |
| shader property mapping tests | 待实现 |
| DebugView source/resource mapping tests | 待实现 |
| SSS consumed resource declaration tests | 待实现 |
| `git diff --check` | 待实现后运行 |
| Unity batchmode / EditMode tests | 当前包目录没有独立 Unity project / `.csproj` |

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
