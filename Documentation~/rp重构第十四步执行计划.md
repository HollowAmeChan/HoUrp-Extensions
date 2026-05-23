# RP 重构第十四步执行计划

> 第十四步目标：在 HoUrp-Extensions 已经补齐 AOV / SSS / ScreenPost / ImagePost / Weighted OIT / ShadowCast 运行时闭环，并且 HoNpr 已经存在完整材质系统雏形之后，进行 **HoURP 与 HoNpr 的真实契约对齐、跨仓验收、真屏幕空间 SSS block 补齐和 ShadowCast receiver 接入修正**。本阶段不是从零设计材质系统，而是让已经存在的 HoNpr 生成材质真正消费 HoURP runtime，并把双方接口差异收敛到可测试的契约表里。

## 0. 现状修正

此前不能再假设“材质系统尚未建立”。当前 HoNpr 仓库已经有一套较完整系统：

- `ShaderSystem/Features/**/*.honprblock`：Feature Block DSL。
- `ShaderSystem/Presets/**/*.honprpreset`：Preset DSL。
- `ShaderSystem/Templates/**/*.honprtemplate`：模板声明。
- `ShaderSystem/Contract/HORP_CONTRACT_INDEX.md`：索引 HoURP 上游契约，明确 HoNpr 不 fork HoURP 定义。
- `ShaderSystem/Features/FEATURE_BLOCK_TABLE.md` 与 `ShaderSystem/Presets/PRESET_TABLE.md`：已生成 block / preset 总表。
- `Shaders/ShaderLibrary/`：StandardSurface、SemanticSurface、Lighting、Transparency、Composite、Stylized 等 include。
- `Shaders/Generated/`：已经生成 LilToon、LilPBR、Debug 系列 shader。
- `Editor/MaterialUi/` 与 `ShaderSystem/MaterialUi/`：已经有材质 UI 数据和 ShaderGUI。

已存在且和 HoURP 直接相关的 HoNpr block / preset 包括：

- `MaterialBlock.AovOutputStandard`
- `MaterialBlock.MaterialSemanticProducer`
- `MaterialBlock.SssSourceProducer`
- `MaterialBlock.ForwardThinSss`
- `MaterialBlock.OitAccumulationOutput`
- `MaterialBlock.HoShadowReceiver`
- `MaterialBlock.UrpMainLightInput`
- `MaterialBlock.UrpAdditionalLightInput`
- `MaterialBlock.IndirectLightInput`
- `MaterialBlock.ScreenAoReceiver`
- `MaterialPreset.Character_LilToon_Lite`
- `MaterialPreset.Character_LilToon_Standard`
- `MaterialPreset.Character_LilToon_Rich`
- `MaterialPreset.Character_LilToon_Skin_fSSS`
- `MaterialPreset.Character_LilToon_Transparent`
- `MaterialPreset.Hair_LilToon`
- `MaterialPreset.Environment_LilPBR`
- `MaterialPreset.Debug_LitSSS_OIT`

所以第十四步的重点是：**核对 HoNpr 已声明的材质能力是否真正消费 HoURP runtime，而不是重新设计 Preset / Feature Block 模型。**

## 1. 本阶段核心问题

从当前仓库检查看，HoNpr 已经生成了带有新 HoURP pass tag 的 shader：

- `UniversalForward`
- `HoUrpAovOutput`
- `HoUrpOitAccumulation`
- `DepthOnly`
- `ShadowCaster`

也已经 include 了 HoURP 材质 ABI：

- `HoUrpMaterialSurface.hlsl`
- `HoUrpMaterialAov.hlsl`
- `HoUrpMaterialOit.hlsl`

但有两个必须先补齐的真实缺口。

### 1.1 SSS 目前仍偏 fSSS

HoNpr 当前的皮肤 preset 是 `Character_LilToon_Skin_fSSS`，它包含：

- `MaterialBlock.ForwardThinSss`：forward/fake SSS 视觉 lobe，产出 `HoNprLobeOutput.transmission`。
- `MaterialBlock.SssSourceProducer`：输出 `Shading.SssSourceColor`、`Shading.SssWeight`、`Aov.SssSource`。

这说明 HoNpr 已经有 SSS source 语义输出基础，但还没有一个命名清楚的“真屏幕空间 SSS 参与 block / preset”。如果直接用 `Character_LilToon_Skin_fSSS` 做验收，很容易把 forward transmission 视觉效果误判成 HoURP 真 SSS runtime 已经被材质消费。

第十四步必须先补一个真 SSS block 身份，例如：

- `MaterialBlock.ScreenSpaceSssSourceProducer`
- 或 `MaterialBlock.HoUrpSssSourceProducer`

并新增或调整一个真 SSS preset，例如：

- `MaterialPreset.Character_LilToon_Skin_SSS`
- 或 `MaterialPreset.Character_LilToon_Skin_ScreenSSS`

该 preset 可以选择是否同时保留 `ForwardThinSss` 作为视觉 lobe，但验收真 SSS 时必须以 `SssSourceProducer` / `ScreenSpaceSssSourceProducer` 写入的 HoURP AOV SSS source 被 `SubsurfaceScatteringRendererFeature` 消费为准。

### 1.2 ShadowCast receiver 仍是占位

ShadowCast receiver 也有明显接入缺口：

```hlsl
lighting = HoNprResolveHoShadowReceiver(lighting, 1.0h);
```

也就是说 HoNpr 已经有 `MaterialBlock.HoShadowReceiver`、`HoNprLightingContext.hoShadow` 和 `HoNprResolveHoShadowReceiver()`，但当前 generated assembly 里仍把 HoCast attenuation 传成常量 `1.0h`。第十四步必须把这个占位改成真实调用 HoURP ShadowCast sampling 契约，并补跨仓测试，确认 HoNpr 材质真的能被第十三步 ShadowCast 系统消费。

## 2. 前置输入

HoUrp-Extensions 侧：

- `Runtime/Semantic/HoUrpMaterialContracts.cs`
- `Runtime/Semantic/MaterialFeatureBlockDefinition.cs`
- `Runtime/Semantic/MaterialPresetDefinition.cs`
- `Runtime/Shaders/ShaderLibrary/HoUrpMaterialSurface.hlsl`
- `Runtime/Shaders/ShaderLibrary/HoUrpMaterialAov.hlsl`
- `Runtime/Shaders/ShaderLibrary/HoUrpMaterialOit.hlsl`
- `Runtime/Shaders/ShaderLibrary/HoUrpShadowCastSampling.hlsl`
- `Runtime/Features/SubsurfaceScatteringRendererFeature.cs`
- `Runtime/ShadowCast/HoShadowCastRendererFeature.cs`
- `Documentation~/ShadowCast/rpShadowCast工作模式与使用方法.md`

HoNpr 侧：

- `ShaderSystem/Contract/HORP_CONTRACT_INDEX.md`
- `ShaderSystem/Generator/GENERATOR_RULES.md`
- `ShaderSystem/Features/FEATURE_BLOCK_TABLE.md`
- `ShaderSystem/Presets/PRESET_TABLE.md`
- `ShaderSystem/Features/Subsurface/ForwardThinSss/Block.honprblock`
- `ShaderSystem/Features/Subsurface/SssSourceProducer/Block.honprblock`
- `ShaderSystem/Features/Lighting/HoShadowReceiver/Block.honprblock`
- `Shaders/ShaderLibrary/Lighting/HoNprLightingInput.hlsl`
- `Shaders/ShaderLibrary/SemanticSurface/HoNprSemanticSurface.hlsl`
- `Shaders/ShaderLibrary/Assemblies/CharacterToon/HoNprCharacterToonShared.hlsl`
- `Shaders/ShaderLibrary/Assemblies/EnvironmentLilPbr/HoNprEnvironmentLilPbr.hlsl`
- `Shaders/Generated/LilToon/*.shader`
- `Shaders/Generated/LilPBR/*.shader`
- `Shaders/Generated/Debug/LitSSS_OIT.shader`

## 3. 本阶段边界

### 做

- 审查 HoNpr 现有 DSL、include、generated shader 与 HoURP 契约的真实差异。
- 补 HoNpr 真屏幕空间 SSS block / preset 身份，避免用 fSSS preset 代替真 SSS 验收。
- 明确 `ForwardThinSss` 与 HoURP screen-space SSS 的边界。
- 确认 HoNpr 真 SSS preset 的 `HoUrpAovOutput` 能写入 `Aov.SssSource`，并被 `SubsurfaceScatteringRendererFeature` 消费。
- 把 ShadowCast receiver 从占位 `1.0h` 接到 `HoUrpSampleShadowCastAttenuation(positionWS, normalWS)` 或正式 wrapper。
- 明确 HoNpr 中 `HoShadowReceiver` 与 URP main light / additional light / indirect light 的边界。
- 确认 HoNpr generated shader 的 `ShadowCaster` pass 能被 HoShadowCast atlas 绘制。
- 确认 HoNpr transparent / debug OIT shader 的 `HoUrpOitAccumulation` 被 Weighted OIT runtime 消费。
- 增加 HoUrp-Extensions 侧测试，至少通过文本 ABI 检查防止 HoNpr 与 HoURP 契约漂移。
- 增加 HoNpr 侧或文档层测试清单，验证生成器重新生成后仍保留 HoURP pass / include / block。

### 不做

- 不重做 HoNpr 的 Feature Block / Preset DSL。
- 不把 HoNpr 的事实来源复制到 HoUrp-Extensions。
- 不让 HoUrp-Extensions 反向拥有 HoNpr 生成器。
- 不在 HoURP runtime 中加入旧 lilToon / lilPBR 兼容层。
- 不把 fSSS forward 视觉效果当成真 HoURP SSS runtime 验收。
- 不把 HoCast 写入 URP main light shadow。
- 不让材质直接裸采 ShadowCast atlas。
- 不以手改 generated shader 作为最终方案；应改 template / assembly / block / include。

## 4. 计划输出

### 14.a HoNpr 完成度与 HoURP 接口审查

输出：

- `Documentation~/rp重构第十四步/rpHoNpr完成度与HoURP接口审查.md`

工作：

- 列出 HoNpr 已完成的系统：DSL、Preset、Feature Block、Generated Shader、Material UI、Contract Index、LegacyInterop。
- 标记哪些 HoURP 能力已经被 HoNpr 真实接入：
  - AOV：已通过 `HoUrpMaterialAov.hlsl` 和 `HoUrpAovOutput` 接入。
  - OIT：已通过 `HoUrpMaterialOit.hlsl` 和 `HoUrpOitAccumulation` 接入。
  - SSS source：已有 `SssSourceProducer` 雏形，但真屏幕空间 SSS preset/block 身份还需要补齐。
  - ShadowCaster：已有 pass，可被 ShadowCast runtime 绘制。
- 标记缺口：
  - `Character_LilToon_Skin_fSSS` 是 fSSS preset，不能作为真 screen-space SSS 验收终点。
  - HoShadowReceiver block 目前存在，但 generated assembly 仍传 `1.0h`。
  - HoURP 侧 `HoUrpMaterialContracts.cs` 仍是早期 prototype 列表，落后于 HoNpr 的实际 block / preset。
  - HoNpr generated shader 与 HoURP contract 的跨仓自动验证不足。

### 14.b HoNpr 真屏幕空间 SSS Block 补齐

输出：

- 新增 HoNpr block：`ShaderSystem/Features/Subsurface/ScreenSpaceSssSourceProducer/Block.honprblock`。
- 新增或调整参数：`ScreenSpaceSssSourceProducer/Parameters.honprparams`。
- 新增真 SSS preset：`Character_LilToon_Skin_SSS.honprpreset` 或同等命名。
- 重新生成 `FEATURE_BLOCK_TABLE.md`、`PRESET_TABLE.md`、generated shader 和 UI table。

建议 block 语义：

```text
block MaterialBlock.ScreenSpaceSssSourceProducer : SemanticProducer in ShadingDomain
{
    consumes HoUrpSurfaceData SemanticMap;
    produces Shading.SssSourceColor Shading.SssWeight Aov.SssSource;
    requires include HoNpr.SemanticSurface;
    requires define HONPR_HAS_SCREEN_SPACE_SSS_SOURCE;
    entry HoNprCreateMaterialSemanticProducer;
    variantPolicy PresetStatic;
    debug Semantic.ScreenSpaceSSS;
}
```

命名要求：

- `ForwardThinSss` 继续表示 forward/fake SSS lobe。
- `ScreenSpaceSssSourceProducer` 表示为 HoURP SSS runtime 生产输入。
- 真 SSS preset 不应叫 `fSSS`。
- 如果同一个 skin preset 同时启用 fake SSS 和 screen-space SSS，名字和文档必须明确它是 dual path，而不是把两者混成一个开关。

验收要求：

- 真 SSS block 必须出现在 generated shader 顶部 block 注释中。
- 真 SSS preset 必须产生 `Aov.SssSource`。
- 开启 HoURP `SubsurfaceScatteringRendererFeature` 后，SSS debug / composite 能观察到该材质贡献。
- 关闭 HoURP SSS runtime 后，只剩 forward/fake SSS 视觉 lobe，不应误判为 screen-space SSS。

### 14.c ShadowCast Receiver 跨仓接入

输出：

- 修改 HoNpr shader library / assembly，不直接手改 generated shader。
- 可选新增 HoNpr wrapper include，例如 `Shaders/ShaderLibrary/Lighting/HoNprHoUrpShadowReceiver.hlsl`。
- 更新 `MaterialBlock.HoShadowReceiver` 的 consumes / include 描述。

工作：

- 在 HoNpr lighting assembly 中 include HoURP ShadowCast sampling：

```hlsl
#include "Packages/com.hollow.hourp-extensions/Runtime/Shaders/ShaderLibrary/HoUrpShadowCastSampling.hlsl"
```

- 在 fragment assembly 中把：

```hlsl
lighting = HoNprResolveHoShadowReceiver(lighting, 1.0h);
```

替换为等价的真实采样：

```hlsl
half hoShadow = HoUrpSampleShadowCastAttenuation(positionWS, normalWS);
lighting = HoNprResolveHoShadowReceiver(lighting, hoShadow);
```

- 保持 HoCast 为 `HoNprLightingContext.hoShadow`，不写入 `mainLightShadow`。
- CharacterToon 和 EnvironmentLilPbr assembly 都要处理，避免只有角色材质接入。
- 如果透明 / OIT assembly 使用同一套 forward lighting，也必须确认 OIT 路径能得到同一 HoCast attenuation。

### 14.d HoURP 材质契约注册表补齐

输出：

- 修改 `HoUrpMaterialContracts.cs` 或新增补充表。
- 测试覆盖 HoNpr 已启用 preset / block 的 HoURP 对应项。

工作：

- 评估 HoURP 是否应该把 HoNpr 已稳定 block 注册为上游 contract name。
- 至少补齐这些长期 HoURP-facing contract：
  - `MaterialBlock.HoShadowReceiver`
  - `MaterialBlock.UrpMainLightInput`
  - `MaterialBlock.UrpAdditionalLightInput`
  - `MaterialBlock.IndirectLightInput`
  - `MaterialBlock.ScreenAoReceiver`
  - `MaterialBlock.MaterialSemanticProducer`
  - `MaterialBlock.ScreenSpaceSssSourceProducer`
  - `MaterialBlock.OitAccumulationOutput`
- 区分 HoURP 公共契约与 HoNpr 私有风格 block：
  - 公共契约：AOV、OIT、SSS source、HoShadowReceiver、URP light input。
  - HoNpr 私有：`LilToon*`、`LilPbr*`、Hair、Stylized lobe、Outline 具体算法。
- 不把 HoNpr 私有 block 全部注册到 HoURP。

### 14.e 跨仓 ABI 测试

输出：

- HoUrp-Extensions 侧新增或扩展 `Tests/Runtime/HoUrpMaterialShaderAbiTests.cs`。
- HoNpr 侧如果暂不加测试，也要在验收文档列出命令和检查项。

测试重点：

- HoNpr generated shader 包含新 pass：
  - `HoUrpAovOutput`
  - `HoUrpOitAccumulation`
  - `ShadowCaster`
- 真 SSS generated shader / preset 包含：
  - `MaterialBlock.ScreenSpaceSssSourceProducer`
  - `Aov.SssSource`
  - `Shading.SssSourceColor`
  - `Shading.SssWeight`
- `Character_LilToon_Skin_fSSS` 不作为真 screen-space SSS 验收 shader。
- HoNpr generated shader 不包含旧 pass：
  - `HoAOV`
  - `HoAOVSSS`
  - `lilToonOIT`
- HoNpr generated shader / assembly include HoURP ABI：
  - `HoUrpMaterialSurface.hlsl`
  - `HoUrpMaterialAov.hlsl`
  - `HoUrpMaterialOit.hlsl`
  - `HoUrpShadowCastSampling.hlsl`
- HoNpr assembly 不再用 `HoNprResolveHoShadowReceiver(lighting, 1.0h)` 作为长期路径。
- `HoShadowReceiver` 不改写 `mainLightShadow`。
- HoNpr generated shader 不直接裸采 `_HoUrpShadowCastAtlas`，除非位于 HoURP sampling include 内。

### 14.f 用户验收场景

输出：

- `Documentation~/rp重构第十四步/rpHoNpr材质消费HoURP验收清单.md`

验收路径：

1. Renderer Data 开启 AOV、SSS、Weighted OIT、ShadowCast。
2. 使用 HoNpr generated shader：
   - `HoNpr/Character_LilToon_Standard`
   - 真 SSS skin preset，例如 `HoNpr/Character_LilToon_Skin_SSS`
   - `HoNpr/Character_LilToon_Transparent`
   - `HoNpr/LilPBR/Environment` 或实际生成菜单名。
3. 使用 `Character_LilToon_Skin_fSSS` 只验证 forward/fake SSS，不把它作为真 SSS 验收终点。
4. 场景放置 Spot / Point / Second Directional HoCast 光源。
5. 使用 ShadowCast feature Inspector 观察参与光源和 atlas。
6. Forward 观察 HoCast receiver 是否影响 HoNpr 材质。
7. AOV Debug 观察 material class / custom / SSS source 是否来自 HoNpr 材质。
8. SSS Debug / composite 观察真 SSS preset 是否被 HoURP SSS runtime 消费。
9. OIT Debug 或透明交错场景观察 `HoUrpOitAccumulation` 是否被 runtime 消费。
10. 关闭 ShadowCast，确认 HoNpr 材质回到无 HoCast receiver 状态，不保留上一帧阴影。

### 14.g 文档同步

输出：

- 更新 `Documentation~/rp重构第十四步执行计划.md`。
- 新增第十四步子文档。
- HoNpr 侧如需要，更新 `HORP_CONTRACT_INDEX.md`、generator rules、SSS block 文档或 HoShadowReceiver 注释。

重点：

- 第十四步不再写“HoNpr 后续才建立材质系统”。
- 明确 HoNpr 已有大系统，HoURP 第十四步是对接与验收。
- 明确 fSSS 和 screen-space SSS 是两条不同链路。
- 明确 HoURP 是上游 runtime / contract source，HoNpr 是材质生产者和 consumer。

## 5. 建议实施顺序

1. 写 HoNpr 完成度与 HoURP 接口审查文档。
2. 在 HoNpr 补真 screen-space SSS source block / preset。
3. 重新生成 HoNpr shader、feature block 表、preset 表和 UI 表。
4. 用 HoURP SSS runtime 验证真 SSS preset，而不是用 fSSS preset 代替。
5. 修改 HoNpr HoShadowReceiver 的 assembly / include，不手改 generated shader。
6. 再次重新生成 HoNpr shader。
7. 扫描 generated shader，确认 HoCast receiver 不再是 `1.0h` 占位。
8. 补 HoUrp-Extensions 跨仓 shader ABI 测试。
9. 补 HoURP 材质契约注册表中缺少的公共 block 名。
10. 写用户验收清单。
11. 手动 Unity 场景验证 HoNpr 材质消费 AOV / SSS / OIT / ShadowCast。

## 6. 验收标准

- 文档明确承认 HoNpr 已有 DSL、Preset、Feature Block、生成器、Material UI 和 generated shader。
- HoNpr 存在独立真 screen-space SSS source block / preset，不再只依赖 `Character_LilToon_Skin_fSSS` 验证 SSS。
- `ForwardThinSss` 与 HoURP screen-space SSS source producer 的职责分离清楚。
- 真 SSS preset 的 `Aov.SssSource` 能被 HoURP `SubsurfaceScatteringRendererFeature` 消费。
- `MaterialBlock.HoShadowReceiver` 不再只是占位；HoNpr forward lighting 真实调用 HoURP ShadowCast sampling。
- HoCast 保持独立 `hoShadow` term，不污染 URP main light shadow。
- HoNpr generated shader 的 `ShadowCaster` pass 能被 HoShadowCast 绘制。
- HoNpr generated shader 的 `HoUrpAovOutput` 能被 AOV runtime 绘制。
- HoNpr generated shader 的 `HoUrpOitAccumulation` 能被 Weighted OIT runtime 绘制。
- HoURP 和 HoNpr 的 contract index / tests 能阻止旧 ABI 回流。
- 用户有一份清单可以验证“HoNpr 材质确实在消费 HoURP 系统”。

## 7. 风险

- 只改 generated shader，下一次生成器刷新后丢失。
- 用 fSSS preset 通过视觉效果冒充真 screen-space SSS 验收。
- 真 SSS source producer 与 forward/fake SSS 共用一个开关，导致用户无法判断当前看到的是哪条链路。
- HoShadowReceiver 只接入角色材质，环境 / 透明 / OIT 路径仍是占位。
- 把 HoCast 混进 main light shadow，导致未来无法区分 URP 主光、天光和 HoCast。
- HoURP 侧把 HoNpr 私有风格 block 全部注册为公共契约，污染 runtime 边界。
- HoNpr 侧 fork HoURP 契约定义，导致两个仓库语义漂移。

## 8. 第十四步之后

第十四步完成后，下一阶段可以进入：

- HoNpr 生成器质量与视觉组分调参。
- HoURP CharacterSpecialization / PlanarReflection 等后续 runtime 对 HoNpr 的消费。
- 更完整的跨仓 CI / Unity Test Runner 验证。

第十四步底线：**HoNpr 已经是材质大系统；HoURP 第十四步不是重造它，而是先补真 SSS 参与身份，再让它真实、可审计、可测试地消费 HoURP runtime。**
