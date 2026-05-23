using HoUrp.Extensions.OIT;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace HoUrp.Extensions.Tests.Runtime
{
    public sealed class HoUrpWeightedOitRuntimeTests
    {
        [Test]
        public void WeightedOitSettingsDefaultToTransparentQueueAndStandardPassEvents()
        {
            var settings = new WeightedOitSettings();

            Assert.That(settings.enabled, Is.True);
            Assert.That(settings.enabledForGameView, Is.True);
            Assert.That(settings.enabledForSceneView, Is.True);
            Assert.That(settings.layerMask.value, Is.EqualTo(-1));
            Assert.That(settings.RenderQueueRange.lowerBound, Is.EqualTo((int)RenderQueue.AlphaTest + 1));
            Assert.That(settings.RenderQueueRange.upperBound, Is.EqualTo((int)RenderQueue.Overlay - 1));
            Assert.That(settings.accumulationPassEvent, Is.EqualTo(RenderPassEvent.BeforeRenderingTransparents));
            Assert.That(settings.compositePassEvent, Is.EqualTo(RenderPassEvent.AfterRenderingTransparents));
            Assert.That(settings.weight, Is.EqualTo(1.0f));
            Assert.That(settings.alphaClipThreshold, Is.EqualTo(0.003921569f));
        }

        [Test]
        public void WeightedOitSettingsClampInvertedQueueRangeForFiltering()
        {
            var settings = new WeightedOitSettings
            {
                minRenderQueue = 4500,
                maxRenderQueue = 2451
            };

            Assert.That(settings.RenderQueueRange.lowerBound, Is.EqualTo(2451));
            Assert.That(settings.RenderQueueRange.upperBound, Is.EqualTo(4500));
        }

        [Test]
        public void WeightedOitSettingsRespectCameraToggles()
        {
            var settings = new WeightedOitSettings
            {
                enabledForGameView = false,
                enabledForSceneView = true
            };

            Assert.That(settings.ShouldRender(CameraType.Game), Is.False);
            Assert.That(settings.ShouldRender(CameraType.SceneView), Is.True);

            settings.enabled = false;
            Assert.That(settings.ShouldRender(CameraType.SceneView), Is.False);
        }
    }
}
