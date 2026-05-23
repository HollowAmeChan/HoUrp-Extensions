using HoUrp.Extensions.Core;
using HoUrp.Extensions.RenderGraph;
using HoUrp.Extensions.ShadowCast;
using HoUrp.Extensions.Features;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace HoUrp.Extensions.Tests.Runtime
{
    public sealed class HoUrpShadowCastRuntimeTests
    {
        [Test]
        public void ShadowCastResourcesAndDebugViewsAreRegistered()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            Assert.That(registry.Resources.TryGet(HoUrpBuiltInNames.Resources.ShadowCastAtlas, out _), Is.True);
            Assert.That(registry.Resources.TryGet(HoUrpBuiltInNames.Resources.ShadowCastSecondDirectionalAtlas, out _), Is.True);
            Assert.That(registry.DebugViews.TryGet(HoUrpBuiltInNames.DebugViews.ShadowCastAtlas, out _), Is.True);
            Assert.That(registry.DebugViews.TryGet(HoUrpBuiltInNames.DebugViews.ShadowCastSecondDirectionalAtlas, out _), Is.True);
            Assert.That(registry.Features.TryGet(HoUrpBuiltInNames.Features.ShadowCast, out FeatureDescriptor feature), Is.True);
            Assert.That(feature.Domain, Is.EqualTo(HoUrpDomain.Shadow));
        }

        [Test]
        public void ShadowCastAtlasDescriptorUsesRequestedSquareDepthAtlas()
        {
            RenderTextureDescriptor descriptor = HoUrpShadowCastResourceDeclaration.CreateAtlasDescriptor(2048);

            Assert.That(descriptor.width, Is.EqualTo(2048));
            Assert.That(descriptor.height, Is.EqualTo(2048));
            Assert.That(descriptor.graphicsFormat, Is.EqualTo(GraphicsFormat.None));
            Assert.That(descriptor.depthStencilFormat, Is.EqualTo(GraphicsFormat.D32_SFloat));
            Assert.That(descriptor.msaaSamples, Is.EqualTo(1));
            Assert.That(descriptor.bindMS, Is.False);
        }

        [Test]
        public void AtlasPackerAllocatesRowsAndRejectsOverflow()
        {
            var packer = new HoShadowCastAtlasPacker(8);

            Assert.That(packer.TryAllocate(4, out int x0, out int y0), Is.True);
            Assert.That(new Vector2Int(x0, y0), Is.EqualTo(new Vector2Int(0, 0)));
            Assert.That(packer.TryAllocate(4, out int x1, out int y1), Is.True);
            Assert.That(new Vector2Int(x1, y1), Is.EqualTo(new Vector2Int(4, 0)));
            Assert.That(packer.TryAllocate(4, out int x2, out int y2), Is.True);
            Assert.That(new Vector2Int(x2, y2), Is.EqualTo(new Vector2Int(0, 4)));
            Assert.That(packer.TryAllocate(8, out _, out _), Is.False);
        }

        [Test]
        public void SettingsValidateClampArraysAndNumericRanges()
        {
            var settings = new HoShadowCastSettings
            {
                spotLights = new Light[1],
                pointLights = new Light[1],
                secondDirectionalLights = new Light[1],
                receiverStrength = 2.0f,
                punctualShadowFadeSpeed = -1.0f,
                atlasSize = 1,
                spotResolution = 1,
                pointFaceResolution = 1,
                secondDirectionalAtlasSize = 1,
                secondDirectionalCascadeCount = 99
            };

            settings.Validate();

            Assert.That(settings.spotLights.Length, Is.EqualTo(HoShadowCastShaderConstants.MaxSpotLights));
            Assert.That(settings.pointLights.Length, Is.EqualTo(HoShadowCastShaderConstants.MaxPointLights));
            Assert.That(settings.secondDirectionalLights.Length, Is.EqualTo(HoShadowCastShaderConstants.MaxDirectionalLights));
            Assert.That(settings.receiverStrength, Is.EqualTo(1.0f));
            Assert.That(settings.punctualShadowFadeSpeed, Is.EqualTo(0.1f));
            Assert.That(settings.atlasSize, Is.EqualTo(256));
            Assert.That(settings.spotResolution, Is.EqualTo(64));
            Assert.That(settings.pointFaceResolution, Is.EqualTo(64));
            Assert.That(settings.secondDirectionalAtlasSize, Is.EqualTo(256));
            Assert.That(settings.secondDirectionalCascadeCount, Is.EqualTo(HoShadowCastShaderConstants.MaxSecondDirectionalCascades));
        }
    }
}
