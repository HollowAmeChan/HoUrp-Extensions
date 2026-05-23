# rpShadowCast 工作模式与使用方法

> 这份文档给两类读者看：一类是实际在 Unity 里验证和使用 HoURP ShadowCast 的用户；另一类是后续继续改这块系统的 AI/开发者。  
> 这里记录的是当前阶段的功能边界和使用方式，不是旧实现兼容说明，也不是未来材质系统的完整 UI 规范。

---

## 1. 系统定位

HoURP ShadowCast 是一条独立的自定义投影/受影链路。

它不写入 URP 内置 main light shadow，不替换 URP 的主光/天光，也不把 HoCast 伪装成 URP 内置光照的一部分。当前系统只负责：

- 收集参与 HoCast 的灯光。
- 用 `ShadowCaster` pass 写入 HoURP 自己的 shadow atlas。
- 发布 HoURP 自己的 receiver sampling 全局数据。
- 由材质主动调用 receiver sampling 函数决定如何混入最终光照。
- 提供 atlas debug、参与灯列表和 receiver debug shader 供验证。

材质系统后续必须能区分：

- URP 主光 / 唯一天光 / 内置光照项。
- HoURP ShadowCast receiver term。

不要把 HoCast attenuation 隐式塞进 URP main light shadow，也不要把它命名成 main shadow。

---

## 2. 当前工作模式

### 2.1 灯光来源

默认工作模式是自动收集当前相机的 `visibleLights`：

- `Spot Light` 进入主 ShadowCast atlas，每盏灯 1 个 slice。
- `Point Light` 进入主 ShadowCast atlas，每盏灯 6 个 cube faces。
- 非 URP main light 的额外 `Directional Light` 进入 second directional atlas，每盏灯按 cascades 写入。

RendererFeature asset 不可靠引用场景对象，所以主界面默认不要求用户拖灯。

手动灯光数组仍然保留，但只作为高级补充入口：

- `手动 Spot Light`
- `手动 Point Light`
- `手动次方向光`

这些列表不应该再作为主要 authoring 方式，也不应该重新放回 Inspector 主界面。

### 2.2 Atlas 分配

主 atlas：

- Spot：单 slice。
- Point：6 faces。
- 使用 `HoShadowCastAtlasPacker` row packing。

Second directional atlas：

- 每盏次方向光按 block 成组分配。
- 1 cascade：`1x1`
- 2 cascades：`2x1`
- 3/4 cascades：`2x2`
- block 作为整体进入 atlas packer。
- 如果一盏次方向光的 block 放不下，整盏光跳过，不允许写入半套 cascade。

Debug atlas 显示必须反映真实分配：

- 主 atlas 用青色线显示每个 slice。
- second directional atlas 用橙色线显示 cascade slice，用黄色粗线显示每盏光的 block。

### 2.3 RenderGraph 生命周期

ShadowCast pass 必须在 RenderGraph 中显式声明资源。

允许：

- ShadowCast atlas 作为 RenderGraph depth texture 生产。
- Publish pass 把必要的 shader globals 发布给 receiver。
- Debug pass 读取已声明的 atlas 资源。

禁止：

- 依赖上帧残留全局纹理。
- 把 camera color / post chain ping-pong 当作 ShadowCast 数据源。
- 在 feature 关闭、相机类型不匹配、无有效灯光时保留上一帧 ShadowCast 状态。

---

## 3. Unity 使用方法

### 3.1 RendererFeature 配置

在 URP Renderer Data 上添加：

```text
HoURP ShadowCast
```

常用设置：

- `启用 ShadowCast`：总开关。
- `游戏视图` / `场景视图`：控制哪些相机运行。
- `自动收集可见灯光`：默认开启。
- `参与灯光层`：只有这些 layer 上的 Light 会参与收集。
- `投射物体层`：只有这些 layer 上的 Renderer 会写入 atlas。
- `点/聚光 Atlas 尺寸`：主 atlas 尺寸。
- `Spot 单片分辨率`：Spot light slice 分辨率。
- `Point 单面分辨率`：Point light cube face 分辨率。
- `次方向光 Atlas 尺寸`：second directional atlas 尺寸。
- `次方向光级联分辨率`：每个 directional cascade slice 分辨率。
- `调试视图`：可切换 atlas debug。

高级设置：

- 手动补充灯光列表只在自动收集不满足验证需求时使用。
- Render Pass 时机通常不要改，除非在查顺序问题。

### 3.2 场景验证步骤

最小验证场景：

- 一个 plane 作为 receiver。
- 一个或多个带 `ShadowCaster` pass 的物体作为 caster。
- 一个 Spot Light 或 Point Light。
- 一个额外 Directional Light，用于验证 second directional atlas。
- Renderer Data 启用 `HoURP ShadowCast`。

验证 atlas：

- `调试视图 = Atlas`：检查 Spot/Point slices。
- `调试视图 = SecondDirectionalAtlas`：检查次方向光 cascades。
- 看 Inspector 的 `运行时参与状态`，确认实际参与灯光和跳过原因。

验证材质消费：

创建材质并选择 shader：

```text
HoURP/Generated/HoUrpShadowCastReceiverDebug
```

`Debug Mode` 约定：

- `0`：combined attenuation，总 HoCast 受影结果。
- `1`：punctual，只看 Spot/Point。
- `2`：second directional，只看次方向光。
- `3`：热力拆分，R=punctual 遮蔽，G=second directional 遮蔽，B=combined 遮蔽。

颜色约定：

- 偏蓝：接近未遮挡。
- 偏红：遮挡更强。

这个 shader 同时有 `UniversalForward` 和 `ShadowCaster` pass，所以可以验证“材质能消费 HoCast”，也可以作为简单投射体参与 atlas。

---

## 4. 材质系统接入方式

正式材质不应该直接读 atlas 纹理。

材质应包含：

```hlsl
#include "Packages/com.hollow.hourp-extensions/Runtime/Shaders/ShaderLibrary/HoUrpShadowCastSampling.hlsl"
```

推荐入口：

```hlsl
half hoCast = HoUrpSampleShadowCastAttenuation(positionWS, normalWS);
```

需要拆分调试或高级材质控制时，可以分别调用：

```hlsl
float punctual = HoUrpSampleShadowCastPunctual(positionWS);
float secondDirectional = HoUrpSampleShadowCastSecondDirectional(positionWS);
```

材质系统后续应把 HoCast 当作独立 receiver term，例如：

```hlsl
float mainLightTerm = EvaluateMainLightOrSky(...);
float hoCastTerm = HoUrpSampleShadowCastAttenuation(positionWS, normalWS);

float3 finalLighting = materialLighting * mainLightTerm;
finalLighting *= lerp(1.0, hoCastTerm, materialHoCastReceiverWeight);
```

不要把 HoCast 写成 URP main light shadow 的替代值。

---

## 5. Inspector/Debug 约定

ShadowCast Inspector 的主界面应保持面向用户：

- 常用配置直接显示。
- 手动灯光数组折叠在高级区。
- 运行时参与状态必须可见。
- 跳过原因必须可见。

运行时参与状态至少回答：

- 当前相机是谁。
- URP 可见灯数量是多少。
- 哪些灯实际参与了 HoCast。
- 每盏灯来自自动收集还是手动补充。
- 使用了哪些 slice。
- 为什么有灯被跳过。

Debug shader 只用于验证，不构成材质 ABI。

---

## 6. AI/开发者维护底线

后续 AI 或开发者改 ShadowCast 时，必须遵守以下边界。

### 必须保持

- HoCast 是独立 ShadowCast 系统，不污染 URP main light shadow。
- 资源名、shader property、shader include 使用 HoURP 命名空间。
- Feature 关闭、无灯、相机类型不匹配时必须 reset globals。
- 自动收集是默认路径，手动灯光列表只是高级补充。
- Inspector 必须能让用户看到实际参与灯光和跳过原因。
- Second directional cascade 必须按光源成组显示，不能回退到不可读的均分假网格。
- Receiver sampling 入口必须稳定保留：
  - `HoUrpSampleShadowCastAttenuation`
  - `HoUrpSampleShadowCastPunctual`
  - `HoUrpSampleShadowCastSecondDirectional`

### 禁止回退

- 不要把三组固定 `4/4/4` 灯光列表重新放回主界面。
- 不要让用户必须拖场景灯才能测试默认流程。
- 不要恢复旧 `_HoShadowCast*` 作为公共 ABI。
- 不要把 Debug shader 的颜色/网格当成材质系统输入。
- 不要让 atlas debug 显示固定假网格。
- 不要让 second directional 某盏光只写入部分 cascade 后仍参与采样。
- 不要把 ShadowCast atlas 接进 Post/ImageChain 生命周期。

### 修改后必须检查

- `git diff --check`
- Shader ABI 文本测试
- ShadowCast runtime tests
- Unity 中至少验证：
  - Atlas debug
  - SecondDirectionalAtlas debug
  - Runtime Participation 列表
  - `HoURP/Generated/HoUrpShadowCastReceiverDebug`

---

## 7. 当前已知限制

- 第一阶段不承诺透明 alpha/dither 精确投影。
- PCSS 参数是可用下限，不是最终质量调参。
- 没有跨帧 atlas cache。
- 没有 CharacterSpecialization 专用投影策略。
- 没有 legacy `_HoShadowCast*` bridge。
- 没有完整材质 Inspector；当前只提供 shader ABI 和 receiver debug shader。

这些限制不是 bug，除非它们破坏了本阶段明确承诺的资源、debug、receiver 或 reset 行为。

