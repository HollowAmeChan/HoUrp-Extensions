# RP AOV 生命周期审查

## 目标

第九阶段先把 AOV 从“一组 MRT”整理成可查询的生命周期链路：

```text
Authoring
  -> Renderer Static Semantic / MPB
  -> AovOutput resources
  -> Derived resources
  -> Debug views
```

这份文档用于约束后续实现：每个 AOV 相关值必须能说明来源、生产者、消费者、生命周期和 debug 观察入口。

## 当前链路

当前新包已经跑通：

```text
ObjectSemanticAuthoring
MaterialSemanticAuthoring
  -> MaterialPropertyBlock
  -> AovOutputRendererFeature
  -> Aov.* resources
  -> SubsurfaceScatteringRendererFeature
  -> SemanticPostProcessRendererFeature
  -> AovDebugRendererFeature
```

关键文件：

```text
Runtime/Semantic/ObjectSemanticAuthoring.cs
Runtime/Semantic/MaterialSemanticAuthoring.cs
Runtime/Features/AovOutputRendererFeature.cs
Runtime/Features/SubsurfaceScatteringRendererFeature.cs
Runtime/Features/SemanticPostProcessRendererFeature.cs
Runtime/Debug/AovDebugRendererFeature.cs
Runtime/Core/HoUrpBuiltInNames.cs
Runtime/Core/HoUrpBuiltInContracts.cs
Runtime/RenderGraph/HoUrpAovResourceDeclaration.cs
```

## 生命周期分层

### Static Authoring

作者输入，不是 RenderGraph 资源。

| Source | Domain | 当前写入方式 | 说明 |
| --- | --- | --- | --- |
| `ObjectSemanticAuthoring` | Object | MPB | 第九步可增加 RSUV fast path |
| `MaterialSemanticAuthoring` | Material / Shading | MPB | 第九步不进入 RSUV |
| Inspector preset | Object / Material | 写 authoring 字段 | preset 不是 ABI |

生命周期：

```text
Scene / Prefab / Authoring-time
```

### Renderer Static Semantic

每个 renderer 上可提前绑定的对象语义。旧实现里的 RSUV 属于这一层。

第一版字段：

| Field | Semantic | 当前 AOV 映射 | 是否可 RSUV |
| --- | --- | --- | --- |
| object custom mask | `Object.Custom0-7` | `Aov.ObjectCustom0_3` / `Aov.ObjectCustom4_7` | 是 |
| group id | `Object.GroupId` | `Aov.MaskId.b` | 是 |
| object id | `Object.Id` | `Aov.MaskId.g` | 是 |
| flags | `Object.Flags` | `Aov.MaskId.a` | 是 |

生命周期：

```text
Per renderer, changed by authoring component or runtime binding
```

### Per-Camera AOV Resource

AOV MRT 资源，只在当前 camera/frame 内成立。

| Resource | Semantic | Producer | Consumers | Debug |
| --- | --- | --- | --- | --- |
| `Aov.MaskId` | `Object.MaskWeight`, `Object.Id`, `Object.GroupId`, `Object.Flags` | `AovOutput` | SSS, SemanticPost, Debug | `AOV.Mask`, `AOV.ObjectId`, `AOV.ObjectFlag0-7` |
| `Aov.NormalDepth` | `Geometry.WorldNormal`, `Geometry.LinearDepth` | `AovOutput` | SSS, SemanticPost, Debug | `AOV.WorldNormal`, `AOV.LinearDepth` |
| `Aov.ObjectCustom0_3` | `Object.Custom0-3` | `AovOutput` | SemanticPost, Debug | `AOV.ObjectCustom0-3` |
| `Aov.ObjectCustom4_7` | `Object.Custom4-7` | `AovOutput` | SemanticPost, Debug | `AOV.ObjectCustom4-7` |
| `Aov.SurfaceData` | `Material.Class`, `Material.SssProfile`, `Material.Thickness`, `Material.Curvature` | `AovOutput` | SSS, SemanticPost, Debug | `AOV.MaterialClass`, `AOV.SssProfile`, `AOV.Thickness`, `AOV.Curvature` |
| `Aov.MaterialCustom0_3` | `Material.Custom0-3` | `AovOutput` | SemanticPost, Debug | `AOV.MaterialCustom0-3` |
| `Aov.SssSource` | `Shading.SssSourceColor`, `Shading.SssWeight` | `AovOutput` | SSS, SemanticPost, Debug | `AOV.SssSource`, `AOV.SssWeight` |

生命周期：

```text
Per camera / frame transient
```

### Derived Resource

AOV 消费者生产的派生资源。

| Resource | Producer | Inputs | Consumers | Debug |
| --- | --- | --- | --- | --- |
| `Sss.Source` | `SubsurfaceScattering` | `Aov.MaskId`, `Aov.NormalDepth`, `Aov.SurfaceData`, `Aov.SssSource` | SSS diffusion / composite, SemanticPost, Debug | `SSS.Source`, `SSS.Mask` |
| `Sss.Diffusion` | `SubsurfaceScattering` | `Sss.Source` | camera color composite, SemanticPost, Debug | `SSS.Diffusion`, `SSS.CompositeWeight` |
| `SemanticPost.Mask` | `SemanticPostProcess` | AOV + SSS resources | camera color composite, Debug | `SemanticPost.Mask` |

生命周期：

```text
Per camera / frame transient, derived after AOV
```

## 资源与语义边界

### 可以提前到 Renderer Static Semantic

- `Object.Custom0-7`
- `Object.Id`
- `Object.GroupId`
- `Object.Flags`

原因：

- 它们不依赖 camera。
- 不依赖 depth / normal。
- 不依赖材质着色结果。
- 旧 RSUV 已验证这类数据可通过 `unity_RendererUserValue` 输入 shader。

### 不应提前到 RSUV

- `Geometry.WorldNormal`
- `Geometry.LinearDepth`
- `Material.Thickness`
- `Material.Curvature`
- `Shading.SssSourceColor`
- `Shading.SssWeight`
- `Sss.Source`
- `Sss.Diffusion`
- `SemanticPost.Mask`

原因：

- geometry 值依赖当前绘制与 camera。
- material/shading 值可能来自材质、贴图、未来 shader generator 或 per-material policy。
- derived resource 由 feature 生产，不是 renderer 静态属性。

## Contract 更新建议

`HoUrpBuiltInContracts` 中不需要立刻新增资源，但需要在文档或注释中明确：

| Semantic | Source Layer |
| --- | --- |
| `Object.Custom0-7` | `RendererStaticSemantic` or MPB |
| `Object.Id` | `RendererStaticSemantic` or MPB |
| `Object.GroupId` | `RendererStaticSemantic` or MPB |
| `Object.Flags` | `RendererStaticSemantic` or MPB |
| `Material.*` | MPB / future material producer |
| `Geometry.*` | AOV pass |
| `Shading.Sss*` | AOV pass / material producer |
| `Composite.SemanticPostMask` | SemanticPost |

## 验收问题

- 任意 `Aov.*` 资源能否说明 producer 和 consumer？
- 任意 debug view 能否追溯到 resource 和 semantic？
- 任意 object semantic 能否说明它来自 RSUV 还是 MPB？
- 禁用 authoring 后 per-renderer 静态语义是否清理？
- derived resource 是否没有被反写回 AOV？

## 风险

- 把 AOV 资源和 authoring 字段混成同一层。
- 把 RSUV 当成资源生命周期管理方式。
- Debug 只显示结果，不记录结果来自哪一层。
- 为了省事把 material/shading 数据也塞进第一版 32-bit RSUV。
