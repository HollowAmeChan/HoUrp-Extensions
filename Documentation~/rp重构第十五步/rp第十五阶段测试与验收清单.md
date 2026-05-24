# 第十五阶段测试与验收清单

> 第十五步验收重点是“滤波代码可复用但调用链简单可 debug”。不以 NRD 接入、复杂 temporal、完整后处理迁移作为完成条件。

## 1. 静态扫描

必须满足：

```text
rg "class .*FilterGraph|FilterRequest|scheduler" Runtime/Filter
```

无结果，除非在 README 中作为禁止项出现。

必须没有新增旧 ABI：

```text
rg "_lilHoSSS|_lilHoAovSssTexture|_lilShoost" Runtime/Filter Runtime/Shaders/Hidden/HoURP/SSS
```

必须满足：

- `HoUrpFilterCommon.hlsl` 不声明 texture。
- `HoUrpFilterBurleyDiffusion.hlsl` 不声明 texture。
- 通用 blur shader 不读取 AOV。
- SSS shader 明确声明 depth / normal / profile / thickness 输入。

## 2. FilterKit 文件验收

检查：

- `Runtime/Filter/README.md`
- `Runtime/Filter/HoUrpFilterIds.cs`
- `Runtime/Filter/HoUrpFilterUtils.cs`
- `Runtime/Filter/Shaders/HoUrpFilterCommon.hlsl`
- `Runtime/Filter/Shaders/HoUrpFilterSampling.hlsl`
- `Runtime/Filter/Shaders/HoUrpFilterDepthNormalGate.hlsl`
- `Runtime/Filter/Shaders/HoUrpFilterBurleyDiffusion.hlsl`
- `Runtime/Filter/Shaders/HoUrpFilterBlur.shader`

通过标准：

- pass id 不靠魔法数字散落。
- shader pass name 可读。
- include 可单独打开理解。
- 每个 shader 顶部记录输入、输出、采样预算。

## 3. SSS 验收

场景：

- 开启 AOV Output。
- 开启 Subsurface Scattering。
- 使用真 screen-space SSS 材质入口。
- 场景内同时放置 receiver 和 non-receiver 对象。

检查：

- `Sss.Source.a` 只在 receiver 上有值。
- `Aov.Diffuse.a` 不影响 SSS 参与。
- profile / thickness / radius 改变扩散。
- depth / normal 边界不明显漏色。
- 关闭 SSS feature 后无残留。

Debug：

- Source
- Diffusion
- CompositeWeight
- ProfileId
- Thickness
- Receiver gate

失败判定：

- forward fSSS 被当作 screen-space SSS 验收。
- source / diffusion / composite 合在一个不可分辨 pass。
- SSS shader 复制了一份未登记的 blur / Burley 公式。

## 4. ImageChain + FilterKit 验收

至少跑一个 simple blur：

- ImageChain 当前图像作为输入。
- `HoUrpFilterBlur.shader` 作为 shader。
- 输出回 ImageChain next。

通过标准：

- WorkA / WorkB 复用。
- 新增 pass 不新增长期 full-res RT 字段。
- Frame Debugger 能看到具体 blur pass。
- 关闭 effect 后画面恢复。

Multi-pass effect 第一轮只需规划或最小验证：

- RGBBlurV2 或 Glow 任选一个。
- transient 命名清楚。
- 不通过 FilterKit 创建资源。

## 5. 外部来源验收

如果引入外部代码或公式，必须存在：

```text
Runtime/Filter/ThirdParty/EXTERNAL_RENDERING_REFERENCES.md
```

记录：

- 仓库 URL。
- commit。
- source file path。
- license。
- HoURP 落点。
- 使用方式。

失败判定：

- 外部文件复制进 runtime 但没有来源记录。
- vendor 目录被 asmdef 编译。
- 外部 SDK native backend 被当作第十五步必要依赖。

## 6. Unity 手动验收

Renderer Data：

- AOV Output 开启。
- Subsurface Scattering 开启。
- Debug view 可切换。
- 如测试 ImageChain，开启对应 ImagePost prototype。

RenderDoc / Frame Debugger 检查：

- SSS Source Prepare pass。
- SSS Diffusion pass。
- SSS Composite pass。
- FilterKit blur pass。
- ImageChain read/write pass。

通过标准：

- pass 名称清楚。
- 输入输出 texture 可追踪。
- 没有不可解释的全局纹理副作用。

## 7. 完成条件

第十五步完成需要同时满足：

- FilterKit 是轻量源码库，不是调度框架。
- SSS 第一版复用 FilterKit HLSL。
- ImageChain 至少有一个 FilterKit 消费者。
- 外部算法来源有登记规则。
- Debug 能拆开看 source / gate / result。
- 没有把 NRD / Falcor / FidelityFX / Filament / pbrt runtime 直接塞进发布路径。
