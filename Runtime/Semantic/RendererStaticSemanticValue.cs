using UnityEngine;

namespace HoUrp.Extensions.Semantic
{
    public readonly struct RendererStaticSemanticValue
    {
        public const int V1ObjectRegionMaskBits = 8;
        public const int V1ObjectFeatureFlagsBits = 13;
        public const int V1ObjectIdBits = 4;
        public const int V1GroupIdBits = 3;
        public const int V1LayoutVersion = 1;
        public const int V1MaxObjectRegionMask = (1 << V1ObjectRegionMaskBits) - 1;
        public const int V1MaxObjectFeatureFlags = (1 << V1ObjectFeatureFlagsBits) - 1;
        public const int V1ReservedFeatureBitMask = 1 << 0;
        public const int V1MaxObjectId = (1 << V1ObjectIdBits) - 1;
        public const int V1MaxGroupId = (1 << V1GroupIdBits) - 1;

        private const uint V1ValidMask = 1u << 31;
        private const int V1RegionShift = 0;
        private const int V1FeatureFlagsShift = 8;
        private const int V1ObjectIdShift = 21;
        private const int V1GroupIdShift = 25;
        private const int V1LayoutVersionShift = 29;

        public RendererStaticSemanticValue(int objectCustomMask, int groupId, int objectId, int flags)
        {
            ObjectCustomMask = ClampToByte(objectCustomMask);
            GroupId = ClampToByte(groupId);
            ObjectId = ClampToByte(objectId);
            Flags = ClampToByte(flags);
            ObjectFeatureFlags = Flags;
            IsV1 = false;
            PackedValue = PackBytes(ObjectCustomMask, GroupId, ObjectId, Flags);
        }

        private RendererStaticSemanticValue(
            byte objectCustomMask,
            byte groupId,
            byte objectId,
            byte flags,
            int objectFeatureFlags,
            uint packedValue,
            bool isV1)
        {
            ObjectCustomMask = objectCustomMask;
            GroupId = groupId;
            ObjectId = objectId;
            Flags = flags;
            ObjectFeatureFlags = Mathf.Clamp(objectFeatureFlags, 0, V1MaxObjectFeatureFlags);
            PackedValue = packedValue;
            IsV1 = isV1;
        }

        public byte ObjectCustomMask { get; }

        public byte GroupId { get; }

        public byte ObjectId { get; }

        public byte Flags { get; }

        public int ObjectFeatureFlags { get; }

        public bool IsV1 { get; }

        public uint PackedValue { get; }

        public static uint Pack(int objectCustomMask, int groupId, int objectId, int flags)
        {
            return new RendererStaticSemanticValue(objectCustomMask, groupId, objectId, flags).PackedValue;
        }

        public static RendererStaticSemanticValue FromPacked(uint packed)
        {
            return new RendererStaticSemanticValue(
                (int)(packed & 0xFFu),
                (int)((packed >> 8) & 0xFFu),
                (int)((packed >> 16) & 0xFFu),
                (int)((packed >> 24) & 0xFFu));
        }

        public static bool TryPackV1(
            int objectRegionMask,
            int objectFeatureFlags,
            int objectId,
            int groupId,
            out uint packed)
        {
            packed = 0u;
            if (objectId < 0 || objectId > V1MaxObjectId || groupId < 0 || groupId > V1MaxGroupId)
            {
                return false;
            }

            uint region = (uint)Mathf.Clamp(objectRegionMask, 0, V1MaxObjectRegionMask);
            uint features = (uint)(Mathf.Clamp(objectFeatureFlags, 0, V1MaxObjectFeatureFlags) & ~V1ReservedFeatureBitMask);
            packed = V1ValidMask
                | ((uint)V1LayoutVersion << V1LayoutVersionShift)
                | (region << V1RegionShift)
                | (features << V1FeatureFlagsShift)
                | ((uint)objectId << V1ObjectIdShift)
                | ((uint)groupId << V1GroupIdShift);
            return true;
        }

        public static bool TryUnpackV1(uint packed, out RendererStaticSemanticValue value)
        {
            value = default;
            if (!IsPackedV1(packed))
            {
                return false;
            }

            int objectFeatureFlags = (int)(((packed >> V1FeatureFlagsShift) & (uint)V1MaxObjectFeatureFlags)
                & ~(uint)V1ReservedFeatureBitMask);
            byte flags = FeatureFlagsToAovFlags(objectFeatureFlags);
            value = new RendererStaticSemanticValue(
                (byte)((packed >> V1RegionShift) & V1MaxObjectRegionMask),
                (byte)((packed >> V1GroupIdShift) & V1MaxGroupId),
                (byte)((packed >> V1ObjectIdShift) & V1MaxObjectId),
                flags,
                objectFeatureFlags,
                packed,
                true);
            return true;
        }

        public static bool IsPackedV1(uint packed)
        {
            return (packed & V1ValidMask) != 0u
                && (int)((packed >> V1LayoutVersionShift) & 0x3u) == V1LayoutVersion;
        }

        public static byte FeatureFlagsToAovFlags(int objectFeatureFlags)
        {
            int features = Mathf.Clamp(objectFeatureFlags, 0, V1MaxObjectFeatureFlags);
            return ClampToByte(features & 0xFE);
        }

        private static byte ClampToByte(int value)
        {
            return (byte)Mathf.Clamp(value, 0, 255);
        }

        private static uint PackBytes(byte objectCustomMask, byte groupId, byte objectId, byte flags)
        {
            uint packed = objectCustomMask;
            packed |= ((uint)groupId) << 8;
            packed |= ((uint)objectId) << 16;
            packed |= ((uint)flags) << 24;
            return packed;
        }
    }
}
