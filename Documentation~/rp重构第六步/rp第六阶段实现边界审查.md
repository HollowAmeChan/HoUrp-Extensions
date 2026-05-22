# RP 第六阶段实现边界审查

第六阶段只迁移屏幕空间 SSS 的最小 source / diffusion / composite 闭环。

## 做

- 新增 `SubsurfaceScattering` feature descriptor。
- 新增 `Sss.Source` / `Sss.Diffusion` 中间资源。
- 读取 `Aov.MaskId`、`Aov.NormalDepth`、`Aov.SurfaceData`、`Aov.SssSource`。
- 在透明前合成回 camera color。
- 接入统一 DebugView registry。
- 让 AllRegistered 自动显示 SSS 中间资源。

## 不做

- 不复制旧 `HoSubsurfaceScatteringRendererFeature.cs`。
- 不迁移 non-RenderGraph compatibility path。
- 不接旧 `HoAOVSSS` LightMode。
- 不读取旧 `_lilHoSSS*` 全局纹理名。
- 不修改 `lilToon` / `lilPBR`。
- 不做 transmission。
- 不做完整 profile asset。
- 不做 half-resolution。
- 不做 filter backend 抽象。

## 成功标准

- 没有 SSS 输入时输出为空贡献。
- 有 `Aov.SssSource.a > 0` 时，SSS composite 可见且可控。
- Debug 能显示 source、diffusion、mask、composite weight。
- 所有资源依赖都通过 registry / RenderGraph 声明。
