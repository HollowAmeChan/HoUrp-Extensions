using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.Capability
{
    public readonly struct CapabilityDefinition
    {
        public CapabilityDefinition(
            HoUrpIdentifier id,
            HoUrpDomain domain,
            CapabilityOwnerKind ownerKind,
            string dataSource,
            string affects,
            bool defaultEnabled,
            string uiSurface,
            string legacyReference = null,
            string description = null)
        {
            Id = id;
            Domain = domain;
            OwnerKind = ownerKind;
            DataSource = dataSource ?? string.Empty;
            Affects = affects ?? string.Empty;
            DefaultEnabled = defaultEnabled;
            UiSurface = uiSurface ?? string.Empty;
            LegacyReference = legacyReference ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public HoUrpIdentifier Id { get; }
        public HoUrpDomain Domain { get; }
        public CapabilityOwnerKind OwnerKind { get; }
        public string DataSource { get; }
        public string Affects { get; }
        public bool DefaultEnabled { get; }
        public string UiSurface { get; }
        public string LegacyReference { get; }
        public string Description { get; }
    }
}
