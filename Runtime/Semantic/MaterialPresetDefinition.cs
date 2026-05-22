using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.Semantic
{
    public readonly struct MaterialPresetDefinition
    {
        public MaterialPresetDefinition(
            HoUrpIdentifier id,
            string displayName,
            HoUrpIdentifier template,
            ReadOnlyArray<HoUrpIdentifier> featureBlocks,
            ReadOnlyArray<HoUrpIdentifier> producedSemantics,
            ReadOnlyArray<HoUrpIdentifier> requiredCapabilities,
            ReadOnlyArray<HoUrpIdentifier> supportedPasses,
            MaterialPhasePolicy phasePolicy,
            bool isPrototype,
            string description = null)
        {
            Id = id;
            DisplayName = displayName ?? string.Empty;
            Template = template;
            FeatureBlocks = featureBlocks;
            ProducedSemantics = producedSemantics;
            RequiredCapabilities = requiredCapabilities;
            SupportedPasses = supportedPasses;
            PhasePolicy = phasePolicy;
            IsPrototype = isPrototype;
            Description = description ?? string.Empty;
        }

        public HoUrpIdentifier Id { get; }
        public string DisplayName { get; }
        public HoUrpIdentifier Template { get; }
        public ReadOnlyArray<HoUrpIdentifier> FeatureBlocks { get; }
        public ReadOnlyArray<HoUrpIdentifier> ProducedSemantics { get; }
        public ReadOnlyArray<HoUrpIdentifier> RequiredCapabilities { get; }
        public ReadOnlyArray<HoUrpIdentifier> SupportedPasses { get; }
        public MaterialPhasePolicy PhasePolicy { get; }
        public bool IsPrototype { get; }
        public string Description { get; }
    }
}
