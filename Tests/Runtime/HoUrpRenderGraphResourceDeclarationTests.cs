using HoUrp.Extensions.Core;
using HoUrp.Extensions.RenderGraph;
using HoUrp.Extensions.Resources;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace HoUrp.Extensions.Tests.Runtime
{
    public sealed class HoUrpRenderGraphResourceDeclarationTests
    {
        [Test]
        public void MaskResourceDescriptorUsesRegisteredNameScaleAndFormat()
        {
            ResourceDefinition mask = HoUrpBuiltInContracts
                .CreateMinimalAovRegistry()
                .Resources
                .Get(HoUrpBuiltInNames.Resources.AovMaskId);

            TextureDesc desc = HoUrpRenderGraphTextureDescFactory.CreateColorDesc(mask, CreateCameraDescriptor());

            Assert.That(desc.name, Is.EqualTo("Aov.MaskId"));
            Assert.That(desc.width, Is.EqualTo(1920));
            Assert.That(desc.height, Is.EqualTo(1080));
            Assert.That(desc.format, Is.EqualTo(GraphicsFormat.R8G8B8A8_UNorm));
            Assert.That(desc.clearBuffer, Is.True);
            Assert.That(desc.clearColor, Is.EqualTo(Color.clear));
            Assert.That(desc.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(desc.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
        }

        [Test]
        public void NormalDepthResourceDescriptorUsesHighPrecisionFormatAndNeutralClear()
        {
            ResourceDefinition normalDepth = HoUrpBuiltInContracts
                .CreateMinimalAovRegistry()
                .Resources
                .Get(HoUrpBuiltInNames.Resources.AovNormalDepth);

            TextureDesc desc = HoUrpRenderGraphTextureDescFactory.CreateColorDesc(normalDepth, CreateCameraDescriptor());

            Assert.That(desc.name, Is.EqualTo("Aov.NormalDepth"));
            Assert.That(desc.format, Is.EqualTo(GraphicsFormat.R16G16B16A16_SFloat));
            Assert.That(desc.clearBuffer, Is.True);
            Assert.That(desc.clearColor, Is.EqualTo(new Color(0.5f, 0.5f, 1.0f, 1.0f)));
        }

        [Test]
        public void ScaledDescriptorRoundsDownButNeverBelowOnePixel()
        {
            ResourceDefinition scaled = new ResourceDefinition(
                "Test.Tiny",
                ResourceKind.Texture2D,
                HoUrpBuiltInNames.Semantics.ObjectMaskWeight,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<HoUrpIdentifier>(),
                ResourceFormatHint.MaskRgba8,
                ResourceScale.Quarter,
                HoUrpLifetime.PerCamera,
                ResourceClearPolicy.ClearZero,
                HoUrpBuiltInNames.DebugViews.AovMask);

            TextureDesc desc = HoUrpRenderGraphTextureDescFactory.CreateColorDesc(
                scaled,
                new RenderTextureDescriptor(2, 3, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.None));

            Assert.That(desc.width, Is.EqualTo(1));
            Assert.That(desc.height, Is.EqualTo(1));
            Assert.That(desc.msaaSamples, Is.EqualTo(MSAASamples.None));
        }

        private static RenderTextureDescriptor CreateCameraDescriptor()
        {
            RenderTextureDescriptor descriptor = new RenderTextureDescriptor(
                1920,
                1080,
                GraphicsFormat.R16G16B16A16_SFloat,
                GraphicsFormat.D32_SFloat);

            descriptor.msaaSamples = 4;
            descriptor.volumeDepth = 1;
            descriptor.useDynamicScale = true;
            return descriptor;
        }
    }
}
