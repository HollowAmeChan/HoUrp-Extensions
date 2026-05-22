# RP 第四阶段实现边界审查

## 本阶段目标

第四阶段只做材质派生语义纵切：

```text
Material semantic authoring
  -> AovOutput writes SurfaceData / MaterialCustom MRT
  -> DebugView displays material semantic channels
  -> SemanticPost reads material semantic resources explicitly
```

它不是完整 HoAOV 迁移，也不是 HoSSS / HoPost rule stack 迁移。

## 做什么

- 新增 `Aov.SurfaceData` 资源。
- 新增 `Aov.MaterialCustom0_3` 资源。
- 激活 `Material.Class`、`Material.SssProfile`、`Material.Thickness`、`Material.Curvature`、`Material.Custom0..3` 语义。
- 登记 `Material.Utility`，但是否在本阶段生产要在 `rpSurfaceData资源与编码审查.md` 中定案。
- 新增材质语义 DebugView。
- 新增最小材质语义 authoring 输入。
- 扩展 AOV fallback shader / AovOutput MRT，写入材质语义资源。
- 扩展 SemanticPost probe，用材质语义做最小 tint / weight 验证。

## 不做什么

- 不做 `Aov.SssSource`。
- 不做 `HoAOVSSS` 等价迁移。
- 不做完整 `SubsurfaceScattering`。
- 不做 HoPost AOV rule language。
- 不做 Shoost AOV composite。
- 不做新材质 template / generated shader 系统。
- 不接旧 `lilToon` / `lilPBR` 原生 `HoAOV` pass。
- 不修改旧材质包。
- 不做材质 inspector UI。
- 不做透明材质 SurfaceData 一致性。

## 关键边界

`SurfaceData` 和 `MaterialCustom` 是 `MaterialDomain` 语义载体，不是对象区域语义，也不是 SSS 本身。

旧系统里 `SurfaceData` 同时承载 material class、profile、thickness、curvature、utility 等输入。第四阶段只把这些输入变成可注册、可调试、可被显式消费的资源与语义；它不负责证明最终 SSS 视觉一致。

最小 authoring 只是迁移期 producer，不是最终新材质系统。后续 HoNpr 统一材质系统应通过正式 material producer 接口写入这些语义；HoToon 只作为轻量历史参考包。

## 成功标准

- `Aov.SurfaceData` 和 `Aov.MaterialCustom0_3` 能被 AovOutput 写入。
- DebugView 能显示至少 `Thickness` 和 `MaterialCustom0`。
- SemanticPost 通过 Resource Registry 显式读取新增资源。
- 新增 shader binding 使用 `_HoUrp*` 命名。
- 旧 `_lilHoAov*` / `_HoAov*` 只在文档 `LegacyName` / `LegacySource` 中出现。
