using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.Resources
{
    public sealed class ResourceRegistry : HoUrpDefinitionRegistry<ResourceDefinition>
    {
        public ResourceRegistry()
            : base(definition => definition.Id)
        {
        }
    }
}
