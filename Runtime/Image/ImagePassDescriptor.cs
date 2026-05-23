using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.Image
{
    public readonly struct ImagePassDescriptor
    {
        public ImagePassDescriptor(
            HoUrpIdentifier passId,
            HoUrpIdentifier layerId,
            HoUrpIdentifier effectId,
            bool needsOriginalSource = false)
        {
            PassId = passId;
            LayerId = layerId;
            EffectId = effectId;
            NeedsOriginalSource = needsOriginalSource;
        }

        public HoUrpIdentifier PassId { get; }
        public HoUrpIdentifier LayerId { get; }
        public HoUrpIdentifier EffectId { get; }
        public bool NeedsOriginalSource { get; }
    }
}
