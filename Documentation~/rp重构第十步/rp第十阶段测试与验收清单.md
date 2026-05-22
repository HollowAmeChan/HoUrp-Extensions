# RP 第十阶段测试与验收清单

## 自动检查

| 检查 | 期望 |
| --- | --- |
| HLSL ABI files | 存在 `HoUrpMaterialSurface.hlsl`、`HoUrpMaterialAov.hlsl`、`HoUrpMaterialOit.hlsl` 或等价实现 |
| `SurfaceData` | 包含 `baseColor`、`alpha`、`normalWS` |
| `MaterialSemanticData` | 覆盖 material class、SSS profile、thickness、curvature、material custom、SSS source、SSS weight |
| `AovOutputData` | 不新增第 8 个 MRT |
| `TransparentOutputData` | 包含 color、alpha、coverage、OIT participation 信息 |
| `OitAccumulationData` | 包含 weighted color / alpha、revealage、weight |
| generated shader | 包含 `HoUrpAovOutput` pass |
| generated shader | 包含 `HoUrpOitAccumulation` pass |
| generated shader | 包含独立 `UniversalForward` 最小 pass |
| generated shader | 不包含 `lilToon`、`lilPBR`、`_lilHoAov`、`_HoAov` |
| generated shader | 不包含 `lilToonOIT`、`_lilOITEnabled`、`_lilOITActive` |
| generated shader | 不引用 URP Lit full include / pass include |
| preset descriptor | 能查询 produced semantics |
| preset descriptor | 能查询 supported passes |
| preset descriptor | 能查询 `SupportsOit` / `ParticipatesOit` |
| contract registry | 能标记 material semantic 可由 `GeneratedMaterial` producer 生产 |
| `git diff --check` | 无空白错误 |

## 建议新增测试

```text
Tests/Runtime/HoUrpMaterialShaderAbiTests.cs
Tests/Runtime/HoUrpMaterialPresetContractTests.cs
Tests/Runtime/HoUrpGeneratedMaterialContractTests.cs
```

测试方向：

```csharp
MaterialPreset exposes HoUrpAovOutput pass
MaterialPreset exposes HoUrpOitAccumulation pass
MaterialPreset exposes Material.Class / Sss.Weight semantics
MaterialPreset exposes SupportsOit / ParticipatesOit
Generated shader text does not contain old OIT ABI
Generated shader text does not contain old AOV ABI
HoUrpBuiltInContracts allows GeneratedMaterial producer for material semantics
```

## 手动 Unity 验收

挂载：

1. `HoURP AOV Output`
2. `HoURP Subsurface Scattering`
3. `HoURP Semantic Post Process`
4. `HoURP AOV Debug`

不挂载：

5. `HoURP Weighted OIT`

第十阶段不应该需要 Weighted OIT runtime 才能验收材质 ABI。

## 场景准备

创建两个对象：

| 对象 | 材质 | 说明 |
| --- | --- | --- |
| A | `MaterialSemanticAuthoring` + 简单材质 | 对照组 |
| B | 第十步 generated shader prototype | 新材质 producer |

B 的 prototype preset 建议：

```text
Character_DebugLit_SSS_OITReady
```

设置：

- `baseColor` 明显可见。
- `alpha` 非 1，例如 0.5，用于 OIT-ready 验证。
- `materialClass` 非 0。
- `sssWeight` 非 0。
- `sssSourceColor` 明显可见。
- `SupportsOit=true`。
- `ParticipatesOit=true`。

## AOV / SSS 验收

| Debug View | 期望 |
| --- | --- |
| `AOV.MaterialClass` | B 能显示 generated material 的 class |
| `AOV.Thickness` | B 能显示 generated material 的 thickness |
| `AOV.Curvature` | B 能显示 generated material 的 curvature |
| `AOV.MaterialCustom0-3` | B 能显示 generated material custom |
| `AOV.SssSource` | B 能显示 generated material 的 SSS source color |
| `AOV.SssWeight` | B 能显示 generated material 的 SSS weight |
| `SSS.Source` | 能读取 B 的 SSS source |
| `SSS.Diffusion` | 能受 B 的 SSS 输入影响 |
| `SemanticPost.Mask` | 能按材质语义规则命中 B |

## Forward 验收

| 检查 | 期望 |
| --- | --- |
| `UniversalForward` | B 可见 |
| base color | 与材质参数一致 |
| alpha | 至少进入 shader 结构；是否透明显示可按第十步实现边界说明 |
| normal/debug light | 可用于判断 shader 正常执行 |

## OIT-ready 验收

第十阶段不画 OIT composite，但必须确认材质侧已经准备好。

| 检查 | 期望 |
| --- | --- |
| Shader pass | 存在 `HoUrpOitAccumulation` |
| Frame Debugger / RenderDoc | 能识别 prototype shader 的 OIT pass，或至少 shader importer pass 列表可见 |
| preset metadata | `SupportsOit=true` |
| preset metadata | `ParticipatesOit=true` |
| shader text | 不含 `lilToonOIT` |
| shader text | 不含 `_lilOITEnabled` / `_lilOITActive` |
| runtime | 不需要 `Oit.*` resource 也能正常显示 / AOV 输出 |

## RenderGraph 验收

| 检查 | 期望 |
| --- | --- |
| AOV pass color attachment | 仍不超过第九阶段限制 |
| `SV_Target7` | 不出现 |
| `Oit.*` resources | 第十步不创建 |
| 控制台 | 无 RenderGraph attachment 上限错误 |
| 控制台 | 无 live camera color 同 pass 读写错误 |

## 当前代码落地检查

第十阶段完成时，应能在代码中找到或解释未落地原因：

```text
Runtime/Shaders/ShaderLibrary/HoUrpMaterialSurface.hlsl
Runtime/Shaders/ShaderLibrary/HoUrpMaterialAov.hlsl
Runtime/Shaders/ShaderLibrary/HoUrpMaterialOit.hlsl
Runtime/Shaders/Generated/HoUrpDebugLitMinimal.shader
Runtime/Semantic/MaterialFeatureBlockDefinition.cs
Runtime/Semantic/MaterialPresetDefinition.cs
Tests/Runtime/HoUrpMaterialShaderAbiTests.cs
```

如果第十阶段只先做手写 prototype shader，没有 generator，也必须在未决项说明。

## 未决项

| 项 | 当前处理 |
| --- | --- |
| 完整 shader generator | 不做，第十步只允许最小原型 |
| 材质 inspector | 不做 |
| HoPbr / HoNpr 接入 | 后续 |
| Weighted OIT runtime | 第十一步 |
| OIT composite shader | 第十一步 |
| transparent SSS / transparent AOV | 第十二步或后续 |
| HoShadow receiver | 后续 shadow/material 阶段 |

## 风险点

- 第十步没有 OIT pass，导致第十一步无法直接测 OIT。
- 第十步做了 OIT runtime，越过大纲边界。
- generated shader 仍引用旧材质 include。
- preset 描述缺少 `SupportedPasses`，后续 runtime 无法查询。
- AOV 输出和 OIT 输出各自定义 alpha，导致透明行为不一致。
