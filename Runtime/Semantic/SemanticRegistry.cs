using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.Semantic
{
    public sealed class SemanticRegistry : HoUrpDefinitionRegistry<SemanticDefinition>
    {
        public SemanticRegistry()
            : base(definition => definition.Id)
        {
        }
    }
}
