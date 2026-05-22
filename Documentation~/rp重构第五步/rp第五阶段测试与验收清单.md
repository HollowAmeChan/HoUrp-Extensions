# RP 第五阶段测试与验收清单

## 自动检查

| 检查 | 状态 |
| --- | --- |
| registry count / link tests | 已补充：resources 7、semantics 25、debug views 25，并覆盖 SSS input 链接 |
| `Aov.SssSource` resource descriptor tests | 已补充：`R16G16B16A16_SFloat`、full scale、zero clear |
| shader property mapping tests | 已补充：`_HoUrpAovSssSourceTexture` / `_HoUrpSssSourceColor` / `_HoUrpSssWeight` |
| DebugView source/channel mapping tests | 已补充：`AOV.SssSource`、`AOV.SssWeight`、`SSS.Thickness` |
| consumer consumed resource declaration tests | 已补充：`SemanticPostProcess` 显式消费 `Aov.SssSource` |
| `git diff --check` | 通过；Git 仅提示 LF/CRLF 转换 |
| Unity batchmode / EditMode tests | 当前包目录没有独立 Unity project / `.csproj`，未运行 |

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
| AOV Output | Frame Debugger 中 `HoURP AOV Output` 写入新增 `Aov.SssSource` MRT |
| Debug SssSource | camera color 显示 SSS source color |
| Debug SssWeight | camera color 显示 SSS weight 灰度图 |
| Debug AllRegistered | 新增 SSS 输入 DebugView 自动进入平铺输出 |
| Consumer probe | SSS 输入命中的 opaque 区域出现额外 tint / weight |
| 空值区域 | 天空/未覆盖区域保持 `(0,0,0,0)`，不参与 SSS |

## 冻结条件

| 条件 | 状态 |
| --- | --- |
| `Aov.SssSource` 已注册 | 已实现 |
| `Shading.SssSourceColor` 已注册 | 已实现 |
| `Shading.SssWeight` 已注册 | 已实现 |
| AovOutput 显式写 `Aov.SssSource` MRT | 已实现 |
| DebugView 能映射到 `Aov.SssSource` resource/channel | 已实现 |
| Consumer 显式读取 `Aov.SssSource` | 已实现 |
| 不依赖旧 `_lilHoAovSssTexture` / `_HoSSS*` 逻辑名 | 已实现；旧名只保留在 registry legacy reference / 文档审查语境 |
| 不修改 `lilToon` / `lilPBR` | 已确认 |
| 未决项已登记 | 已登记在 `rp第五阶段未决项登记.md` |
