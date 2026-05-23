using System;
using System.Reflection;
using HoUrp.Extensions.Capability;
using HoUrp.Extensions.Core;
using HoUrp.Extensions.Debugging;
using HoUrp.Extensions.Features;
using HoUrp.Extensions.Resources;
using HoUrp.Extensions.Semantic;
using NUnit.Framework;
using UnityEngine.Rendering;

namespace HoUrp.Extensions.Tests.Runtime
{
    public sealed class HoUrpContractRegistryTests
    {
        [Test]
        public void MinimalAovRegistryRegistersExpectedContractCounts()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            Assert.That(registry.Features.Count, Is.EqualTo(7));
            Assert.That(registry.Resources.Count, Is.EqualTo(10));
            Assert.That(registry.Semantics.Count, Is.EqualTo(33));
            Assert.That(registry.DebugViews.Count, Is.EqualTo(43));
            Assert.That(registry.Capabilities.Count, Is.EqualTo(7));
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
            Assert.That(custom2.LegacyReference, Is.EqualTo("HoAovDebugMode.Hair"));
            Assert.That(custom2.Description, Does.Contain("Hair object semantic bit"));
            Assert.That(custom5.SourceResource, Is.EqualTo(HoUrpBuiltInNames.Resources.AovObjectCustom4_7));
            Assert.That(custom5.SourceSemantic, Is.EqualTo(HoUrpBuiltInNames.Semantics.ObjectCustom5));
            Assert.That(custom5.Range, Is.EqualTo(DebugValueRange.ZeroToOne));
            Assert.That(custom5.LegacyReference, Is.EqualTo("HoAovDebugMode.Cloth"));
            Assert.That(custom5.Description, Does.Contain("Cloth object semantic bit"));
        }

        [Test]
        public void MinimalAovRegistryLinksObjectFlagDebugViews()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            DebugViewDefinition emptyFlag = registry.DebugViews.Get(HoUrpBuiltInNames.DebugViews.AovObjectFlag0);
            DebugViewDefinition postReceiver = registry.DebugViews.Get(HoUrpBuiltInNames.DebugViews.AovObjectFlag1);
            DebugViewDefinition flag2 = registry.DebugViews.Get(HoUrpBuiltInNames.DebugViews.AovObjectFlag2);
            DebugViewDefinition flag7 = registry.DebugViews.Get(HoUrpBuiltInNames.DebugViews.AovObjectFlag7);
            DebugViewDefinition flag8 = registry.DebugViews.Get(HoUrpBuiltInNames.DebugViews.AovObjectFlag8);
            DebugViewDefinition flag12 = registry.DebugViews.Get(HoUrpBuiltInNames.DebugViews.AovObjectFlag12);

            Assert.That(emptyFlag.Description, Does.Contain("reserved empty"));
            Assert.That(emptyFlag.Description, Does.Contain("Aov.MaskId.a"));
            Assert.That(postReceiver.SourceResource, Is.EqualTo(HoUrpBuiltInNames.Resources.AovMaskId));
            Assert.That(postReceiver.SourceSemantic, Is.EqualTo(HoUrpBuiltInNames.Semantics.ObjectFlags));
            Assert.That(postReceiver.Range, Is.EqualTo(DebugValueRange.ZeroToOne));
            Assert.That(postReceiver.Description, Does.Contain("screen post receiver gate"));
            Assert.That(postReceiver.Description, Does.Contain("Aov.MaskId.a"));
            Assert.That(flag2.Description, Does.Contain("Object.FeatureFlags bit 2"));
            Assert.That(flag7.SourceResource, Is.EqualTo(HoUrpBuiltInNames.Resources.AovMaskId));
            Assert.That(flag7.SourceSemantic, Is.EqualTo(HoUrpBuiltInNames.Semantics.ObjectFlags));
            Assert.That(flag7.Range, Is.EqualTo(DebugValueRange.ZeroToOne));
            Assert.That(flag7.LegacyReference, Is.EqualTo("HoAovDebugMode.ObjectFlag7"));
            Assert.That(flag8.SourceResource, Is.EqualTo(HoUrpBuiltInNames.Resources.AovMaskId));
            Assert.That(flag8.SourceSemantic, Is.EqualTo(HoUrpBuiltInNames.Semantics.ObjectFlags));
            Assert.That(flag8.LegacyReference, Is.EqualTo("HoAovDebugMode.ObjectFlag8"));
            Assert.That(flag12.SourceResource, Is.EqualTo(HoUrpBuiltInNames.Resources.AovMaskId));
            Assert.That(flag12.Description, Does.Contain("bit 12"));
        }

        [Test]
        public void AovDebugFeatureMapsObjectFeatureFlagsWithoutDuplicateFlag1View()
        {
            Type featureType = typeof(AovDebugRendererFeature);
            Type viewType = featureType.GetNestedType("AovDebugView", BindingFlags.NonPublic);
            MethodInfo resolveDebugViewId = featureType.GetMethod("ResolveDebugViewId", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo resolveShaderMode = featureType.GetMethod("ResolveShaderMode", BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(viewType, Is.Not.Null);
            Assert.That(resolveDebugViewId, Is.Not.Null);
            Assert.That(resolveShaderMode, Is.Not.Null);
            Assert.That(Enum.GetNames(viewType), Does.Not.Contain("Flag1"));

            object flag0Reserved = Enum.Parse(viewType, "Flag0Reserved");
            object postReceiver = Enum.Parse(viewType, "PostReceiver");
            object flag2 = Enum.Parse(viewType, "Flag2");
            object flag7 = Enum.Parse(viewType, "Flag7");
            object flag8 = Enum.Parse(viewType, "Flag8");
            object flag12 = Enum.Parse(viewType, "Flag12");

            Assert.That(resolveDebugViewId.Invoke(null, new[] { flag0Reserved }), Is.EqualTo(HoUrpBuiltInNames.DebugViews.AovObjectFlag0));
            Assert.That(resolveDebugViewId.Invoke(null, new[] { postReceiver }), Is.EqualTo(HoUrpBuiltInNames.DebugViews.AovObjectFlag1));
            Assert.That(resolveDebugViewId.Invoke(null, new[] { flag2 }), Is.EqualTo(HoUrpBuiltInNames.DebugViews.AovObjectFlag2));
            Assert.That(resolveDebugViewId.Invoke(null, new[] { flag7 }), Is.EqualTo(HoUrpBuiltInNames.DebugViews.AovObjectFlag7));
            Assert.That(resolveDebugViewId.Invoke(null, new[] { flag8 }), Is.EqualTo(HoUrpBuiltInNames.DebugViews.AovObjectFlag8));
            Assert.That(resolveDebugViewId.Invoke(null, new[] { flag12 }), Is.EqualTo(HoUrpBuiltInNames.DebugViews.AovObjectFlag12));

            Assert.That(resolveShaderMode.Invoke(null, new[] { flag0Reserved }), Is.EqualTo(27));
            Assert.That(resolveShaderMode.Invoke(null, new[] { postReceiver }), Is.EqualTo(28));
            Assert.That(resolveShaderMode.Invoke(null, new[] { flag2 }), Is.EqualTo(29));
            Assert.That(resolveShaderMode.Invoke(null, new[] { flag7 }), Is.EqualTo(34));
            Assert.That(resolveShaderMode.Invoke(null, new[] { flag8 }), Is.EqualTo(35));
            Assert.That(resolveShaderMode.Invoke(null, new[] { flag12 }), Is.EqualTo(39));
        }

        [Test]
        public void AovOutputFeatureDrawsExplicitAovPassesAcrossRenderQueues()
        {
            Type featureType = typeof(AovOutputRendererFeature);
            Type passType = featureType.GetNestedType("AovOutputPass", BindingFlags.NonPublic);
            FieldInfo explicitTagsField = passType?.GetField("ExplicitAovShaderTagIds", BindingFlags.Static | BindingFlags.NonPublic);
            FieldInfo explicitQueueField = passType?.GetField("ExplicitAovRenderQueueRange", BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(passType, Is.Not.Null);
            Assert.That(explicitTagsField, Is.Not.Null);
            Assert.That(explicitQueueField, Is.Not.Null);

            var explicitTags = explicitTagsField.GetValue(null) as System.Collections.IEnumerable;
            Assert.That(explicitTags, Is.Not.Null);
            Assert.That(explicitTags, Has.Some.EqualTo(new ShaderTagId("HoUrpAovOutput")));
            Assert.That(explicitQueueField.GetValue(null), Is.EqualTo(RenderQueueRange.all));
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
        public void MinimalAovRegistryLinksScreenPostAsAovConsumer()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            ResourceDefinition maskId = registry.Resources.Get(HoUrpBuiltInNames.Resources.AovMaskId);
            ResourceDefinition objectCustom0 = registry.Resources.Get(HoUrpBuiltInNames.Resources.AovObjectCustom0_3);
            ResourceDefinition objectCustom4 = registry.Resources.Get(HoUrpBuiltInNames.Resources.AovObjectCustom4_7);
            ResourceDefinition surfaceData = registry.Resources.Get(HoUrpBuiltInNames.Resources.AovSurfaceData);
            ResourceDefinition materialCustom = registry.Resources.Get(HoUrpBuiltInNames.Resources.AovMaterialCustom0_3);
            ResourceDefinition sssSource = registry.Resources.Get(HoUrpBuiltInNames.Resources.AovSssSource);
            ResourceDefinition sssPrepared = registry.Resources.Get(HoUrpBuiltInNames.Resources.SssSource);
            ResourceDefinition sssDiffusion = registry.Resources.Get(HoUrpBuiltInNames.Resources.SssDiffusion);
            FeatureDescriptor screenPost = registry.Features.Get(HoUrpBuiltInNames.Features.ScreenPost);

            Assert.That(maskId.ConsumerFeatures, Contains.Item(HoUrpBuiltInNames.Features.ScreenPost));
            Assert.That(objectCustom0.ConsumerFeatures, Contains.Item(HoUrpBuiltInNames.Features.ScreenPost));
            Assert.That(objectCustom4.ConsumerFeatures, Contains.Item(HoUrpBuiltInNames.Features.ScreenPost));
            Assert.That(surfaceData.ConsumerFeatures, Contains.Item(HoUrpBuiltInNames.Features.ScreenPost));
            Assert.That(materialCustom.ConsumerFeatures, Contains.Item(HoUrpBuiltInNames.Features.ScreenPost));
            Assert.That(sssSource.ConsumerFeatures, Contains.Item(HoUrpBuiltInNames.Features.ScreenPost));
            Assert.That(sssPrepared.ConsumerFeatures, Contains.Item(HoUrpBuiltInNames.Features.ScreenPost));
            Assert.That(sssDiffusion.ConsumerFeatures, Contains.Item(HoUrpBuiltInNames.Features.ScreenPost));
            Assert.That(
                registry.Resources.Get(HoUrpBuiltInNames.Resources.AovNormalDepth).ConsumerFeatures,
                Contains.Item(HoUrpBuiltInNames.Features.ScreenPost));
            Assert.That(screenPost.ProducedResources.Count, Is.EqualTo(0));
            Assert.That(screenPost.ConsumedResources, Contains.Item(HoUrpBuiltInNames.Resources.AovMaskId));
            Assert.That(screenPost.ConsumedResources, Contains.Item(HoUrpBuiltInNames.Resources.AovObjectCustom0_3));
            Assert.That(screenPost.ConsumedResources, Contains.Item(HoUrpBuiltInNames.Resources.AovObjectCustom4_7));
            Assert.That(screenPost.ConsumedResources, Contains.Item(HoUrpBuiltInNames.Resources.AovSurfaceData));
            Assert.That(screenPost.ConsumedResources, Contains.Item(HoUrpBuiltInNames.Resources.AovMaterialCustom0_3));
            Assert.That(screenPost.ConsumedResources, Contains.Item(HoUrpBuiltInNames.Resources.AovSssSource));
            Assert.That(screenPost.ConsumedResources, Contains.Item(HoUrpBuiltInNames.Resources.SssSource));
            Assert.That(screenPost.ConsumedResources, Contains.Item(HoUrpBuiltInNames.Resources.SssDiffusion));
        }

        [Test]
        public void MinimalAovRegistryExposesStageElevenPostPrototypeFeatures()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            FeatureDescriptor screenPost = registry.Features.Get(HoUrpBuiltInNames.Features.ScreenPost);
            FeatureDescriptor imagePost = registry.Features.Get(HoUrpBuiltInNames.Features.ImagePost);

            Assert.That(screenPost.Domain, Is.EqualTo(HoUrpDomain.Composite));
            Assert.That(screenPost.Stage, Is.EqualTo(HoUrpPassStage.SemanticPost));
            Assert.That(screenPost.ConsumedResources, Contains.Item(HoUrpBuiltInNames.Resources.AovMaskId));
            Assert.That(screenPost.ConsumedResources, Contains.Item(HoUrpBuiltInNames.Resources.AovNormalDepth));
            Assert.That(screenPost.RequiredCapabilities, Contains.Item(HoUrpBuiltInNames.Capabilities.RequiresAov));

            Assert.That(imagePost.Domain, Is.EqualTo(HoUrpDomain.Image));
            Assert.That(imagePost.Stage, Is.EqualTo(HoUrpPassStage.ImagePost));
            Assert.That(imagePost.ProducedResources.Count, Is.EqualTo(0));
            Assert.That(imagePost.ConsumedResources.Count, Is.EqualTo(0));
            Assert.That(imagePost.Description, Does.Contain("Image.WorkA/Image.WorkB"));
        }

        [Test]
        public void MinimalAovRegistryDoesNotExposeLegacySemanticPostMaskDebugView()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            Assert.That(registry.DebugViews.TryGet(HoUrpIdentifier.From("SemanticPost.Mask"), out _), Is.False);
            Assert.That(registry.Resources.TryGet(HoUrpIdentifier.From("SemanticPost.Mask"), out _), Is.False);
            Assert.That(registry.Features.TryGet(HoUrpIdentifier.From("SemanticPostProcess"), out _), Is.False);
        }

        [Test]
        public void MinimalAovRegistryRegistersObjectCapabilityUiEntries()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            var writesAov = registry.Capabilities.Get(HoUrpBuiltInNames.Capabilities.WritesAov);
            var writesObjectCustom = registry.Capabilities.Get(HoUrpBuiltInNames.Capabilities.WritesObjectCustom);
            var receivesSemanticPost = registry.Capabilities.Get(HoUrpBuiltInNames.Capabilities.ReceivesSemanticPost);

            Assert.That(writesAov.OwnerKind, Is.EqualTo(CapabilityOwnerKind.Object));
            Assert.That(writesObjectCustom.OwnerKind, Is.EqualTo(CapabilityOwnerKind.Object));
            Assert.That(receivesSemanticPost.OwnerKind, Is.EqualTo(CapabilityOwnerKind.Object));
            Assert.That(receivesSemanticPost.Affects, Is.EqualTo("ScreenPost rules"));
        }

        [Test]
        public void MinimalAovRegistryRegistersGeneratedMaterialDeclarationWithoutOitResources()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            FeatureDescriptor feature = registry.Features.Get(HoUrpBuiltInNames.Features.GeneratedMaterial);
            SemanticDefinition transparentAlpha = registry.Semantics.Get(HoUrpBuiltInNames.Semantics.TransparentAlpha);
            SemanticDefinition oitAccumulation = registry.Semantics.Get(HoUrpBuiltInNames.Semantics.OitAccumulationInput);
            var supportsOit = registry.Capabilities.Get(HoUrpBuiltInNames.Capabilities.SupportsOit);
            var participatesOit = registry.Capabilities.Get(HoUrpBuiltInNames.Capabilities.ParticipatesOit);

            Assert.That(feature.Domain, Is.EqualTo(HoUrpDomain.Material));
            Assert.That(feature.Stage, Is.EqualTo(HoUrpPassStage.MaterialShadingSemanticAov));
            Assert.That(feature.ProducedResources.Count, Is.EqualTo(0));
            Assert.That(feature.ConsumedResources.Count, Is.EqualTo(0));
            Assert.That(feature.ProducedSemantics, Contains.Item(HoUrpBuiltInNames.Semantics.MaterialClass));
            Assert.That(feature.ProducedSemantics, Contains.Item(HoUrpBuiltInNames.Semantics.ShadingSssWeight));
            Assert.That(feature.ProducedSemantics, Contains.Item(HoUrpBuiltInNames.Semantics.TransparentColor));
            Assert.That(feature.ProducedSemantics, Contains.Item(HoUrpBuiltInNames.Semantics.TransparentAlpha));
            Assert.That(feature.ProducedSemantics, Contains.Item(HoUrpBuiltInNames.Semantics.TransparentCoverage));
            Assert.That(feature.ProducedSemantics, Contains.Item(HoUrpBuiltInNames.Semantics.OitAccumulationInput));
            Assert.That(feature.ProducedSemantics, Contains.Item(HoUrpBuiltInNames.Semantics.OitRevealageInput));
            Assert.That(feature.RequiredCapabilities, Contains.Item(HoUrpBuiltInNames.Capabilities.SupportsOit));
            Assert.That(feature.RequiredCapabilities, Contains.Item(HoUrpBuiltInNames.Capabilities.ParticipatesOit));

            Assert.That(transparentAlpha.Producer, Is.EqualTo(HoUrpBuiltInNames.Features.GeneratedMaterial));
            Assert.That(transparentAlpha.Consumers, Contains.Item(HoUrpBuiltInNames.Features.AovOutput));
            Assert.That(oitAccumulation.Producer, Is.EqualTo(HoUrpBuiltInNames.Features.GeneratedMaterial));
            Assert.That(oitAccumulation.Format, Is.EqualTo(SemanticFormat.Float4));
            Assert.That(oitAccumulation.Consumers, Contains.Item(HoUrpBuiltInNames.Features.TransparentOit));
            Assert.That(supportsOit.OwnerKind, Is.EqualTo(CapabilityOwnerKind.Material));
            Assert.That(participatesOit.OwnerKind, Is.EqualTo(CapabilityOwnerKind.Material));
            Assert.That(feature.ProducedResources, Does.Not.Contain(HoUrpBuiltInNames.Resources.OitAccumulation));
            Assert.That(feature.ProducedResources, Does.Not.Contain(HoUrpBuiltInNames.Resources.OitRevealage));
        }

        [Test]
        public void MinimalAovRegistryLinksTransparentOitRuntimeResources()
        {
            HoUrpContractRegistry registry = HoUrpBuiltInContracts.CreateMinimalAovRegistry();

            FeatureDescriptor feature = registry.Features.Get(HoUrpBuiltInNames.Features.TransparentOit);
            ResourceDefinition opaque = registry.Resources.Get(HoUrpBuiltInNames.Resources.OitOpaqueColor);
            ResourceDefinition accumulation = registry.Resources.Get(HoUrpBuiltInNames.Resources.OitAccumulation);
            ResourceDefinition revealage = registry.Resources.Get(HoUrpBuiltInNames.Resources.OitRevealage);
            ResourceDefinition compositeSource = registry.Resources.Get(HoUrpBuiltInNames.Resources.OitCompositeSource);
            DebugViewDefinition accumulationDebug = registry.DebugViews.Get(HoUrpBuiltInNames.DebugViews.OitAccumulation);
            DebugViewDefinition revealageDebug = registry.DebugViews.Get(HoUrpBuiltInNames.DebugViews.OitRevealage);

            Assert.That(feature.Domain, Is.EqualTo(HoUrpDomain.Composite));
            Assert.That(feature.Stage, Is.EqualTo(HoUrpPassStage.TransparentOit));
            Assert.That(feature.ProducedResources, Contains.Item(HoUrpBuiltInNames.Resources.OitOpaqueColor));
            Assert.That(feature.ProducedResources, Contains.Item(HoUrpBuiltInNames.Resources.OitAccumulation));
            Assert.That(feature.ProducedResources, Contains.Item(HoUrpBuiltInNames.Resources.OitRevealage));
            Assert.That(feature.ProducedResources, Contains.Item(HoUrpBuiltInNames.Resources.OitCompositeSource));
            Assert.That(feature.ConsumedSemantics, Contains.Item(HoUrpBuiltInNames.Semantics.OitAccumulationInput));
            Assert.That(feature.ConsumedSemantics, Contains.Item(HoUrpBuiltInNames.Semantics.OitRevealageInput));
            Assert.That(feature.RequiredCapabilities, Contains.Item(HoUrpBuiltInNames.Capabilities.SupportsOit));
            Assert.That(feature.RequiredCapabilities, Contains.Item(HoUrpBuiltInNames.Capabilities.ParticipatesOit));
            Assert.That(feature.DebugViews, Contains.Item(HoUrpBuiltInNames.DebugViews.OitAccumulation));
            Assert.That(feature.DebugViews, Contains.Item(HoUrpBuiltInNames.DebugViews.OitRevealage));

            Assert.That(opaque.ProducerFeature, Is.EqualTo(HoUrpBuiltInNames.Features.TransparentOit));
            Assert.That(opaque.ClearPolicy, Is.EqualTo(ResourceClearPolicy.CopySource));
            Assert.That(accumulation.ProducerFeature, Is.EqualTo(HoUrpBuiltInNames.Features.TransparentOit));
            Assert.That(accumulation.Format, Is.EqualTo(ResourceFormatHint.HighPrecisionRgba16Float));
            Assert.That(accumulation.ClearPolicy, Is.EqualTo(ResourceClearPolicy.ClearZero));
            Assert.That(revealage.ProducerFeature, Is.EqualTo(HoUrpBuiltInNames.Features.TransparentOit));
            Assert.That(revealage.Format, Is.EqualTo(ResourceFormatHint.R8Unorm));
            Assert.That(revealage.ClearPolicy, Is.EqualTo(ResourceClearPolicy.ClearWhite));
            Assert.That(compositeSource.ProducerFeature, Is.EqualTo(HoUrpBuiltInNames.Features.TransparentOit));
            Assert.That(compositeSource.ClearPolicy, Is.EqualTo(ResourceClearPolicy.CopySource));
            Assert.That(accumulationDebug.SourceResource, Is.EqualTo(HoUrpBuiltInNames.Resources.OitAccumulation));
            Assert.That(revealageDebug.SourceResource, Is.EqualTo(HoUrpBuiltInNames.Resources.OitRevealage));
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
            Assert.That(debugComposite.DebugViews, Contains.Item(HoUrpBuiltInNames.DebugViews.AovObjectFlag8));
            Assert.That(debugComposite.DebugViews, Contains.Item(HoUrpBuiltInNames.DebugViews.AovObjectFlag12));
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
            Assert.That(debugComposite.DebugViews, Does.Not.Contain(HoUrpIdentifier.From("SemanticPost.Mask")));
        }

        [Test]
        public void MinimalAovShaderBindingsUseNewHoUrpNames()
        {
            Assert.That(HoUrpShaderPropertyIds.AovOutputFallbackShaderName, Is.EqualTo("Hidden/HoURP/AOV/AovOutputFallback"));
            Assert.That(HoUrpShaderPropertyIds.AovDebugShaderName, Is.EqualTo("Hidden/HoURP/Debug/AovDebug"));
            Assert.That(HoUrpShaderPropertyIds.SubsurfaceScatteringShaderName, Is.EqualTo("Hidden/HoURP/SSS/SubsurfaceScattering"));
            Assert.That(HoUrpShaderPropertyIds.ScreenPostPrototypeShaderName, Is.EqualTo("Hidden/HoURP/ScreenPost/Prototype"));
            Assert.That(HoUrpShaderPropertyIds.ImagePostPrototypeShaderName, Is.EqualTo("Hidden/HoURP/ImagePost/Prototype"));

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
            Assert.That(HoUrpShaderPropertyIds.ImagePostColorTint, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpImagePostColorTint")));
            Assert.That(HoUrpShaderPropertyIds.ImagePostParams, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpImagePostParams")));
            Assert.That(HoUrpShaderPropertyIds.ScreenPostTintColor, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpScreenPostTintColor")));
            Assert.That(HoUrpShaderPropertyIds.ScreenPostParams, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpScreenPostParams")));
            Assert.That(HoUrpShaderPropertyIds.ScreenPostMaskTexture, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpScreenPostMaskTexture")));
            Assert.That(HoUrpShaderPropertyIds.ScreenPostLayerParams, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpScreenPostLayerParams")));
            Assert.That(HoUrpShaderPropertyIds.ScreenPostRuleParams, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpScreenPostRuleParams")));
            Assert.That(HoUrpShaderPropertyIds.ScreenPostRuleValues, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpScreenPostRuleValues")));
            Assert.That(HoUrpShaderPropertyIds.AovDebugMode, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpAovDebugMode")));
            Assert.That(HoUrpShaderPropertyIds.AovDebugTileGrid, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpAovDebugTileGrid")));
            Assert.That(HoUrpShaderPropertyIds.AovMaskWeight, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpAovMaskWeight")));
            Assert.That(HoUrpShaderPropertyIds.ObjectId, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpObjectId")));
            Assert.That(HoUrpShaderPropertyIds.ObjectGroupId, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpObjectGroupId")));
            Assert.That(HoUrpShaderPropertyIds.ObjectFlags, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpObjectFlags")));
            Assert.That(HoUrpShaderPropertyIds.ObjectCustomMask, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpObjectCustomMask")));
            Assert.That(HoUrpShaderPropertyIds.MaterialClass, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpMaterialClass")));
            Assert.That(HoUrpShaderPropertyIds.MaterialSssProfile, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpMaterialSssProfile")));
            Assert.That(HoUrpShaderPropertyIds.MaterialThickness, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpMaterialThickness")));
            Assert.That(HoUrpShaderPropertyIds.MaterialCurvature, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpMaterialCurvature")));
            Assert.That(HoUrpShaderPropertyIds.MaterialUtility, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpMaterialUtility")));
            Assert.That(HoUrpShaderPropertyIds.MaterialCustom0_3, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpMaterialCustom0_3")));
            Assert.That(HoUrpShaderPropertyIds.SssSourceColor, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSssSourceColor")));
            Assert.That(HoUrpShaderPropertyIds.SssWeight, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSssWeight")));
            Assert.That(HoUrpShaderPropertyIds.SssStrength, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSssStrength")));
            Assert.That(HoUrpShaderPropertyIds.SssRadius, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSssRadius")));
            Assert.That(HoUrpShaderPropertyIds.SssDepthTolerance, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSssDepthTolerance")));
            Assert.That(HoUrpShaderPropertyIds.SssNormalTolerance, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSssNormalTolerance")));
            Assert.That(HoUrpShaderPropertyIds.SssSourcePreserve, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSssSourcePreserve")));
            Assert.That(HoUrpShaderPropertyIds.SssProfileIds, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSssProfileIds")));
            Assert.That(HoUrpShaderPropertyIds.SssProfileDiffusionParams, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSssProfileDiffusionParams")));
            Assert.That(HoUrpShaderPropertyIds.SssProfileShapeParams, Is.EqualTo(UnityEngine.Shader.PropertyToID("_HoUrpSssProfileShapeParams")));
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
