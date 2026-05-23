# HoNpr 完成度与 HoURP 接口审查

> 本文用于第十四步开始前核对事实。结论是：HoNpr 已经有完整材质系统雏形，第十四步不是从零设计材质，而是 HoUrp-Extensions 与 HoNpr 的跨仓契约对齐。

## 1. HoNpr 已完成的系统

HoNpr 当前已经具备以下结构：

- `ShaderSystem/Features/**/*.honprblock`：Feature Block DSL。
- `ShaderSystem/Features/**/*.honprparams`：参数与语义声明。
- `ShaderSystem/Templates/**/*.honprtemplate`：pass / ShaderLab 模板。
- `ShaderSystem/Presets/**/*.honprpreset`：静态材质组合。
- `ShaderSystem/Includes/INCLUDE_REGISTRY.honprinclude`：include alias 注册。
- `ShaderSystem/Contract/HORP_CONTRACT_INDEX.md`：上游 HoURP 契约索引。
- `Shaders/ShaderLibrary/`：手写 HLSL 实现层。
- `Shaders/ShaderLibrary/Assemblies/`：preset 级稳定入口层。
- `Shaders/Generated/`：generated shader 审查产物。
- `Editor/MaterialUi/` 与 `ShaderSystem/MaterialUi/`：材质 UI 数据和 ShaderGUI。

这说明 HoNpr 已经不是“待建立材质系统”，而是已有大系统，需要和 HoURP runtime 对齐。

## 2. 已经接入 HoURP 的部分

当前 HoNpr generated shader 已经能看到这些 HoURP pass / ABI：

- `UniversalForward`
- `HoUrpAovOutput`
- `HoUrpOitAccumulation`
- `DepthOnly`
- `ShadowCaster`
- `HoUrpMaterialSurface.hlsl`
- `HoUrpMaterialAov.hlsl`
- `HoUrpMaterialOit.hlsl`

对应能力：

- AOV：`MaterialBlock.AovOutputStandard` 已能声明并生成 `HoUrpAovOutput`。
- OIT：`MaterialBlock.OitAccumulationOutput` 已能声明并生成 `HoUrpOitAccumulation`。
- ShadowCaster：角色和环境 generated shader 已有 `ShadowCaster` pass，可被 HoShadowCast atlas 绘制。
- 材质语义：`MaterialBlock.MaterialSemanticProducer` 已能生产 material class / custom / SSS profile / thickness / curvature 等语义。

## 3. 当前缺口

### 3.1 真 SSS 身份缺口

HoNpr 当前有：

- `MaterialBlock.ForwardThinSss`
- `MaterialBlock.SssSourceProducer`
- `MaterialPreset.Character_LilToon_Skin_fSSS`

其中 `ForwardThinSss` 是 forward/fake SSS 视觉 lobe，输出 `HoNprLobeOutput.transmission`。它不是 HoURP screen-space SSS runtime 本身。

`SssSourceProducer` 已经能输出：

- `Shading.SssSourceColor`
- `Shading.SssWeight`
- `Aov.SssSource`

但当前 preset 名和能力边界仍偏 `fSSS`。第十四步需要补一个明确的 screen-space SSS block / preset 身份，例如：

- `MaterialBlock.ScreenSpaceSssSourceProducer`
- `MaterialPreset.Character_LilToon_Skin_SSS`

验收真 SSS 时，不能用 `Character_LilToon_Skin_fSSS` 的 forward 视觉效果替代。必须观察 HoURP `SubsurfaceScatteringRendererFeature` 是否消费了材质写入的 `Aov.SssSource`。

### 3.2 HoShadowReceiver 占位缺口

HoNpr 当前有：

- `MaterialBlock.HoShadowReceiver`
- `HoNprLightingContext.hoShadow`
- `HoNprResolveHoShadowReceiver()`

但当前 assembly 里仍存在占位调用：

```hlsl
lighting = HoNprResolveHoShadowReceiver(lighting, 1.0h);
```

第十四步需要把它改成真实调用 HoURP ShadowCast sampling，例如：

```hlsl
half hoShadow = HoUrpSampleShadowCastAttenuation(positionWS, normalWS);
lighting = HoNprResolveHoShadowReceiver(lighting, hoShadow);
```

HoCast 必须保持为独立 `hoShadow` term，不写入 `mainLightShadow`，避免未来无法区分 URP 主光、天光和 HoCast。

### 3.3 HoURP 契约注册表落后

HoURP 侧 `HoUrpMaterialContracts.cs` 仍是较早的 prototype 列表。HoNpr 已经有更多 HoURP-facing block：

- `MaterialBlock.HoShadowReceiver`
- `MaterialBlock.UrpMainLightInput`
- `MaterialBlock.UrpAdditionalLightInput`
- `MaterialBlock.IndirectLightInput`
- `MaterialBlock.ScreenAoReceiver`
- `MaterialBlock.MaterialSemanticProducer`
- `MaterialBlock.SssSourceProducer`
- `MaterialBlock.OitAccumulationOutput`

第十四步需要评估哪些应成为 HoURP 公共契约，哪些仍是 HoNpr 私有风格 block。

## 4. 第十四步判断

第十四步应按以下判断推进：

- HoNpr 已经是材质大系统。
- HoURP 是 runtime / contract source。
- HoNpr 是材质 producer / consumer。
- 第十四步不重做 HoNpr 系统，只修接口事实。
- 先补真 SSS 身份，再做 SSS runtime 验收。
- 再接 HoShadowReceiver 真采样。
- 最后做 AOV / SSS / OIT / ShadowCast 跨仓综合验收。
