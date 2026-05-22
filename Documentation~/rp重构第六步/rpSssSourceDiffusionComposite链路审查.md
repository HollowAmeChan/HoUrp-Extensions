# RP SSS Source / Diffusion / Composite 链路审查

## Pass

```text
SSS Source
SSS Diffusion
SSS Composite
```

## Source

Reads:

```text
Aov.MaskId
Aov.NormalDepth
Aov.SurfaceData
Aov.SssSource
```

Writes:

```text
Sss.Source
```

## Diffusion

Reads:

```text
Sss.Source
Aov.NormalDepth
Aov.SurfaceData
```

Writes:

```text
Sss.Diffusion
```

## Composite

Reads:

```text
Camera.Color
Sss.Diffusion
Aov.NormalDepth
Aov.SurfaceData
```

Writes:

```text
Camera.Color
```

## Timing

默认在 `BeforeRenderingTransparents` 完成 composite。`AovOutput` 必须已经完成。

## 本阶段限制

- 不做 transmission。
- 不做 OIT 排序调整。
- 不做 camera color fallback source。
- 不做 compute shader。
