# RP SSS 中间资源与编码审查

## 资源

| Resource | Domain | Format | Scale | Clear |
| --- | --- | --- | --- | --- |
| `Sss.Source` | Shading | `R16G16B16A16_SFloat` | Full | `(0,0,0,0)` |
| `Sss.Diffusion` | Shading | `R16G16B16A16_SFloat` | Full | `(0,0,0,0)` |

## 编码

`Sss.Source`：

```text
RGB = source color
A   = participation weight
```

`Sss.Diffusion`：

```text
RGB = diffused source color
A   = composite weight
```

## 输入关系

`Sss.Source` 从 `Aov.SssSource` 派生，不从 camera color fallback 猜测。

`Sss.Diffusion` 至少读取：

```text
Sss.Source
Aov.NormalDepth
Aov.SurfaceData
```

## Clear 语义

透明黑表示没有 SSS 贡献。天空和未覆盖区域必须保持 `(0,0,0,0)`。
