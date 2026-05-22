# RP 第四阶段测试与验收清单

## 自动检查

| 检查 | 状态 |
| --- | --- |
| registry count / link tests | 待实现后补充 |
| `Aov.SurfaceData` resource descriptor tests | 待实现后补充 |
| `Aov.MaterialCustom0_3` resource descriptor tests | 待实现后补充 |
| shader property mapping tests | 待实现后补充 |
| DebugView source/channel mapping tests | 待实现后补充 |
| SemanticPost consumed resource declaration tests | 待实现后补充 |
| `git diff --check` | 规划阶段通过后更新 |
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
| `Aov.SurfaceData` 已注册 | 待实现 |
| `Aov.MaterialCustom0_3` 已注册 | 待实现 |
| `Material.Class` / `Material.SssProfile` / `Material.Thickness` / `Material.Curvature` 已注册 | 待实现 |
| `Material.Custom0..3` 已注册 | 待实现 |
| `Material.Utility` 有明确未决或生产判定 | 待实现 |
| AovOutput 显式写新增材质语义 MRT | 待实现 |
| DebugView 能映射到新增 resource/channel | 待实现 |
| SemanticPost 显式读取新增资源 | 待实现 |
| 不依赖旧 `_lilHoAov*` / `_HoAov*` 逻辑名 | 待实现 |
| 不修改 `lilToon` / `lilPBR` | 待实现 |
| 未决项已登记 | 待实现 |

