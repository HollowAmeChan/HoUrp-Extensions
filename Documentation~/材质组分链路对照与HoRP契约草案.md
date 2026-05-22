# 材质组分链路对照与 HoRP 契约草案

> 本文是 `材质重构初步大纲.md` 的深化版本，目标是把旧 `lilToon` / `lilPBR` 的真实光照与语义输出链路拆成可判断、可裁剪、可重组的材质组件对照表。
>
> 结论先行：旧实现只能作为行为样本和能力来源；新 HoRP 材质系统必须重新定义 URP-only、HoRP-only、静态组合式的材质组件 ABI。不得继承 Built-in/LWRP/HDRP 支持、VRChat 兼容层、旧 inspector/keyword/属性名、旧 `HoAOV` / `lilToonOIT` pass 名，也不得让 UI 反向决定 shader 结构。

---

## 0. 对照来源

本轮对照主要查阅以下旧实现和新契约文件。

### 0.1 lilPBR 侧

```text
D:\Unity_Fork\lilPBR\Shaders\pbr.hlsl
D:\Unity_Fork\lilPBR\Shaders\pbr_core.hlsl
D:\Unity_Fork\lilPBR\Shaders\unity_urp.hlsl
D:\Unity_Fork\lilPBR\Shaders\hoaov.hlsl
D:\Unity_Fork\lilPBR\Shaders\lilPBR.shader
D:\Unity_Fork\lilPBR\Shaders\pbr_properties.hlsl
```

重点事实：

- `pbr.hlsl` 定义 `ShadingParams`、`GetDiffuse()`、`GetSpecular()`、`GetReflectionStrength()` 和基础 `DoLight()`。
- `pbr_core.hlsl` 的 `Shading()` 是 lilPBR 的主材质组装链：输入采样、POM、detail、normal、PBR map、anisotropy、lighting、clear coat、wetness、specular highlight、cloth、translucent、subsurface、alpha、emission。
- `unity_urp.hlsl` 负责 URP lighting 集成、AO 接收、reflection / planar / SSR、HoShadowCast 接收、subsurface lighting。
- `hoaov.hlsl` 负责旧 HoAOV / HoAOVSSS 输出，包括 material class、SSS profile、thickness、curvature、utility、custom0-3、object custom、SSS source。

### 0.2 lilToon 侧

```text
D:\Unity_Fork\lilToon\Assets\lilToon\Shader\Includes\lil_common.hlsl
D:\Unity_Fork\lilToon\Assets\lilToon\Shader\Includes\lil_common_frag.hlsl
D:\Unity_Fork\lilToon\Assets\lilToon\Shader\Includes\lil_pass_forward_normal.hlsl
D:\Unity_Fork\lilToon\Assets\lilToon\Shader\Includes\lil_pass_hoaov.hlsl
D:\Unity_Fork\lilToon\Assets\lilToon\Shader\Includes\lil_pass_hocharacter_capture.hlsl
D:\Unity_Fork\lilToon\Assets\lilToon\Shader\Includes\lil_oit.hlsl
```

重点事实：

- `lil_common.hlsl` 定义 `lilFragData`，它是旧 toon forward 中几乎所有组分共享的累积态。
- `lil_pass_forward_normal.hlsl` 按固定顺序执行材质链：基础色/alpha/normal、anisotropy、shadow、SSAO、SSS、rim shade、backlight、premultiply、refraction、reflection、matcap、rim、glitter、emission、planar reflection、fog、output。
- `lil_common_frag.hlsl` 内部包含真实 toon shadow、SSAO、SSS、specular、reflection、matcap、rim、emission 等具体实现。
- `lil_pass_hoaov.hlsl` 和 lilPBR 的 `hoaov.hlsl` 基本暴露了旧 HoAOV 语义输出的一致性。

### 0.3 HoRP 新契约侧

```text
D:\Unity_Fork\HoUrp-Extensions\Runtime\Core\HoUrpBuiltInNames.cs
D:\Unity_Fork\HoUrp-Extensions\Runtime\Core\HoUrpBuiltInContracts.cs
D:\Unity_Fork\HoUrp-Extensions\Runtime\Core\HoUrpShaderPropertyIds.cs
D:\Unity_Fork\HoUrp-Extensions\Documentation~\rp重构第十步执行计划.md
D:\Unity_Fork\HoUrp-Extensions\Documentation~\rp重构第十步\rp材质ShaderABI审查.md
D:\Unity_Fork\HoUrp-Extensions\Documentation~\rp重构第十步\rp材质Preset与FeatureBlock审查.md
```

当前新 HoRP 已有正式命名：

- Feature：`GeneratedMaterial`、`AovOutput`、`SubsurfaceScattering`、`SemanticPostProcess`、`DebugComposite`。
- Pass：`HoUrpAovOutput`。
- Resource：`Aov.MaskId`、`Aov.NormalDepth`、`Aov.ObjectCustom0_3`、`Aov.ObjectCustom4_7`、`Aov.SurfaceData`、`Aov.MaterialCustom0_3`、`Aov.SssSource`、`Sss.Source`、`Sss.Diffusion`。
- Semantics：`Material.Class`、`Material.SssProfile`、`Material.Thickness`、`Material.Curvature`、`Material.Utility`、`Material.Custom0-3`、`Shading.SssSourceColor`、`Shading.SssWeight`。
- Shader property：`_HoUrpAov*`、`_HoUrpMaterial*`、`_HoUrpSss*`。

这些新名字是后续材质系统必须对接的目标；旧 `_lilHoAov*`、`_HoAov*`、`_lilOIT*`、`lilToonOIT` 只能放在 Legacy 对照表里。

---

## 1. 总设计判断

### 1.1 旧实现的价值

旧实现证明了以下能力可行：

- 材质可以直接生产 AOV / SSS source / material custom 等语义数据。
- Forward shading 和屏幕空间 SSS / SemanticPost / Debug 可以通过 AOV 串起来。
- Toon 阴影、PBR specular、matcap、rim、SSS、reflection 等可以被看成相对独立的组分。
- Weighted OIT 需要材质侧独立 accumulation pass，而不是只改 blend state。
- HoShadowCast 旧消费路径主要是材质 forward 阶段采样 shadow atlas。

### 1.2 旧实现不能继承的结构

不能继承：

- 多管线支持：Built-in、LWRP、HDRP、ForwardBase、ForwardAdd、UsePass 生成链。
- VRChat 兼容层：Udon、AudioLink、VRC Light Volumes、VRC fallback、avatar/world SDK 条件编译。
- 厚 inspector：大量属性折叠、反射式属性绑定、UI 控制 shader 结构。
- 无边界 keyword：每个小功能都用材质开关扩展变体空间。
- 旧 pass 名和资源名：`HoAOV`、`HoAOVSSS`、`HoCharacterCapture`、`lilToonOIT`、`_lilHoAov*`、`_HoAov*`、`_lilOIT*`。
- 旧大一统 fragment state：`lilFragData` / `ShadingParams` 可以参考字段，但不能作为新 ABI 原样继承。

### 1.3 新系统应该是什么

新系统应是：

```text
HoRP semantic/resource contract
  -> Material Template
  -> Feature Block
  -> Material Preset
  -> Generated Shader
  -> Forward / AOV / OIT / Shadow / Depth passes
```

核心是静态组合，不是运行时任意拼装。

UI 只负责：

- 选择 preset。
- 编辑少量参数。
- 绑定纹理和 ramp atlas。
- 显示 debug / validation。

UI 不负责：

- 决定 pass 是否存在。
- 增删 shader feature block。
- 自动制造 keyword 组合。
- 定义 RP 资源名。

---

## 2. 旧 lilPBR 真实组分链路

### 2.1 `ShadingParams` 的意义

lilPBR 的 `ShadingParams` 是一个 monolithic surface + lighting state：

```text
albedo / albedoback / alpha
uv[4]
posWorld / posWorldOrig
T / B / N / bentN / V / refN / origN
metallic / occlusion / smoothness / perceptualRoughness / roughness / reflectance / specular
isAnisotropy / anisotropy
emission
subsurfaceThickness / subsurfaceColor
ssaoMask
oneMinusReflectionStrength
```

新系统不能照搬这个结构，但可以拆成：

- `HoSurfaceData`：材质表面。
- `HoMaterialSemanticData`：材质语义。
- `HoGeometryData`：几何与视角。
- `HoLightingContext`：灯光和 GI 输入。
- `HoLobeAccumulation`：diffuse/specular/sss/rim/matcap 等组分输出。
- `HoCompositeData`：最终合成与透明/OIT 输出。

### 2.2 lilPBR 主链路

lilPBR `Shading()` 大致顺序如下：

```text
UV mode / randomize / POM / atlas
  -> ssao mask
  -> albedo / alpha / backface color
  -> normal map / detail normal / detail albedo
  -> PBR map: metallic / occlusion / smoothness
  -> vertex color policy
  -> metallic energy split
  -> GSAA smoothness clamp
  -> anisotropy setup
  -> emission
  -> URP lighting: diff / spec / reflectionStrength
  -> clear coat as second lighting evaluation
  -> wetness as second lighting evaluation
  -> specular highlight as extra lighting evaluation
  -> cloth diffuse modification
  -> base composite: albedo * diff * oneMinusReflectionStrength
  -> translucent
  -> subsurface
  -> cutout / dither / transparent
  -> specular add
  -> highlight add
  -> distance fade
  -> emission add
```

这条链路说明：lilPBR 已经把很多“材质效果”做成了重新调用 `ComputeLights()` 的二次/三次 lobe。新系统应该把这种思路显式化，而不是在一个函数里硬编码：

- `BasePbrLobe`
- `ClearCoatLobe`
- `WetnessLobe`
- `SpecularHighlightLobe`
- `SubsurfaceLobe`
- `ClothDiffuseModifier`
- `TransparentComposite`

### 2.3 lilPBR lighting 链路

`unity_urp.hlsl` 中的 `ComputeLights()` 大致是：

```text
InputData + SurfaceData
  -> AO factor
  -> HoShadowCastAttenuation(positionWS)
  -> baked GI / reflection
  -> main light
  -> additional lights
```

`DoLight()` 内部：

```text
shadow = remap(URP shadow * HoShadowCast)
lightColor = light.color * distanceAttenuation * shadow
diff += GetDiffuse(p, L) * lightColor
spec += GetSpecular(p, L) * lightColor
diff += shadowTint on shadow area
```

新系统应拆成：

- `LightGather.Main`
- `LightGather.Additional`
- `LightGather.Indirect`
- `ShadowTerm.URP`
- `ShadowTerm.HoShadow`
- `ShadowTerm.StylizedRemap`
- `DiffuseLobe.PBR`
- `SpecularLobe.GGX`
- `SpecularLobe.Anisotropic`
- `ReflectionLobe.Environment`
- `ReflectionLobe.Planar`
- `ReflectionLobe.ScreenSpace`

### 2.4 lilPBR BRDF 本体

lilPBR 的 `pbr.hlsl` 中：

- `GetDiffuse()` 使用 NdotL，并按 smoothness 做轻微曲线变化。
- `GetSpecular()` 使用 GGX-like normal distribution、Smith-like visibility、Schlick Fresnel。
- `SpecularTermAniso()` 支持 anisotropy。
- `GetReflectionStrength()` 计算 environment reflection 的 Fresnel 和 surface reduction。

这部分可以作为 HoNpr 中 `HoStandardSurface` / PBR lobe 第一版的数学参考，但要清楚：

- 公式可以参考。
- 参数名不能继承。
- 结构不能继承。
- URP Lit 的 `SurfaceData` 不能直接作为 HoRP 材质 ABI。

### 2.5 lilPBR HoAOV 输出

lilPBR `hoaov.hlsl` 旧输出：

```text
MaskId:
  mask weight
  group id
  object id
  flags

NormalDepth:
  normal / linear depth

SurfaceData:
  thickness
  curvature
  material profile/class
  utility

MaterialCustom0_3:
  custom0-3

ObjectCustom0_3 / ObjectCustom4_7:
  renderer user value or material property fallback

SssSource:
  sss source color
  sss weight
```

新 HoRP 已经有对应资源，但新材质必须输出新语义名：

| 旧 lilPBR | 新 HoRP |
| --- | --- |
| `_HoAovMaterialClass` | `Material.Class` / `_HoUrpMaterialClass` |
| `_HoSSSProfileId` | `Material.SssProfile` / `_HoUrpMaterialSssProfile` |
| `_HoAovThickness` + subsurface map | `Material.Thickness` / `_HoUrpMaterialThickness` |
| `_HoAovCurvature` + transmission boost | `Material.Curvature` / `_HoUrpMaterialCurvature` |
| `_HoAovUtility` | `Material.Utility` / `_HoUrpMaterialUtility` |
| `_HoAovCustom0-3` | `Material.Custom0-3` / `_HoUrpMaterialCustom0_3` |
| `_SubsurfaceColor` + albedo blend | `Shading.SssSourceColor` / `_HoUrpSssSourceColor` |
| `_SubsurfaceScattering` | `Shading.SssWeight` / `_HoUrpSssWeight` |

---

## 3. 旧 lilToon 真实组分链路

### 3.1 `lilFragData` 的意义

`lilFragData` 包含：

```text
col / albedo / emissionColor
lightColor / indLightColor / addLightColor / attenuation / invLighting
uv sets / derivatives
position OS/WS/CS/SS / depth
camera basis / TBN / T / B / N / V / L / origN / origL / headV
matcap normals / facing
vl / hl / ln / nv / nvabs
anisotropy / smoothness / roughness / perceptualRoughness
shadowmix
renderingLayers / featureFlags
```

它适合作为旧实现的“数据需求清单”，但不适合作为新 ABI。原因：

- 它同时混合 surface、geometry、lighting、composite、debug state。
- 很多字段只为旧多管线/ForwardAdd/VRChat 路径服务。
- 它鼓励任意组分直接读写全局累积状态。

新系统应该改为显式输入输出：

```text
HoSurfaceData
HoGeometryData
HoLightContext
HoShadowData
HoLobeOutput
HoShadingSemanticData
HoCompositeOutput
```

### 3.2 lilToon forward 顺序

`lil_pass_forward_normal.hlsl` 中的真实顺序可概括为：

```text
Base / Alpha / Normal / Fur / Anisotropy
  -> copy albedo
  -> Shadow or direct light multiply
  -> additional light accumulation
  -> SSAO
  -> SSS
  -> RimShade
  -> Backlight
  -> Premultiply
  -> Refraction
  -> Reflection / Specular
  -> MatCap 1
  -> MatCap 2
  -> RimLight
  -> Glitter
  -> Emission 1
  -> Emission 2
  -> BlendEmission
  -> BackfaceColor
  -> DistanceFade
  -> PlanarReflection
  -> Fog
  -> Output or OIT output
```

这条顺序非常有参考价值。新 HoNpr 可以把它变成静态 component stack；HoToon 只作为轻量历史参考：

```text
BaseSurface
StylizedShadow
ScreenSpaceAoReceiver
ThinSss
RimShade
Backlight
SpecularOrReflection
MatCap
RimLight
Glitter
Emission
DistanceFade
PlanarReflection
Fog
TransparentOrOitOutput
```

### 3.3 lilToon shadow

`lilGetShading()` 旧实现包含：

- 多套 normal strength。
- shadow strength mask。
- face SDF / flat shadow mask。
- realtime shadow receive。
- shadow blur mask。
- shadow border mask / AO shift。
- 1st / 2nd / 3rd shadow color。
- LUT shadow color。
- shadow main strength。
- environment strength。
- shadow border color。

这些旧参数数量过多。新系统应保留概念，不保留 UI 形态：

| 旧概念 | 新组件 |
| --- | --- |
| NdotL toon border / blur | `ToonShadowTerm` |
| Face SDF | `FaceShadowTerm` 或 `CharacterFaceShadow` |
| shadow color 1/2/3 | `ShadowRampAtlas` |
| shadow receive | `RealtimeShadowReceiver` |
| shadow AO shift | `ShadowRampCoordModifier` |
| shadow color LUT | `ShadowRampAtlas` 或 `StyleRampAtlas` |

新输入建议：

```text
StyleRampAtlas:
  shadow ramp
  face shadow ramp
  specular ramp
  rim ramp
  sss ramp

RegionMask:
  face / hair / cloth / skin / accessory

StyleControl:
  shadowIntensity
  shadowSoftness
  shadowRampIndex
```

不要保留旧的几十个 `_Shadow*` slider。

### 3.4 lilToon specular / reflection

旧 `lilCalcSpecular()` 支持：

- toon specular。
- GGX specular。
- anisotropy。
- dual anisotropic highlight。
- per-feature masks / shift noise。

旧 `lilReflection()` 支持：

- metallic split。
- direct specular。
- environment reflection。
- refraction branch。
- blend mode。

新系统应拆为：

```text
SpecularLobe.PbrGGX
SpecularLobe.ToonSpecular
SpecularLobe.HairAnisotropicPrimary
SpecularLobe.HairAnisotropicSecondary
SpecularLobe.DebugSpecular
ReflectionLobe.Environment
ReflectionLobe.Planar
ReflectionLobe.ScreenSpace
```

这样可以避免“高光”成为单个不可调的大块。发丝高光、PBR 高光、toon 高光、debug 高光应分别生产 `HoLobeOutput`，再由 preset 决定合成。

### 3.5 lilToon matcap / rim / emission

旧实现说明：

- MatCap 不只是贴图叠加，它还读取 normal、view、VR parallax、lighting、shadowmix、transparency、mask、blend mode。
- RimLight 有 directional / indirect 两套路径，也读取 shadowmix、lighting、transparency。
- Emission 支持多个 emission 层、blend mode、main strength、alpha 影响。

新系统保留：

```text
StylizedLobe.MatCap
StylizedLobe.SecondaryMatCap
StylizedLobe.RimLight
StylizedLobe.RimShade
StylizedLobe.Backlight
EmissionLobe.Primary
EmissionLobe.Secondary
```

新系统裁剪：

- VR parallax 特化。
- 过多 blend mode 暴露。
- 每层都单独一套贴图/mask/normal 的无限扩展。

第一版建议只保留：

```text
MatCap:
  texture
  mask
  normalSource
  lightingInfluence
  shadowInfluence
  blendWeight

Rim:
  ramp or color
  mask
  shadowInfluence
  blendWeight

Emission:
  colorOrTexture
  mask
  intensity
```

### 3.6 lilToon SSS

旧 `lilSSS()` 属于材质内 fake SSS，旧 HoSSS 则是屏幕空间 consumer。两者需要拆开：

| 旧路径 | 新定位 |
| --- | --- |
| `lilSSS()` forward fake SSS | `SubsurfaceLobe.ThinForward` |
| `lil_pass_hoaov.hlsl` SSS source | `Shading.SssSourceColor` / `Shading.SssWeight` producer |
| `HoSubsurfaceScatteringRendererFeature` | HoRP `SubsurfaceScattering` consumer |

新系统不要把 forward fake SSS 和 screen-space SSS 混成一个开关。

---

## 4. 新 HoRP 材质数据 ABI 草案

本节补足第十阶段文档中缺少的“组分级”数据结构意图。

注意：4.1-4.6 是从旧 `lilToon` / `lilPBR` 链路反推得到的第一版内部草案；4.7 在对照 glTF / USD Preview Surface / OpenPBR / MaterialX / URP 后给出修正版边界。后续落地应以 4.7 的分层结论为准。

### 4.1 `HoSurfaceData`

用途：描述材质表面，不携带灯光和合成结果。

```hlsl
struct HoSurfaceData
{
    half3 baseColor;
    half alpha;
    half3 normalWS;
    half3 normalTS;
    half3 tangentWS;
    half3 bitangentWS;

    half metallic;
    half roughness;
    half occlusion;
    half3 emission;

    half coverage;
    half regionMask;
};
```

第一版最小字段：

- `baseColor`
- `alpha`
- `normalWS`
- `roughness`
- `metallic`
- `occlusion`
- `emission`
- `coverage`

### 4.2 `HoGeometryData`

用途：几何和视角输入。

```hlsl
struct HoGeometryData
{
    float3 positionWS;
    float4 positionCS;
    float2 uv0;
    float2 uv1;
    half3 viewDirWS;
    half facing;
    half linearDepth;
};
```

不应把 object id / flags 塞进这里。对象静态语义仍属于 ObjectDomain。

### 4.3 `HoMaterialSemanticData`

用途：材质生产给 HoRP semantic / AOV 的数据。

```hlsl
struct HoMaterialSemanticData
{
    half materialClass;
    half sssProfile;
    half thickness;
    half curvature;
    half utility;
    half4 custom0_3;
    half3 sssSourceColor;
    half sssWeight;
};
```

映射到当前 HoRP：

| Field | Semantic | Resource |
| --- | --- | --- |
| `materialClass` | `Material.Class` | `Aov.SurfaceData` |
| `sssProfile` | `Material.SssProfile` | `Aov.SurfaceData` |
| `thickness` | `Material.Thickness` | `Aov.SurfaceData` |
| `curvature` | `Material.Curvature` | `Aov.SurfaceData` |
| `utility` | `Material.Utility` | `Aov.SurfaceData` |
| `custom0_3` | `Material.Custom0-3` | `Aov.MaterialCustom0_3` |
| `sssSourceColor` | `Shading.SssSourceColor` | `Aov.SssSource.rgb` |
| `sssWeight` | `Shading.SssWeight` | `Aov.SssSource.a` |

### 4.4 `HoLightingContext`

用途：收集灯光输入，不直接决定材质组分。

```hlsl
struct HoLightingContext
{
    half3 mainLightDirWS;
    half3 mainLightColor;
    half mainLightDistanceAttenuation;
    half mainLightShadow;

    half3 indirectDiffuse;
    half3 indirectSpecular;
    half screenAoDirect;
    half screenAoIndirect;
    half hoShadow;
};
```

后续 additional lights 可以通过循环回调输入每个 lobe，不必让每个 lobe 自己查 URP 全局状态。

### 4.5 `HoLobeOutput`

用途：每个组分只产出自己的 contribution，不直接改全局 color。

```hlsl
struct HoLobeOutput
{
    half3 diffuse;
    half3 specular;
    half3 transmission;
    half3 emission;
    half alpha;
    half energyWeight;
    half semanticWeight;
};
```

这样 `PbrSpecular`、`HairSpecular`、`ToonSpecular`、`DebugSpecular` 可以并列存在。

### 4.6 `HoCompositeData`

用途：最终合成。

```hlsl
struct HoCompositeData
{
    half3 color;
    half alpha;
    half coverage;
    half3 debugColor;
};
```

### 4.7 公有材质契约对照

只看 `lilToon` / `lilPBR` 容易把旧工程问题误判成材质模型问题。HoRP 的材质 ABI 应该同时对照已经被资产交换、DCC、运行时渲染验证过的公有契约：

| 契约 | 成熟度 | 适合借鉴 | 不适合照抄 |
| --- | --- | --- | --- |
| glTF 2.0 Metallic-Roughness | 运行时资产交换事实标准 | `baseColor` / `metallic` / `roughness` / `normal` / `occlusion` / `emission` 最小公共子集 | toon、hair、SSS、AOV、风格化 ramp、HoRP pass 语义 |
| USD Preview Surface | DCC/管线间 preview interchange | 同时承认 metallic/specular workflow；把 preview surface 限定为“足够交换”而非全能材质 | 高级 skin、hair、cloth、volume 本来就被它排除 |
| OpenPBR Surface | 公开标准化 uber-shader | base substrate、coat、fuzz、opacity、normal/coat normal、layering/mixing 的物理分层思路 | 参数量过大，不适合直接作为实时 toon/角色材质 authoring 面板 |
| MaterialX PBS / NPR | 节点图和材质网络标准 | “材质定义”和“光照传输实现”分离；component/BSDF/EDF 的组合思路 | HoRP 第一版不应做完整节点图解释器 |
| Unity URP `SurfaceData` / `InputData` | 当前运行时宿主接口 | surface 输入和 lighting 输入分开；`UniversalFragmentPBR(InputData, SurfaceData)` 这种函数边界 | 字段名、smoothness 工作流、URP Lit pass/keyword 不能成为 HoRP 公共 ABI |

资料入口：

```text
glTF 2.0 Specification:
https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html

USD Preview Surface:
https://openusd.org/dev/spec_usdpreviewsurface.html

OpenPBR Surface:
https://academysoftwarefoundation.github.io/OpenPBR/
https://github.com/AcademySoftwareFoundation/OpenPBR

MaterialX Specification:
https://materialx.org/Specification.html

Unity URP Lighting.hlsl:
https://github.com/Unity-Technologies/Graphics/blob/master/Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl
```

#### 4.7.1 结论：HoRP 应分三层，而不是一个巨型 surface

对照这些契约后，HoRP 的接口应分成三层：

```text
HoStandardSurface
  公有可交换子集，对齐 glTF / USD Preview / URP SurfaceData。

HoStylizedSurface
  HoRP 风格化扩展，对应 toon、hair、rim、matcap、style ramp、debug lobe。

HoSemanticSurface
  HoRP 渲染管线语义输出，对应 AOV、SSS source、material class、object/material custom。
```

这样做的原因：

- `HoStandardSurface` 负责“资产能交换、能导入、能退化到普通 PBR”。
- `HoStylizedSurface` 负责“角色/动画/风格化能表达”，不污染 PBR 基础字段。
- `HoSemanticSurface` 负责“HoRP 后处理、AOV、debug、分层渲染能消费”，不伪装成物理材质参数。

换句话说，`HoSurfaceData` 不应该继续膨胀成一个包含所有 toon/SSS/AOV/调试字段的巨型结构。它应当是 Standard Surface 子集；风格化和语义输出走独立结构。

#### 4.7.2 建议修正后的核心结构边界

原草案里的 `HoSurfaceData` 可以保留，但语义上应改名或限定为 `HoStandardSurfaceData`：

```hlsl
struct HoStandardSurfaceData
{
    half3 baseColor;
    half alpha;

    half metallic;
    half roughness;
    half occlusion;
    half3 emission;

    half3 normalWS;
    half3 normalTS;
    half3 tangentWS;
    half3 bitangentWS;

    half coverage;
};
```

其中：

| HoRP 字段 | 对照契约 | 判断 |
| --- | --- | --- |
| `baseColor` | glTF `baseColorFactor/Texture`、USD `diffuseColor`、OpenPBR `base_color` | 保留为 Standard Surface 核心字段 |
| `alpha` | glTF baseColor alpha、USD `opacity`、OpenPBR `geometry_opacity` | 保留，但要明确 straight alpha / coverage / blend alpha 的差异 |
| `metallic` | glTF `metallicFactor`、USD `metallic`、OpenPBR `base_metalness` | 保留，作为 metallic workflow 主线 |
| `roughness` | glTF `roughnessFactor`、USD `roughness`、OpenPBR `specular_roughness` | 保留；HoRP 内部统一 roughness，不用 Unity smoothness 作为 ABI |
| `occlusion` | glTF `occlusionTexture.strength`、URP `surfaceData.occlusion` | 保留，但只表示 material AO，不替代 screen-space AO |
| `emission` | glTF `emissiveFactor/Texture`、USD `emissiveColor`、OpenPBR `emission_color/luminance` | 保留为 HDR color；是否物理 luminance 可由 preset 决定 |
| `normalWS/normalTS/tangentWS` | glTF normal texture、OpenPBR `geometry_normal/tangent`、URP normal input | 保留，但要区分 geometry normal、base normal、coat/hair normal |
| `coverage` | OpenPBR `geometry_opacity`、alpha-test coverage | 保留为几何存在度/裁剪覆盖，不等同于透明折射 |
| `regionMask` | 无公有直接对照 | 移出 Standard Surface，进入 `HoStylizedSurfaceData` 或 mask texture contract |

`regionMask` 不应留在标准 surface 核心里。它是 HoRP 风格化/分区 authoring 字段，不是跨工具材质交换字段。

#### 4.7.3 `alpha` / `coverage` / `opacity` 必须拆开

公有契约里最容易混淆的是透明：

| 概念 | 公有对照 | HoRP 建议 |
| --- | --- | --- |
| Surface opacity | USD `opacity`、OpenPBR `geometry_opacity` | `coverage`，表示表面是否存在，可用于 alpha test / foliage / dither |
| Blend alpha | glTF baseColor alpha、URP `surfaceData.alpha` | `alpha`，表示颜色混合/透明队列输出 |
| Transmission | glTF `KHR_materials_transmission`、OpenPBR `transmission_weight` | 独立 `TransmissionLobe`，不要塞进 `alpha` |
| Refraction | glTF `KHR_materials_ior/volume`、OpenPBR translucent base | 独立 `Refraction/Volume` block，第一版可以只占位 |
| OIT accumulation weight | 运行时透明合成策略 | `HoTransparencyOutput`，不属于材质外观输入 |

建议新增：

```hlsl
struct HoTransparencyData
{
    half coverage;
    half blendAlpha;
    half transmissionWeight;
    half refractionWeight;
    half oitWeight;
};
```

这样旧 `lilToonOIT` 的经验可以迁移到 `HoTransparencyData` 的 consumer，而不是变成一个新 pass 名或一个 `_OIT*` 材质属性堆。

#### 4.7.4 Metallic/specular workflow：只把 metallic 放进核心

USD Preview Surface 明确支持 metallic 和 specular 两种 workflow；URP Lit 也有 metallic/specular workflow；glTF core 只定义 metallic-roughness，specular 通过扩展补足。

HoRP 第一版建议：

```text
核心 Standard Surface:
  baseColor
  metallic
  roughness
  normal
  occlusion
  emission
  alpha/coverage

可选 Specular Extension:
  specularColor
  specularWeight
  ior
```

原因：

- metallic-roughness 是最稳的导入/导出子集。
- 角色 toon 里的“高光颜色/强度”通常不是物理 F0，而是风格化 lobe 参数。
- 让 `specularColor` 进入核心会让 PBR specular、toon specular、hair specular、debug specular 重新混在一起。

建议新增：

```hlsl
struct HoSpecularExtensionData
{
    half specularWeight;
    half3 specularColor;
    half ior;
};
```

该结构只由 `PbrSpecularLobe` 消费。`ToonSpecularLobe` 和 `HairSpecularLobe` 不直接消费它，除非 preset 显式做字段映射。

#### 4.7.5 OpenPBR 给 HoRP 的最大启发是分层，不是参数全集

OpenPBR 的成熟点不是“字段多”，而是它把材质理解为：

```text
base substrate
  + coat
  + fuzz
  + opacity
  + emission
  + geometry normal/tangent
```

并且用 layering/mixing 解释能量关系。HoRP 应借鉴这个结构，但不直接继承完整参数面板。

建议 HoRP lobe 层级改成：

```text
Base Layer
  PbrDiffuseLobe
  PbrSpecularLobe
  ToonDiffuseLobe
  ToonSpecularLobe
  HairSpecularLobe
  SkinSubsurfaceLobe

Top Layer
  ClearCoatLobe
  Fuzz/RimLobe
  MatCapLobe

Emission Layer
  EmissionLobe
  DebugEmissionLobe

Semantic Layer
  MaterialSemanticProducer
  SssSourceProducer
  DebugSemanticProducer
```

OpenPBR 的 `coat` 可以对应 HoRP `ClearCoatLobe`；`fuzz` 对 toon 侧的 rim/fresnel/fabric/hair edge 很有参考价值，但 HoRP 不应把所有 rim 都命名成 fuzz。`fuzz` 是物理解释，`rim` 是风格化控制，二者可共享一个上层 lobe 合成位置。

#### 4.7.6 MaterialX 给 HoRP 的启发是“图语义”，不是第一版节点图

MaterialX 的价值在于它把材质外观描述成可组合节点网络。HoRP 第一版不应尝试实现完整 MaterialX，但应该吸收两个规则：

1. 每个 component 要声明输入和输出。
2. 每个 component 的输出类型要可组合，而不是直接写 final color。

对应到 HoRP：

```text
FeatureBlock
  inputs:
    HoStandardSurfaceData.baseColor
    HoLightingContext.mainLightShadow
    HoStyleRamp.shadowBand

  outputs:
    HoLobeOutput.diffuse
    HoLobeOutput.semanticWeight

  consumes:
    Aov.NormalDepth
    MainLight

  produces:
    Lobe.ToonDiffuse
```

这比“shader 里 include 一堆函数，然后靠宏开关串起来”更接近可维护的现代材质契约。

#### 4.7.7 URP `SurfaceData/InputData` 只作为宿主边界参考

URP 的成熟点是函数边界清晰：

```hlsl
UniversalFragmentPBR(InputData inputData, SurfaceData surfaceData)
```

它把几何/灯光输入和材质表面输入分开。HoRP 应保留这个分离：

| URP | HoRP |
| --- | --- |
| `InputData` | `HoGeometryData` + `HoLightingContext` |
| `SurfaceData` | `HoStandardSurfaceData` |
| `BRDFData` | `HoStandardBrdfData`，仅 PBR lobe 内部使用 |
| `LightingData` | `HoCompositeData` / `HoLobeAccumulationData` |
| `UniversalFragmentPBR` | `HoMaterialEvaluate()` + `HoMaterialComposite()` |

但是 HoRP 不应继承以下 URP 习惯作为公共 ABI：

- `smoothness` 命名。HoRP 公共字段统一 `roughness`。
- `_BaseMap` / `_BaseColor` / `_MetallicGlossMap` 等 Unity Lit 属性名。
- URP Lit 的 keyword 组合方式。
- URP Lit pass 结构。
- ShaderGraph debug hook 作为 HoRP debug 语义来源。

#### 4.7.8 HoRP ABI 建议分解版

综合上述对照，建议最终接口从原先 6 个结构调整为 8 个结构：

```hlsl
struct HoStandardSurfaceData
{
    half3 baseColor;
    half alpha;
    half metallic;
    half roughness;
    half occlusion;
    half3 emission;
    half3 normalWS;
    half3 normalTS;
    half3 tangentWS;
    half3 bitangentWS;
    half coverage;
};

struct HoGeometryData
{
    float3 positionWS;
    float4 positionCS;
    float2 uv0;
    float2 uv1;
    half3 viewDirWS;
    half facing;
    half linearDepth;
};

struct HoLightingContext
{
    half3 mainLightDirWS;
    half3 mainLightColor;
    half mainLightDistanceAttenuation;
    half mainLightShadow;
    half3 indirectDiffuse;
    half3 indirectSpecular;
    half screenAoDirect;
    half screenAoIndirect;
    half hoShadow;
};

struct HoStylizedSurfaceData
{
    half regionMask;
    half styleRampId;
    half toonBandBias;
    half toonBandSoftness;
    half rimWeight;
    half matcapWeight;
    half hairHighlightMask;
};

struct HoSpecularExtensionData
{
    half specularWeight;
    half3 specularColor;
    half ior;
};

struct HoTransparencyData
{
    half coverage;
    half blendAlpha;
    half transmissionWeight;
    half refractionWeight;
    half oitWeight;
};

struct HoMaterialSemanticData
{
    half materialClass;
    half sssProfile;
    half thickness;
    half curvature;
    half utility;
    half4 custom0_3;
    half3 sssSourceColor;
    half sssWeight;
};

struct HoLobeOutput
{
    half3 diffuse;
    half3 specular;
    half3 transmission;
    half3 emission;
    half alpha;
    half energyWeight;
    half semanticWeight;
};
```

第一版实现可以不把所有结构都暴露到 shader include 外部，但文档契约应先这么分。否则后面做 toon/hair/skin/debug 时，会再次把风格参数、物理参数、AOV 参数混成一锅。

#### 4.7.9 公有契约到 HoRP 的字段映射

| glTF / USD / OpenPBR / URP | HoRP 字段 | 说明 |
| --- | --- | --- |
| glTF `baseColorFactor.rgb` | `HoStandardSurfaceData.baseColor` | 非金属 diffuse color，金属 F0 颜色来源 |
| glTF `baseColorFactor.a` | `HoStandardSurfaceData.alpha` 或 `HoTransparencyData.blendAlpha` | 取决于 material alpha mode |
| glTF `metallicFactor` | `HoStandardSurfaceData.metallic` | 直接映射 |
| glTF `roughnessFactor` | `HoStandardSurfaceData.roughness` | 直接映射 |
| glTF `metallicRoughnessTexture.g/b` | packed texture -> `roughness/metallic` | 保持 glTF G=roughness、B=metallic 的导入规则 |
| glTF `normalTexture` | `normalTS` -> `normalWS` | import 阶段统一切线空间约定 |
| glTF `occlusionTexture` | `occlusion` | 不替代 HoRP SSAO |
| glTF `emissiveFactor/Texture` | `emission` | HDR 放大由导入器或 preset 决定 |
| USD `diffuseColor` | `baseColor` | metallic workflow 下等价 albedo/base color |
| USD `roughness` | `roughness` | 避免 Unity smoothness |
| USD `opacity` | `alpha` / `coverage` | 由 alpha mode 决定 |
| USD `opacityThreshold` | alpha test cutoff / coverage threshold | 不放进 surface data，可放进 pass state |
| USD `clearcoat` | `ClearCoatLobe.weight` | 可作为 PBR extension，不进核心 |
| OpenPBR `base_weight` | preset/lobe weight | 不进 Standard Surface 核心 |
| OpenPBR `base_color` | `baseColor` | 直接映射 |
| OpenPBR `base_metalness` | `metallic` | 直接映射 |
| OpenPBR `specular_roughness` | `roughness` | HoRP 第一版统一 roughness |
| OpenPBR `coat_weight` | `ClearCoatLobe.weight` | 可选 block |
| OpenPBR `fuzz_weight/color/roughness` | `FuzzOrRimLobe` | toon rim 可借位置，不必借名字 |
| OpenPBR `geometry_opacity` | `coverage` | 表面存在度 |
| OpenPBR `geometry_normal` | `normalTS/normalWS` | base normal |
| OpenPBR `geometry_coat_normal` | `ClearCoatLobe.normalWS` | 可选 block |
| URP `SurfaceData.albedo` | `baseColor` | 仅运行时适配 |
| URP `SurfaceData.smoothness` | `1 - roughness` | 适配层转换，不进入 ABI |
| URP `SurfaceData.clearCoatMask` | `ClearCoatLobe.weight` | 可选 block |
| URP `InputData` | `HoGeometryData` + `HoLightingContext` | 拆得更明确 |

#### 4.7.10 哪些不应该向公有契约对齐

这些字段是 HoRP 自己的渲染系统能力，不应该为了“看起来标准”而塞到 Standard Surface：

| HoRP 能力 | 不对齐原因 | 应放位置 |
| --- | --- | --- |
| `materialClass` | AOV/后处理分类，不是材质外观 | `HoMaterialSemanticData` |
| `sssProfile` | HoRP SSS consumer 索引，不是通用 surface 字段 | `HoMaterialSemanticData` |
| `thickness` | 可参考 glTF volume/OpenPBR subsurface，但 HoRP 当前是 SSS/AOV 语义 | `HoMaterialSemanticData` 或 `SubsurfaceLobe` |
| `curvature` | 调试/SSS/风格化辅助 | `HoMaterialSemanticData` |
| `custom0_3` | HoRP 私有扩展 | `HoMaterialSemanticData` |
| toon ramp | 风格化控制，不是物理参数 | `HoStylizedSurfaceData` + ramp atlas |
| matcap | 视角空间风格 lobe | `MatCapLobe` |
| rim | 可能对应 OpenPBR fuzz/sheen，但 toon 中语义不同 | `RimLobe` |
| hair highlight | 专门 lobe | `HairSpecularLobe` |
| debug specular | debug view | `DebugLobe` |
| OIT weight | 透明合成策略 | `HoTransparencyData` / transparent pass consumer |

#### 4.7.11 对原草案接口的具体修改建议

基于公有契约对照，原第 4 节接口需要这样修：

| 原草案 | 修改建议 | 理由 |
| --- | --- | --- |
| `HoSurfaceData` | 改为 `HoStandardSurfaceData` | 明确它是可交换 PBR 子集 |
| `regionMask` 在 `HoSurfaceData` | 移到 `HoStylizedSurfaceData` | 非公有 standard surface 字段 |
| `alpha` 和 `coverage` 同时在 `HoSurfaceData` | 保留，但补 `HoTransparencyData` 解释 | 避免 alpha blend / alpha test / transmission 混淆 |
| `HoMaterialSemanticData` 包含 `sssSourceColor` | 可保留，但注明它是 semantic producer，不是 surface 输入 | 防止被材质 authoring 当成普通颜色参数 |
| `HoLobeOutput` 只有一个 `specular` | 第一版可保留，registry 中标明 lobe kind | 结构简单，但 debug/pbr/hair/toon 要能追踪来源 |
| `HoLightingContext` 混入 `hoShadow` | 可保留为 HoRP light modifier | 但 shadow/ramp 不应由 lobe 自己查全局 |

最终设计原则：

```text
公有契约字段进入 HoStandardSurfaceData。
风格化字段进入 HoStylizedSurfaceData。
后处理/AOV/调试字段进入 HoMaterialSemanticData。
透明合成字段进入 HoTransparencyData。
每个高光/漫反射/SSS/MatCap/Rim/Hair/Debug 只产出 HoLobeOutput。
```

这样 HoRP 既能从 glTF/USD/OpenPBR/MaterialX 接资产，又不会被它们限制住 toon 和角色渲染能力。

---

## 5. 新材质组件分层

### 5.1 Surface Input 组件

| 新组件 | 来源参考 | 说明 |
| --- | --- | --- |
| `BaseColorTexture` | lilPBR main tex / lilToon main tex | 生产 `baseColor` / `alpha` |
| `NormalMap` | lilPBR normal / lilToon normal | 生产 `normalWS` / `normalTS` |
| `MaterialMapPacked` | lilPBR PBR map | metallic / roughness / AO / height |
| `DetailLayer` | lilPBR detail | 第一版可延后 |
| `RegionMask` | lilToon masks / HoAOV custom | 用于皮肤、头发、衣物等区域 |
| `StyleRampAtlas` | lilToon shadow LUT / ramp | toon shadow、rim、spec、SSS 统一 ramp |

第一版建议只做：

```text
BaseColorTexture
NormalMap
MaterialMapPacked
StyleRampAtlas
SemanticMap
```

### 5.2 Lighting Input 组件

| 新组件 | 来源参考 | 说明 |
| --- | --- | --- |
| `UrpMainLightInput` | lilPBR `GetMainLight` | HoRP/URP 主光输入 |
| `UrpAdditionalLightInput` | lilPBR additional loop | 可选 |
| `IndirectLightInput` | lilPBR baked GI / reflection | 间接光 |
| `ScreenAoReceiver` | lilPBR/lilToon SSAO | 屏幕 AO 接收端 |
| `HoShadowReceiver` | lilPBR `HoShadowCastAttenuation` | HoShadow atlas 接收 |
| `LightingDebugGate` | URP debug flags | DebugDomain 一等公民 |

注意：这些组件应由 HoRP lighting contract 驱动，不应由材质自己到处采全局纹理。

### 5.3 Diffuse Lobe 组件

| 新组件 | 来源参考 | 适用 |
| --- | --- | --- |
| `PbrDiffuse` | lilPBR `GetDiffuse()` | 环境 / PBR |
| `ToonDiffuseRamp` | lilToon `lilGetShading()` | 角色 / NPR |
| `FaceShadowDiffuse` | lilToon face SDF shadow | 脸部 |
| `WrappedDiffuse` | lilPBR subsurface wrap | 皮肤 / 蜡 / 透光 |
| `ClothDiffuse` | lilPBR cloth | 布料 |

### 5.4 Specular Lobe 组件

| 新组件 | 来源参考 | 说明 |
| --- | --- | --- |
| `PbrSpecularGGX` | lilPBR `GetSpecular()` | 标准 PBR 高光 |
| `PbrSpecularAnisotropic` | lilPBR `SpecularTermAniso()` | 各向异性 |
| `ToonSpecular` | lilToon `_SpecularToon` | toon 高光 |
| `HairSpecularPrimary` | lilToon anisotropy primary | 发丝主高光 |
| `HairSpecularSecondary` | lilToon anisotropy 2nd | 发丝副高光 |
| `ClearCoatSpecular` | lilPBR clear coat | 清漆 |
| `DebugSpecular` | 新增 | 调试 lobe |

高光不应再是一个总开关。每个高光 lobe 都应明确：

- 输入 normal / tangent / roughness / mask。
- 是否受 shadow。
- 是否受 AO。
- 是否写 debug。
- 合成方式。

### 5.5 Stylized Lobe 组件

| 新组件 | 来源参考 | 说明 |
| --- | --- | --- |
| `RimShade` | lilToon rim shade | 乘暗/染色边缘 |
| `RimLight` | lilToon rim light | 加亮边缘 |
| `Backlight` | lilToon backlight | 逆光 |
| `MatCap` | lilToon matcap | 风格化材质光 |
| `SecondaryMatCap` | lilToon matcap2nd | 第二 matcap |
| `Glitter` | lilToon glitter | 可延后 |
| `StylizedReflection` | lilToon reflection blend | NPR reflection |

### 5.6 Subsurface / Transmission 组件

| 新组件 | 来源参考 | 说明 |
| --- | --- | --- |
| `ForwardThinSss` | lilToon `lilSSS()` | forward 假 SSS |
| `PbrSubsurface` | lilPBR `_SUBSURFACE` | PBR 侧 subsurface |
| `SssSourceProducer` | lilToon/lilPBR HoAOVSSS | 写 `Aov.SssSource` |
| `TransmissionGatherInput` | 旧 HoSSS transmission | 后续屏幕空间 transmission |

必须拆开：

```text
Forward SSS visual lobe
Screen-space SSS source producer
Screen-space SSS renderer feature consumer
```

### 5.7 Semantic / AOV 组件

| 新组件 | 来源参考 | 说明 |
| --- | --- | --- |
| `AovOutputStandard` | old HoAOV | 写标准 AOV |
| `MaterialSemanticProducer` | lilPBR/lilToon HoAOV material fields | 写材质语义 |
| `SssSourceProducer` | HoAOVSSS | 写 SSS source |
| `ObjectSemanticReader` | renderer user value / MPB | 只读对象语义 |
| `AovDebugProducer` | new debug | debug 输出 |

材质不能私自定义 object semantics。ObjectDomain 仍由 authoring / RSUV / renderer binding 管理。

### 5.8 Transparent / OIT 组件

| 新组件 | 来源参考 | 说明 |
| --- | --- | --- |
| `AlphaClipPolicy` | lilPBR/lilToon alpha clip | cutout |
| `DitherPolicy` | old dither | dither transparency |
| `TransparentComposite` | old transparent | 普通透明 |
| `OitAccumulationOutput` | old `lilToonOIT` | 新 `HoUrpOitAccumulation` |
| `ForwardSkipWhenOit` | old `_lilOITActive` 逻辑 | 新 phase policy |

旧 `_lilOITEnabled` / `_lilOITActive` 不能进入新 ABI。新系统应声明：

```text
Preset.SupportsOit
MaterialInstance.ParticipatesOit
PhasePolicy.SkipForwardWhenOitAccumulating
Pass.HoUrpOitAccumulation
```

---

## 6. 输入收敛与纹理压缩方案

### 6.1 旧问题

lilToon / lilPBR 暴露了大量参数，原因包括：

- 面向普通用户调材质。
- 面向 VRChat avatar / world 兼容。
- 多管线支持。
- 每个功能自带 mask、normal、blend、enable lighting、shadow mask。
- inspector 需要让所有功能都能独立调。

HoRP 不需要这样做。HoRP 的 feature / semantic / debug 能承担更多系统能力，所以材质参数可以强收敛。

### 6.2 建议第一版输入

```text
BaseMap
  rgb: base color
  a: alpha

NormalMap
  xy/z: normal

MaterialMap
  r: metallic or material region dependent scalar
  g: roughness
  b: occlusion
  a: height or smoothness override

SemanticMap
  r: SSS weight / thickness
  g: specular mask
  b: rim / matcap mask
  a: region / utility

StyleRampAtlas
  x: ramp coordinate
  y: ramp row index

RegionMap
  optional, maps skin / hair / cloth / accessory
```

### 6.3 Ramp atlas 替代旧参数

旧 lilToon：

- shadow color 1/2/3。
- shadow color texture。
- LUT shadow。
- border / blur / range。
- rim color / rim indir color。
- specular toon border / blur。

新 HoRP：

```text
StyleRampAtlas rows:
  0: base shadow ramp
  1: face shadow ramp
  2: skin shadow ramp
  3: hair shadow ramp
  4: toon specular ramp
  5: hair specular primary ramp
  6: hair specular secondary ramp
  7: rim ramp
  8: SSS tint ramp
```

材质只暴露：

- ramp atlas。
- ramp row preset。
- ramp coordinate bias/scale 少量参数。

---

## 7. 旧参数到新输入的对照

### 7.1 PBR 基础

| 旧 lilPBR | 新 HoRP | 处理 |
| --- | --- | --- |
| `_MainTex` / `_Color` | `BaseMap` / `BaseColorTint` | 保留概念，改名 |
| `_BumpMap` / `_BumpScale` | `NormalMap` / `NormalStrength` | 保留 |
| `_PBRMap` | `MaterialMap` | 保留 packed 思路 |
| `_MetallicChannel` 等 channel selector | Preset 固定解释 | 裁掉材质侧任意 channel selector |
| `_Glossiness` / `_InvertSmoothness` | `RoughnessScale` / import preset | 收敛 |
| `_OcclusionStrength` | `OcclusionStrength` | 保留少量 |

### 7.2 Toon shadow

| 旧 lilToon | 新 HoRP | 处理 |
| --- | --- | --- |
| `_ShadowColor` / `_Shadow2ndColor` / `_Shadow3rdColor` | `StyleRampAtlas` | 合并 |
| `_ShadowBorder` / `_ShadowBlur` | `ToonShadowTerm` preset params | 少量保留 |
| `_ShadowBorderMask` / `_ShadowBlurMask` / `_ShadowStrengthMask` | `SemanticMap` / `RegionMap` | 压缩 |
| `_ShadowColorTex` / LUT | `StyleRampAtlas` | 合并 |
| `_ShadowMaskType == face SDF` | `FaceShadowDiffuse` | 独立组件 |

### 7.3 Specular / hair

| 旧项 | 新组件 | 处理 |
| --- | --- | --- |
| `_SpecularToon` | `ToonSpecular` | 保留概念 |
| `_Smoothness` / `_Reflectance` | `PbrSpecularGGX` inputs | 收敛 |
| `_UseAnisotropy` | `HairSpecularPrimary/Secondary` preset | 不做运行时随意开关 |
| `_AnisotropyShiftNoiseMask` | `HairSpecularNoise` | 后续 |
| lilPBR `_SpecularHighlight*` | `DebugSpecular` 或 `StylizedSpecular` | 独立组件 |

### 7.4 MatCap / rim / emission

| 旧项 | 新组件 | 处理 |
| --- | --- | --- |
| `_MatCapTex` / `_MatCap2ndTex` | `MatCap` / `SecondaryMatCap` | 保留 |
| `_MatCapBlendMode` 多模式 | preset 固定 blend | 裁剪 |
| `_RimColor` / `_RimIndirColor` | `RimLight` + ramp | 保留概念 |
| `_EmissionMap` / `_Emission2ndMap` | `EmissionPrimary/Secondary` | 第一版只保留 primary |

### 7.5 SSS / AOV

| 旧项 | 新 HoRP | 处理 |
| --- | --- | --- |
| `_UseSSS` / `_SubsurfaceScattering` | `Shading.SssWeight` | 改名 |
| `_HoSSSProfileId` | `Material.SssProfile` | 改名 |
| `_SSSThicknessMap` / `_SubsurfaceMap` | `SemanticMap.r` or `ThicknessMap` | 收敛 |
| `_SSSColor` / `_SubsurfaceColor` | `SssSourceColor` / ramp | 保留 |
| `_HoAovThickness` | `Material.Thickness` | 改名 |
| `_HoAovCurvature` | `Material.Curvature` | 改名 |

---

## 8. Preset 草案

### 8.1 `Character_Toon_Core`

用途：普通 toon 角色。

```text
Template:
  CharacterForward
  CharacterAov
  CharacterDepth
  CharacterShadow

Components:
  BaseColorTexture
  NormalMap
  RegionMask
  StyleRampAtlas
  ToonDiffuseRamp
  RealtimeShadowReceiver
  RimShade
  RimLight
  MatCap
  EmissionPrimary
  AovOutputStandard

Produces:
  Material.Class
  Material.Custom0-3 optional
  Aov.MaskId
  Aov.NormalDepth
  Aov.SurfaceData
```

### 8.2 `Character_Skin_SSS`

用途：皮肤。

```text
Components:
  BaseColorTexture
  NormalMap
  SemanticMap
  StyleRampAtlas
  ToonDiffuseRamp or WrappedDiffuse
  SkinSssSourceProducer
  ForwardThinSss optional
  RimLight
  AovOutputStandard

Produces:
  Material.Class = Skin
  Material.SssProfile
  Material.Thickness
  Material.Curvature
  Shading.SssSourceColor
  Shading.SssWeight
  Aov.SssSource
```

### 8.3 `Hair_Toon`

用途：头发。

```text
Components:
  BaseColorTexture
  NormalMap
  HairTangentInput
  ToonDiffuseRamp
  HairSpecularPrimary
  HairSpecularSecondary
  RimLight
  MatCap optional
  AovOutputStandard

Produces:
  Material.Class = Hair
  Material.Custom0-3 optional
```

注意：头发高光必须和 PBR 高光拆开，不能共用一个 `Specular` block。

### 8.4 `Environment_PBR`

用途：场景 PBR。

```text
Components:
  BaseColorTexture
  NormalMap
  MaterialMapPacked
  PbrDiffuse
  PbrSpecularGGX
  ReflectionEnvironment
  ClearCoat optional
  AovOutputStandard optional

Produces:
  Material.Class = Environment
```

### 8.5 `Transparent_OIT`

用途：半透明角色部件、玻璃、透明衣料。

```text
Components:
  BaseColorTexture
  NormalMap optional
  TransparentComposite
  OitAccumulationOutput
  AovOutputStandard optional

Passes:
  UniversalForward
  HoUrpAovOutput
  HoUrpOitAccumulation

Policy:
  SupportsOit = true
  ParticipatesOit = material instance or preset policy
  SkipForwardWhenOitAccumulating = true
```

---

## 9. Feature Block 描述模型补充

第十阶段已有最小模型。这里补充“组分级输入输出”要求。

```text
MaterialFeatureBlock
  Id
  Domain
  Stage
  RequiredSurfaceFields
  RequiredGeometryFields
  RequiredLightingFields
  RequiredTextures
  RequiredSemantics
  ProducedSurfaceFields
  ProducedLobeFields
  ProducedSemantics
  ProducedResources
  CompatiblePresets
  DebugViews
  VariantPolicy
```

### 9.1 Variant policy

每个 block 必须声明：

```text
AlwaysCompiled
PresetStatic
MaterialInstanceToggle
DebugOnly
Unsupported
```

原则：

- 第一版尽量使用 `PresetStatic`。
- 少量 debug 使用 `DebugOnly`。
- 避免 `MaterialInstanceToggle`。
- 禁止 UI 动态添加 `Unsupported` 之外的新组合。

### 9.2 组分输出不得直接写 final color

除 `Composite` 类 block 外，所有 lobe 只写 `HoLobeOutput`。

错误模式：

```text
Specular block 直接修改 color
Rim block 直接修改 color
MatCap block 直接修改 color
SSS block 直接修改 color
```

正确模式：

```text
Specular block -> HoLobeOutput.specular
Rim block -> HoLobeOutput.emission or stylized
MatCap block -> HoLobeOutput.specular/stylized
Composite block -> final color
```

---

## 10. HoRP AOV 生产规则

### 10.1 ObjectDomain

ObjectDomain 不由材质 shader 定义。

来源：

- `ObjectSemanticAuthoring`
- RSUV / renderer user value
- MPB fallback

材质 shader 可以读取 object semantics 以做局部表现，但不能发明新的 object id / flags 语义。

### 10.2 MaterialDomain

MaterialDomain 可以由材质 producer 生成：

```text
Material.Class
Material.SssProfile
Material.Thickness
Material.Curvature
Material.Utility
Material.Custom0-3
```

第一版全部写入现有 AOV：

```text
Aov.SurfaceData
Aov.MaterialCustom0_3
```

### 10.3 ShadingDomain

ShadingDomain 是派生结果：

```text
Shading.SssSourceColor
Shading.SssWeight
future:
  Shading.StylizedShadow
  Shading.SpecularMask
  Shading.RampCoord
  Shading.DebugSpecular
```

当前第一版必须输出：

```text
Aov.SssSource.rgb = Shading.SssSourceColor
Aov.SssSource.a = Shading.SssWeight
```

### 10.4 AOV pass 名

旧：

```text
HoAOV
HoAOVSSS
```

新：

```text
HoUrpAovOutput
```

是否拆出独立 `HoUrpSssSourceOutput` 需后续评估。第一版可把 SSS source 放在同一个 AOV output pass，以减少 pass 数，但结构上仍要把 `SssSourceProducer` 作为独立 component。

---

## 11. 必须裁剪清单

### 11.1 管线裁剪

删除长期支持：

- Built-in Render Pipeline。
- LWRP。
- HDRP。
- ForwardBase / ForwardAdd。
- UsePass 共享旧 pass。
- `lil_multi_compile_*` 旧生成宏。

仅支持：

```text
Unity 6000.3+
URP
RenderGraph-first
HoRP pass / resource / semantic contract
```

### 11.2 VRChat / world 兼容裁剪

删除长期支持：

- Udon。
- AudioLink。
- VRC Light Volumes。
- VRC fallback。
- avatar safety fallback。
- world SDK build optimization。
- Quest / event submission 特化。

这些能力可以作为 LegacyInterop 事实记录，但不能进入 HoRP 材质核心。

### 11.3 UI / keyword 裁剪

删除：

- 反射式 inspector 绑定。
- 材质 UI 决定 pass 结构。
- 任意用户开关映射 shader feature。
- 每个小组件各自暴露完整 texture/mask/blend/lighting/shadow 参数。

保留：

- preset 选择。
- 少量 scalar。
- ramp / atlas / packed map 输入。
- debug / validation。

---

## 12. 建议落地顺序

### Phase A：对照表冻结

输出本文后，补一份机器可读/表格化对照：

```text
OldSymbol
OldFile
OldMeaning
NewComponent
NewSemantic
MigrationDecision
Notes
```

重点覆盖：

- lilPBR `ShadingParams`。
- lilPBR `ComputeLights()`。
- lilToon `lilFragData`。
- lilToon shadow / SSS / specular / matcap / rim / emission。
- old HoAOV / HoAOVSSS fields。

### Phase B：HLSL ABI 草案

在 HoUrp-Extensions 中定义：

```text
HoUrpMaterialSurface.hlsl
HoUrpMaterialSemantic.hlsl
HoUrpMaterialLighting.hlsl
HoUrpMaterialLobes.hlsl
HoUrpMaterialAov.hlsl
HoUrpMaterialOit.hlsl
```

只定义结构和最小 encode/decode，不做完整 PBR/NPR。

### Phase C：最小 generated shader

做一个：

```text
Hidden/HoURP/Generated/Character_DebugLit_SSS_OITReady
```

必须包含：

- `UniversalForward`
- `HoUrpAovOutput`
- `HoUrpOitAccumulation`

不得引用：

- lilToon include。
- lilPBR include。
- `_HoAov*`。
- `_lil*`。
- URP Lit full pass include。

### Phase D：Preset/FeatureBlock registry

先做数据模型，不做 UI。

```text
MaterialPresetDefinition
MaterialFeatureBlockDefinition
MaterialPassDefinition
MaterialSemanticProduction
MaterialVariantPolicy
```

### Phase E：HoNpr 统一材质系统接入

`HoUrp-Extensions` 只定义契约和测试原型。真正材质包应放在：

```text
HoNpr
```

`HoNpr` 是未来统一 HoRP 材质系统承载仓库。PBR 不是独立产品方向，只作为 `HoStandardSurface`、BRDF/PBR lobe、导入/退化路径存在。`HoToon` 的 URP 半调 toon shader、半调贴图和导入工具已迁入 `HoNpr` 作为小模块；独立 `HoToon` 仓库仍有 Built-in shader，只能作为历史参考，不再承担完整 HoRP 材质系统主线。

---

## 13. 第一版验收问题

### 13.1 架构问题

- 能否从 preset 查询它启用哪些 component？
- 能否从 component 查询它消费/生产哪些字段？
- 能否从 component 查询它生产哪些 semantic / resource？
- 能否从 generated shader diff 里看出每个 block 的来源？
- 是否所有 pass 都由 template 决定，而不是 UI 决定？

### 13.2 旧实现隔离问题

- 是否没有 `lilToon` / `lilPBR` include？
- 是否没有 `_lil*` 新绑定？
- 是否没有 `_HoAov*` 新材质属性？
- 是否没有 `HoAOV` / `HoAOVSSS` / `lilToonOIT` 新 pass？
- 是否没有 Built-in / LWRP / HDRP 分支？
- 是否没有 VRChat / AudioLink / Udon / VRC Light Volumes 条件编译？

### 13.3 语义问题

- `Material.Class` 是否来自 `HoMaterialSemanticData`？
- `Material.SssProfile` 是否不再叫 `_HoSSSProfileId`？
- `Shading.SssSourceColor` / `Shading.SssWeight` 是否能被 AOV / SSS / Debug 消费？
- Object semantics 是否仍由 ObjectDomain authoring/RSUV/MPB 管理？
- Debug view 是否能显示每个材质语义来源？

### 13.4 组分问题

- PBR specular、toon specular、hair specular、debug specular 是否是独立 lobe？
- Forward fake SSS 和 screen-space SSS source 是否拆开？
- MatCap / Rim / Emission 是否不直接写 final color，而是产出 lobe output？
- StyleRampAtlas 是否替代了大部分 shadow/rim/spec 细碎参数？

---

## 14. 当前结论

新材质系统不应是“lilToon/lilPBR 的 HoRP 精简移植版”，而应是：

```text
HoStandardSurface
  + HoStylizedSurface
  + HoSemanticSurface
  + 静态 FeatureBlock
  + 少量 preset
  + 组分级 lobe 输出
  + AOV/SSS/OIT/Debug 一等接入
```

lilPBR 更适合提供 PBR lobe 和材质输入规范化参考；lilToon 更适合提供 toon shadow、matcap、rim、emission 的顺序和组分经验；旧 HoAOV/HoSSS 更适合提供语义输出和 consumer 链路参考；glTF / USD Preview / OpenPBR / MaterialX / URP 更适合提供“哪些字段是公开可交换 surface，哪些字段必须留在 HoRP 扩展层”的判断边界。

真正要继承的是能力边界和行为经验，不是旧 shader 结构。
