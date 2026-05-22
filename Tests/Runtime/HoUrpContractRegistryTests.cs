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

            Assert.That(registry.Features.Count, Is.EqualTo(4));
            Assert.That(registry.Resources.Count, Is.EqualTo(9));
            Assert.That(registry.Semantics.Count, Is.EqualTo(27));
            Assert.That(registry.DebugViews.Count, Is.EqualTo(29));
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
        public void MinimalAovRegistryExposesMaterialSemanticResources()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            ResourceDefinition surfaceData = registry.Resources.Get(HoUrpBuiltInNames.Resources.AovSurfaceData);
            ResourceDefinition materialCustom = registry.Resources.Get(HoUrpBuiltInNames.Resources.AovMaterialCustom0_3);

            Assert.That(surfaceData.Kind, Is.EqualTo(ResourceKind.Texture2D));
            Assert.That(surfaceData.Format, Is.EqualTo(ResourceFormatHint.HighPrecisionRgba16Float));
            Assert.That(surfaceData.ProducerFeature, Is.EqualTo(HoUrpBuiltInNames.Features.AovOutput));
            Assert.That(surfaceData.LegacyName, Is.EqualTo("_lilHoAovSurfaceDataTexture"));
            Assert.That(materialCustom.Kind, Is.EqualTo(ResourceKind.Texture2D));
            Assert.That(materialCustom.Format, Is.EqualTo(ResourceFormatHint.HighPrecisionRgba16Float));
            Assert.That(materialCustom.ProducerFeature, Is.EqualTo(HoUrpBuiltInNames.Features.AovOutput));
            Assert.That(materialCustom.LegacyName, Is.EqualTo("_lilHoAovCustom0_3Texture"));
        }

        [Test]
        public void MinimalAovRegistryLinksMaterialSemanticDebugViews()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            DebugViewDefinition thickness = registry.DebugViews.Get(HoUrpBuiltInNames.DebugViews.AovThickness);
            DebugViewDefinition materialCustom2 = registry.DebugViews.Get(HoUrpBuiltInNames.DebugViews.AovMaterialCustom2);

            Assert.That(thickness.SourceResource, Is.EqualTo(HoUrpBuiltInNames.Resources.AovSurfaceData));
            Assert.That(thickness.SourceSemantic, Is.EqualTo(HoUrpBuiltInNames.Semantics.MaterialThickness));
            Assert.That(thickness.Range, Is.EqualTo(DebugValueRange.ZeroToOne));
            Assert.That(materialCustom2.SourceResource, Is.EqualTo(HoUrpBuiltInNames.Resources.AovMaterialCustom0_3));
            Assert.That(materialCustom2.SourceSemantic, Is.EqualTo(HoUrpBuiltInNames.Semantics.MaterialCustom2));
            Assert.That(materialCustom2.Range, Is.EqualTo(DebugValueRange.ZeroToOne));
        }

        [Test]
        public void MinimalAovRegistryExposesSssInputResource()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            ResourceDefinition sssSource = registry.Resources.Get(HoUrpBuiltInNames.Resources.AovSssSource);

            Assert.That(sssSource.Kind, Is.EqualTo(ResourceKind.Texture2D));
            Assert.That(sssSource.Format, Is.EqualTo(ResourceFormatHint.HighPrecisionRgba16Float));
            Assert.That(sssSource.ProducerFeature, Is.EqualTo(HoUrpBuiltInNames.Features.AovOutput));
            Assert.That(sssSource.LegacyName, Is.EqualTo("_lilHoAovSssTexture"));
            Assert.That(sssSource.DebugView, Is.EqualTo(HoUrpBuiltInNames.DebugViews.AovSssSource));
        }

        [Test]
        public void MinimalAovRegistryLinksSssInputDebugViews()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            DebugViewDefinition sssSource = registry.DebugViews.Get(HoUrpBuiltInNames.DebugViews.AovSssSource);
            DebugViewDefinition sssWeight = registry.DebugViews.Get(HoUrpBuiltInNames.DebugViews.AovSssWeight);
            DebugViewDefinition sssThickness = registry.DebugViews.Get(HoUrpBuiltInNames.DebugViews.SssThickness);

            Assert.That(sssSource.SourceResource, Is.EqualTo(HoUrpBuiltInNames.Resources.AovSssSource));
            Assert.That(sssSource.SourceSemantic, Is.EqualTo(HoUrpBuiltInNames.Semantics.ShadingSssSourceColor));
            Assert.That(sssSource.Range, Is.EqualTo(DebugValueRange.HdrColor));
            Assert.That(sssWeight.SourceResource, Is.EqualTo(HoUrpBuiltInNames.Resources.AovSssSource));
            Assert.That(sssWeight.SourceSemantic, Is.EqualTo(HoUrpBuiltInNames.Semantics.ShadingSssWeight));
            Assert.That(sssWeight.Range, Is.EqualTo(DebugValueRange.ZeroToOne));
            Assert.That(sssThickness.SourceResource, Is.EqualTo(HoUrpBuiltInNames.Resources.AovSurfaceData));
            Assert.That(sssThickness.SourceSemantic, Is.EqualTo(HoUrpBuiltInNames.Semantics.MaterialThickness));
        }

        [Test]
        public void MaterialUtilityIsRegisteredButNotProducedInPhaseFour()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            var utility = registry.Semantics.Get(HoUrpBuiltInNames.Semantics.MaterialUtility);

            Assert.That(utility.Domain, Is.EqualTo(HoUrpDomain.Material));
            Assert.That(utility.DebugView, Is.EqualTo(HoUrpBuiltInNames.DebugViews.None));
        }

        [Test]
        public void MinimalAovRegistryLinksSemanticPostAsAovConsumer()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            ResourceDefinition maskId = registry.Resources.Get(HoUrpBuiltInNames.Resources.AovMaskId);
            ResourceDefinition objectCustom0 = registry.Resources.Get(HoUrpBuiltInNames.Resources.AovObjectCustom0_3);
            ResourceDefinition surfaceData = registry.Resources.Get(HoUrpBuiltInNames.Resources.AovSurfaceData);
            ResourceDefinition materialCustom = registry.Resources.Get(HoUrpBuiltInNames.Resources.AovMaterialCustom0_3);
            ResourceDefinition sssSource = registry.Resources.Get(HoUrpBuiltInNames.Resources.AovSssSource);

            Assert.That(maskId.ConsumerFeatures, Contains.Item(HoUrpBuiltInNames.Features.SemanticPostProcess));
            Assert.That(objectCustom0.ConsumerFeatures, Contains.Item(HoUrpBuiltInNames.Features.SemanticPostProcess));
            Assert.That(surfaceData.ConsumerFeatures, Contains.Item(HoUrpBuiltInNames.Features.SemanticPostProcess));
            Assert.That(materialCustom.ConsumerFeatures, Contains.Item(HoUrpBuiltInNames.Features.SemanticPostProcess));
            Assert.That(sssSource.ConsumerFeatures, Contains.Item(HoUrpBuiltInNames.Features.SemanticPostProcess));
            Assert.That(
                registry.Features.Get(HoUrpBuiltInNames.Features.SemanticPostProcess).ConsumedResources,
                Contains.Item(HoUrpBuiltInNames.Resources.AovMaskId));
            Assert.That(
                registry.Features.Get(HoUrpBuiltInNames.Features.SemanticPostProcess).ConsumedResources,
                Contains.Item(HoUrpBuiltInNames.Resources.AovObjectCustom0_3));
            Assert.That(
                registry.Features.Get(HoUrpBuiltInNames.Features.SemanticPostProcess).ConsumedResources,
                Contains.Item(HoUrpBuiltInNames.Resources.AovSurfaceData));
            Assert.That(
                registry.Features.Get(HoUrpBuiltInNames.Features.SemanticPostProcess).ConsumedResources,
                Contains.Item(HoUrpBuiltInNames.Resources.AovMaterialCustom0_3));
            Assert.That(
                registry.Features.Get(HoUrpBuiltInNames.Features.SemanticPostProcess).ConsumedResources,
                Contains.Item(HoUrpBuiltInNames.Resources.AovSssSource));
        }

        [Test]
        public void MinimalAovRegistryLinksSubsurfaceScatteringFeature()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            var feature = registry.Features.Get(HoUrpBuiltInNames.Features.SubsurfaceScattering);
            ResourceDefinition sssSource = registry.Resources.Get(HoUrpBuiltInNames.Resources.SssSource);
            ResourceDefinition sssDiffusion = registry.Resources.Get(HoUrpBuiltInNames.Resources.SssDiffusion);

            Assert.That(feature.Stage, Is.EqualTo(HoUrpPassStage.ScreenSss));
            Assert.That(feature.ProducedResources, Contains.Item(HoUrpBuiltInNames.Resources.SssSource));
            Assert.That(feature.ProducedResources, Contains.Item(HoUrpBuiltInNames.Resources.SssDiffusion));
            Assert.That(feature.ConsumedResources, Contains.Item(HoUrpBuiltInNames.Resources.AovMaskId));
            Assert.That(feature.ConsumedResources, Contains.Item(HoUrpBuiltInNames.Resources.AovNormalDepth));
            Assert.That(feature.ConsumedResources, Contains.Item(HoUrpBuiltInNames.Resources.AovSurfaceData));
            Assert.That(feature.ConsumedResources, Contains.Item(HoUrpBuiltInNames.Resources.AovSssSource));
            Assert.That(sssSource.ProducerFeature, Is.EqualTo(HoUrpBuiltInNames.Features.SubsurfaceScattering));
            Assert.That(sssDiffusion.ProducerFeature, Is.EqualTo(HoUrpBuiltInNames.Features.SubsurfaceScattering));
            Assert.That(sssSource.DebugView, Is.EqualTo(HoUrpBuiltInNames.DebugViews.SssSource));
            Assert.That(sssDiffusion.DebugView, Is.EqualTo(HoUrpBuiltInNames.DebugViews.SssDiffusion));
            Assert.That(
                registry.Resources.Get(HoUrpBuiltInNames.Resources.AovMaskId).ConsumerFeatures,
                Contains.Item(HoUrpBuiltInNames.Features.SubsurfaceScattering));
            Assert.That(
                registry.Resources.Get(HoUrpBuiltInNames.Resources.AovNormalDepth).ConsumerFeatures,
                Contains.Item(HoUrpBuiltInNames.Features.SubsurfaceScattering));
            Assert.That(
                registry.Resources.Get(HoUrpBuiltInNames.Resources.AovSurfaceData).ConsumerFeatures,
                Contains.Item(HoUrpBuiltInNames.Features.SubsurfaceScattering));
            Assert.That(
                registry.Resources.Get(HoUrpBuiltInNames.Resources.AovSssSource).ConsumerFeatures,
                Contains.Item(HoUrpBuiltInNames.Features.SubsurfaceScattering));
        }

        [Test]
        public void MinimalAovRegistryLinksSssDebugViews()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            DebugViewDefinition mask = registry.DebugViews.Get(HoUrpBuiltInNames.DebugViews.SssMask);
            DebugViewDefinition source = registry.DebugViews.Get(HoUrpBuiltInNames.DebugViews.SssSource);
            DebugViewDefinition diffusion = registry.DebugViews.Get(HoUrpBuiltInNames.DebugViews.SssDiffusion);
            DebugViewDefinition weight = registry.DebugViews.Get(HoUrpBuiltInNames.DebugViews.SssCompositeWeight);

            Assert.That(mask.SourceResource, Is.EqualTo(HoUrpBuiltInNames.Resources.SssSource));
            Assert.That(mask.SourceSemantic, Is.EqualTo(HoUrpBuiltInNames.Semantics.ShadingSssWeight));
            Assert.That(source.SourceResource, Is.EqualTo(HoUrpBuiltInNames.Resources.SssSource));
            Assert.That(source.SourceSemantic, Is.EqualTo(HoUrpBuiltInNames.Semantics.ShadingSssSourceColor));
            Assert.That(diffusion.SourceResource, Is.EqualTo(HoUrpBuiltInNames.Resources.SssDiffusion));
            Assert.That(diffusion.SourceSemantic, Is.EqualTo(HoUrpBuiltInNames.Semantics.ShadingSssDiffusionColor));
            Assert.That(weight.SourceResource, Is.EqualTo(HoUrpBuiltInNames.Resources.SssDiffusion));
            Assert.That(weight.SourceSemantic, Is.EqualTo(HoUrpBuiltInNames.Semantics.ShadingSssCompositeWeight));
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
            Assert.That(debugComposite.DebugViews, Contains.Item(HoUrpBuiltInNames.DebugViews.AovThickness));
            Assert.That(debugComposite.DebugViews, Contains.Item(HoUrpBuiltInNames.DebugViews.AovMaterialCustom3));
            Assert.That(debugComposite.DebugViews, Contains.Item(HoUrpBuiltInNames.DebugViews.AovSssSource));
            Assert.That(debugComposite.DebugViews, Contains.Item(HoUrpBuiltInNames.DebugViews.AovSssWeight));
            Assert.That(debugComposite.DebugViews, Contains.Item(HoUrpBuiltInNames.DebugViews.SssMask));
            Assert.That(debugComposite.DebugViews, Contains.Item(HoUrpBuiltInNames.DebugViews.SssSource));
            Assert.That(debugComposite.DebugViews, Contains.Item(HoUrpBuiltInNames.DebugViews.SssDiffusion));
            Assert.That(debugComposite.DebugViews, Contains.Item(HoUrpBuiltInNames.DebugViews.SssCompositeWeight));
            Assert.That(debugComposite.DebugViews, Contains.Item(HoUrpBuiltInNames.DebugViews.SssProfileId));
            Assert.That(debugComposite.DebugViews, Contains.Item(HoUrpBuiltInNames.DebugViews.SssThickness));
            Assert.That(debugComposite.DebugViews, Contains.Item(HoUrpBuiltInNames.DebugViews.SssCurvature));
        }

        [Test]
        public void MinimalAovShaderBindingsUseNewHoUrpNames()
        {
            Assert.That(HoUrpShaderPropertyIds.AovOutputFallbackShaderName, Is.EqualTo("Hidden/HoURP/AOV/AovOutputFallback"));
            Assert.That(HoUrpShaderPropertyIds.AovDebugShaderName, Is.EqualTo("Hidden/HoURP/Debug/AovDebug"));
            Assert.That(HoUrpShaderPropertyIds.SemanticPostAovReadProbeShaderName, Is.EqualTo("Hidden/HoURP/SemanticPost/AovReadProbe"));
            Assert.That(HoUrpShaderPropertyIds.SubsurfaceScatteringShaderName, Is.EqualTo("Hidden/HoURP/SSS/SubsurfaceScattering"));

            Assert.That(HoUrpShaderPropertyIds.AovMaskIdTexture, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpAovMaskIdTexture")));
            Assert.That(HoUrpShaderPropertyIds.AovNormalDepthTexture, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpAovNormalDepthTexture")));
            Assert.That(HoUrpShaderPropertyIds.AovObjectCustom0_3Texture, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpAovObjectCustom0_3Texture")));
            Assert.That(HoUrpShaderPropertyIds.AovObjectCustom4_7Texture, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpAovObjectCustom4_7Texture")));
            Assert.That(HoUrpShaderPropertyIds.AovSurfaceDataTexture, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpAovSurfaceDataTexture")));
            Assert.That(HoUrpShaderPropertyIds.AovMaterialCustom0_3Texture, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpAovMaterialCustom0_3Texture")));
            Assert.That(HoUrpShaderPropertyIds.AovSssSourceTexture, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpAovSssSourceTexture")));
            Assert.That(HoUrpShaderPropertyIds.SssSourceTexture, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSssSourceTexture")));
            Assert.That(HoUrpShaderPropertyIds.SssDiffusionTexture, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSssDiffusionTexture")));
            Assert.That(HoUrpShaderPropertyIds.SourceColorTexture, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSourceColorTexture")));
            Assert.That(HoUrpShaderPropertyIds.AovDebugMode, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpAovDebugMode")));
            Assert.That(HoUrpShaderPropertyIds.AovDebugTileGrid, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpAovDebugTileGrid")));
            Assert.That(HoUrpShaderPropertyIds.SemanticPostTintColor, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSemanticPostTintColor")));
            Assert.That(HoUrpShaderPropertyIds.AovMaskWeight, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpAovMaskWeight")));
            Assert.That(HoUrpShaderPropertyIds.ObjectCustomMask, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpObjectCustomMask")));
            Assert.That(HoUrpShaderPropertyIds.MaterialClass, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpMaterialClass")));
            Assert.That(HoUrpShaderPropertyIds.MaterialSssProfile, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpMaterialSssProfile")));
            Assert.That(HoUrpShaderPropertyIds.MaterialThickness, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpMaterialThickness")));
            Assert.That(HoUrpShaderPropertyIds.MaterialCurvature, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpMaterialCurvature")));
            Assert.That(HoUrpShaderPropertyIds.MaterialUtility, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpMaterialUtility")));
            Assert.That(HoUrpShaderPropertyIds.MaterialCustom0_3, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpMaterialCustom0_3")));
            Assert.That(HoUrpShaderPropertyIds.SssSourceColor, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSssSourceColor")));
            Assert.That(HoUrpShaderPropertyIds.SssWeight, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSssWeight")));
            Assert.That(HoUrpShaderPropertyIds.SemanticPostObjectCustomChannel, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSemanticPostObjectCustomChannel")));
            Assert.That(HoUrpShaderPropertyIds.SemanticPostMaterialCustomChannel, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSemanticPostMaterialCustomChannel")));
            Assert.That(HoUrpShaderPropertyIds.SssStrength, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSssStrength")));
            Assert.That(HoUrpShaderPropertyIds.SssRadius, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSssRadius")));
            Assert.That(HoUrpShaderPropertyIds.SssDepthTolerance, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSssDepthTolerance")));
            Assert.That(HoUrpShaderPropertyIds.SssNormalTolerance, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSssNormalTolerance")));
            Assert.That(HoUrpShaderPropertyIds.SssSourcePreserve, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSssSourcePreserve")));
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
