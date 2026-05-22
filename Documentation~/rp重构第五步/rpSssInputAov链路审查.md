# RP SssInput AOV 链路审查

## 目标

把 `AovOutput` 从材质基础语义扩展到 SSS 输入语义。

第五阶段新增：

```text
MaterialShadingSemanticAov
  -> Aov.SssSource
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
       SssSource
```

第一版可以继续用同一个 RenderGraph raster pass 写多 MRT，但 descriptor、文档和 DebugView mapping 必须把 `Aov.SssSource` 归入 SSS input。

## Shader 策略

扩展现有 fallback AOV shader，新增输出：

```text
SV_Target6 = Aov.SssSource
```

具体 target index 可按当前代码实际 MRT 顺序调整，但必须写进实现审查或代码注释，避免 DebugView channel mapping 和 MRT 顺序脱节。

## 不做项

- 不 include 旧 `lil_pass_hoaov.hlsl`。
- 不 include 旧 `HoAovSampling.hlsl` 作为正式依赖。
- 不接旧材质原生 `HoAOVSSS` LightMode。
- 不做 transparent SSS。
- 不做 SSS composite。

