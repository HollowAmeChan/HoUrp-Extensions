namespace HoUrp.Extensions.Core
{
    public static class HoUrpBuiltInNames
    {
        public static class Features
        {
            public static readonly HoUrpIdentifier AovOutput = "AovOutput";
            public static readonly HoUrpIdentifier DebugComposite = "DebugComposite";
        }

        public static class Semantics
        {
            public static readonly HoUrpIdentifier ObjectMaskWeight = "Object.MaskWeight";
            public static readonly HoUrpIdentifier ObjectId = "Object.Id";
            public static readonly HoUrpIdentifier ObjectGroupId = "Object.GroupId";
            public static readonly HoUrpIdentifier ObjectFlags = "Object.Flags";
            public static readonly HoUrpIdentifier GeometryWorldNormal = "Geometry.WorldNormal";
            public static readonly HoUrpIdentifier GeometryLinearDepth = "Geometry.LinearDepth";
        }

        public static class Resources
        {
            public static readonly HoUrpIdentifier AovMaskId = "Aov.MaskId";
            public static readonly HoUrpIdentifier AovNormalDepth = "Aov.NormalDepth";
        }

        public static class DebugViews
        {
            public static readonly HoUrpIdentifier AovMask = "AOV.Mask";
            public static readonly HoUrpIdentifier AovObjectId = "AOV.ObjectId";
            public static readonly HoUrpIdentifier AovLinearDepth = "AOV.LinearDepth";
            public static readonly HoUrpIdentifier AovWorldNormal = "AOV.WorldNormal";
        }

        public static class Capabilities
        {
            public static readonly HoUrpIdentifier WritesAov = "WritesAov";
            public static readonly HoUrpIdentifier SupportsDebugView = "SupportsDebugView";
        }
    }
}
