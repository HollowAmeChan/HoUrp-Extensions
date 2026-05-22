using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.Debugging
{
    public readonly struct DebugViewDefinition
    {
        public DebugViewDefinition(
            HoUrpIdentifier id,
            HoUrpDomain domain,
            HoUrpIdentifier sourceResource,
            HoUrpIdentifier sourceSemantic,
            HoUrpIdentifier producer,
            ReadOnlyArray<DebugDisplayMode> displayModes,
            DebugValueRange range,
            HoUrpIdentifier ownerFeature,
            string legacyReference = null,
            string description = null)
        {
            Id = id;
            Domain = domain;
            SourceResource = sourceResource;
            SourceSemantic = sourceSemantic;
            Producer = producer;
            DisplayModes = displayModes;
            Range = range;
            OwnerFeature = ownerFeature;
            LegacyReference = legacyReference ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public HoUrpIdentifier Id { get; }
        public HoUrpDomain Domain { get; }
        public HoUrpIdentifier SourceResource { get; }
        public HoUrpIdentifier SourceSemantic { get; }
        public HoUrpIdentifier Producer { get; }
        public ReadOnlyArray<DebugDisplayMode> DisplayModes { get; }
        public DebugValueRange Range { get; }
        public HoUrpIdentifier OwnerFeature { get; }
        public string LegacyReference { get; }
        public string Description { get; }
    }
}
