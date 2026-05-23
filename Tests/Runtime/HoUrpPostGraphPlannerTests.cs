using System;
using HoUrp.Extensions.Core;
using HoUrp.Extensions.PostProcess;
using HoUrp.Extensions.Resources;
using NUnit.Framework;

namespace HoUrp.Extensions.Tests.Runtime
{
    public sealed class HoUrpPostGraphPlannerTests
    {
        [Test]
        public void DisabledLayerDoesNotCreateNodeOrResourceRequest()
        {
            PostGraphPlan plan = BuildPlan(new PostLayerDefinition(
                "Layer.Disabled",
                PostEffectDomain.ImagePost,
                false,
                new ReadOnlyArray<HoUrpIdentifier>(HoUrpBuiltInNames.PostEffects.ImagePostColorAdjustPrototype)));

            Assert.That(plan.Nodes.Count, Is.EqualTo(0));
            Assert.That(plan.ResourceRequests.Count, Is.EqualTo(0));
            Assert.That(plan.Diagnostics.Count, Is.EqualTo(0));
        }

        [Test]
        public void EnabledSingleImagePassRequestsOnlyTwoImageChainWorkTextures()
        {
            PostGraphPlan plan = BuildPlan(new PostLayerDefinition(
                "Layer.ColorAdjust",
                PostEffectDomain.ImagePost,
                true,
                new ReadOnlyArray<HoUrpIdentifier>(HoUrpBuiltInNames.PostEffects.ImagePostColorAdjustPrototype)));

            Assert.That(plan.Nodes.Count, Is.EqualTo(1));
            Assert.That(plan.ResourceRequests.Count, Is.EqualTo(3));
            Assert.That(ContainsRequest(plan, request =>
                request.Kind == PostResourceRequestKind.SourceImage &&
                request.ResourceId == HoUrpBuiltInNames.PostFrameResources.ImagePrimary), Is.True);
            Assert.That(ContainsRequest(plan, request =>
                request.Kind == PostResourceRequestKind.ImageChainWork &&
                request.ResourceId == HoUrpBuiltInNames.PostFrameResources.ImageWorkA &&
                request.Lifetime == HoUrpLifetime.PerFrame), Is.True);
            Assert.That(ContainsRequest(plan, request =>
                request.Kind == PostResourceRequestKind.ImageChainWork &&
                request.ResourceId == HoUrpBuiltInNames.PostFrameResources.ImageWorkB &&
                request.Lifetime == HoUrpLifetime.PerFrame), Is.True);
        }

        [Test]
        public void ImagePostAovCompositeRequestsAovOnlyWhenEnabled()
        {
            var enabledPlan = BuildPlan(new PostLayerDefinition(
                "Layer.AovComposite",
                PostEffectDomain.ImagePost,
                true,
                new ReadOnlyArray<HoUrpIdentifier>(HoUrpBuiltInNames.PostEffects.ImagePostAovCompositePrototype)));
            var disabledPlan = BuildPlan(new PostLayerDefinition(
                "Layer.AovComposite",
                PostEffectDomain.ImagePost,
                false,
                new ReadOnlyArray<HoUrpIdentifier>(HoUrpBuiltInNames.PostEffects.ImagePostAovCompositePrototype)));

            Assert.That(ContainsRequest(enabledPlan, request =>
                request.Kind == PostResourceRequestKind.SemanticInput &&
                request.ResourceId == HoUrpBuiltInNames.Resources.AovMaskId &&
                request.OwnerFeature == HoUrpBuiltInNames.Features.ImagePost), Is.True);
            Assert.That(ContainsRequest(disabledPlan, request =>
                request.ResourceId == HoUrpBuiltInNames.Resources.AovMaskId), Is.False);
        }

        [Test]
        public void ScreenPostSemanticPassDeclaresSemanticInputsWithoutImageChainWork()
        {
            PostGraphPlan plan = BuildPlan(ScreenPostLayerWithRule(
                "Layer.RuleMask",
                ScreenPostRuleSource.MaskWeight,
                ScreenPostRuleSource.LinearDepth));

            Assert.That(plan.Nodes.Count, Is.EqualTo(1));
            Assert.That(ContainsRequest(plan, request =>
                request.Kind == PostResourceRequestKind.ImageChainWork), Is.False);
            Assert.That(ContainsRequest(plan, request =>
                request.Kind == PostResourceRequestKind.SemanticInput &&
                request.ResourceId == HoUrpBuiltInNames.Resources.AovMaskId &&
                request.OwnerFeature == HoUrpBuiltInNames.Features.ScreenPost), Is.True);
            Assert.That(ContainsRequest(plan, request =>
                request.Kind == PostResourceRequestKind.SemanticInput &&
                request.ResourceId == HoUrpBuiltInNames.Resources.AovNormalDepth &&
                request.ReadSemantics[0] == HoUrpBuiltInNames.Semantics.GeometryLinearDepth), Is.True);
        }

        [Test]
        public void ScreenPostRuleSourcesCreateOnlyRequiredAovRequests()
        {
            PostGraphPlan plan = BuildPlan(ScreenPostLayerWithRule(
                "Layer.ObjectCustom",
                ScreenPostRuleSource.ObjectCustom4,
                ScreenPostRuleSource.Thickness));

            Assert.That(ContainsRequest(plan, request =>
                request.Kind == PostResourceRequestKind.SemanticInput &&
                request.ResourceId == HoUrpBuiltInNames.Resources.AovObjectCustom4_7), Is.True);
            Assert.That(ContainsRequest(plan, request =>
                request.Kind == PostResourceRequestKind.SemanticInput &&
                request.ResourceId == HoUrpBuiltInNames.Resources.AovSurfaceData), Is.True);
            Assert.That(ContainsRequest(plan, request =>
                request.ResourceId == HoUrpBuiltInNames.Resources.AovMaskId), Is.False);
            Assert.That(ContainsRequest(plan, request =>
                request.ResourceId == HoUrpBuiltInNames.Resources.AovNormalDepth), Is.False);
        }

        [Test]
        public void StackSettingsPreserveSerializedListOrder()
        {
            var filters = new[]
            {
                ImagePostFilterSettings.CreateDefault("First"),
                ImagePostFilterSettings.CreateDefault("Second")
            };

            PostGraphPlan plan = new PostGraphPlanner(HoUrpPostPrototypeCatalog.CreateRegistry())
                .Build(PostStackSettings.BuildImagePostStack(filters));

            Assert.That(plan.Nodes.Count, Is.EqualTo(2));
            Assert.That(plan.Nodes[0].LayerId, Is.EqualTo(HoUrpIdentifier.From("ImagePost.Layer.00")));
            Assert.That(plan.Nodes[1].LayerId, Is.EqualTo(HoUrpIdentifier.From("ImagePost.Layer.01")));
        }

        [Test]
        public void DisabledScreenPostRuleSetCreatesNoNodeOrRequest()
        {
            var layer = ScreenPostLayerSettings.CreateDefault();
            layer.ruleSet.enabled = false;

            PostGraphPlan plan = new PostGraphPlanner(HoUrpPostPrototypeCatalog.CreateRegistry())
                .Build(PostStackSettings.BuildScreenPostStack(new[] { layer }));

            Assert.That(plan.Nodes.Count, Is.EqualTo(0));
            Assert.That(plan.ResourceRequests.Count, Is.EqualTo(0));
        }

        [Test]
        public void MissingEffectCreatesDiagnosticAndNoRequests()
        {
            PostGraphPlan plan = BuildPlan(new PostLayerDefinition(
                "Layer.Missing",
                PostEffectDomain.ImagePost,
                true,
                new ReadOnlyArray<HoUrpIdentifier>(HoUrpIdentifier.From("ImagePost.Missing"))));

            Assert.That(plan.Nodes.Count, Is.EqualTo(0));
            Assert.That(plan.ResourceRequests.Count, Is.EqualTo(0));
            Assert.That(plan.Diagnostics.Count, Is.EqualTo(1));
            Assert.That(plan.Diagnostics[0].Severity, Is.EqualTo(PostGraphDiagnosticSeverity.Error));
            Assert.That(plan.Diagnostics[0].Code, Is.EqualTo("PostEffect.Missing"));
        }

        [Test]
        public void UnregisteredEffectIsRemovedFromDynamicPlan()
        {
            PostEffectRegistry registry = HoUrpPostPrototypeCatalog.CreateRegistry();
            Assert.That(registry.Unregister(HoUrpBuiltInNames.PostEffects.ImagePostColorAdjustPrototype), Is.True);

            var planner = new PostGraphPlanner(registry);
            PostGraphPlan plan = planner.Build(new PostStackDefinition(
                new ReadOnlyArray<PostLayerDefinition>(new PostLayerDefinition(
                    "Layer.ColorAdjust",
                    PostEffectDomain.ImagePost,
                    true,
                    new ReadOnlyArray<HoUrpIdentifier>(HoUrpBuiltInNames.PostEffects.ImagePostColorAdjustPrototype)))));

            Assert.That(plan.Nodes.Count, Is.EqualTo(0));
            Assert.That(plan.ResourceRequests.Count, Is.EqualTo(0));
            Assert.That(plan.Diagnostics[0].Code, Is.EqualTo("PostEffect.Missing"));
        }

        [Test]
        public void UnsupportedHistoryRequestCreatesStageElevenDiagnostic()
        {
            PostGraphPlan plan = BuildPlan(new PostLayerDefinition(
                "Layer.History",
                PostEffectDomain.ImagePost,
                true,
                new ReadOnlyArray<HoUrpIdentifier>(HoUrpIdentifier.From("ImagePost.PlannedHistoryPrototype"))));

            Assert.That(ContainsRequest(plan, request =>
                request.Kind == PostResourceRequestKind.History), Is.True);
            Assert.That(ContainsDiagnostic(plan, diagnostic =>
                diagnostic.Code == "PostResource.UnsupportedInStage11" &&
                diagnostic.Severity == PostGraphDiagnosticSeverity.Warning), Is.True);
        }

        [Test]
        public void PostRequestsDoNotExposeLegacyGlobalTextureNames()
        {
            PostGraphPlan plan = BuildPlan(ScreenPostLayerWithRule(
                "Layer.RuleMask",
                ScreenPostRuleSource.MaskWeight,
                ScreenPostRuleSource.LinearDepth));

            for (int i = 0; i < plan.ResourceRequests.Count; i++)
            {
                PostResourceRequest request = plan.ResourceRequests[i];
                Assert.That(request.RequestId.Value, Does.Not.Contain("_lilHo"));
                Assert.That(request.RequestId.Value, Does.Not.Contain("_HoAov"));
                Assert.That(request.ResourceId.Value, Does.Not.Contain("_lilHo"));
                Assert.That(request.ResourceId.Value, Does.Not.Contain("_HoAov"));
                Assert.That(request.DebugName, Does.Not.Contain("_lilHo"));
                Assert.That(request.DebugName, Does.Not.Contain("_HoAov"));
            }
        }

        private static PostGraphPlan BuildPlan(PostLayerDefinition layer)
        {
            var planner = new PostGraphPlanner(HoUrpPostPrototypeCatalog.CreateRegistry());
            return planner.Build(new PostStackDefinition(new ReadOnlyArray<PostLayerDefinition>(layer)));
        }

        private static PostLayerDefinition ScreenPostLayerWithRule(
            HoUrpIdentifier layerId,
            ScreenPostRuleSource first,
            ScreenPostRuleSource second)
        {
            var settings = ScreenPostLayerSettings.CreateDefault();
            settings.ruleSet.rules[0].source = first;
            settings.ruleSet.rules[1].enabled = true;
            settings.ruleSet.rules[1].source = second;
            settings.ruleSet.rules[1].op = ScreenPostRuleOperator.Greater;
            return new PostLayerDefinition(
                layerId,
                PostEffectDomain.ScreenPost,
                true,
                new ReadOnlyArray<HoUrpIdentifier>(HoUrpBuiltInNames.PostEffects.ScreenPostRuleMaskPrototype),
                settings.ruleSet.BuildResourceRequests(layerId));
        }

        private static bool ContainsRequest(PostGraphPlan plan, Predicate<PostResourceRequest> predicate)
        {
            for (int i = 0; i < plan.ResourceRequests.Count; i++)
            {
                if (predicate(plan.ResourceRequests[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsDiagnostic(PostGraphPlan plan, Predicate<PostGraphDiagnostic> predicate)
        {
            for (int i = 0; i < plan.Diagnostics.Count; i++)
            {
                if (predicate(plan.Diagnostics[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
