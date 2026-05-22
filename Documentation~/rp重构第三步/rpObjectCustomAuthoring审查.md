# RP ObjectCustom Authoring 审查

## 目标

提供最小对象 authoring component，让场景对象能显式声明 `Object.Custom0..7`。

## 新组件

建议新增：

```text
Runtime/Semantic/ObjectSemanticAuthoring.cs
```

第一版职责：

- 在自身或子级 Renderer 上写 MaterialPropertyBlock。
- 写入 `_HoUrpObjectCustomMask`。
- 可选写入 `_HoUrpAovMaskWeight`，用于保持和第二步 mask 输出一致。

## 第一版字段

```text
includeChildren: bool
maskWeight: float 0..1
objectCustomMask: byte
```

Inspector 可后续优化为 8 个 bool。第三步先保持数据结构简单。

## 不做项

- 不做全局 active group list。
- 不做跨对象优先级仲裁。
- 不做 `SetShaderUserValue` 路径。
- 不写旧 `_HoAov*` 属性。
- 不接角色 preset UI。

## 与旧 HoAovGroup 的关系

旧 `HoAovGroup` 的价值是证明“角色/区域位”有用，但它还包含旧项目的优先级、角色 ID、部件 ID 和 renderer user value 打包。第三步只迁移最小 ObjectCustom authoring 概念，不迁移旧结构。

