# RP MaterialShadingSemanticAov 链路审查

## 目标

把 `AovOutput` 从对象/几何语义纵切扩展到材质派生语义纵切。

第四阶段需要清楚区分：

```text
GeometrySemanticAov
  -> Aov.MaskId
  -> Aov.NormalDepth
  -> Aov.ObjectCustom0_3
  -> Aov.ObjectCustom4_7

MaterialShadingSemanticAov
  -> Aov.SurfaceData
  -> Aov.MaterialCustom0_3
```

## 绘制链路

候选链路：

```text
AovOutputRendererFeature
  -> Declare AOV resources
  -> Build renderer list
  -> Draw override/fallback material
  -> Write MRT:
       MaskId
       NormalDepth
       ObjectCustom0_3
       ObjectCustom4_7
       SurfaceData
       MaterialCustom0_3
```

第一版可以继续用同一个 RenderGraph raster pass 写多 MRT，但 descriptor、文档和 DebugView mapping 必须把几何语义与材质语义分开。

如果实现时拆成两个 pass，也允许：

```text
HoURP AOV Geometry
HoURP AOV Material Shading
```

拆分条件：

- 两个 pass 的 produced resources 独立登记。
- 资源 clear 仍由 resource declaration 控制。
- SemanticPost 明确声明读取依赖。
- DebugView source resource 不依赖隐式全局纹理。

## Shader 策略

扩展现有 fallback AOV shader，新增输出：

```text
SV_Target4 = Aov.SurfaceData
SV_Target5 = Aov.MaterialCustom0_3
```

具体 target index 可按当前代码实际 MRT 顺序调整，但必须写进实现审查或代码注释，避免 DebugView channel mapping 和 MRT 顺序脱节。

## 不做项

- 不 include 旧 `lil_pass_hoaov.hlsl`。
- 不 include 旧 `HoAovSampling.hlsl` 作为正式依赖。
- 不接旧材质原生 `HoAOV` LightMode。
- 不做 alpha clip 旧行为一致性。
- 不做 transparent AOV。

## 成功标准

- Frame Debugger 中能看到新增 MRT 被 AovOutput 写入。
- `Aov.SurfaceData` 和 `Aov.MaterialCustom0_3` producer 是 AovOutput。
- Debug 和 SemanticPost 都从 RenderGraph resources / registry mapping 读取，不从旧全局名读取。

