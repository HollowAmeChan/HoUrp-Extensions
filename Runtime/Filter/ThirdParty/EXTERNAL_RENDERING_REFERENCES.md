# External Rendering References

| Source | Source file path | Usage | HoURP landing point | License status |
| --- | --- | --- | --- | --- |
| HoSSS legacy implementation | `lilToon-URP-Extensions/Runtime/SubsurfaceScattering/HoSubsurfaceScattering.shader` | Migrated gate shape, sampling pattern, and Burley-like diffusion approximation. Names and ABI were not carried over. | `Runtime/Filter/Shaders/HoUrpFilterDepthNormalGate.hlsl`, `Runtime/Filter/Shaders/HoUrpFilterSampling.hlsl`, `Runtime/Filter/Shaders/HoUrpFilterBurleyDiffusion.hlsl`, `Runtime/Filter/SSS/HoUrpSssFilter.hlsl` | Local legacy reference |
| Falcor Gaussian blur | `Source/RenderPasses/Utils/GaussianBlur/GaussianBlur.ps.slang` | Reference only for separable blur structure and caller-controlled sample count. | `Runtime/Filter/Shaders/HoUrpFilterBlur.shader` | reference only |
| Filament separable/bilateral blur | `filament/src/materials/separableGaussianBlur.fs`, `filament/src/materials/ssao/bilateralBlur.mat`, `filament/src/materials/ssao/ssaoUtils.fs` | Reference only for separable image blur and depth/normal gate boundaries. | `Runtime/Filter/Shaders/HoUrpFilterBlur.shader`, `Runtime/Filter/Shaders/HoUrpFilterDepthNormalGate.hlsl` | reference only |
| FidelityFX SPD | `Kits/FidelityFX/upscalers/fsr3/include/gpu/spd/ffx_spd.h` | Reference only. No SPD code is imported in this step. | Future pyramid shader, not implemented in this pass. | reference only |
| NVIDIA NRD | NRD denoiser documentation/source | Reference only. Temporal denoise is explicitly out of scope for this step. | None | reference only |
| pbrt-v4 subsurface scattering | `src/pbrt/bssrdf.*` and related documentation | Reference only for offline BSSRDF concepts. No runtime equivalence is claimed. | None | reference only |
