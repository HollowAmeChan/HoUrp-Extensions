using HoUrp.Extensions.Core;
using HoUrp.Extensions.Resources;

namespace HoUrp.Extensions.PostProcess
{
    public static class HoUrpPostPrototypeCatalog
    {
        public static PostEffectRegistry CreateRegistry()
        {
            var registry = new PostEffectRegistry();
            Register(registry);
            return registry;
        }

        public static void Register(PostEffectRegistry registry)
        {
            if (registry == null)
            {
                throw new System.ArgumentNullException(nameof(registry));
            }

            registry.Register(CreateImagePostColorAdjustPrototype());
            registry.Register(CreateImagePostAovCompositePrototype());
            registry.Register(CreateScreenPostRuleMaskPrototype());
            registry.Register(CreatePlannedHistoryPrototype());
        }

        public static PostEffectDefinition CreateImagePostColorAdjustPrototype()
        {
            return new PostEffectDefinition(
                HoUrpBuiltInNames.PostEffects.ImagePostColorAdjustPrototype,
                PostEffectDomain.ImagePost,
                PostEffectExecutionKind.SingleImagePass,
                new ReadOnlyArray<PostResourceRequest>(
                    CreateSourceImageRequest(HoUrpBuiltInNames.Features.ImagePost)),
                1,
                "Shoost color adjustment behavior reference",
                "Pure image-space prototype effect routed through ImageChain.");
        }

        public static PostEffectDefinition CreateImagePostAovCompositePrototype()
        {
            return new PostEffectDefinition(
                HoUrpBuiltInNames.PostEffects.ImagePostAovCompositePrototype,
                PostEffectDomain.ImagePost,
                PostEffectExecutionKind.SemanticImagePass,
                new ReadOnlyArray<PostResourceRequest>(
                    CreateSourceImageRequest(HoUrpBuiltInNames.Features.ImagePost),
                    CreateSemanticInputRequest(
                        HoUrpIdentifier.From("ImagePost.AovComposite.MaskId.Request"),
                        HoUrpBuiltInNames.Features.ImagePost,
                        HoUrpBuiltInNames.Resources.AovMaskId,
                        HoUrpBuiltInNames.Semantics.ObjectMaskWeight,
                        "ImagePost.AovComposite.AovMaskId")),
                1,
                "Shoost AOV composite behavior reference",
                "Lightweight ImagePost AOV composite prototype without ScreenPost rule language.");
        }

        public static PostEffectDefinition CreateScreenPostRuleMaskPrototype()
        {
            return new PostEffectDefinition(
                HoUrpBuiltInNames.PostEffects.ScreenPostRuleMaskPrototype,
                PostEffectDomain.ScreenPost,
                PostEffectExecutionKind.SemanticImagePass,
                new ReadOnlyArray<PostResourceRequest>(
                    CreateSourceImageRequest(HoUrpBuiltInNames.Features.ScreenPost)),
                1,
                "HoPost rule mask behavior reference",
                "ScreenPost rule-mask effect. Layer rule sets declare AOV inputs dynamically.");
        }

        public static PostEffectDefinition CreatePlannedHistoryPrototype()
        {
            return new PostEffectDefinition(
                HoUrpIdentifier.From("ImagePost.PlannedHistoryPrototype"),
                PostEffectDomain.ImagePost,
                PostEffectExecutionKind.Stateful,
                new ReadOnlyArray<PostResourceRequest>(
                    new PostResourceRequest(
                        HoUrpIdentifier.From("ImagePost.PlannedHistory.Request"),
                        HoUrpBuiltInNames.Features.ImagePost,
                        HoUrpIdentifier.From("Post.UnboundLayer"),
                        HoUrpIdentifier.From("ImagePost.PlannedHistoryPrototype"),
                        PostResourceRequestKind.History,
                        HoUrpBuiltInNames.PostFrameResources.ImageHistoryPlanned,
                        ResourceFormatHint.CameraColor,
                        ResourceScale.Full,
                        HoUrpLifetime.Persistent,
                        ResourceClearPolicy.CopySource,
                        new ReadOnlyArray<HoUrpIdentifier>(HoUrpBuiltInNames.PostInputs.PrimaryImage),
                        HoUrpBuiltInNames.PostInputs.History,
                        "ImagePost.PlannedHistory")),
                1,
                "Temporal image post behavior reference",
                "Registration-only prototype. Stage 11 reports a diagnostic instead of creating history resources.");
        }

        private static PostResourceRequest CreateSourceImageRequest(HoUrpIdentifier ownerFeature)
        {
            return new PostResourceRequest(
                HoUrpIdentifier.From(ownerFeature.Value + ".SourceImage.Request"),
                ownerFeature,
                HoUrpIdentifier.From("Post.UnboundLayer"),
                HoUrpIdentifier.From("Post.UnboundEffect"),
                PostResourceRequestKind.SourceImage,
                HoUrpBuiltInNames.PostFrameResources.ImagePrimary,
                ResourceFormatHint.CameraColor,
                ResourceScale.Full,
                HoUrpLifetime.PerFrame,
                ResourceClearPolicy.External,
                new ReadOnlyArray<HoUrpIdentifier>(HoUrpBuiltInNames.PostInputs.PrimaryImage),
                HoUrpBuiltInNames.PostInputs.None,
                ownerFeature.Value + ".SourceImage");
        }

        private static PostResourceRequest CreateSemanticInputRequest(
            HoUrpIdentifier requestId,
            HoUrpIdentifier ownerFeature,
            HoUrpIdentifier resourceId,
            HoUrpIdentifier semantic,
            string debugName)
        {
            return new PostResourceRequest(
                requestId,
                ownerFeature,
                HoUrpIdentifier.From("Post.UnboundLayer"),
                HoUrpIdentifier.From("Post.UnboundEffect"),
                PostResourceRequestKind.SemanticInput,
                resourceId,
                ResourceFormatHint.External,
                ResourceScale.External,
                HoUrpLifetime.Imported,
                ResourceClearPolicy.External,
                new ReadOnlyArray<HoUrpIdentifier>(semantic),
                HoUrpBuiltInNames.PostInputs.None,
                debugName);
        }
    }
}
