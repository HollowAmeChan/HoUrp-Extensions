using System;
using System.Collections.Generic;
using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.PostProcess
{
    public sealed class PostEffectRegistry
    {
        private readonly Dictionary<HoUrpIdentifier, PostEffectDefinition> definitions =
            new Dictionary<HoUrpIdentifier, PostEffectDefinition>();

        public int Count => definitions.Count;

        public IReadOnlyCollection<PostEffectDefinition> Definitions => definitions.Values;

        public void Register(PostEffectDefinition definition)
        {
            if (definitions.ContainsKey(definition.Id))
            {
                throw new InvalidOperationException($"Post effect '{definition.Id}' is already registered.");
            }

            definitions.Add(definition.Id, definition);
        }

        public bool Unregister(HoUrpIdentifier id)
        {
            return definitions.Remove(id);
        }

        public bool Contains(HoUrpIdentifier id)
        {
            return definitions.ContainsKey(id);
        }

        public bool TryGet(HoUrpIdentifier id, out PostEffectDefinition definition)
        {
            return definitions.TryGetValue(id, out definition);
        }

        public PostEffectDefinition Get(HoUrpIdentifier id)
        {
            if (!TryGet(id, out PostEffectDefinition definition))
            {
                throw new KeyNotFoundException($"Post effect '{id}' is not registered.");
            }

            return definition;
        }
    }
}
