# RP 第七阶段实现边界审查

第七阶段只把 `SemanticPostProcess` 从 AOV read probe 推进到最小语义后处理框架。

## 做

- 建立 `SemanticPostLayer` / rule / effect 的最小数据模型。
- 支持固定上限 layer 和固定上限 rule。
- 读取已注册 AOV / SSS resources。
- 实现 `SemanticTint` 最小效果。
- 视实现成本决定是否加入 `EdgeLight` 或 `OutlineLite`。
- 输出或可 debug `SemanticPost.Mask`。
- 保持 RenderGraph 显式资源声明。

## 不做

- 不复制旧 `HoPostProcessRendererFeature.cs`。
- 不迁移旧 compatibility path。
- 不迁移旧 Volume stack。
- 不迁移 Shoost。
- 不做 final image style stack。
- 不做 DropShadow / DoF / PostLighting / CustomMaterial。
- 不做完整 filter backend。
- 不接旧材质包。
- 不引入旧 `_lilHoPost*` / `_HoPost*` 长期 ABI。

## 成功标准

- `SemanticPostProcess` 不再只是 read probe，而是 rule-driven layer composite。
- layer mask 能由 AOV / SSS 语义生成。
- camera color 读写遵守先 copy 再读的规则。
- debug 能显示 semantic post mask。
- 没有新增 RenderGraph resource conflict。
