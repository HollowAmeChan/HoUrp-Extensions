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
        public void NormalDepthResourceDescriptorUsesHighPrecisionFormatAndFarDepthClear()
        {
            ResourceDefinition normalDepth = HoUrpBuiltInContracts
                .CreateMinimalAovRegistry()
                .Resources
                .Get(HoUrpBuiltInNames.Resources.AovNormalDepth);

            TextureDesc desc = HoUrpRenderGraphTextureDescFactory.CreateColorDesc(normalDepth, CreateCameraDescriptor());

            Assert.That(desc.name, Is.EqualTo("Aov.NormalDepth"));
            Assert.That(desc.format, Is.EqualTo(GraphicsFormat.R16G16B16A16_SFloat));
            Assert.That(desc.clearBuffer, Is.True);
            Assert.That(desc.clearColor, Is.EqualTo(new Color(0.0f, 0.0f, 0.0f, 1.0f)));
        }

        [Test]
        public void ObjectCustomResourceDescriptorsUseMaskFormatAndZeroClear()
        {
            var resources = HoUrpBuiltInContracts
                .CreateMinimalAovRegistry()
                .Resources;
            ResourceDefinition objectCustom0 = resources.Get(HoUrpBuiltInNames.Resources.AovObjectCustom0_3);
            ResourceDefinition objectCustom1 = resources.Get(HoUrpBuiltInNames.Resources.AovObjectCustom4_7);

            TextureDesc desc0 = HoUrpRenderGraphTextureDescFactory.CreateColorDesc(objectCustom0, CreateCameraDescriptor());
            TextureDesc desc1 = HoUrpRenderGraphTextureDescFactory.CreateColorDesc(objectCustom1, CreateCameraDescriptor());

            Assert.That(desc0.name, Is.EqualTo("Aov.ObjectCustom0_3"));
            Assert.That(desc0.format, Is.EqualTo(GraphicsFormat.R8G8B8A8_UNorm));
            Assert.That(desc0.clearBuffer, Is.True);
            Assert.That(desc0.clearColor, Is.EqualTo(Color.clear));
            Assert.That(desc1.name, Is.EqualTo("Aov.ObjectCustom4_7"));
            Assert.That(desc1.format, Is.EqualTo(GraphicsFormat.R8G8B8A8_UNorm));
            Assert.That(desc1.clearBuffer, Is.True);
            Assert.That(desc1.clearColor, Is.EqualTo(Color.clear));
        }

        [Test]
        public void MaterialSemanticResourceDescriptorsUseHighPrecisionFormatAndZeroClear()
        {
            var resources = HoUrpBuiltInContracts
                .CreateMinimalAovRegistry()
                .Resources;
            ResourceDefinition surfaceData = resources.Get(HoUrpBuiltInNames.Resources.AovSurfaceData);
            ResourceDefinition materialCustom = resources.Get(HoUrpBuiltInNames.Resources.AovMaterialCustom0_3);

            TextureDesc surfaceDesc = HoUrpRenderGraphTextureDescFactory.CreateColorDesc(surfaceData, CreateCameraDescriptor());
            TextureDesc customDesc = HoUrpRenderGraphTextureDescFactory.CreateColorDesc(materialCustom, CreateCameraDescriptor());

            Assert.That(surfaceDesc.name, Is.EqualTo("Aov.SurfaceData"));
            Assert.That(surfaceDesc.format, Is.EqualTo(GraphicsFormat.R16G16B16A16_SFloat));
            Assert.That(surfaceDesc.clearBuffer, Is.True);
            Assert.That(surfaceDesc.clearColor, Is.EqualTo(Color.clear));
            Assert.That(customDesc.name, Is.EqualTo("Aov.MaterialCustom0_3"));
            Assert.That(customDesc.format, Is.EqualTo(GraphicsFormat.R16G16B16A16_SFloat));
            Assert.That(customDesc.clearBuffer, Is.True);
            Assert.That(customDesc.clearColor, Is.EqualTo(Color.clear));
        }

        [Test]
        public void SssSourceResourceDescriptorUsesHighPrecisionFormatAndZeroClear()
        {
            ResourceDefinition sssSource = HoUrpBuiltInContracts
                .CreateMinimalAovRegistry()
                .Resources
                .Get(HoUrpBuiltInNames.Resources.AovSssSource);

            TextureDesc desc = HoUrpRenderGraphTextureDescFactory.CreateColorDesc(sssSource, CreateCameraDescriptor());

            Assert.That(desc.name, Is.EqualTo("Aov.SssSource"));
            Assert.That(desc.format, Is.EqualTo(GraphicsFormat.R16G16B16A16_SFloat));
            Assert.That(desc.clearBuffer, Is.True);
            Assert.That(desc.clearColor, Is.EqualTo(Color.clear));
        }

        [Test]
        public void SssIntermediateResourceDescriptorsUseHighPrecisionFormatAndZeroClear()
        {
            var resources = HoUrpBuiltInContracts
                .CreateMinimalAovRegistry()
                .Resources;
            ResourceDefinition sssSource = resources.Get(HoUrpBuiltInNames.Resources.SssSource);
            ResourceDefinition sssDiffusion = resources.Get(HoUrpBuiltInNames.Resources.SssDiffusion);

            TextureDesc sourceDesc = HoUrpRenderGraphTextureDescFactory.CreateColorDesc(sssSource, CreateCameraDescriptor());
            TextureDesc diffusionDesc = HoUrpRenderGraphTextureDescFactory.CreateColorDesc(sssDiffusion, CreateCameraDescriptor());

            Assert.That(sourceDesc.name, Is.EqualTo("Sss.Source"));
            Assert.That(sourceDesc.format, Is.EqualTo(GraphicsFormat.R16G16B16A16_SFloat));
            Assert.That(sourceDesc.clearBuffer, Is.True);
            Assert.That(sourceDesc.clearColor, Is.EqualTo(Color.clear));
            Assert.That(diffusionDesc.name, Is.EqualTo("Sss.Diffusion"));
            Assert.That(diffusionDesc.format, Is.EqualTo(GraphicsFormat.R16G16B16A16_SFloat));
            Assert.That(diffusionDesc.clearBuffer, Is.True);
            Assert.That(diffusionDesc.clearColor, Is.EqualTo(Color.clear));
        }

        [Test]
        public void OitResourceDescriptorsUseExplicitFormatsAndClearValues()
        {
            var resources = HoUrpBuiltInContracts
                .CreateMinimalAovRegistry()
                .Resources;
            ResourceDefinition opaque = resources.Get(HoUrpBuiltInNames.Resources.OitOpaqueColor);
            ResourceDefinition accumulation = resources.Get(HoUrpBuiltInNames.Resources.OitAccumulation);
            ResourceDefinition revealage = resources.Get(HoUrpBuiltInNames.Resources.OitRevealage);
            ResourceDefinition compositeSource = resources.Get(HoUrpBuiltInNames.Resources.OitCompositeSource);

            TextureDesc opaqueDesc = HoUrpRenderGraphTextureDescFactory.CreateColorDesc(opaque, CreateCameraDescriptor());
            TextureDesc accumulationDesc = HoUrpRenderGraphTextureDescFactory.CreateColorDesc(accumulation, CreateCameraDescriptor());
            TextureDesc revealageDesc = HoUrpRenderGraphTextureDescFactory.CreateColorDesc(revealage, CreateCameraDescriptor());
            TextureDesc compositeSourceDesc = HoUrpRenderGraphTextureDescFactory.CreateColorDesc(compositeSource, CreateCameraDescriptor());

            Assert.That(opaqueDesc.name, Is.EqualTo("Oit.OpaqueColor"));
            Assert.That(opaqueDesc.format, Is.EqualTo(GraphicsFormat.R16G16B16A16_SFloat));
            Assert.That(opaqueDesc.clearBuffer, Is.False);
            Assert.That(accumulationDesc.name, Is.EqualTo("Oit.Accumulation"));
            Assert.That(accumulationDesc.format, Is.EqualTo(GraphicsFormat.R16G16B16A16_SFloat));
            Assert.That(accumulationDesc.clearBuffer, Is.True);
            Assert.That(accumulationDesc.clearColor, Is.EqualTo(Color.clear));
            Assert.That(revealageDesc.name, Is.EqualTo("Oit.Revealage"));
            Assert.That(revealageDesc.format, Is.EqualTo(GraphicsFormat.R8_UNorm));
            Assert.That(revealageDesc.clearBuffer, Is.True);
            Assert.That(revealageDesc.clearColor, Is.EqualTo(Color.white));
            Assert.That(compositeSourceDesc.name, Is.EqualTo("Oit.CompositeSource"));
            Assert.That(compositeSourceDesc.format, Is.EqualTo(GraphicsFormat.R16G16B16A16_SFloat));
            Assert.That(compositeSourceDesc.clearBuffer, Is.False);
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
