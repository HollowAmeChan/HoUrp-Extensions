using HoUrp.Extensions.Resources;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace HoUrp.Extensions.RenderGraph
{
    public static class HoUrpRenderGraphTextureDescFactory
    {
        public static TextureDesc CreateColorDesc(
            ResourceDefinition definition,
            RenderTextureDescriptor cameraTextureDescriptor)
        {
            int divisor = GetScaleDivisor(definition.Scale);
            TextureDesc descriptor = new TextureDesc(
                Mathf.Max(1, cameraTextureDescriptor.width / divisor),
                Mathf.Max(1, cameraTextureDescriptor.height / divisor));

            descriptor.name = definition.Id.Value;
            descriptor.format = ResolveColorFormat(definition.Format, cameraTextureDescriptor);
            descriptor.dimension = cameraTextureDescriptor.dimension;
            descriptor.slices = cameraTextureDescriptor.volumeDepth;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = divisor == 1
                ? (MSAASamples)Mathf.Max(1, cameraTextureDescriptor.msaaSamples)
                : MSAASamples.None;
            descriptor.clearBuffer = ShouldClear(definition.ClearPolicy);
            descriptor.clearColor = ResolveClearColor(definition.ClearPolicy);
            descriptor.filterMode = FilterMode.Point;
            descriptor.wrapMode = TextureWrapMode.Clamp;
            descriptor.bindTextureMS = cameraTextureDescriptor.bindMS && divisor == 1;
            descriptor.useDynamicScale = cameraTextureDescriptor.useDynamicScale;
            descriptor.useDynamicScaleExplicit = cameraTextureDescriptor.useDynamicScaleExplicit;
            descriptor.vrUsage = cameraTextureDescriptor.vrUsage;
            return descriptor;
        }

        public static RenderTextureDescriptor CreateDepthDesc(
            ResourceDefinition definition,
            RenderTextureDescriptor cameraTextureDescriptor)
        {
            int divisor = GetScaleDivisor(definition.Scale);
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(
                Mathf.Max(1, cameraTextureDescriptor.width / divisor),
                Mathf.Max(1, cameraTextureDescriptor.height / divisor),
                GraphicsFormat.None,
                ResolveDepthStencilFormat(cameraTextureDescriptor));

            descriptor.dimension = cameraTextureDescriptor.dimension;
            descriptor.volumeDepth = cameraTextureDescriptor.volumeDepth;
            descriptor.msaaSamples = divisor == 1 ? Mathf.Max(1, cameraTextureDescriptor.msaaSamples) : 1;
            descriptor.bindMS = cameraTextureDescriptor.bindMS && divisor == 1;
            descriptor.useDynamicScale = cameraTextureDescriptor.useDynamicScale;
            descriptor.vrUsage = cameraTextureDescriptor.vrUsage;
            return descriptor;
        }

        public static GraphicsFormat ResolveColorFormat(
            ResourceFormatHint format,
            RenderTextureDescriptor cameraTextureDescriptor)
        {
            switch (format)
            {
                case ResourceFormatHint.MaskRgba8:
                    return GetSupportedColorFormat(GraphicsFormat.R8G8B8A8_UNorm, DefaultFormat.LDR);
                case ResourceFormatHint.HighPrecisionRgba16Float:
                case ResourceFormatHint.HdrColor:
                    return GetSupportedColorFormat(GraphicsFormat.R16G16B16A16_SFloat, DefaultFormat.HDR);
                case ResourceFormatHint.CameraColor:
                    return cameraTextureDescriptor.graphicsFormat != GraphicsFormat.None
                        ? cameraTextureDescriptor.graphicsFormat
                        : SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);
                case ResourceFormatHint.R8Unorm:
                    return GetSupportedColorFormat(GraphicsFormat.R8_UNorm, DefaultFormat.LDR);
                default:
                    return cameraTextureDescriptor.graphicsFormat != GraphicsFormat.None
                        ? cameraTextureDescriptor.graphicsFormat
                        : SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);
            }
        }

        public static GraphicsFormat ResolveDepthStencilFormat(RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (IsDepthStencilFormatUsable(cameraTextureDescriptor.depthStencilFormat))
            {
                return cameraTextureDescriptor.depthStencilFormat;
            }

            GraphicsFormat format = CoreUtils.GetDefaultDepthStencilFormat();
            if (IsDepthStencilFormatUsable(format))
            {
                return format;
            }

            format = GraphicsFormatUtility.GetDepthStencilFormat(24);
            if (IsDepthStencilFormatUsable(format))
            {
                return format;
            }

            format = GraphicsFormatUtility.GetDepthStencilFormat(32);
            if (IsDepthStencilFormatUsable(format))
            {
                return format;
            }

            return GraphicsFormat.D32_SFloat;
        }

        private static int GetScaleDivisor(ResourceScale scale)
        {
            switch (scale)
            {
                case ResourceScale.Half:
                    return 2;
                case ResourceScale.Quarter:
                    return 4;
                default:
                    return 1;
            }
        }

        private static bool ShouldClear(ResourceClearPolicy clearPolicy)
        {
            return clearPolicy != ResourceClearPolicy.Undefined
                && clearPolicy != ResourceClearPolicy.CopySource
                && clearPolicy != ResourceClearPolicy.External;
        }

        private static Color ResolveClearColor(ResourceClearPolicy clearPolicy)
        {
            switch (clearPolicy)
            {
                case ResourceClearPolicy.ClearNeutralNormal:
                    return new Color(0.5f, 0.5f, 1.0f, 1.0f);
                case ResourceClearPolicy.ClearBlack:
                    return Color.black;
                case ResourceClearPolicy.ClearTransparentBlack:
                case ResourceClearPolicy.ClearZero:
                    return Color.clear;
                default:
                    return Color.clear;
            }
        }

        private static GraphicsFormat GetSupportedColorFormat(GraphicsFormat preferredFormat, DefaultFormat fallbackFormat)
        {
            if (IsColorFormatUsable(preferredFormat))
            {
                return preferredFormat;
            }

            GraphicsFormat fallback = SystemInfo.GetGraphicsFormat(fallbackFormat);
            if (IsColorFormatUsable(fallback))
            {
                return fallback;
            }

            return fallbackFormat == DefaultFormat.LDR
                ? GraphicsFormat.B8G8R8A8_UNorm
                : GraphicsFormat.R16G16B16A16_SFloat;
        }

        private static bool IsColorFormatUsable(GraphicsFormat format)
        {
            return format != GraphicsFormat.None && SystemInfo.IsFormatSupported(format, FormatUsage.Render);
        }

        private static bool IsDepthStencilFormatUsable(GraphicsFormat format)
        {
            return format != GraphicsFormat.None && SystemInfo.IsFormatSupported(format, FormatUsage.Render);
        }
    }
}
