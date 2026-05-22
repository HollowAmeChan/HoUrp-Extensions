# RP 第三阶段未决项登记

| Item | 本阶段处理方式 | 后续阶段 |
| --- | --- | --- |
| 角色区域中文 preset | 只在文档保留迁移参照 | Capability UI / Object Semantic UI |
| ObjectCustom soft mask | 第一版 binary 0/1 | 区域权重或手绘 mask 阶段 |
| 角色 ID / Part ID / Flags authoring | 不扩展 | Object semantic authoring 第二轮 |
| `SetShaderUserValue` 路径 | 不接 | 需要性能或兼容时复核 |
| HoCharacterSpecialization 消费 | 不做 | CharacterComposite 阶段 |
| HoPost rule language 消费 | 不做 | SemanticPost/HoPost rule 阶段 |
| SurfaceData / SSS | 不做 | 材质派生语义阶段 |
| AOV clear 机制 | 第三阶段收敛到资源声明层 `TextureDesc.clearBuffer/clearColor`，统一空值为 zero；不保留额外 clear draw/pass | 后续资源增多时复核 RenderGraph clear-on-first-use 与原生 render pass load-action 行为 |
| 天空/未覆盖区域 AOV 语义 | 明确为 no geometry / zero，不给 NormalDepth 写默认法线 fallback | 后续 Debug UI 可增加 checker/overlay 显示空值区域 |
| 最小测试场景资产 | 不创建 | package samples 或 integration project |
| Unity 自动化验收 | 本阶段未运行，包目录没有独立 Unity project / `.csproj` | 接入 integration project 后跑 batchmode/EditMode/Frame Debugger 验收 |
