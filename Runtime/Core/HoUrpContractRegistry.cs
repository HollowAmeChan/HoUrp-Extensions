using HoUrp.Extensions.Capability;
using HoUrp.Extensions.Debugging;
using HoUrp.Extensions.Features;
using HoUrp.Extensions.Resources;
using HoUrp.Extensions.Semantic;

namespace HoUrp.Extensions.Core
{
    public sealed class HoUrpContractRegistry
    {
        public HoUrpContractRegistry()
        {
            Semantics = new SemanticRegistry();
            Resources = new ResourceRegistry();
            Features = new FeatureRegistry();
            DebugViews = new DebugViewRegistry();
            Capabilities = new CapabilityRegistry();
        }

        public SemanticRegistry Semantics { get; }
        public ResourceRegistry Resources { get; }
        public FeatureRegistry Features { get; }
        public DebugViewRegistry DebugViews { get; }
        public CapabilityRegistry Capabilities { get; }
    }
}
