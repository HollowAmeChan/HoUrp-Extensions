using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.Semantic
{
    public readonly struct SemanticDefinition
    {
        public SemanticDefinition(
            HoUrpIdentifier id,
            HoUrpDomain domain,
            SemanticFormat format,
            HoUrpPassStage stage,
            HoUrpLifetime lifetime,
            HoUrpIdentifier producer,
            ReadOnlyArray<HoUrpIdentifier> consumers,
            HoUrpIdentifier debugView,
            HoUrpMigrationDecision migrationDecision,
            string legacySource = null,
            string description = null)
        {
            Id = id;
            Domain = domain;
            Format = format;
            Stage = stage;
            Lifetime = lifetime;
            Producer = producer;
            Consumers = consumers;
            DebugView = debugView;
            MigrationDecision = migrationDecision;
            LegacySource = legacySource ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public HoUrpIdentifier Id { get; }
        public HoUrpDomain Domain { get; }
        public SemanticFormat Format { get; }
        public HoUrpPassStage Stage { get; }
        public HoUrpLifetime Lifetime { get; }
        public HoUrpIdentifier Producer { get; }
        public ReadOnlyArray<HoUrpIdentifier> Consumers { get; }
        public HoUrpIdentifier DebugView { get; }
        public HoUrpMigrationDecision MigrationDecision { get; }
        public string LegacySource { get; }
        public string Description { get; }
    }
}
