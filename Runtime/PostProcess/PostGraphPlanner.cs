using System.Collections.Generic;
using HoUrp.Extensions.Core;
using HoUrp.Extensions.Resources;

namespace HoUrp.Extensions.PostProcess
{
    public sealed class PostGraphPlanner
    {
        private static readonly HoUrpIdentifier EmptySemantic = HoUrpBuiltInNames.PostInputs.None;

        private readonly PostEffectRegistry effects;

        public PostGraphPlanner(PostEffectRegistry effects)
        {
            this.effects = effects ?? throw new System.ArgumentNullException(nameof(effects));
        }

        public PostGraphPlan Build(PostStackDefinition stack)
        {
            var nodes = new List<PostGraphNode>();
            var requests = new List<PostResourceRequest>();
            var diagnostics = new List<PostGraphDiagnostic>();
            bool imageChainRequested = false;
            bool originalSourceRequested = false;

            for (int layerIndex = 0; layerIndex < stack.Layers.Count; layerIndex++)
            {
                PostLayerDefinition layer = stack.Layers[layerIndex];
                if (!layer.Enabled)
                {
                    continue;
                }

                for (int effectIndex = 0; effectIndex < layer.EffectIds.Count; effectIndex++)
                {
                    HoUrpIdentifier effectId = layer.EffectIds[effectIndex];
                    if (!effects.TryGet(effectId, out PostEffectDefinition effect))
                    {
                        diagnostics.Add(new PostGraphDiagnostic(
                            PostGraphDiagnosticSeverity.Error,
                            layer.Id,
                            "PostEffect.Missing",
                            $"Post effect '{effectId}' is not registered."));
                        continue;
                    }

                    if (effect.Domain != layer.Domain)
                    {
                        diagnostics.Add(new PostGraphDiagnostic(
                            PostGraphDiagnosticSeverity.Error,
                            layer.Id,
                            "PostEffect.DomainMismatch",
                            $"Layer '{layer.Id}' is {layer.Domain}, but effect '{effect.Id}' is {effect.Domain}."));
                        continue;
                    }

                    if (effect.ExecutionKind == PostEffectExecutionKind.Removed)
                    {
                        diagnostics.Add(new PostGraphDiagnostic(
                            PostGraphDiagnosticSeverity.Warning,
                            effect.Id,
                            "PostEffect.Removed",
                            $"Post effect '{effect.Id}' is registered as removed and will not be planned."));
                        continue;
                    }

                    nodes.Add(new PostGraphNode(
                        layer.Id,
                        effect.Id,
                        effect.Domain,
                        effect.ExecutionKind,
                        effect.PassCount));

                    if (IsImageChainEffect(effect) && !imageChainRequested)
                    {
                        requests.Add(CreateImageChainRequest(
                            layer.Id,
                            effect.Id,
                            HoUrpBuiltInNames.PostFrameResources.ImageWorkA,
                            "ImageChain.WorkA"));
                        requests.Add(CreateImageChainRequest(
                            layer.Id,
                            effect.Id,
                            HoUrpBuiltInNames.PostFrameResources.ImageWorkB,
                            "ImageChain.WorkB"));
                        imageChainRequested = true;
                    }

                    for (int requestIndex = 0; requestIndex < effect.ResourceRequests.Count; requestIndex++)
                    {
                        PostResourceRequest request = effect.ResourceRequests[requestIndex];

                        if (request.Kind == PostResourceRequestKind.OriginalSource)
                        {
                            if (originalSourceRequested)
                            {
                                continue;
                            }

                            originalSourceRequested = true;
                        }

                        requests.Add(WithOwner(request, layer.Id, effect.Id));

                        if (IsUnsupportedInStage11(request.Kind))
                        {
                            diagnostics.Add(new PostGraphDiagnostic(
                                PostGraphDiagnosticSeverity.Warning,
                                effect.Id,
                                "PostResource.UnsupportedInStage11",
                                $"Resource request '{request.RequestId}' uses {request.Kind}, which is planned but not implemented in stage 11."));
                        }
                    }

                    for (int requestIndex = 0; requestIndex < layer.AdditionalResourceRequests.Count; requestIndex++)
                    {
                        PostResourceRequest request = layer.AdditionalResourceRequests[requestIndex];

                        requests.Add(WithOwner(request, layer.Id, effect.Id));

                        if (IsUnsupportedInStage11(request.Kind))
                        {
                            diagnostics.Add(new PostGraphDiagnostic(
                                PostGraphDiagnosticSeverity.Warning,
                                effect.Id,
                                "PostResource.UnsupportedInStage11",
                                $"Resource request '{request.RequestId}' uses {request.Kind}, which is planned but not implemented in stage 11."));
                        }
                    }
                }
            }

            return new PostGraphPlan(
                new ReadOnlyArray<PostGraphNode>(nodes.ToArray()),
                new ReadOnlyArray<PostResourceRequest>(requests.ToArray()),
                new ReadOnlyArray<PostGraphDiagnostic>(diagnostics.ToArray()));
        }

        private static bool IsImageChainEffect(PostEffectDefinition effect)
        {
            return effect.Domain == PostEffectDomain.ImagePost &&
                (effect.ExecutionKind == PostEffectExecutionKind.SingleImagePass ||
                    effect.ExecutionKind == PostEffectExecutionKind.SemanticImagePass);
        }

        private static bool IsUnsupportedInStage11(PostResourceRequestKind kind)
        {
            return kind == PostResourceRequestKind.LocalPingPong ||
                kind == PostResourceRequestKind.Pyramid ||
                kind == PostResourceRequestKind.History;
        }

        private static PostResourceRequest CreateImageChainRequest(
            HoUrpIdentifier ownerLayer,
            HoUrpIdentifier ownerEffect,
            HoUrpIdentifier resourceId,
            string debugName)
        {
            return new PostResourceRequest(
                HoUrpIdentifier.From(debugName + ".Request"),
                HoUrpBuiltInNames.Features.ImagePost,
                ownerLayer,
                ownerEffect,
                PostResourceRequestKind.ImageChainWork,
                resourceId,
                ResourceFormatHint.CameraColor,
                ResourceScale.Full,
                HoUrpLifetime.PerFrame,
                ResourceClearPolicy.CopySource,
                new ReadOnlyArray<HoUrpIdentifier>(HoUrpBuiltInNames.PostInputs.PrimaryImage),
                EmptySemantic,
                debugName);
        }

        private static PostResourceRequest WithOwner(
            PostResourceRequest request,
            HoUrpIdentifier ownerLayer,
            HoUrpIdentifier ownerEffect)
        {
            return new PostResourceRequest(
                HoUrpIdentifier.From(ownerLayer.Value + "." + ownerEffect.Value + "." + request.Kind + "." + request.ResourceId.Value),
                request.OwnerFeature,
                ownerLayer,
                ownerEffect,
                request.Kind,
                request.ResourceId,
                request.Format,
                request.Scale,
                request.Lifetime,
                request.ClearPolicy,
                request.ReadSemantics,
                request.WriteSemantic,
                request.DebugName,
                request.Required);
        }
    }
}
