# 第十五步具体执行总清单

> 本清单用于把第十五步从规划推进到代码实现。执行目标是轻量 FilterKit + 真 screen-space SSS 第一版 + ImageChain 最小消费者。不要先做复杂 FilterGraph。

## Step 1. 冻结轻量边界

先读：

- `Documentation~/rp重构第十五步执行计划.md`
- `Documentation~/rp重构第十五步/rp第十五步FilterKit具体实施步骤.md`
- `Documentation~/rp重构第十五步/rp外部滤波算法借鉴清单.md`

要求：

- 承认 FilterKit 只是 HLSL + shader + C# helper。
- 不新增 `FilterRequest`。
- 不新增 scheduler。
- 不把外部库直接编译进 runtime。

验收：

- 文档中能搜到 `FilterKit 不是调度框架`。
- `Runtime/Filter` 规划中没有 `FilterGraph.cs`。

## Step 2. 建立 FilterKit 文件骨架

新增：

- `Runtime/Filter/README.md`
- `Runtime/Filter/HoUrpFilterIds.cs`
- `Runtime/Filter/HoUrpFilterUtils.cs`
- `Runtime/Filter/HoUrpFilterResources.cs`
- `Runtime/Filter/Shaders/HoUrpFilterCommon.hlsl`
- `Runtime/Filter/Shaders/HoUrpFilterSampling.hlsl`
- `Runtime/Filter/Shaders/HoUrpFilterDepthNormalGate.hlsl`
- `Runtime/Filter/Shaders/HoUrpFilterBurleyDiffusion.hlsl`
- `Runtime/Filter/Shaders/HoUrpFilterBlur.shader`

要求：

- helper 只放 id / pass index / descriptor / texel 参数。
- include 不绑定业务纹理。
- shader pass name 清楚。

验收：

- `rg "FilterRequest|scheduler|class .*FilterGraph" Runtime/Filter` 无结果。
- `HoUrpFilterIds.cs` 里能找到 pass index。

## Step 3. 拆旧 HoSSS 公式

来源：

- `lilToon-URP-Extensions/Runtime/SubsurfaceScattering/HoSubsurfaceScattering.shader`

落点：

- `HoUrpFilterBurleyDiffusion.hlsl`
- `HoUrpFilterDepthNormalGate.hlsl`
- `Runtime/Filter/SSS/HoUrpSssFilter.hlsl`

要求：

- Burley / gate 公式只迁移必要函数。
- 旧 `_lilHoSSS*` 名称不进入新 runtime。
- 注释登记旧来源。

验收：

- SSS shader include FilterKit 文件。
- 公式不在多个 shader 中重复复制。

## Step 4. 实现 SSS Source / Diffusion / Composite

参考：

- `Documentation~/rp重构第十五步/rpSSS_FilterKit落地执行计划.md`

修改：

- `Runtime/Features/SubsurfaceScatteringRendererFeature.cs`
- `Runtime/Shaders/Hidden/HoURP/SSS/SubsurfaceScattering.shader`
- 或新增 `Runtime/Filter/SSS/HoUrpSssDiffusion.shader`

要求：

- Source Prepare 写 `Sss.Source.a`。
- Diffusion 写 `Sss.Diffusion.rgb/a`。
- Composite 读取 `Sss.Diffusion.a`。
- 不读取 `Aov.Diffuse.a` 作为权重。
- 非 receiver 不参与。

验收：

- SSS Debug 能看 Source / Diffusion / CompositeWeight。
- RenderDoc / Frame Debugger 中 pass 可单独定位。

## Step 5. 接入 simple image blur

参考：

- `Documentation~/rp重构第十五步/rpImageChain与FilterKit资源策略.md`

修改：

- `Runtime/Image/*`
- `Runtime/PostProcess/*` 中现有 ImagePost prototype 或新增最小消费者。

要求：

- 用 `HoUrpFilterBlur.shader`。
- ImageChain 显式管理 WorkA / WorkB。
- 不让 FilterKit 创建资源。

验收：

- 一个 simple blur 可运行。
- 关闭 effect 后画面恢复。
- 新增 image pass 不新增长期 full-res RT 字段。

## Step 6. 补外部来源登记

新增：

- `Runtime/Filter/ThirdParty/README.md`
- `Runtime/Filter/ThirdParty/EXTERNAL_RENDERING_REFERENCES.md`

要求：

- 记录旧 HoSSS、Falcor、Filament、FidelityFX、NRD、pbrt-v4 的用途。
- 只记录实际借鉴的来源，不写成大型百科。
- 如果没有实际移植外部代码，标记 `reference only`。

验收：

- 借鉴表中有 source file path 和 HoURP 落点。
- 没有 license 记录的外部文件不进入 runtime。

## Step 7. 静态测试与扫描

参考：

- `Documentation~/rp重构第十五步/rp第十五阶段测试与验收清单.md`

新增 / 修改：

- `Tests/Runtime/HoUrpFilterContractTests.cs`
- 可扩展已有 ABI 测试。

测试：

- 通用 include 不绑定 texture。
- FilterKit 没有 scheduler。
- SSS shader 不读取旧 ABI。
- 通用 blur 不读取 AOV。
- SSS shader 声明 guide。

## Step 8. Unity 手动验收

场景：

- AOV Output 开启。
- Subsurface Scattering 开启。
- 至少一个 receiver 和一个 non-receiver。
- 至少一个 ImageChain simple blur 消费者。

检查：

- Source / Diffusion / Composite pass 分开。
- `Sss.Source.a` gating 正确。
- profile / thickness / radius 可调。
- simple blur pass 可定位。
- Debug 能看 source / gate / result。

## Step 9. 文档回填

更新：

- `Documentation~/rp重构第十五步执行计划.md`
- `Documentation~/rp重构第十五步/rp第十五阶段测试与验收清单.md`
- 如果实际代码落地，补执行记录。

要求：

- 文档不再要求复杂 FilterGraph。
- 记录实际实现了哪些 shader/pass。
- 记录未做项：NRD、temporal、FSR、frame interpolation、DOF。

## 完成判定

第十五步完成条件：

- FilterKit 文件存在并被至少一个实际 pass 使用。
- SSS 第一版用 FilterKit include 实现 diffusion。
- ImageChain 至少有一个简单 FilterKit 消费者。
- 外部来源登记完整。
- 静态扫描和手动验收通过。
