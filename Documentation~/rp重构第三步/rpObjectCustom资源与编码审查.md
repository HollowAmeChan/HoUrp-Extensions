# RP ObjectCustom 资源与编码审查

## 资源

| Resource | Domain | Format | Scale | Clear |
| --- | --- | --- | --- | --- |
| `Aov.ObjectCustom0_3` | Object | RGBA8 UNorm | Full | zero |
| `Aov.ObjectCustom4_7` | Object | RGBA8 UNorm | Full | zero |

## AOV 空值与清理契约

第三阶段明确把“没有几何覆盖”编码为空值，而不是为天空或未绘制区域写伪造 fallback 语义：

```text
Aov.MaskId           clear = (0, 0, 0, 0)
Aov.NormalDepth      clear = (0, 0, 0, 0)
Aov.ObjectCustom0_3  clear = (0, 0, 0, 0)
Aov.ObjectCustom4_7  clear = (0, 0, 0, 0)
```

`Aov.NormalDepth` 的 `(0, 0, 0, 0)` 表示 no geometry / sky / undefined，不表示合法世界法线。Debug 和 consumer 不应该把天空解释成默认 normal，例如 `(0.5, 0.5, 1, 1)`。

清理入口放在资源声明层：`HoUrpRenderGraphTextureDescFactory` 根据 `ResourceClearPolicy` 设置 `TextureDesc.clearBuffer` / `TextureDesc.clearColor`。AOV Output pass 只负责绘制几何，并用 `ReadWrite` attachment 保留资源初始 clear 值作为天空/未绘制区域。

不要再额外插入每资源 clear pass，也不要用一个手写 MRT clear pass 做清理。清理是资源生命周期属性，不是 AOV Output 的绘制职责；手写 MRT `ClearRenderTarget` 在多 attachment 上也容易出现只清部分目标或 Frame Debugger 噪音。

## 语义映射

| Semantic | Resource | Channel |
| --- | --- | --- |
| `Object.Custom0` | `Aov.ObjectCustom0_3` | R |
| `Object.Custom1` | `Aov.ObjectCustom0_3` | G |
| `Object.Custom2` | `Aov.ObjectCustom0_3` | B |
| `Object.Custom3` | `Aov.ObjectCustom0_3` | A |
| `Object.Custom4` | `Aov.ObjectCustom4_7` | R |
| `Object.Custom5` | `Aov.ObjectCustom4_7` | G |
| `Object.Custom6` | `Aov.ObjectCustom4_7` | B |
| `Object.Custom7` | `Aov.ObjectCustom4_7` | A |

## 第一版编码

ObjectCustom channel 第一版写 0 或 1：

```text
0 = 不属于该对象语义区域
1 = 属于该对象语义区域
```

后续如需要软 mask 或权重，可以扩展为 normalized float，但第三步只承诺 binary participation。

## Shader binding

逻辑资源名：

```text
Aov.ObjectCustom0_3
Aov.ObjectCustom4_7
```

shader binding 名：

```text
_HoUrpAovObjectCustom0_3Texture
_HoUrpAovObjectCustom4_7Texture
_HoUrpObjectCustomMask
```

shader binding 是 backend detail，不是 Resource Registry 主键。

## 旧实现对照

旧全局名：

```text
_lilHoAovObjectCustom0_3Texture
_lilHoAovObjectCustom4_7Texture
_HoAovObjectCustomMask
```

旧角色区域含义只作为迁移参照，不作为新长期 ABI。
