# RP SSS 输入 Authoring 审查

## 目标

提供最小 SSS input producer，让场景对象能显式声明 SSS source color 与 SSS weight。

这个 producer 是迁移期 authoring / test producer，不是最终材质系统。

## 推荐方案

扩展：

```text
Runtime/Semantic/MaterialSemanticAuthoring.cs
```

新增字段：

```text
sssSourceColor: Color
sssWeight: float 0..1
```

写入 shader binding：

```text
_HoUrpSssSourceColor
_HoUrpSssWeight
```

## 不做项

- 不做材质 inspector UI。
- 不修改 material asset。
- 不接旧 `lilToon` / `lilPBR` property。
- 不读取旧 shader keywords。
- 不做 profile kernel 参数。
- 不做 transmission 参数。

## 与最终材质系统的关系

`MaterialSemanticAuthoring` 只证明 SSS 输入语义能以显式 producer 进入 RP。最终 HoPbr / HoNpr / HoToon 应直接实现 material producer contract，而不是依赖这个组件。

