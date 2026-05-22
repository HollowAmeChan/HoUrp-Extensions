# RP 第四阶段未决项登记

| Item | 本阶段处理方式 | 后续阶段 |
| --- | --- | --- |
| `Material.Utility` source channel | 默认注册但不生产，除非实现审查明确给出稳定通道 | SurfaceData 扩展或 HoSSS 输入阶段 |
| `Aov.SssSource` | 不做 | HoSSS 输入纵切 |
| `Shading.SssWeight` | 不做完整生产 | HoSSS 输入纵切 |
| SSS profile registry | 只写 profile id 数值 | HoSSS / material system 阶段 |
| 旧材质原生 `HoAOV` pass | 不接 | 新材质 producer 或 legacy validation 阶段 |
| texture-driven `Material.Custom0..3` | 不做 | 新材质系统阶段 |
| SurfaceData 旧视觉一致性 | 只保留旧实现参照，不在第四阶段验收 | HoSSS 迁移对比阶段 |
| 材质 inspector UI | 不做 | HoPbr / HoNpr 材质系统 |
| transparent material semantic | 不做 | Transparent / OIT 阶段 |
| alpha clip 与旧 AOV 一致性 | 不做 | AOV material producer 阶段 |
| Unity 自动化验收 | 本阶段规划先登记，包目录没有独立 Unity project / `.csproj` | 接入 integration project 后跑 batchmode/EditMode/Frame Debugger 验收 |

