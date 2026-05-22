using System;
using HoUrp.Extensions.Core;
using HoUrp.Extensions.Debugging;
using HoUrp.Extensions.Resources;
using NUnit.Framework;

namespace HoUrp.Extensions.Tests.Runtime
{
    public sealed class HoUrpContractRegistryTests
    {
        [Test]
        public void MinimalAovRegistryRegistersExpectedContractCounts()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            Assert.That(registry.Features.Count, Is.EqualTo(3));
            Assert.That(registry.Resources.Count, Is.EqualTo(2));
            Assert.That(registry.Semantics.Count, Is.EqualTo(6));
            Assert.That(registry.DebugViews.Count, Is.EqualTo(4));
            Assert.That(registry.Capabilities.Count, Is.EqualTo(3));
        }

        [Test]
        public void MinimalAovRegistryExposesMaskIdResource()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            ResourceDefinition maskId = registry.Resources.Get(HoUrpBuiltInNames.Resources.AovMaskId);

            Assert.That(maskId.Kind, Is.EqualTo(ResourceKind.Texture2D));
            Assert.That(maskId.Format, Is.EqualTo(ResourceFormatHint.MaskRgba8));
            Assert.That(maskId.ProducerFeature, Is.EqualTo(HoUrpBuiltInNames.Features.AovOutput));
            Assert.That(maskId.LegacyName, Is.EqualTo("_lilHoAovMaskIdTexture"));
        }

        [Test]
        public void MinimalAovRegistryLinksWorldNormalDebugViewToNormalDepth()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            DebugViewDefinition worldNormal = registry.DebugViews.Get(HoUrpBuiltInNames.DebugViews.AovWorldNormal);

            Assert.That(worldNormal.SourceResource, Is.EqualTo(HoUrpBuiltInNames.Resources.AovNormalDepth));
            Assert.That(worldNormal.SourceSemantic, Is.EqualTo(HoUrpBuiltInNames.Semantics.GeometryWorldNormal));
            Assert.That(worldNormal.Range, Is.EqualTo(DebugValueRange.MinusOneToOne));
            Assert.That(worldNormal.DisplayModes.Count, Is.EqualTo(1));
            Assert.That(worldNormal.DisplayModes[0], Is.EqualTo(DebugDisplayMode.Replace));
        }

        [Test]
        public void MinimalAovRegistryLinksSemanticPostAsAovConsumer()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            ResourceDefinition maskId = registry.Resources.Get(HoUrpBuiltInNames.Resources.AovMaskId);

            Assert.That(maskId.ConsumerFeatures, Contains.Item(HoUrpBuiltInNames.Features.SemanticPostProcess));
            Assert.That(
                registry.Features.Get(HoUrpBuiltInNames.Features.SemanticPostProcess).ConsumedResources,
                Contains.Item(HoUrpBuiltInNames.Resources.AovMaskId));
        }

        [Test]
        public void MinimalAovRegistryLinksDebugCompositeToRegisteredAovDebugViews()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            var debugComposite = registry.Features.Get(HoUrpBuiltInNames.Features.DebugComposite);

            Assert.That(debugComposite.DebugViews, Contains.Item(HoUrpBuiltInNames.DebugViews.AovMask));
            Assert.That(debugComposite.DebugViews, Contains.Item(HoUrpBuiltInNames.DebugViews.AovObjectId));
            Assert.That(debugComposite.DebugViews, Contains.Item(HoUrpBuiltInNames.DebugViews.AovLinearDepth));
            Assert.That(debugComposite.DebugViews, Contains.Item(HoUrpBuiltInNames.DebugViews.AovWorldNormal));
        }

        [Test]
        public void MinimalAovShaderBindingsUseNewHoUrpNames()
        {
            Assert.That(HoUrpShaderPropertyIds.AovOutputFallbackShaderName, Is.EqualTo("Hidden/HoURP/AOV/AovOutputFallback"));
            Assert.That(HoUrpShaderPropertyIds.AovDebugShaderName, Is.EqualTo("Hidden/HoURP/Debug/AovDebug"));
            Assert.That(HoUrpShaderPropertyIds.SemanticPostAovReadProbeShaderName, Is.EqualTo("Hidden/HoURP/SemanticPost/AovReadProbe"));

            Assert.That(HoUrpShaderPropertyIds.AovMaskIdTexture, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpAovMaskIdTexture")));
            Assert.That(HoUrpShaderPropertyIds.AovNormalDepthTexture, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpAovNormalDepthTexture")));
            Assert.That(HoUrpShaderPropertyIds.SourceColorTexture, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSourceColorTexture")));
            Assert.That(HoUrpShaderPropertyIds.AovDebugMode, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpAovDebugMode")));
            Assert.That(HoUrpShaderPropertyIds.SemanticPostTintColor, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSemanticPostTintColor")));
        }

        [Test]
        public void RegistryRejectsDuplicateDefinitions()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            Assert.Throws<InvalidOperationException>(() =>
                HoUrpBuiltInContracts.RegisterMinimalAovContracts(registry));
        }
    }
}
