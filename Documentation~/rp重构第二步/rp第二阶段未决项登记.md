# RP 第二阶段未决项登记

| Item | 本阶段处理方式 | 后续阶段 |
| --- | --- | --- |
| Object ID / Group ID / Flags 来源 | fallback shader 写常量 | Object semantic authoring / Capability UI |
| Depth exact encoding | 先写 normalized depth | 扩到 SSS / SurfaceData 前复核 linear eye depth |
| Alpha clip 一致性 | 不做 | 新材质 producer pass 阶段 |
| Transparent AOV | 不做 | Transparent / OIT AOV 阶段 |
| 旧材质 `HoAOV` pass 接入 | 不做 | 新材质系统或 legacy validation |
| `Aov.TangentNormal` / `SurfaceData` / custom / SSS source | 不做 | 第三阶段资源扩展 |
| Debug overlay / HUD / capture | 只做 replace | Debug Framework 阶段 |
| Global texture ABI | 不建立长期 ABI | shader migration 前复核是否需要 backend binding |
| RenderDoc 自动验收 | 未接入 | 有完整 Unity Project 后接入 |
| 最小测试场景资产 | 未创建 | package samples 或 integration project 阶段 |

## 推荐下一阶段

下一阶段优先扩展 `Aov.SurfaceData` 或 `Aov.ObjectCustom0_3` 二选一：

- 如果先验证角色/区域选择，优先 `Aov.ObjectCustom0_3`。
- 如果先接 SSS / 材质派生语义，优先 `Aov.SurfaceData`。

两者都不应绕过本阶段建立的 Resource Registry、DebugView Registry 和 explicit consumer 读取路径。
