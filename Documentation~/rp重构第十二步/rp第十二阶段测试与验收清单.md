# RP 第十二阶段测试与验收清单

## 自动检查

| 检查 | 期望 |
| --- | --- |
| Feature registry | 存在 `TransparentOit` 或最终确定的 OIT runtime feature |
| Stage | OIT runtime feature 使用 `HoUrpPassStage.TransparentOit` |
| Produced resources | 包含 `Oit.OpaqueColor`、`Oit.Accumulation`、`Oit.Revealage`、`Oit.CompositeSource` |
| GeneratedMaterial | 不拥有 OIT runtime resources |
| Semantics | `OIT.AccumulationInput` / `OIT.RevealageInput` producer 仍为 material side |
| Consumers | `TransparentOit` 消费 OIT material semantics |
| Capabilities | `SupportsOit` / `ParticipatesOit` 保持 material capability |
| Resource desc | `Oit.Accumulation` 使用 HDR accumulation format |
| Resource desc | `Oit.Revealage` clear 为 one / white |
| Resource desc | `Oit.OpaqueColor` / `Oit.CompositeSource` 使用 camera color format |
| Shader name | composite shader 是 `Hidden/HoURP/OIT/WeightedComposite` |
| Pass tag | material pass 是 `HoUrpOitAccumulation` |
| Legacy ABI | shader / constants 不包含 `_lilOIT` |
| Legacy pass | shader / constants 不包含 `lilToonOIT` |
| Active state | 使用 `_HoUrpOitActive` |
| RenderGraph | composite 前有 `Oit.CompositeSource` copy |
| RenderGraph | accumulation / revealage 由显式 pass 写入 |
| RenderGraph | composite pass 不读写同一个 camera color handle |
| Post 回归 | ScreenPost / ImagePost 不默认消费 OIT runtime resources |
| `git diff --check` | 无空白错误 |

## 建议新增测试文件

```text
Tests/Runtime/HoUrpOitContractRegistryTests.cs
Tests/Runtime/HoUrpOitResourceDeclarationTests.cs
Tests/Runtime/HoUrpOitShaderAbiTests.cs
Tests/Runtime/HoUrpOitRuntimeStructureTests.cs
```

也可以先合并进现有：

```text
Tests/Runtime/HoUrpContractRegistryTests.cs
Tests/Runtime/HoUrpRenderGraphResourceDeclarationTests.cs
Tests/Runtime/HoUrpMaterialShaderAbiTests.cs
```

## 测试用例建议

```text
TransparentOitFeatureDescriptorOwnsRuntimeResources
GeneratedMaterialDoesNotOwnOitRuntimeResources
TransparentOitConsumesOitMaterialSemantics
OitAccumulationResourceUsesHdrFormatAndZeroClear
OitRevealageResourceUsesOneClear
OitShaderPropertyIdsUseHoUrpNames
WeightedOitCompositeShaderUsesHoUrpNames
WeightedOitCompositeShaderDoesNotUseLegacyLilNames
GeneratedDebugLitShaderUsesHoUrpOitAccumulationPass
GeneratedDebugLitShaderDoesNotUseLilToonOitPass
ScreenPostDoesNotConsumeOitRuntimeResources
ImagePostDoesNotConsumeOitRuntimeResources
```

## Runtime 结构检查

如果 editmode 无法直接执行 RenderGraph，至少检查：

| 检查 | 期望 |
| --- | --- |
| Settings default | enabled true、queue range 覆盖 transparent、weight 合理 |
| ShaderTagId | 使用 `HoUrpOitAccumulation` |
| FilteringSettings | 使用 layer mask / render queue |
| Disabled feature | 不 enqueue accumulation / composite |
| Missing shader | warning 并不执行 composite |
| Final reset | composite 后 `_HoUrpOitActive = 0` |

## 手动 Unity 验收场景

### 基础场景

Renderer Features：

1. `HoURP AOV Output` 可选。
2. `HoURP Subsurface Scattering` 可选。
3. `HoURP Weighted OIT`。
4. `HoURP ScreenPost Prototype` 可选。
5. `HoURP ImagePost Prototype` 可选。
6. `HoURP AOV Debug` 可选。

场景内容：

- 一个 opaque 背景。
- 两个交错深度的透明 mesh。
- 使用 `HoUrpDebugLitMinimal` 或最小 generated material。
- material 支持并参与 OIT。

### 验收 1：OIT 基础可见

操作：

1. 关闭 ScreenPost / ImagePost。
2. 开启 OIT。
3. 调整两个透明 mesh 深度和颜色。

期望：

- 透明对象影响最终画面。
- 深度交错时比普通 alpha sorting 更稳定。
- RenderGraph 中可见 OIT pass 链。

### 验收 2：资源有效

打开 debug：

- `OIT.Accumulation`
- `OIT.Revealage`

期望：

- accumulation 非全黑。
- revealage 在透明区域变化。
- 无透明对象时资源为空或 clear 状态可解释。

### 验收 3：禁用清理

操作：

1. 开启 OIT 并观察画面。
2. 禁用 OIT renderer feature。
3. 重新播放 / 刷新 camera。

期望：

- OIT 影响消失。
- Debug 不显示上一帧 OIT 图。
- `_HoUrpOitActive` 不残留为 1。
- 控制台无持续错误。

### 验收 4：Post 回归

操作：

1. 开启 OIT。
2. 开启 ScreenPost prototype。
3. 开启 ImagePost prototype。

期望：

- OIT composite 后的透明结果仍被 ImagePost 影响。
- ScreenPost 不读取 OIT 中间资源。
- ImagePost 不读取 OIT 中间资源。
- 没有 camera color read/write conflict。

### 验收 5：旧 ABI 排查

搜索：

```powershell
rg -n "_lilOIT|lilToonOIT|Hidden/lilToon/URP/WeightedOITComposite" Runtime Tests
```

期望：

- 只允许出现在测试的禁止断言或文档中。
- Runtime shader / C# constants 不出现旧 ABI。

## 通过条件

第十二阶段通过必须满足：

- `HoUrpOitAccumulation` pass 被 runtime draw。
- `Oit.Accumulation` / `Oit.Revealage` 被显式创建、清理、写入、读取。
- OIT composite 写回 camera color。
- 禁用后无 stale state。
- 不继承旧 `_lilOIT*` / `lilToonOIT`。
- Contract / resource / shader ABI 测试通过。
- `git diff --check` 通过。

