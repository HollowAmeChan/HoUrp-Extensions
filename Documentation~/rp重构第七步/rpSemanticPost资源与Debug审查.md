# RP SemanticPost 资源与 Debug 审查

## 目标

让语义后处理不只“产生画面变化”，还要能被 debug 系统观察。

## 候选资源

```text
SemanticPost.Mask
SemanticPost.LayerResult
```

## 资源登记策略

`SemanticPost.Mask` 建议登记为正式资源，原因：

- 它是 rule evaluation 的直接结果。
- 它能回答某个像素为什么被 SemanticPost 影响。
- 它适合进入 AllRegistered debug。

`SemanticPost.LayerResult` 可延后，除非第一版需要在 debug 中观察 layer composite 前后的颜色差异。

## DebugView

```text
SemanticPost.Mask
SemanticPost.LayerResult
```

第一版优先完成：

```text
SemanticPost.Mask
```

## RenderGraph 依赖

Mask pass：

| Resource | Access |
| --- | --- |
| `Aov.MaskId` | read |
| `Aov.NormalDepth` | read |
| `Aov.ObjectCustom0_3` | read |
| `Aov.ObjectCustom4_7` | read |
| `Aov.SurfaceData` | read |
| `Aov.MaterialCustom0_3` | read |
| `Aov.SssSource` | read |
| `Sss.Source` | read if valid |
| `Sss.Diffusion` | read if valid |
| `SemanticPost.Mask` | write |

Composite pass：

| Resource | Access |
| --- | --- |
| camera color copy | read |
| `SemanticPost.Mask` | read |
| active camera color | write |

## Camera Color 规则

不能直接读取 live `activeColorTexture` 后再写回同一 handle。必须：

```text
activeColorTexture -> explicit copy -> composite source
```
