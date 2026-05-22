# RP 第十阶段材质 Shader ABI 审查

## 目标

第十阶段的核心不是先做完整材质系统，而是冻结新材质 shader 与 `HoUrp-Extensions` 的第一版 ABI。这个 ABI 必须服务当前已经成立的资源链路：

```text
Generated material
  -> AovOutput
  -> SSS / SemanticPost / Debug
```

同时它还必须给第十一步 Weighted OIT runtime 铺路：

```text
Generated transparent material
  -> HoUrpOitAccumulation pass
  -> Oit.Accumulation / Oit.Revealage
```

## 不继承旧 ABI

第十阶段可以阅读旧 `lilToon/lilPBR`，但不能继承：

| 旧项 | 第十阶段处理 |
| --- | --- |
| `HoAOV` / `HoAOVSSS` | 旧 pass 名，只作为参照 |
| `lilToonOIT` | 旧 OIT pass 名，不进入新 ABI |
| `_lilHoAov*` | 旧资源名，不进入新 shader binding |
| `_HoAov*` | 旧材质属性名，不进入新 ABI |
| `_lilOITEnabled` | 旧材质开关，替换为 `SupportsOit` / `ParticipatesOit` |
| `_lilOITActive` | 旧 runtime phase 标记，不作为材质语义 |
| `lilToon/lilPBR` include | 不引用 |
| URP Lit full include / inspector / keyword | 不引用，不继承 |

第十阶段的最小 shader 应完全独立实现，只允许依赖 URP 基础 shader library，例如 `Core.hlsl`、矩阵变换和基础纹理采样。

## 必须定义的 HLSL ABI

建议新增：

```text
Runtime/Shaders/ShaderLibrary/HoUrpMaterialSurface.hlsl
Runtime/Shaders/ShaderLibrary/HoUrpMaterialAov.hlsl
Runtime/Shaders/ShaderLibrary/HoUrpMaterialOit.hlsl
```

### `SurfaceData`

| Field | 必要性 | 用途 |
| --- | --- | --- |
| `baseColor` | 必须 | forward 显示、SSS source、OIT color |
| `alpha` | 必须 | forward coverage、AOV、OIT |
| `normalWS` | 必须 | forward/debug、AOV normal |
| `normalTS` | 可选 | 后续 tangent normal |
| `roughness` | 可选 | 后续 PBR |
| `metallic` | 可选 | 后续 PBR |
| `emission` | 可选 | 后续 composite |
| `occlusion` | 可选 | 后续 shading |

`alpha` 在第十阶段必须进入统一结构，因为第十一步 OIT runtime 直接依赖它。

### `MaterialSemanticData`

| Field | 对应语义 | AOV 目标 |
| --- | --- | --- |
| `materialClass` | `Material.Class` | `Aov.SurfaceData` |
| `sssProfile` | `Material.SssProfile` | `Aov.SurfaceData` |
| `thickness` | `Material.Thickness` | `Aov.SurfaceData` |
| `curvature` | `Material.Curvature` | `Aov.SurfaceData` |
| `materialCustom0_3` | `Material.Custom0-3` | `Aov.MaterialCustom0_3` |
| `sssSourceColor` | `Sss.SourceColor` | `Aov.SssSource.rgb` |
| `sssWeight` | `Sss.Weight` | `Aov.SssSource.a` |

这些字段是新材质 producer 的语义输出，不等同旧材质属性名。

### `AovOutputData`

`AovOutputData` 负责把材质语义、几何信息和 shading 输入编码到现有 AOV 资源。第十阶段不新增 AOV MRT。

约束：

- Object 静态语义仍由 AOV pass 从 RSUV / MPB 解码。
- Material 语义由 generated shader 生产。
- `Aov.MaskId.b` 的 group / high flags 布局遵守第九阶段契约。
- 不新增 `SV_Target7`。
- 不新增 `Aov.ObjectFeatureMask` 或 high flags 专用资源。

### `TransparentOutputData`

| Field | 用途 |
| --- | --- |
| `color` | transparent / OIT color |
| `alpha` | transparent opacity |
| `coverage` | alpha clip / dither 后覆盖率；第一版可等于 `alpha` |
| `depthWeight` | OIT weight 的深度相关输入；第一版可由统一函数计算 |
| `supportsOit` | preset / capability 元数据 |
| `participatesOit` | 当前材质实例是否进入 OIT |

`supportsOit` / `participatesOit` 不应编译成旧 `_lilOITEnabled`。

### `OitAccumulationData`

| Field | 用途 |
| --- | --- |
| `weightedColor` | 写入 `Oit.Accumulation.rgb` 的输入 |
| `weightedAlpha` | 写入 `Oit.Accumulation.a` 的输入 |
| `revealage` | 写入 `Oit.Revealage` 的输入 |
| `weight` | weighted blended OIT 权重 |

第十阶段只定义 shader 输出和 pass，不创建 `Oit.*` RenderGraph resource。

## Pass ABI

| Pass | LightMode | 第十阶段要求 |
| --- | --- | --- |
| Forward | `UniversalForward` | 独立最小显示，基础 base color / simple light / debug normal |
| AOV Output | `HoUrpAovOutput` | 必须写出材质语义和 SSS 输入 |
| OIT Accumulation | `HoUrpOitAccumulation` | 必须存在，供第十一步 runtime 绘制 |
| DepthOnly | `DepthOnly` | 可只定义边界 |
| ShadowCaster | `ShadowCaster` | 可只定义边界 |

第十阶段不是完整 forward lighting 阶段。Forward pass 只用于让材质可见、可调试。

## 验收问题

- 是否所有新 include 都使用 `HoUrp` 命名？
- 是否没有引用 `lilToon/lilPBR` include？
- 是否没有引用 URP Lit full pass include？
- 是否 `SurfaceData.alpha` 能同时被 forward / AOV / OIT pass 使用？
- 是否 `AovOutputData` 没有增加第 8 个 color target？
- 是否 `HoUrpOitAccumulation` 不依赖旧 `_lilOIT*`？

## 风险

- 把 URP Lit 裁剪版误当成独立 shader，实际仍继承隐藏 keyword 和 inspector 假设。
- 为了快速验证 OIT，偷用旧 `lilToonOIT` pass。
- 在第十步新增 OIT RenderGraph resource，提前越界到第十一步。
- AOV 输出为了方便又增加 MRT，重现第九阶段 RenderGraph attachment 上限问题。
