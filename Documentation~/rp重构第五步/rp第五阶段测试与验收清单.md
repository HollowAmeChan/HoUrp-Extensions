# RP 第五阶段测试与验收清单

## 自动检查

| 检查 | 状态 |
| --- | --- |
| registry count / link tests | 待实现后补充 |
| `Aov.SssSource` resource descriptor tests | 待实现后补充 |
| shader property mapping tests | 待实现后补充 |
| DebugView source/channel mapping tests | 待实现后补充 |
| consumer consumed resource declaration tests | 待实现后补充 |
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
sssSourceColor = non-black
sssWeight = 1
sssProfile = non-zero
thickness = non-zero
```

期望：

| 场景 | 期望 |
| --- | --- |
| AOV Output | Frame Debugger 中 `HoURP AOV Output` 写入新增 `Aov.SssSource` MRT。 |
| Debug SssSource | camera color 显示 SSS source color。 |
| Debug SssWeight | camera color 显示 SSS weight 灰度图。 |
| Debug AllRegistered | 新增 SSS 输入 DebugView 自动进入平铺输出。 |
| Consumer probe | SSS 输入命中的 opaque 区域出现额外 tint / weight。 |
| 空值区域 | 天空/未覆盖区域保持 `(0,0,0,0)`，不参与 SSS。 |

## 冻结条件

| 条件 | 状态 |
| --- | --- |
| `Aov.SssSource` 已注册 | 待实现 |
| `Shading.SssSourceColor` 已注册 | 待实现 |
| `Shading.SssWeight` 已注册 | 待实现 |
| AovOutput 显式写 `Aov.SssSource` MRT | 待实现 |
| DebugView 能映射到 `Aov.SssSource` resource/channel | 待实现 |
| Consumer 显式读取 `Aov.SssSource` | 待实现 |
| 不依赖旧 `_lilHoAovSssTexture` / `_HoSSS*` 逻辑名 | 待实现 |
| 不修改 `lilToon` / `lilPBR` | 待实现 |
| 未决项已登记 | 待实现 |

