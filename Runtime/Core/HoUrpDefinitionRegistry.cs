using System;
using System.Collections.Generic;

namespace HoUrp.Extensions.Core
{
    public class HoUrpDefinitionRegistry<TDefinition>
    {
        private readonly Dictionary<HoUrpIdentifier, TDefinition> definitions =
            new Dictionary<HoUrpIdentifier, TDefinition>();

        private readonly Func<TDefinition, HoUrpIdentifier> getId;

        public HoUrpDefinitionRegistry(Func<TDefinition, HoUrpIdentifier> getId)
        {
            this.getId = getId ?? throw new ArgumentNullException(nameof(getId));
        }

        public int Count => definitions.Count;

        public IReadOnlyCollection<TDefinition> Definitions => definitions.Values;

        public void Register(TDefinition definition)
        {
            HoUrpIdentifier id = getId(definition);
            if (definitions.ContainsKey(id))
            {
                throw new InvalidOperationException($"Definition '{id}' is already registered.");
            }

            definitions.Add(id, definition);
        }

        public bool TryGet(HoUrpIdentifier id, out TDefinition definition)
        {
            return definitions.TryGetValue(id, out definition);
        }

        public TDefinition Get(HoUrpIdentifier id)
        {
            if (!TryGet(id, out TDefinition definition))
            {
                throw new KeyNotFoundException($"Definition '{id}' is not registered.");
            }

            return definition;
        }

        public bool Contains(HoUrpIdentifier id)
        {
            return definitions.ContainsKey(id);
        }
    }
}
