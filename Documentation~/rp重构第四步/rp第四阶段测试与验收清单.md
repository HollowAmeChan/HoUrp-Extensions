# RP 第四阶段测试与验收清单

## 自动检查

| 检查 | 状态 |
| --- | --- |
| registry count / link tests | 已补充测试 |
| `Aov.SurfaceData` resource descriptor tests | 已补充测试 |
| `Aov.MaterialCustom0_3` resource descriptor tests | 已补充测试 |
| shader property mapping tests | 已补充测试 |
| DebugView source/channel mapping tests | 已补充测试 |
| SemanticPost consumed resource declaration tests | 已补充测试 |
| `git diff --check` | 通过 |
| Unity batchmode / EditMode tests | 当前包目录没有独立 Unity project / `.csproj` |

## 手动 Unity 验收

在 Unity 中挂载：

1. `HoURP AOV Output`
2. `HoURP Semantic Post Process`
3. 可选：`HoURP AOV Debug`

给测试对象添加：

1. `ObjectSemanticAuthoring`
2. `MaterialSemanticAuthoring`

设置：

```text
thickness = 1
curvature = 0.5
materialCustom0 = 1
```

期望：

| 场景 | 期望 |
| --- | --- |
| AOV Output | Frame Debugger 中 `HoURP AOV Output` 写入新增材质语义 MRT。 |
| Debug Thickness | camera color 显示 Thickness 灰度图。 |
| Debug Curvature | camera color 显示 Curvature 灰度图。 |
| Debug MaterialCustom0 | camera color 显示 MaterialCustom0 灰度图。 |
| Debug AllRegistered | 新增材质语义 DebugView 自动进入平铺输出。 |
| SemanticPost | 材质语义命中的 opaque 区域出现额外 tint / weight。 |
| 空值区域 | 天空/未覆盖区域保持 zero，不被解释为合法 material profile。 |

## 冻结条件

| 条件 | 状态 |
| --- | --- |
| `Aov.SurfaceData` 已注册 | 通过 |
| `Aov.MaterialCustom0_3` 已注册 | 通过 |
| `Material.Class` / `Material.SssProfile` / `Material.Thickness` / `Material.Curvature` 已注册 | 通过 |
| `Material.Custom0..3` 已注册 | 通过 |
| `Material.Utility` 有明确未决或生产判定 | 通过，已注册但第四阶段不生产稳定 resource channel |
| AovOutput 显式写新增材质语义 MRT | 通过 |
| DebugView 能映射到新增 resource/channel | 通过 |
| SemanticPost 显式读取新增资源 | 通过 |
| 不依赖旧 `_lilHoAov*` / `_HoAov*` 逻辑名 | 通过，旧名只在 legacy 字段和文档中保留 |
| 不修改 `lilToon` / `lilPBR` | 通过 |
| 未决项已登记 | 通过 |

## 本轮已知限制

- 未运行 Unity batchmode / EditMode tests；当前目录是 Unity package，没有独立 Unity project / `.csproj`。
- 未做 RenderDoc 自动截帧。
- 未创建 package sample scene。
- `Material.Utility` 本阶段只注册，不写入 `Aov.SurfaceData` 或 `Aov.MaterialCustom0_3`。
- 旧 `SurfaceData` / HoSSS 视觉一致性不在第四阶段验收范围内。
