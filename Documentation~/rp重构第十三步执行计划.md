# rp重构第十三步执行计划

## 目标

第十三步进入 HoShadowCast 子系统迁移第一阶段：把旧仓库里已经具备的 atlas packing、聚光/点光投影、额外方向光 atlas、debug view 和 receiver sampling 拆成新的资源契约、RenderGraph 执行节点、Shader 接收 ABI 和验收路径。

这一阶段不是重建完整材质系统，也不是把旧实现整段搬回。目标是让新 HoURP 管线具备一条接近旧 ShadowCast 能力下限、但命名和资源边界重新整理过的自定义阴影链路：

- 能在 RenderGraph 中分配并发布 HoURP 自有 ShadowCast 资源。
- 能用标准 `ShadowCaster` pass 写入 atlas slice。
- 能 pack spot/point light slice，点光按 6 faces 写入主 atlas。
- 能为额外方向光生成独立 second directional atlas，并支持 cascades。
- 能在生成材质的 Forward/OIT 路径中读取自定义阴影衰减。
- 能在 feature 关闭、无光源、无接收者、Scene/Game View 切换时清理全局状态。
- 能用 Debug 输出定位 atlas、second directional atlas、light/slice 数据、receiver attenuation 和资源生命周期问题。

## 旧实现能力基线

旧 `lilToon-URP-Extensions` 中 HoShadowCast 可以作为行为参考，但不能作为 ABI 直接继承。旧实现不是单光原型，它已经具备：

- `MaxSpotLights = 4`、`MaxPointLights = 4`，点光最多 24 个 faces。
- 主 atlas 使用简单 row packer 分配 spot/point slices。
- second directional atlas 支持最多 4 个额外方向光，每光最多 4 cascades。
- debug mode 支持主 atlas 与 second directional atlas。
- sampling include 中已有 punctual attenuation、second directional attenuation、manual PCF/PCSS 分支。

第十三步应保留这些能力层级，但不保留旧名字污染：

- 旧 RenderFeature、Controller、Resource、Sampling include 可作为数据流参考。
- 新公共资源名、shader property、debug id 必须归入 HoURP 命名空间。
- 不直接暴露旧 `_HoShadowCast*` 名称作为新契约；确需兼容时另开 legacy bridge 文档和开关。

## 分段实施

### 13.a 边界与旧实现审查

输出：

- `Documentation~/rp重构第十三步/rp第十三阶段实现边界审查.md`
- `Documentation~/rp重构第十三步/rpHoShadowCast旧实现数据流审查.md`

工作：

- 归档旧 `Runtime/ShadowCast` 的 atlas、spot、point、second directional、debug、sampling 数据流。
- 标注“保留概念”“改名重建”“分层迁移”“暂缓”。
- 明确第十三步不处理 CharacterSpecialization、Planar Reflection、完整材质 UI、透明精确投影。

### 13.b ShadowCast 资源契约与多光源布局

输出：

- `Documentation~/rp重构第十三步/rpShadowCast资源与契约规划.md`
- `Documentation~/rp重构第十三步/rpShadowCastAtlasPack与多光源执行规划.md`

工作：

- 在 `HoUrpRenderGraphResourceIds` 增加 ShadowCast 资源族。
- 建立 `HoUrpShadowCastResourceDeclaration`，负责主 atlas、second directional atlas、light data、slice data、world-to-shadow 数据的声明和 debug 命名。
- 规划主 atlas row packing、spot 单 slice、point 六 faces。
- 规划 second directional atlas 的 grid/cascade 布局。
- 明确无光源或 feature 关闭时发布 inactive 状态，而不是留下上一帧全局纹理。

### 13.c RenderGraph 执行路径

输出：

- `Documentation~/rp重构第十三步/rpShadowCastRenderGraph执行计划.md`

工作：

- 新建 `Runtime/ShadowCast/HoShadowCastRendererFeature.cs`。
- 新建 settings/constants/resources/packer 相关文件。
- Pass 顺序建议：
  1. Reset/Inactive。
  2. Build punctual frame data。
  3. Allocate main atlas。
  4. Draw spot/point caster slices。
  5. Build second directional frame data。
  6. Allocate second directional atlas。
  7. Draw second directional cascade slices。
  8. Publish receiver globals。
  9. Debug atlas view。

第一个版本不再只做单主光。必须覆盖旧实现已有的三类输入：

- spot lights：每光 1 slice，进入主 atlas。
- point lights：每光 6 slices，进入主 atlas。
- second directional lights：每光 N cascades，进入 second directional atlas。

后置项包括更优 packing、跨帧 atlas 缓存、透明 alpha/dither 精确投影和高级软阴影调参。

### 13.d Shader Receiver ABI 与生成材质接入

输出：

- `Documentation~/rp重构第十三步/rpShadowReceiverShaderABI与材质接入审查.md`

工作：

- 新建 `Runtime/Shaders/ShaderLibrary/HoUrpShadowCastSampling.hlsl`。
- 规定接收函数，例如 `HoUrpSampleShadowCastAttenuation(positionWS, normalWS)`。
- sampling 第一版应包含：
  - punctual attenuation：spot/point 影响范围、spot cone、point face selection。
  - second directional attenuation：额外方向光 cascade selection。
  - PCF/PCSS：先迁移结构和参数位，质量优化可后续调参。
- 更新 debug/generated lit shader：
  - `UniversalForward` 接收 HoShadowCast attenuation。
  - `OIT` 接收同一 attenuation，避免 OIT 开启后光照/阴影退化成纯色。
  - `ShadowCaster` pass 独立存在，用于产生投影。

### 13.e Debug 与状态清理

输出：

- `Documentation~/rp重构第十三步/rpShadowCastDebug与状态清理审查.md`

工作：

- Debug mode 至少覆盖 `Atlas` 与 `SecondDirectionalAtlas`。
- Receiver attenuation debug 可作为第三项，若实现成本低则同阶段完成。
- 所有全局 shader id 集中在 constants 文件。
- feature disable、camera type mismatch、无有效 light、atlas allocation failure 都必须 reset globals。
- Debug 输出不能成为材质 ABI；只用于排错。

### 13.f 与 OIT/SSS/Post/AOV 顺序回归

输出：

- `Documentation~/rp重构第十三步/rpShadowCast与OitSssPost顺序回归审查.md`

工作：

- 确认 ShadowCast pass 不读写 camera color。
- ShadowCast 应先于 transparent/OIT receiver 路径发布 receiver globals。
- 主 atlas 与 second directional atlas 不应进入 post chain ping-pong。
- SSS/AOV/Post 不应依赖 ShadowCast atlas 的 lifetime，除非显式声明资源依赖。
- 关闭 ShadowCast 后，OIT、SSS、ScreenPost、ImagePost 仍应保持第十一、十二步行为。

### 13.g 测试与验收

输出：

- `Documentation~/rp重构第十三步/rp第十三阶段测试与验收清单.md`

工作：

- 增加资源契约测试。
- 增加 atlas packer 测试。
- 增加 shader ABI 测试。
- 增加 generated/debug shader pass 测试。
- 增加 spot/point/second directional Unity 手工验收场景说明。
- 增加 RenderDoc 可选验收项。

## 建议文件结构

```text
Runtime/
  ShadowCast/
    HoShadowCastRendererFeature.cs
    HoShadowCastSettings.cs
    HoShadowCastShaderConstants.cs
    HoShadowCastResources.cs
    HoShadowCastAtlasPacker.cs
  RenderGraph/
    HoUrpShadowCastResourceDeclaration.cs
  Shaders/
    ShaderLibrary/
      HoUrpShadowCastSampling.hlsl
    Hidden/HoURP/ShadowCast/
      Debug.shader
```

测试建议：

```text
Tests/
  Runtime/
    HoUrpShadowCastResourceContractTests.cs
    HoUrpShadowCastAtlasPackerTests.cs
    HoUrpShadowCastShaderAbiTests.cs
```

## 新命名建议

资源 id：

- `ShadowCast.Atlas`
- `ShadowCast.AtlasSize`
- `ShadowCast.LightData`
- `ShadowCast.LightAttenuation`
- `ShadowCast.LightColor`
- `ShadowCast.SliceData`
- `ShadowCast.WorldToShadow`
- `ShadowCast.SecondDirectionalAtlas`
- `ShadowCast.SecondDirectionalAtlasSize`
- `ShadowCast.SecondDirectionalLightData`
- `ShadowCast.SecondDirectionalSliceData`
- `ShadowCast.SecondDirectionalWorldToShadow`
- `ShadowCast.DebugAtlas`
- `ShadowCast.DebugSecondDirectionalAtlas`
- `ShadowCast.DebugAttenuation`

Shader property：

- `_HoUrpShadowCastAtlas`
- `_HoUrpShadowCastAtlasSize`
- `_HoUrpShadowCastActive`
- `_HoUrpShadowCastLightCount`
- `_HoUrpShadowCastSliceCount`
- `_HoUrpShadowCastWorldToShadow`
- `_HoUrpShadowCastLightData`
- `_HoUrpShadowCastLightAttenuation`
- `_HoUrpShadowCastLightColor`
- `_HoUrpShadowCastSliceData`
- `_HoUrpShadowCastPcssParams`
- `_HoUrpShadowCastPcssParams2`
- `_HoUrpShadowCastSecondDirectionalAtlas`
- `_HoUrpShadowCastSecondDirectionalParams`
- `_HoUrpShadowCastSecondDirectionalAtlasSize`
- `_HoUrpShadowCastSecondDirectionalWorldToShadow`
- `_HoUrpShadowCastSecondDirectionalLightData`
- `_HoUrpShadowCastSecondDirectionalSliceData`
- `_HoUrpShadowCastSecondDirectionalPcssParams`
- `_HoUrpShadowReceiverStrength`

## 完成定义

第十三步完成时，应满足：

- HoShadowCast feature 可开关，且不会污染关闭后的后续帧。
- 主 atlas 能 pack spot/point slices，点光 6 faces 的 slice 数据可 debug。
- second directional atlas 能显示额外方向光 cascades。
- 至少一个 debug/generated lit 材质既能产生 shadow caster depth，也能接收 HoURP ShadowCast attenuation。
- OIT 打开后，透明对象仍能复用同一 receiver attenuation，不退回纯色合成。
- AOV/SSS/Post 不因 ShadowCast 资源引入产生顺序或生命周期回归。
- 测试和文档都能说明当前透明投影限制，以及后续 alpha/dither shadow 的入口。

