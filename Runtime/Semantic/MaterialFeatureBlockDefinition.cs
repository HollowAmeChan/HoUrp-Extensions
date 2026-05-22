using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.Semantic
{
    public readonly struct MaterialFeatureBlockDefinition
    {
        public MaterialFeatureBlockDefinition(
            HoUrpIdentifier id,
            string displayName,
            HoUrpDomain domain,
            ReadOnlyArray<HoUrpIdentifier> requiredInputs,
            ReadOnlyArray<HoUrpIdentifier> producedFields,
            ReadOnlyArray<string> requiredIncludes,
            ReadOnlyArray<HoUrpIdentifier> compatibleTemplates,
            string description = null)
        {
            Id = id;
            DisplayName = displayName ?? string.Empty;
            Domain = domain;
            RequiredInputs = requiredInputs;
            ProducedFields = producedFields;
            RequiredIncludes = requiredIncludes;
            CompatibleTemplates = compatibleTemplates;
            Description = description ?? string.Empty;
        }

        public HoUrpIdentifier Id { get; }
        public string DisplayName { get; }
        public HoUrpDomain Domain { get; }
        public ReadOnlyArray<HoUrpIdentifier> RequiredInputs { get; }
        public ReadOnlyArray<HoUrpIdentifier> ProducedFields { get; }
        public ReadOnlyArray<string> RequiredIncludes { get; }
        public ReadOnlyArray<HoUrpIdentifier> CompatibleTemplates { get; }
        public string Description { get; }
    }
}
