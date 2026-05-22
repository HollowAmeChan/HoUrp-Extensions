using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.Features
{
    public readonly struct FeatureDescriptor
    {
        public FeatureDescriptor(
            HoUrpIdentifier id,
            HoUrpDomain domain,
            HoUrpPassStage stage,
            ReadOnlyArray<HoUrpIdentifier> producedResources,
            ReadOnlyArray<HoUrpIdentifier> consumedResources,
            ReadOnlyArray<HoUrpIdentifier> producedSemantics,
            ReadOnlyArray<HoUrpIdentifier> consumedSemantics,
            ReadOnlyArray<HoUrpIdentifier> requiredCapabilities,
            ReadOnlyArray<HoUrpIdentifier> debugViews,
            HoUrpMigrationDecision migrationDecision,
            string legacyReference = null,
            string description = null)
        {
            Id = id;
            Domain = domain;
            Stage = stage;
            ProducedResources = producedResources;
            ConsumedResources = consumedResources;
            ProducedSemantics = producedSemantics;
            ConsumedSemantics = consumedSemantics;
            RequiredCapabilities = requiredCapabilities;
            DebugViews = debugViews;
            MigrationDecision = migrationDecision;
            LegacyReference = legacyReference ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public HoUrpIdentifier Id { get; }
        public HoUrpDomain Domain { get; }
        public HoUrpPassStage Stage { get; }
        public ReadOnlyArray<HoUrpIdentifier> ProducedResources { get; }
        public ReadOnlyArray<HoUrpIdentifier> ConsumedResources { get; }
        public ReadOnlyArray<HoUrpIdentifier> ProducedSemantics { get; }
        public ReadOnlyArray<HoUrpIdentifier> ConsumedSemantics { get; }
        public ReadOnlyArray<HoUrpIdentifier> RequiredCapabilities { get; }
        public ReadOnlyArray<HoUrpIdentifier> DebugViews { get; }
        public HoUrpMigrationDecision MigrationDecision { get; }
        public string LegacyReference { get; }
        public string Description { get; }
    }
}
