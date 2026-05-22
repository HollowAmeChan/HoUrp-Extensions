namespace HoUrp.Extensions.Core
{
    public static class HoUrpBuiltInNames
    {
        public static class Features
        {
            public static readonly HoUrpIdentifier AovOutput = "AovOutput";
            public static readonly HoUrpIdentifier SemanticPostProcess = "SemanticPostProcess";
            public static readonly HoUrpIdentifier DebugComposite = "DebugComposite";
        }

        public static class Semantics
        {
            public static readonly HoUrpIdentifier ObjectMaskWeight = "Object.MaskWeight";
            public static readonly HoUrpIdentifier ObjectId = "Object.Id";
            public static readonly HoUrpIdentifier ObjectGroupId = "Object.GroupId";
            public static readonly HoUrpIdentifier ObjectFlags = "Object.Flags";
            public static readonly HoUrpIdentifier ObjectCustom0 = "Object.Custom0";
            public static readonly HoUrpIdentifier ObjectCustom1 = "Object.Custom1";
            public static readonly HoUrpIdentifier ObjectCustom2 = "Object.Custom2";
            public static readonly HoUrpIdentifier ObjectCustom3 = "Object.Custom3";
            public static readonly HoUrpIdentifier ObjectCustom4 = "Object.Custom4";
            public static readonly HoUrpIdentifier ObjectCustom5 = "Object.Custom5";
            public static readonly HoUrpIdentifier ObjectCustom6 = "Object.Custom6";
            public static readonly HoUrpIdentifier ObjectCustom7 = "Object.Custom7";
            public static readonly HoUrpIdentifier GeometryWorldNormal = "Geometry.WorldNormal";
            public static readonly HoUrpIdentifier GeometryLinearDepth = "Geometry.LinearDepth";
        }

        public static class Resources
        {
            public static readonly HoUrpIdentifier AovMaskId = "Aov.MaskId";
            public static readonly HoUrpIdentifier AovNormalDepth = "Aov.NormalDepth";
            public static readonly HoUrpIdentifier AovObjectCustom0_3 = "Aov.ObjectCustom0_3";
            public static readonly HoUrpIdentifier AovObjectCustom4_7 = "Aov.ObjectCustom4_7";
        }

        public static class DebugViews
        {
            public static readonly HoUrpIdentifier AovMask = "AOV.Mask";
            public static readonly HoUrpIdentifier AovObjectId = "AOV.ObjectId";
            public static readonly HoUrpIdentifier AovLinearDepth = "AOV.LinearDepth";
            public static readonly HoUrpIdentifier AovWorldNormal = "AOV.WorldNormal";
            public static readonly HoUrpIdentifier AovObjectCustom0 = "AOV.ObjectCustom0";
            public static readonly HoUrpIdentifier AovObjectCustom1 = "AOV.ObjectCustom1";
            public static readonly HoUrpIdentifier AovObjectCustom2 = "AOV.ObjectCustom2";
            public static readonly HoUrpIdentifier AovObjectCustom3 = "AOV.ObjectCustom3";
            public static readonly HoUrpIdentifier AovObjectCustom4 = "AOV.ObjectCustom4";
            public static readonly HoUrpIdentifier AovObjectCustom5 = "AOV.ObjectCustom5";
            public static readonly HoUrpIdentifier AovObjectCustom6 = "AOV.ObjectCustom6";
            public static readonly HoUrpIdentifier AovObjectCustom7 = "AOV.ObjectCustom7";
        }

        public static class Capabilities
        {
            public static readonly HoUrpIdentifier WritesAov = "WritesAov";
            public static readonly HoUrpIdentifier RequiresAov = "RequiresAov";
            public static readonly HoUrpIdentifier SupportsDebugView = "SupportsDebugView";
        }
    }
}
