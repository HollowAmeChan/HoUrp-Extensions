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
        {
            Id = id;
            Domain = domain;
            Enabled = enabled;
            EffectIds = effectIds;
        }

        public HoUrpIdentifier Id { get; }
        public PostEffectDomain Domain { get; }
        public bool Enabled { get; }
        public ReadOnlyArray<HoUrpIdentifier> EffectIds { get; }
    }
}
