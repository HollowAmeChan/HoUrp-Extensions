using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.Debugging
{
    public sealed class DebugViewRegistry : HoUrpDefinitionRegistry<DebugViewDefinition>
    {
        public DebugViewRegistry()
            : base(definition => definition.Id)
        {
        }
    }
}
