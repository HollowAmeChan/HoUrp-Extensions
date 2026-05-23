# rpShadowCast与OitSssPost顺序回归审查

## 目标

第十三步新增 ShadowCast 后，不能破坏第十一、十二步已经建立的 ScreenPost、ImagePost、AOV、SSS、OIT 顺序和资源生命周期。新增的主 atlas 与 second directional atlas 都必须保持在 ShadowCast 子系统边界内。

## 基本顺序

ShadowCast 推荐顺序：

```text
ShadowCast main atlas/depth
ShadowCast second directional atlas/depth
ShadowCast receiver globals publish
Opaque/Forward receivers
Transparent/OIT receivers
SSS/AOV consumers
ScreenPost/ImagePost
```

实际 URP 注入点可能不同，但必须满足：

- Receiver 绘制前主 atlas 与 second directional data 已发布。
- ShadowCast 不读写 camera color。
- ShadowCast 不占用 post chain 的 ping-pong 资源。
- OIT 使用 ShadowCast receiver sampling 时，主 atlas 与 second directional atlas 仍有效。

## 与 OIT 的关系

OIT pass 只采样 receiver attenuation，不生成 ShadowCast atlas。

回归风险：

- OIT pass 只输出 albedo，导致开启 OIT 后光照/阴影丢失。
- OIT pass 使用不同 shadow strength，导致 Forward/OIT 视觉不一致。
- OIT composite 读写 source texture 与 ShadowCast atlas 混淆。

验收：

- Forward 和 OIT 使用同一 receiver sampling include。
- OIT composite 只处理 OIT accumulation/revealage 与 camera source。
- 主 atlas 不出现在 OIT resource id 中。
- Second directional atlas 不出现在 OIT resource id 中。

## 与 SSS 的关系

SSS 不应拥有 ShadowCast atlas。

允许：

- SSS 材质最终颜色已经包含 ShadowCast attenuation。
- SSS debug/AOV 观察最终颜色。

禁止：

- SSS pass 隐式读取 ShadowCast atlas。
- ShadowCast pass 写入 SSS profile/intermediate。

## 与 AOV 的关系

AOV 可以增加 ShadowCast debug channel，但必须声明为 debug/inspection：

```text
AOV.ShadowCast.Attenuation
AOV.ShadowCast.AtlasPreview
AOV.ShadowCast.SecondDirectionalAtlasPreview
```

不建议第十三步马上加入 AOV channel，除非 debug 需求强。第一版可只保留 Debug shader。

## 与 ScreenPost/ImagePost 的关系

Post chain 不应知道 ShadowCast 资源。

验收：

- ScreenPost/ImagePost feature 全关或全开，都不影响 ShadowCast atlas 生成。
- ShadowCast feature 关闭后，post 输出不应因为旧 globals 发生变暗。

## RenderDoc/Frame Debugger 检查

建议检查点：

- ShadowCast pass 目标是 depth atlas，不是 camera color。
- Second directional pass 目标是 second directional depth atlas。
- ShadowCast publish 在 receiver draw 前。
- OIT accumulation shader 绑定 ShadowCast receiver 数据。
- Post pass 不绑定 ShadowCast atlas，除非 debug mode 显式开启。

## 回归测试建议

组合矩阵：

| ShadowCast | OIT | SSS | Post | 预期 |
| --- | --- | --- | --- | --- |
| off | off | off | off | 基线无自定义 shadow |
| on | off | off | off | Forward receiver 受影 |
| on | on | off | off | OIT receiver 受影且仍有光照 |
| on | on | on | off | SSS 不破坏 ShadowCast |
| on | on | on | on | Post 只处理最终颜色 |
| off | on | on | on | 无 ShadowCast 残留 |

额外组合：

| Spot | Point | Second Directional | 预期 |
| --- | --- | --- | --- |
| 1 | 0 | 0 | 主 atlas 1 slice |
| 0 | 1 | 0 | 主 atlas 6 face slices |
| 2 | 2 | 0 | 主 atlas packing 无重叠 |
| 0 | 0 | 1 | second directional atlas 有 cascade tiles |
| 2 | 2 | 1 | receiver attenuation 同时包含 punctual 与 second directional |

