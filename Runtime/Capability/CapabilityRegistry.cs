using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.Capability
{
    public sealed class CapabilityRegistry : HoUrpDefinitionRegistry<CapabilityDefinition>
    {
        public CapabilityRegistry()
            : base(definition => definition.Id)
        {
        }
    }
}
