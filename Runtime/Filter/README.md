# HoURP FilterKit

FilterKit is not a scheduler. It is a small source library made of shader includes, hidden shaders, and C# constants/helpers for reusable filtering code.

Callers own:

- RenderGraph pass creation and ordering.
- Frame-transient resources such as Image.WorkA, Image.WorkB, Sss.Source, and Sss.Diffusion.
- Explicit texture binding for every guide input.

FilterKit does not own:

- `FilterRequest`, a scheduler, or a second RenderGraph wrapper.
- Long-lived RTHandle or TextureHandle fields.
- AOV, SSS, shadow, or image-chain resource lifetimes.

## Shader Passes

- `Hidden/HoURP/Filter/Blur`
  - Pass 0: `HoURP Filter Copy`
  - Pass 1: `HoURP Filter Separable Blur`
  - Pass 2: `HoURP Filter Depth Normal Aware Blur`
- `Hidden/HoURP/SSS/SubsurfaceScattering`
  - Pass 0: `HoURP SSS Source Prepare`
  - Pass 1: `HoURP SSS Profile Diffusion`
  - Pass 2: `HoURP SSS Composite`
  - Pass 3: `HoURP SSS Debug` reserved

## Include Boundaries

- `HoUrpFilterCommon.hlsl` contains math helpers only and declares no textures.
- `HoUrpFilterSampling.hlsl` contains sample pattern helpers only and declares no textures.
- `HoUrpFilterDepthNormalGate.hlsl` contains gate functions only; callers choose which textures and channels feed it.
- `HoUrpFilterBurleyDiffusion.hlsl` contains Burley-like profile weight helpers only and does not sample AOV or SSS textures.

Generic blur does not read AOV. Edge-aware effects such as SSS, AO, and shadow filtering must bind guide textures explicitly in their own caller pass.

External algorithm references or migrated code must be listed in `Runtime/Filter/ThirdParty/EXTERNAL_RENDERING_REFERENCES.md` before they become runtime code.
