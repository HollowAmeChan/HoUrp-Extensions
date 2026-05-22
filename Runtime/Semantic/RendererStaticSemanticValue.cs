using UnityEngine;

namespace HoUrp.Extensions.Semantic
{
    public readonly struct RendererStaticSemanticValue
    {
        public RendererStaticSemanticValue(int objectCustomMask, int groupId, int objectId, int flags)
        {
            ObjectCustomMask = ClampToByte(objectCustomMask);
            GroupId = ClampToByte(groupId);
            ObjectId = ClampToByte(objectId);
            Flags = ClampToByte(flags);
            PackedValue = PackBytes(ObjectCustomMask, GroupId, ObjectId, Flags);
        }

        public byte ObjectCustomMask { get; }

        public byte GroupId { get; }

        public byte ObjectId { get; }

        public byte Flags { get; }

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
