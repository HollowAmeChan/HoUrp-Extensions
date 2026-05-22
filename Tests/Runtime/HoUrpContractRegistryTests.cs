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
            Assert.That(registry.Resources.Count, Is.EqualTo(4));
            Assert.That(registry.Semantics.Count, Is.EqualTo(14));
            Assert.That(registry.DebugViews.Count, Is.EqualTo(12));
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
        public void MinimalAovRegistryExposesObjectCustomResources()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            ResourceDefinition objectCustom0 = registry.Resources.Get(HoUrpBuiltInNames.Resources.AovObjectCustom0_3);
            ResourceDefinition objectCustom1 = registry.Resources.Get(HoUrpBuiltInNames.Resources.AovObjectCustom4_7);

            Assert.That(objectCustom0.Kind, Is.EqualTo(ResourceKind.Texture2D));
            Assert.That(objectCustom0.Format, Is.EqualTo(ResourceFormatHint.MaskRgba8));
            Assert.That(objectCustom0.ProducerFeature, Is.EqualTo(HoUrpBuiltInNames.Features.AovOutput));
            Assert.That(objectCustom0.LegacyName, Is.EqualTo("_lilHoAovObjectCustom0_3Texture"));
            Assert.That(objectCustom1.Kind, Is.EqualTo(ResourceKind.Texture2D));
            Assert.That(objectCustom1.Format, Is.EqualTo(ResourceFormatHint.MaskRgba8));
            Assert.That(objectCustom1.ProducerFeature, Is.EqualTo(HoUrpBuiltInNames.Features.AovOutput));
            Assert.That(objectCustom1.LegacyName, Is.EqualTo("_lilHoAovObjectCustom4_7Texture"));
        }

        [Test]
        public void MinimalAovRegistryLinksObjectCustomDebugViews()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            DebugViewDefinition custom2 = registry.DebugViews.Get(HoUrpBuiltInNames.DebugViews.AovObjectCustom2);
            DebugViewDefinition custom5 = registry.DebugViews.Get(HoUrpBuiltInNames.DebugViews.AovObjectCustom5);

            Assert.That(custom2.SourceResource, Is.EqualTo(HoUrpBuiltInNames.Resources.AovObjectCustom0_3));
            Assert.That(custom2.SourceSemantic, Is.EqualTo(HoUrpBuiltInNames.Semantics.ObjectCustom2));
            Assert.That(custom2.Range, Is.EqualTo(DebugValueRange.ZeroToOne));
            Assert.That(custom5.SourceResource, Is.EqualTo(HoUrpBuiltInNames.Resources.AovObjectCustom4_7));
            Assert.That(custom5.SourceSemantic, Is.EqualTo(HoUrpBuiltInNames.Semantics.ObjectCustom5));
            Assert.That(custom5.Range, Is.EqualTo(DebugValueRange.ZeroToOne));
        }

        [Test]
        public void MinimalAovRegistryLinksSemanticPostAsAovConsumer()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            ResourceDefinition maskId = registry.Resources.Get(HoUrpBuiltInNames.Resources.AovMaskId);
            ResourceDefinition objectCustom0 = registry.Resources.Get(HoUrpBuiltInNames.Resources.AovObjectCustom0_3);

            Assert.That(maskId.ConsumerFeatures, Contains.Item(HoUrpBuiltInNames.Features.SemanticPostProcess));
            Assert.That(objectCustom0.ConsumerFeatures, Contains.Item(HoUrpBuiltInNames.Features.SemanticPostProcess));
            Assert.That(
                registry.Features.Get(HoUrpBuiltInNames.Features.SemanticPostProcess).ConsumedResources,
                Contains.Item(HoUrpBuiltInNames.Resources.AovMaskId));
            Assert.That(
                registry.Features.Get(HoUrpBuiltInNames.Features.SemanticPostProcess).ConsumedResources,
                Contains.Item(HoUrpBuiltInNames.Resources.AovObjectCustom0_3));
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
            Assert.That(debugComposite.DebugViews, Contains.Item(HoUrpBuiltInNames.DebugViews.AovObjectCustom0));
            Assert.That(debugComposite.DebugViews, Contains.Item(HoUrpBuiltInNames.DebugViews.AovObjectCustom7));
        }

        [Test]
        public void MinimalAovShaderBindingsUseNewHoUrpNames()
        {
            Assert.That(HoUrpShaderPropertyIds.AovOutputFallbackShaderName, Is.EqualTo("Hidden/HoURP/AOV/AovOutputFallback"));
            Assert.That(HoUrpShaderPropertyIds.AovDebugShaderName, Is.EqualTo("Hidden/HoURP/Debug/AovDebug"));
            Assert.That(HoUrpShaderPropertyIds.SemanticPostAovReadProbeShaderName, Is.EqualTo("Hidden/HoURP/SemanticPost/AovReadProbe"));

            Assert.That(HoUrpShaderPropertyIds.AovMaskIdTexture, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpAovMaskIdTexture")));
            Assert.That(HoUrpShaderPropertyIds.AovNormalDepthTexture, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpAovNormalDepthTexture")));
            Assert.That(HoUrpShaderPropertyIds.AovObjectCustom0_3Texture, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpAovObjectCustom0_3Texture")));
            Assert.That(HoUrpShaderPropertyIds.AovObjectCustom4_7Texture, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpAovObjectCustom4_7Texture")));
            Assert.That(HoUrpShaderPropertyIds.SourceColorTexture, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSourceColorTexture")));
            Assert.That(HoUrpShaderPropertyIds.AovDebugMode, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpAovDebugMode")));
            Assert.That(HoUrpShaderPropertyIds.SemanticPostTintColor, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSemanticPostTintColor")));
            Assert.That(HoUrpShaderPropertyIds.AovMaskWeight, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpAovMaskWeight")));
            Assert.That(HoUrpShaderPropertyIds.ObjectCustomMask, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpObjectCustomMask")));
            Assert.That(HoUrpShaderPropertyIds.SemanticPostObjectCustomChannel, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSemanticPostObjectCustomChannel")));
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
