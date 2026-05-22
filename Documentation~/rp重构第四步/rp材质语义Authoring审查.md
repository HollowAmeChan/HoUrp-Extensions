# RP 材质语义 Authoring 审查

## 目标

提供最小材质语义 producer，让场景对象能显式声明第四阶段需要的 `MaterialDomain` 输入。

这个 producer 是迁移期 authoring / test producer，不是最终材质系统。

## 新组件

建议新增：

```text
Runtime/Semantic/MaterialSemanticAuthoring.cs
```

第一版职责：

- 在自身或子级 Renderer 上写 `MaterialPropertyBlock`。
- 写入 `_HoUrpMaterialClass`。
- 写入 `_HoUrpMaterialSssProfile`。
- 写入 `_HoUrpMaterialThickness`。
- 写入 `_HoUrpMaterialCurvature`。
- 写入 `_HoUrpMaterialCustom0_3`。
- 可预留 `_HoUrpMaterialUtility`，但只有本阶段实际生产时才启用。

## 第一版字段

```text
includeChildren: bool
materialClass: int or float
sssProfile: int or float
thickness: float
curvature: float
materialCustom0_3: Vector4
```

数值范围在 Inspector 层可先保持简单；编码解释以资源与编码审查文档为准。

## 不做项

- 不做材质 inspector UI。
- 不修改 material asset。
- 不接旧 `lilToon` / `lilPBR` property。
- 不读取旧 shader keywords。
- 不做 texture-driven custom。
- 不做 profile registry UI。
- 不做 SSS 视觉参数 authoring。

## 与最终材质系统的关系

`MaterialSemanticAuthoring` 只证明 `MaterialDomain` 语义能以显式 producer 进入 RP。最终 HoPbr / HoNpr / HoToon 应直接实现 material producer contract，而不是依赖这个组件。

后续如果生成式材质系统接入，它应生产同名语义和资源写入，不应反过来改第四阶段定义的 Resource / Semantic 主键。

