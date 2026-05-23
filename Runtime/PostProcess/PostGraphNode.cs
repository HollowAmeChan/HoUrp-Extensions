using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.PostProcess
{
    public readonly struct PostGraphNode
    {
        public PostGraphNode(
            HoUrpIdentifier layerId,
            HoUrpIdentifier effectId,
            PostEffectDomain domain,
            PostEffectExecutionKind executionKind,
            int passCount)
        {
            LayerId = layerId;
            EffectId = effectId;
            Domain = domain;
            ExecutionKind = executionKind;
            PassCount = passCount;
        }

        public HoUrpIdentifier LayerId { get; }
        public HoUrpIdentifier EffectId { get; }
        public PostEffectDomain Domain { get; }
        public PostEffectExecutionKind ExecutionKind { get; }
        public int PassCount { get; }
    }
}
