using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.Resources
{
    public readonly struct ResourceDefinition
    {
        public ResourceDefinition(
            HoUrpIdentifier id,
            ResourceKind kind,
            HoUrpIdentifier semantic,
            HoUrpIdentifier producerFeature,
            ReadOnlyArray<HoUrpIdentifier> consumerFeatures,
            ResourceFormatHint format,
            ResourceScale scale,
            HoUrpLifetime lifetime,
            ResourceClearPolicy clearPolicy,
            HoUrpIdentifier debugView,
            string legacyName = null,
            string description = null)
        {
            Id = id;
            Kind = kind;
            Semantic = semantic;
            ProducerFeature = producerFeature;
            ConsumerFeatures = consumerFeatures;
            Format = format;
            Scale = scale;
            Lifetime = lifetime;
            ClearPolicy = clearPolicy;
            DebugView = debugView;
            LegacyName = legacyName ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public HoUrpIdentifier Id { get; }
        public ResourceKind Kind { get; }
        public HoUrpIdentifier Semantic { get; }
        public HoUrpIdentifier ProducerFeature { get; }
        public ReadOnlyArray<HoUrpIdentifier> ConsumerFeatures { get; }
        public ResourceFormatHint Format { get; }
        public ResourceScale Scale { get; }
        public HoUrpLifetime Lifetime { get; }
        public ResourceClearPolicy ClearPolicy { get; }
        public HoUrpIdentifier DebugView { get; }
        public string LegacyName { get; }
        public string Description { get; }
    }
}
