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
        public void AtlasPackerAllocatesRectangularBlocks()
        {
            var packer = new HoShadowCastAtlasPacker(8);

            Assert.That(packer.TryAllocate(4, 8, out int x0, out int y0), Is.True);
            Assert.That(new Vector2Int(x0, y0), Is.EqualTo(new Vector2Int(0, 0)));
            Assert.That(packer.TryAllocate(4, 4, out int x1, out int y1), Is.True);
            Assert.That(new Vector2Int(x1, y1), Is.EqualTo(new Vector2Int(4, 0)));
            Assert.That(packer.TryAllocate(1, 1, out _, out _), Is.False);
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
                secondDirectionalCascadeResolution = 1,
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
            Assert.That(settings.secondDirectionalCascadeResolution, Is.EqualTo(64));
            Assert.That(settings.secondDirectionalCascadeCount, Is.EqualTo(HoShadowCastShaderConstants.MaxSecondDirectionalCascades));
        }

        [Test]
        public void RuntimeReportCopiesAcceptedLightsAndSkipCounters()
        {
            var punctualFrame = new HoShadowCastFrameReport
            {
                requestedSlices = 7,
                skippedDuplicateCount = 1,
                skippedCapacityCount = 2
            };
            punctualFrame.AddAccepted(null, LightType.Spot, 3, 0, 1, "Visible");

            var directionalFrame = new HoShadowCastSecondDirectionalFrameReport
            {
                requestedSlices = 4,
                skippedNotCollectableCount = 1,
                skippedMainDirectionalCount = 1
            };
            directionalFrame.AddAccepted(null, 1, 4, "Explicit");

            var report = new HoShadowCastRuntimeReport();
            report.Reset(null, 5, "Collecting");
            report.CopyFromFrames(punctualFrame, directionalFrame);
            report.MarkRendered("Published");

            Assert.That(report.rendered, Is.True);
            Assert.That(report.status, Is.EqualTo("Published"));
            Assert.That(report.visibleLightCount, Is.EqualTo(5));
            Assert.That(report.requestedPunctualSlices, Is.EqualTo(7));
            Assert.That(report.punctualLightCount, Is.EqualTo(1));
            Assert.That(report.punctualSliceCount, Is.EqualTo(1));
            Assert.That(report.punctualLights[0].lightType, Is.EqualTo(LightType.Spot));
            Assert.That(report.punctualLights[0].source, Is.EqualTo("Visible"));
            Assert.That(report.requestedSecondDirectionalSlices, Is.EqualTo(4));
            Assert.That(report.secondDirectionalLightCount, Is.EqualTo(1));
            Assert.That(report.secondDirectionalSliceCount, Is.EqualTo(4));
            Assert.That(report.secondDirectionalLights[0].source, Is.EqualTo("Explicit"));
            Assert.That(report.skippedNotCollectableCount, Is.EqualTo(1));
            Assert.That(report.skippedMainDirectionalCount, Is.EqualTo(1));
            Assert.That(report.skippedDuplicateCount, Is.EqualTo(1));
            Assert.That(report.skippedCapacityCount, Is.EqualTo(2));
        }
    }
}
