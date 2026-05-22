# RP 第五阶段未决项登记

| Item | 本阶段处理方式 | 后续阶段 |
| --- | --- | --- |
| 完整 HoSSS diffusion | 不做 | HoSSS 迁移阶段 |
| transmission gather / blur | 不做 | HoSSS 迁移阶段 |
| SSS composite | 不做 | HoSSS composite 阶段 |
| profile kernel / radius | 只登记 profile id，不计算 kernel | HoSSS profile registry 阶段 |
| half-resolution SSS | 不做 | Filter / SSS 性能阶段 |
| bilateral / depth-aware blur | 不做 | Filter backend / HoSSS 阶段 |
| 旧 `HoAOVSSS` pass | 不接 | 新材质 producer 或 legacy validation 阶段 |
| 旧材质 SSS 属性迁移 | 不做 | 新材质系统 / migration tool 阶段 |
| transparent SSS | 不做 | Transparent / OIT / Character 阶段 |
| Unity 自动化验收 | 本阶段规划先登记，包目录没有独立 Unity project / `.csproj` | 接入 integration project 后跑 batchmode/EditMode/Frame Debugger 验收 |

