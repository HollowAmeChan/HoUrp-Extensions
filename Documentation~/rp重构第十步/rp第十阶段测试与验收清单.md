# RP 第十阶段测试与验收清单

## 自动检查

| 检查 | 期望 |
| --- | --- |
| HLSL ABI files | 存在 `HoUrpObjectSemantic.hlsl`、`HoUrpMaterialSurface.hlsl`、`HoUrpMaterialAov.hlsl`、`HoUrpMaterialOit.hlsl` 或等价实现 |
| object semantic ABI | 对象语义从 RSUV / MPB 解析，材质 asset 不暴露 `AOV Mask Weight`、ObjectId、GroupId、Flags、ObjectCustom 等对象字段 |
| `SurfaceData` | 包含 `baseColor`、`alpha`、`normalWS` |
| `MaterialSemanticData` | 覆盖 material class、SSS profile、thickness、curvature、material custom、SSS source、SSS weight |
| `AovOutputData` | 不新增第 8 个 MRT |
| `AovOutputData` | byte-like material semantic 不被 fractional mask coverage 压低到不可解码；mask 只 gate participation，SSS contribution 可继续按 coverage 缩放 |
| `TransparentOutputData` | 包含 color、alpha、coverage、OIT participation 信息 |
| `OitAccumulationData` | 包含 weighted color / alpha、revealage、weight |
| generated shader | 包含 `HoUrpAovOutput` pass |
| generated shader | 包含 `HoUrpOitAccumulation` pass |
| generated shader | 包含独立 `UniversalForward` 最小 pass |
| generated shader | 只暴露材质侧参数；对象/RSUV 字段通过 object semantic ABI 消费，不进入材质 Properties 面板 |
| generated shader | 不包含 `lilToon`、`lilPBR`、`_lilHoAov`、`_HoAov` |
| generated shader | 不包含 `lilToonOIT`、`_lilOITEnabled`、`_lilOITActive` |
| generated shader | 不引用 URP Lit full include / pass include |
| AOV renderer feature | 保留 fallback overrideMaterial 路径，并额外绘制 explicit `HoUrpAovOutput` pass；explicit pass 必须覆盖 transparent queue |
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
AOV renderer draws explicit HoUrpAovOutput passes across render queues
Generated material shader does not expose object/RSUV fields as material Properties
Material AOV ABI does not scale material semantic ids by fractional coverage
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
| B | `ObjectSemanticAuthoring` + 第十步 generated shader prototype | 新材质 producer；对象语义来自 ObjectSemanticAuthoring / RSUV，材质语义来自 generated material |

注意：

- B 不应同时挂 `MaterialSemanticAuthoring`。该组件会通过 `MaterialPropertyBlock` 覆盖 `_HoUrpMaterial*`，用于对照组而不是 generated material producer。
- 如果曾经在同一个 Renderer 上挂过 `MaterialSemanticAuthoring`，要确认没有残留 MPB 覆盖材质侧语义。prototype shader 的材质侧属性应使用 generated-material 专用命名，避免被过渡组件覆盖。
- generated material 面板不应出现 AOV Mask Weight / Object Id / Object Group Id / Object Flags / Object Custom Mask。这些属于对象侧 authoring。

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
- `ObjectSemanticAuthoring.MaskWeight` 可为 1 做首轮验收；如果使用 fractional mask，material class/profile 等 byte-like 语义仍应能被 debug view 解码。

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

Frame Debugger / RenderDoc 侧应能看到 AOV pass 中 B 使用自己的 `HoUrpAovOutput` shader pass，而不是只被 fallback `AovOutputFallback` overrideMaterial 绘制。fallback path 仍用于没有 explicit AOV pass 的普通/对照材质。

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
Runtime/Shaders/ShaderLibrary/HoUrpObjectSemantic.hlsl
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
| HoNpr 统一材质系统接入 | 后续 |
| Weighted OIT runtime | 后续透明阶段 |
| OIT composite shader | 后续透明阶段 |
| transparent SSS / transparent AOV | 第十二步或后续 |
| HoShadow receiver | 后续 shadow/material 阶段 |

## 风险点

- 第十步没有 OIT pass，导致后续无法直接测 OIT。
- 第十步做了 OIT runtime，越过大纲边界。
- generated shader 仍引用旧材质 include。
- preset 描述缺少 `SupportedPasses`，后续 runtime 无法查询。
- AOV 输出和 OIT 输出各自定义 alpha，导致透明行为不一致。
- generated material 与 `MaterialSemanticAuthoring` 使用同名材质语义属性，导致 MPB 覆盖材质 asset 参数。
- AOV renderer 只走 opaque fallback overrideMaterial，导致 transparent generated shader 的 `HoUrpAovOutput` pass 不会写入 HoAOV。
- 将 Object/RSUV 字段暴露在 generated material 面板上，造成“可调但无效”的错误 authoring 入口。
- fractional `AOV Mask Weight` 被直接乘到 material class/profile 这类 byte-like 语义上，导致 debug 解码回 0。
