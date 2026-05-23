using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.PostProcess
{
    public readonly struct PostLayerDefinition
    {
        public PostLayerDefinition(
            HoUrpIdentifier id,
            PostEffectDomain domain,
            bool enabled,
            ReadOnlyArray<HoUrpIdentifier> effectIds)
            : this(id, domain, enabled, effectIds, new ReadOnlyArray<PostResourceRequest>())
        {
        }

        public PostLayerDefinition(
            HoUrpIdentifier id,
            PostEffectDomain domain,
            bool enabled,
            ReadOnlyArray<HoUrpIdentifier> effectIds,
            ReadOnlyArray<PostResourceRequest> additionalResourceRequests)
        {
            Id = id;
            Domain = domain;
            Enabled = enabled;
            EffectIds = effectIds;
            AdditionalResourceRequests = additionalResourceRequests;
        }

        public HoUrpIdentifier Id { get; }
        public PostEffectDomain Domain { get; }
        public bool Enabled { get; }
        public ReadOnlyArray<HoUrpIdentifier> EffectIds { get; }
        public ReadOnlyArray<PostResourceRequest> AdditionalResourceRequests { get; }
    }
}
