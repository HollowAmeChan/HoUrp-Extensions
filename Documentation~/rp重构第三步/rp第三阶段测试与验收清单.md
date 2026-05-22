# RP 第三阶段测试与验收清单

## 自动检查

| 检查 | 状态 |
| --- | --- |
| registry count / link tests | 待完成 |
| ObjectCustom resource descriptor tests | 待完成 |
| shader property mapping tests | 待完成 |
| `git diff --check` | 待运行 |

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
| SemanticPost | 只有 ObjectCustom 命中的 opaque 区域出现额外 tint 权重。 |

## 冻结条件

- `Aov.ObjectCustom0_3` / `Aov.ObjectCustom4_7` 已注册。
- `Object.Custom0..7` 已注册。
- DebugView 已注册并能映射到对应 resource/channel。
- AovOutput 显式写 ObjectCustom MRT。
- SemanticPost 显式读 ObjectCustom MRT。
- 不依赖旧 `_lilHoAov*` 逻辑名。
- 未决项已登记。

