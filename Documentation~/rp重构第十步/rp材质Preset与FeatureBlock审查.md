# RP 第十阶段 Material Preset / Feature Block 审查

## 目标

第十阶段需要建立最小的材质组合描述，让 generated shader 的结构可查询、可测试、可 diff。它不是完整材质生成器，也不是厚 UI。

目标结构：

```text
MaterialPreset
  -> FeatureBlock list
  -> Produced semantics
  -> Supported passes
  -> Required capabilities
  -> Phase policy
```

## Feature Block

建议第一版定义：

```text
MaterialFeatureBlock
  Id
  DisplayName
  Domain
  RequiredInputs
  ProducedFields
  RequiredIncludes
  CompatibleTemplates
```

首批 block：

| Block | Domain | Produced |
| --- | --- | --- |
| `BaseColorConstant` | Material | `SurfaceData.baseColor` |
| `BaseColorTexture` | Material | `SurfaceData.baseColor`, `SurfaceData.alpha` |
| `DebugNormal` | Geometry / Debug | `SurfaceData.normalWS` |
| `SkinSss` | Shading | `MaterialSemanticData.sssSourceColor`, `sssWeight`, `sssProfile` |
| `MaterialClass` | Material | `MaterialSemanticData.materialClass` |
| `MaterialCustom` | Material | `MaterialSemanticData.materialCustom0_3` |
| `AovOutputStandard` | AOV | `AovOutputData` |
| `OitTransparent` | Composite / Transparent | `TransparentOutputData`, `OitAccumulationData` |

## Material Preset

建议第一版定义：

```text
MaterialPreset
  Id
  DisplayName
  Template
  FeatureBlocks
  DefaultProperties
  ProducedSemantics
  RequiredCapabilities
  SupportedPasses
  PhasePolicy
```

第十阶段 prototype preset 应优先选择 OIT-ready 版本：

```text
Character_DebugLit_SSS_OITReady
```

原因：

- AOV / SSS / SemanticPost 可以在第十步验证。
- `HoUrpOitAccumulation` 可以在第十步被 Frame Debugger / RenderDoc 识别。
- 后续透明阶段可以直接接 Weighted OIT runtime。

## Preset 必须声明的内容

| 项 | 第十阶段要求 |
| --- | --- |
| `ProducedSemantics` | `Material.Class`, `Material.SssProfile`, `Material.Thickness`, `Material.Curvature`, `Material.Custom0-3`, `Sss.SourceColor`, `Sss.Weight` |
| `SupportedPasses` | `UniversalForward`, `HoUrpAovOutput`, `HoUrpOitAccumulation` |
| `RequiredCapabilities` | `SupportsOit` 可选，prototype 必须声明 |
| `PhasePolicy` | `ParticipatesOit` 时普通透明 forward 是否跳过 |
| `DefaultProperties` | base color、alpha、material class、sss profile、thickness、curvature、sss weight |

## 不做

- 不做 inspector 动态增删 block。
- 不做运行时 shader 拼装。
- 不做任意组合开放。
- 不做完整 PBR / NPR preset 集。
- 不把 preset 变成 RenderFeature。
- 不让 UI 决定 pass 是否存在。

## 验收问题

- 能否从 preset 查询它生产哪些语义？
- 能否从 preset 查询它支持哪些 pass？
- 能否从 preset 查询它是否 OIT-ready？
- `OitTransparent` block 是否只定义 shader 输出，不创建 runtime resource？
- 是否没有旧材质属性名进入 `DefaultProperties`？

## 风险

- FeatureBlock 变成 UI 面板，而不是编译期结构描述。
- Preset 变成无限 keyword 组合。
- `SupportsOit` 被做成旧 `_lilOITEnabled` 的别名。
- 只做 AOV preset，忘记后续 OIT runtime 需要 `HoUrpOitAccumulation`。
