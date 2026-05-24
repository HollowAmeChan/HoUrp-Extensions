# 第十四步首批执行记录

> 当前有效执行记录。本文只记录已经落到源声明和源 assembly 的事实，不把 generated shader / generated table 当作已刷新产物。

## 本轮已落地

- HoNpr 新增 `MaterialBlock.ScreenSpaceSssSourceProducer`。
- HoNpr 新增 `MaterialPreset.Character_LilToon_Skin_SSS`，用于区分真 screen-space SSS 验收入口与 `Character_LilToon_Skin_fSSS`。
- `ScreenSpaceSssSourceProducer` 只生产 `Shading.SssSourceColor` / `Aov.Diffuse` 颜色输入。
- `ScreenSpaceSssSourceProducer` 不生产 `Shading.SssWeight`，权重 / participation / control 仍归 SSS runtime 自己的 RDG/MRT 或 `Sss.Source.a`。
- HoNpr 新增 `HoNprHoUrpShadowReceiver.hlsl` wrapper，通过 HoURP `HoUrpShadowCastSampling.hlsl` 调用 ShadowCast sampling。
- Character LilToon 与 Environment LilPBR 源 assembly 已从 `HoNprResolveHoShadowReceiver(lighting, 1.0h)` 改为真实 HoURP ShadowCast sampling。
- HoURP `HoUrpMaterialContracts` 已补齐 HoURP-facing 公共 block：`ScreenSpaceSssSourceProducer`、`MaterialSemanticProducer`、URP light inputs、`ScreenAoReceiver`、`HoShadowReceiver`、`OitAccumulationOutput`。
- HoURP 测试已增加跨仓源声明 / assembly 文本 ABI 检查。

## 本轮未做

- 未手工编辑 HoNpr generated shader。
- 未手工编辑 `FEATURE_BLOCK_TABLE.md`、`PRESET_TABLE.md`、`MATERIAL_UI_TABLE.md`。
- 未声称 `Shaders/Generated/LilToon/Skin_SSS.shader` 已经存在或可用。
- 未把 `Shading.SssWeight` 塞回 HoAOV / `Aov.Diffuse.a`。

## 后续必须执行

在 Unity 中运行 HoNpr 生成器，刷新：

- `ShaderSystem/Features/FEATURE_BLOCK_TABLE.md`
- `ShaderSystem/Presets/PRESET_TABLE.md`
- `ShaderSystem/MaterialUi/MATERIAL_UI_TABLE.md`
- `Shaders/Generated/**/*.shader`

刷新后再验收：

- `Skin_SSS.shader` 顶部 block mapping 包含 `MaterialBlock.ScreenSpaceSssSourceProducer`。
- `Skin_SSS.shader` 包含 `HoUrpAovOutput` / `DepthOnly` / `ShadowCaster`。
- `Skin_SSS.shader` 不以 `fSSS` 身份作为真 screen-space SSS 验收目标。
- generated shader 不回流旧 ABI：`HoAOV`、`HoAOVSSS`、`lilToonOIT`、`_HoAov`。
- HoShadowReceiver 仍通过 wrapper / HoURP sampling，不直接裸采 `_HoUrpShadowCastAtlas`。

