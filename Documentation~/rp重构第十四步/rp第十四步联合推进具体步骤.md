# 第十四步联合推进具体步骤

> 本清单用于 HoUrp-Extensions 与 HoNpr 两仓同步推进。执行时不要手改 generated shader；结构变更先改 HoNpr DSL / Assembly / ShaderLibrary，再运行生成器。

## Step 1. 冻结事实与边界

修改 / 新增：

- HoURP：`Documentation~/rp重构第十四步/rpHoNpr完成度与HoURP接口审查.md`
- HoNpr：`Documentation~/06_HoURP联合推进第十四步.md`
- HoNpr：`README.md`
- HoNpr：`ShaderSystem/README.md`
- HoNpr：`ShaderSystem/Contract/HORP_CONTRACT_INDEX.md`

要求：

- 承认 HoNpr 已有 DSL、preset、block、generated shader 和 Material UI。
- 明确 `Character_LilToon_Skin_fSSS` 只验证 forward/fake SSS。
- 明确真 screen-space SSS 必须由独立 block / preset 贡献 HoAOV 基础语义输入，SSS runtime 再消费这些输入。
- 明确 HoShadowReceiver 当前仍是 `1.0h` 占位。

验收：

- 文档中能搜到 `ForwardThinSss`、`ScreenSpaceSssSourceProducer`、`HoShadowReceiver`、`1.0h`。
- 文档不再说 HoNpr “后续才建立材质系统”。

## Step 2. 补 HoNpr 真 SSS Block

新增：

```text
HoNpr/ShaderSystem/Features/Subsurface/ScreenSpaceSssSourceProducer/
  Block.honprblock
  Parameters.honprparams
```

建议声明：

```text
block MaterialBlock.ScreenSpaceSssSourceProducer : SemanticProducer in ShadingDomain
{
    consumes HoUrpSurfaceData SemanticMap;
    produces Shading.SssSourceColor Aov.Diffuse; // SSS weight/control uses SSS-owned RDG/MRT
    requires include HoNpr.SemanticSurface;
    requires define HONPR_HAS_SCREEN_SPACE_SSS_SOURCE;
    entry HoNprCreateMaterialSemanticProducer;
    variantPolicy PresetStatic;
    debug Semantic.ScreenSpaceSSS;
}
```

要求：

- 不复用 `ForwardThinSss` 作为真 SSS block 名。
- 参数可先沿用 `_HoUrpGeneratedSssSourceColor`、`_HoUrpGeneratedSssWeight`，但 owner 必须能追溯到新的 block。
- 如果与旧 `SssSourceProducer` 并存，文档必须说明旧 block 是过渡名还是被替换。

验收：

- `FEATURE_BLOCK_TABLE.md` 重建后出现 `MaterialBlock.ScreenSpaceSssSourceProducer`。
- 该 block 产出 `Shading.SssSourceColor` / `Aov.Diffuse` 等可进入 HoAOV 基础语义缓存 的通用输入语义。

## Step 3. 补 HoNpr 真 SSS Preset

新增或调整：

```text
HoNpr/ShaderSystem/Presets/Character/Character_LilToon_Skin_SSS.honprpreset
```

要求：

- shaderName 使用真 SSS 名称，例如 `HoNpr/Character_LilToon_Skin_SSS`。
- generatedShader 使用新路径，例如 `Shaders/Generated/LilToon/Skin_SSS.shader`。
- blocks 至少包含：
  - `MaterialBlock.BaseColorTexture`
  - `MaterialBlock.NormalMap`
  - `MaterialBlock.SemanticMap`
  - `MaterialBlock.ScreenSpaceSssSourceProducer`
  - `MaterialBlock.MaterialSemanticProducer`
  - `MaterialBlock.AovOutputStandard`
  - `MaterialBlock.HoShadowReceiver`
- 可选保留 `ForwardThinSss`，但 preset 名或注释必须说明这是 dual path。
- produces 必须包含：
  - `Material.SssProfile`
  - `Material.Thickness`
  - `Material.Curvature`
  - `Shading.SssSourceColor`
  - `Shading.SssWeight`
  - HoAOV 中可供 SSS 消费的颜色输入语义；权重/控制量走 SSS 自己的 RDG/MRT

验收：

- `PRESET_TABLE.md` 重建后出现真 SSS preset。
- generated shader 顶部 source mapping 包含 `MaterialBlock.ScreenSpaceSssSourceProducer`。
- `Character_LilToon_Skin_fSSS` 不作为真 SSS 验收目标。

## Step 4. 重新生成 HoNpr 派生产物

执行：

- 使用 HoNpr 生成器菜单：`Assets > HoNpr > 生成器 > [材质] 强制刷新 Shader 与材质 UI`

生成 / 更新：

- `ShaderSystem/Features/FEATURE_BLOCK_TABLE.md`
- `ShaderSystem/Presets/PRESET_TABLE.md`
- `ShaderSystem/MaterialUi/MATERIAL_UI_TABLE.md`
- `Shaders/Generated/**/*.shader`

验收：

- 不手动编辑派生表格行。
- 不手动编辑 generated shader。
- generated shader 不包含旧 ABI：`_lil`、`_HoAov`、`HoAOV`、`HoAOVSSS`、`lilToonOIT`。

## Step 5. 接入 HoShadowReceiver 真采样

修改：

- `HoNpr/Shaders/ShaderLibrary/Assemblies/CharacterToon/HoNprCharacterToonShared.hlsl`
- `HoNpr/Shaders/ShaderLibrary/Assemblies/EnvironmentLilPbr/HoNprEnvironmentLilPbr.hlsl`
- 可选新增 wrapper：`HoNpr/Shaders/ShaderLibrary/Lighting/HoNprHoUrpShadowReceiver.hlsl`

要求：

- include HoURP ShadowCast sampling：

```hlsl
#include "Packages/com.hollow.hourp-extensions/Runtime/Shaders/ShaderLibrary/HoUrpShadowCastSampling.hlsl"
```

- 把占位调用：

```hlsl
lighting = HoNprResolveHoShadowReceiver(lighting, 1.0h);
```

替换为：

```hlsl
half hoShadow = HoUrpSampleShadowCastAttenuation(positionWS, normalWS);
lighting = HoNprResolveHoShadowReceiver(lighting, hoShadow);
```

- HoCast 只写 `HoNprLightingContext.hoShadow`。
- 不写 `mainLightShadow`。
- 不在 HoNpr generated shader 中裸采 `_HoUrpShadowCastAtlas`。

验收：

- `rg "HoNprResolveHoShadowReceiver\\(lighting, 1\\.0h\\)" HoNpr/Shaders` 没有长期路径。
- `rg "HoUrpShadowCastSampling.hlsl" HoNpr/Shaders` 能找到 assembly 或 wrapper。

## Step 6. HoURP 契约注册表补齐

修改：

- `HoUrp-Extensions/Runtime/Semantic/HoUrpMaterialContracts.cs`

候选公共 block：

- `MaterialBlock.HoShadowReceiver`
- `MaterialBlock.UrpMainLightInput`
- `MaterialBlock.UrpAdditionalLightInput`
- `MaterialBlock.IndirectLightInput`
- `MaterialBlock.ScreenAoReceiver`
- `MaterialBlock.MaterialSemanticProducer`
- `MaterialBlock.ScreenSpaceSssSourceProducer`
- `MaterialBlock.OitAccumulationOutput`

要求：

- 只注册 HoURP-facing 公共契约。
- 不把 `LilToon*`、`LilPbr*`、Hair、Outline、Stylized lobe 注册成 HoURP runtime 公共契约。

验收：

- HoURP 测试能查询这些公共 block id。
- HoNpr 私有风格 block 不污染 HoURP contract registry。

## Step 7. 补跨仓 ABI 测试

修改：

- `HoUrp-Extensions/Tests/Runtime/HoUrpMaterialShaderAbiTests.cs`
- 可选 HoNpr 侧新增 generator validation 测试。

测试点：

- 真 SSS generated shader 包含 `MaterialBlock.ScreenSpaceSssSourceProducer`。
- 真 SSS generated shader 包含 `HoUrpAovOutput`。
- 真 SSS generated shader 不叫 `fSSS`。
- HoNpr generated shader 包含 `HoUrpAovOutput`、`HoUrpOitAccumulation`、`ShadowCaster`。
- HoNpr assembly include `HoUrpShadowCastSampling.hlsl`。
- HoNpr assembly 不再把 `1.0h` 作为 HoShadowReceiver 的长期输入。
- HoNpr generated shader 不包含旧 ABI。

## Step 8. Unity 手动验收

场景：

- Renderer Data 开启 AOV。
- Renderer Data 开启 SSS。
- Renderer Data 开启 Weighted OIT。
- Renderer Data 开启 ShadowCast。

材质：

- `HoNpr/Character_LilToon_Standard`
- `HoNpr/Character_LilToon_Skin_SSS`
- `HoNpr/Character_LilToon_Transparent`
- `HoNpr/Environment_LilPBR`

检查：

- Render Cache Debug 能看到 AOV material class / custom / surface data。
- SSS Debug / Render Cache Debug 能看到 HoAOV 基础语义输入被 SSS runtime 消费后的 source / diffusion / composite 输出。
- SSS Debug / composite 能看到真 SSS preset 贡献。
- `Skin_fSSS` 只用于对比 forward/fake SSS。
- ShadowCast Inspector 能看到参与光源。
- HoNpr forward 受 HoCast receiver 影响。
- 透明交错对象进入 OIT accumulation。
- 关闭 ShadowCast 后 HoNpr 材质没有上一帧残留阴影。

## Step 9. 文档回填

更新：

- HoURP 第十四步执行计划。
- HoURP 第十四步接口审查。
- HoURP 第十四步验收清单。
- HoNpr README。
- HoNpr ShaderSystem README。
- HoNpr Contract Index。
- HoNpr 执行清单。

要求：

- 文档中明确 fSSS 与 screen-space SSS 是两条链路。
- 文档中明确 HoCast 不写入 URP main light shadow。
- 文档中明确 HoNpr 不 fork HoURP contract。

## Step 10. 完成判定

第十四步完成条件：

- HoNpr 有真 screen-space SSS block / preset。
- HoNpr HoShadowReceiver 调用真实 HoURP sampling。
- HoURP contract registry 补齐公共 block。
- 跨仓 ABI 测试通过。
- Unity 场景能验证 HoNpr 材质消费 AOV / SSS / OIT / ShadowCast。
