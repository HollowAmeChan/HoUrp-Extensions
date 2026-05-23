using HoUrp.Extensions.Core;
using HoUrp.Extensions.Resources;

namespace HoUrp.Extensions.PostProcess
{
    public readonly struct PostResourceRequest
    {
        public PostResourceRequest(
            HoUrpIdentifier requestId,
            HoUrpIdentifier ownerFeature,
            HoUrpIdentifier ownerLayer,
            HoUrpIdentifier ownerEffect,
            PostResourceRequestKind kind,
            HoUrpIdentifier resourceId,
            ResourceFormatHint format,
            ResourceScale scale,
            HoUrpLifetime lifetime,
            ResourceClearPolicy clearPolicy,
            ReadOnlyArray<HoUrpIdentifier> readSemantics,
            HoUrpIdentifier writeSemantic,
            string debugName,
            bool required = true)
        {
            RequestId = requestId;
            OwnerFeature = ownerFeature;
            OwnerLayer = ownerLayer;
            OwnerEffect = ownerEffect;
            Kind = kind;
            ResourceId = resourceId;
            Format = format;
            Scale = scale;
            Lifetime = lifetime;
            ClearPolicy = clearPolicy;
            ReadSemantics = readSemantics;
            WriteSemantic = writeSemantic;
            DebugName = debugName ?? string.Empty;
            Required = required;
        }

        public HoUrpIdentifier RequestId { get; }
        public HoUrpIdentifier OwnerFeature { get; }
        public HoUrpIdentifier OwnerLayer { get; }
        public HoUrpIdentifier OwnerEffect { get; }
        public PostResourceRequestKind Kind { get; }
        public HoUrpIdentifier ResourceId { get; }
        public ResourceFormatHint Format { get; }
        public ResourceScale Scale { get; }
        public HoUrpLifetime Lifetime { get; }
        public ResourceClearPolicy ClearPolicy { get; }
        public ReadOnlyArray<HoUrpIdentifier> ReadSemantics { get; }
        public HoUrpIdentifier WriteSemantic { get; }
        public string DebugName { get; }
        public bool Required { get; }
    }
}
