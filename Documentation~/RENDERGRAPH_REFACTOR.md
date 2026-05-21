# RenderGraph Refactor Notes

HoUrp Extensions 的迁移目标是把旧 `lilToon-URP-Extensions` 中的渲染扩展整理成
RenderGraph-first 的实现。

## 初始模块边界

- OIT：迁移 weighted blended OIT 的 accumulation、revealage 和 composite。
- AOV：迁移 HoAOV 的 clear、write、fallback、debug 和采样契约。
- CharacterSpecialization：迁移角色捕获和角色特化后处理。
- HoPostProcessing：迁移 HoPost 图层栈和 mask rule。
- ShoostPostProcessing：迁移 Shoost 风格化后处理。
- PlanarReflection：评估是否继续保持场景组件驱动，或抽出可被 RenderGraph 消费的资源发布层。
- ShadowCast：迁移 HoShadowCast atlas 的创建、更新和 shader 绑定。

## 设计原则

- RendererFeature 只负责接入 URP renderer 和暴露配置。
- RenderGraph pass 明确声明读取和写入的 texture/resource。
- 尽量避免永久全局 RT，必须发布全局纹理时也要把生命周期写清楚。
- 每个模块先保留旧功能契约，再逐步收敛实现。
