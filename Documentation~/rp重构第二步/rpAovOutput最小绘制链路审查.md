# RP AovOutput 最小绘制链路审查

## 目标

把 `AovOutputRendererFeature` 从资源声明推进到真正写入 `Aov.MaskId` / `Aov.NormalDepth`。

## 决策

| 项 | 判定 |
| --- | --- |
| 绘制对象来源 | scene renderer list |
| shader 来源 | 新 `Hidden/HoURP/AOV/AovOutputFallback` |
| LightMode | 新 `HoUrpAovOutput` 留档；第一版实际用 override material，不依赖材质 pass |
| fallback 策略 | override material |
| filtering | opaque only，可配置 layer mask |
| pass stage | `GeometrySemanticAov`，当前注入点 `AfterRenderingOpaques` |

## 最小链路

```text
AovOutputRendererFeature
  -> AovOutputPass.RecordRenderGraph
  -> HoUrpAovResourceDeclaration.DeclareMinimalAovTextures
  -> UniversalRenderingData.cullResults
  -> FilteringSettings(RenderQueueRange.opaque, layerMask)
  -> DrawingSettings + overrideMaterial
  -> RendererListHandle
  -> MRT write:
       SV_Target0 = Aov.MaskId
       SV_Target1 = Aov.NormalDepth
```

## RenderGraph 资源关系

| Resource | Access | 说明 |
| --- | --- | --- |
| `Aov.MaskId` | write attachment 0 | mask/id/group/flags packed output |
| `Aov.NormalDepth` | write attachment 1 | encoded world normal + depth output |
| `activeDepthTexture` | depth read | 仅用于 depth test；不写 camera depth |

## 不进入本步

- 不支持透明 AOV。
- 不支持 alpha clip 与旧材质完全一致。
- 不支持旧材质原生 `HoAOV` pass。
- 不输出 MaterialCustom/ObjectCustom/SurfaceData/SSS。

## 旧实现对照

旧 `LightMode = HoAOV` 和 `_lilHoAov*` 只作为行为和编码参照。本阶段 runtime 不读取旧材质 pass，也不把旧全局纹理名作为逻辑资源名。
