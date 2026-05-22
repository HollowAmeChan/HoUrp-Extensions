using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.Features
{
    public sealed class FeatureRegistry : HoUrpDefinitionRegistry<FeatureDescriptor>
    {
        public FeatureRegistry()
            : base(definition => definition.Id)
        {
        }
    }
}
