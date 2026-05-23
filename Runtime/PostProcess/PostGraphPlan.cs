using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.PostProcess
{
    public readonly struct PostGraphPlan
    {
        public PostGraphPlan(
            ReadOnlyArray<PostGraphNode> nodes,
            ReadOnlyArray<PostResourceRequest> resourceRequests,
            ReadOnlyArray<PostGraphDiagnostic> diagnostics)
        {
            Nodes = nodes;
            ResourceRequests = resourceRequests;
            Diagnostics = diagnostics;
        }

        public ReadOnlyArray<PostGraphNode> Nodes { get; }
        public ReadOnlyArray<PostResourceRequest> ResourceRequests { get; }
        public ReadOnlyArray<PostGraphDiagnostic> Diagnostics { get; }
    }
}
