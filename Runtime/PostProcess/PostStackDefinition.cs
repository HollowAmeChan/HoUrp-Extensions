using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.PostProcess
{
    public readonly struct PostStackDefinition
    {
        public PostStackDefinition(ReadOnlyArray<PostLayerDefinition> layers)
        {
            Layers = layers;
        }

        public ReadOnlyArray<PostLayerDefinition> Layers { get; }
    }
}
