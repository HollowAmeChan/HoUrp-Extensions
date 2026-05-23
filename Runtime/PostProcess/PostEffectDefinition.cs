using System;
using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.PostProcess
{
    public readonly struct PostEffectDefinition
    {
        public PostEffectDefinition(
            HoUrpIdentifier id,
            PostEffectDomain domain,
            PostEffectExecutionKind executionKind,
            ReadOnlyArray<PostResourceRequest> resourceRequests,
            int passCount = 1,
            string legacyReference = null,
            string description = null)
        {
            if (passCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(passCount), "Pass count must be at least one.");
            }

            Id = id;
            Domain = domain;
            ExecutionKind = executionKind;
            ResourceRequests = resourceRequests;
            PassCount = passCount;
            LegacyReference = legacyReference ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public HoUrpIdentifier Id { get; }
        public PostEffectDomain Domain { get; }
        public PostEffectExecutionKind ExecutionKind { get; }
        public ReadOnlyArray<PostResourceRequest> ResourceRequests { get; }
        public int PassCount { get; }
        public string LegacyReference { get; }
        public string Description { get; }
    }
}
