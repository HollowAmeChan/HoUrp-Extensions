# RP 第八阶段实现边界审查

第八阶段只建立 Capability UI / Authoring 的最小闭环。

## 做

- 整理 `ObjectSemanticAuthoring` 的 Inspector。
- 整理 `MaterialSemanticAuthoring` 的 Inspector。
- 增加对象 preset 和材质 preset。
- 明确 Capability 与 Policy 的字段边界。
- 让 object custom / material custom 的第一版含义可被 UI 选择。
- 保持现有 `MaterialPropertyBlock` 写入链路。
- 补 authoring / preset / registry 测试。

## 不做

- 不迁移旧 `HoAovGroup` 完整优先级系统。
- 不实现全局对象分组 resolver。
- 不把 renderer user value 作为唯一语义来源。
- 不做新材质系统。
- 不做 Light Capability。
- 不做 Feature 调度器。
- 不做 Debug Framework。
- 不新增 SemanticPost effect。
- 不迁移 CharacterSpecialization。
- 不接旧 lilToon / lilPBR inspector。

## 成功标准

- 对象能通过 UI 明确声明 `WritesAov` / `ReceivesSemanticPost`。
- 对象 preset 能驱动 AOV object custom debug 和 SemanticPost mask。
- 材质 preset 能驱动 material debug / SSS debug。
- 低层语义写入仍由已有 authoring component 完成。
- 没有新增资源链路或 RenderGraph 隐式依赖。

## 边界原则

Capability 表示“允许参与什么”；Policy 表示“如何参与”。Inspector 不能把二者混成一个隐式开关。
