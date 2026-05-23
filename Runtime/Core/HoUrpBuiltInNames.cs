namespace HoUrp.Extensions.Core
{
    public static class HoUrpBuiltInNames
    {
        public static class Features
        {
            public static readonly HoUrpIdentifier AovOutput = "AovOutput";
            public static readonly HoUrpIdentifier SubsurfaceScattering = "SubsurfaceScattering";
            public static readonly HoUrpIdentifier ScreenPost = "ScreenPost";
            public static readonly HoUrpIdentifier ImagePost = "ImagePost";
            public static readonly HoUrpIdentifier TransparentOit = "TransparentOit";
            public static readonly HoUrpIdentifier ShadowCast = "ShadowCast";
            public static readonly HoUrpIdentifier GeneratedMaterial = "GeneratedMaterial";
            public static readonly HoUrpIdentifier DebugComposite = "DebugComposite";
        }

        public static class PostEffects
        {
            public static readonly HoUrpIdentifier ImagePostColorAdjustPrototype = "ImagePost.ColorAdjustPrototype";
            public static readonly HoUrpIdentifier ImagePostAovCompositePrototype = "ImagePost.AovCompositePrototype";
            public static readonly HoUrpIdentifier ScreenPostRuleMaskPrototype = "ScreenPost.RuleMaskPrototype";
        }

        public static class PostInputs
        {
            public static readonly HoUrpIdentifier None = "Post.None";
            public static readonly HoUrpIdentifier PrimaryImage = "Image.Primary";
            public static readonly HoUrpIdentifier OriginalSource = "Image.OriginalSource";
            public static readonly HoUrpIdentifier History = "Image.History";
        }

        public static class PostFrameResources
        {
            public static readonly HoUrpIdentifier ImagePrimary = "Image.Primary";
            public static readonly HoUrpIdentifier ImageOriginalSource = "Image.OriginalSource";
            public static readonly HoUrpIdentifier ImageWorkA = "Image.WorkA";
            public static readonly HoUrpIdentifier ImageWorkB = "Image.WorkB";
            public static readonly HoUrpIdentifier ImageHistoryPlanned = "Image.History.Planned";
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
            public static readonly HoUrpIdentifier MaterialClass = "Material.Class";
            public static readonly HoUrpIdentifier MaterialSssProfile = "Material.SssProfile";
            public static readonly HoUrpIdentifier MaterialThickness = "Material.Thickness";
            public static readonly HoUrpIdentifier MaterialCurvature = "Material.Curvature";
            public static readonly HoUrpIdentifier MaterialUtility = "Material.Utility";
            public static readonly HoUrpIdentifier MaterialCustom0 = "Material.Custom0";
            public static readonly HoUrpIdentifier MaterialCustom1 = "Material.Custom1";
            public static readonly HoUrpIdentifier MaterialCustom2 = "Material.Custom2";
            public static readonly HoUrpIdentifier MaterialCustom3 = "Material.Custom3";
            public static readonly HoUrpIdentifier ShadingSssSourceColor = "Shading.SssSourceColor";
            public static readonly HoUrpIdentifier ShadingSssWeight = "Shading.SssWeight";
            public static readonly HoUrpIdentifier ShadingSssDiffusionColor = "Shading.SssDiffusionColor";
            public static readonly HoUrpIdentifier ShadingSssCompositeWeight = "Shading.SssCompositeWeight";
            public static readonly HoUrpIdentifier GeometryWorldNormal = "Geometry.WorldNormal";
            public static readonly HoUrpIdentifier GeometryLinearDepth = "Geometry.LinearDepth";
            public static readonly HoUrpIdentifier TransparentColor = "Transparent.Color";
            public static readonly HoUrpIdentifier TransparentAlpha = "Transparent.Alpha";
            public static readonly HoUrpIdentifier TransparentCoverage = "Transparent.Coverage";
            public static readonly HoUrpIdentifier OitAccumulationInput = "OIT.AccumulationInput";
            public static readonly HoUrpIdentifier OitRevealageInput = "OIT.RevealageInput";
            public static readonly HoUrpIdentifier ShadowCastAttenuation = "ShadowCast.Attenuation";
        }

        public static class Resources
        {
            public static readonly HoUrpIdentifier AovMaskId = "Aov.MaskId";
            public static readonly HoUrpIdentifier AovNormalDepth = "Aov.NormalDepth";
            public static readonly HoUrpIdentifier AovObjectCustom0_3 = "Aov.ObjectCustom0_3";
            public static readonly HoUrpIdentifier AovObjectCustom4_7 = "Aov.ObjectCustom4_7";
            public static readonly HoUrpIdentifier AovSurfaceData = "Aov.SurfaceData";
            public static readonly HoUrpIdentifier AovMaterialCustom0_3 = "Aov.MaterialCustom0_3";
            public static readonly HoUrpIdentifier AovSssSource = "Aov.SssSource";
            public static readonly HoUrpIdentifier SssSource = "Sss.Source";
            public static readonly HoUrpIdentifier SssDiffusion = "Sss.Diffusion";
            public static readonly HoUrpIdentifier OitOpaqueColor = "Oit.OpaqueColor";
            public static readonly HoUrpIdentifier OitAccumulation = "Oit.Accumulation";
            public static readonly HoUrpIdentifier OitRevealage = "Oit.Revealage";
            public static readonly HoUrpIdentifier OitCompositeSource = "Oit.CompositeSource";
            public static readonly HoUrpIdentifier ShadowCastAtlas = "ShadowCast.Atlas";
            public static readonly HoUrpIdentifier ShadowCastSecondDirectionalAtlas = "ShadowCast.SecondDirectionalAtlas";
        }

        public static class DebugViews
        {
            public static readonly HoUrpIdentifier None = "Debug.None";
            public static readonly HoUrpIdentifier AovMask = "AOV.Mask";
            public static readonly HoUrpIdentifier AovObjectId = "AOV.ObjectId";
            public static readonly HoUrpIdentifier AovObjectFlag0 = "AOV.ObjectFlag0";
            public static readonly HoUrpIdentifier AovObjectFlag1 = "AOV.ObjectFlag1";
            public static readonly HoUrpIdentifier AovObjectFlag2 = "AOV.ObjectFlag2";
            public static readonly HoUrpIdentifier AovObjectFlag3 = "AOV.ObjectFlag3";
            public static readonly HoUrpIdentifier AovObjectFlag4 = "AOV.ObjectFlag4";
            public static readonly HoUrpIdentifier AovObjectFlag5 = "AOV.ObjectFlag5";
            public static readonly HoUrpIdentifier AovObjectFlag6 = "AOV.ObjectFlag6";
            public static readonly HoUrpIdentifier AovObjectFlag7 = "AOV.ObjectFlag7";
            public static readonly HoUrpIdentifier AovObjectFlag8 = "AOV.ObjectFlag8";
            public static readonly HoUrpIdentifier AovObjectFlag9 = "AOV.ObjectFlag9";
            public static readonly HoUrpIdentifier AovObjectFlag10 = "AOV.ObjectFlag10";
            public static readonly HoUrpIdentifier AovObjectFlag11 = "AOV.ObjectFlag11";
            public static readonly HoUrpIdentifier AovObjectFlag12 = "AOV.ObjectFlag12";
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
            public static readonly HoUrpIdentifier AovMaterialClass = "AOV.MaterialClass";
            public static readonly HoUrpIdentifier AovSssProfile = "AOV.SssProfile";
            public static readonly HoUrpIdentifier AovThickness = "AOV.Thickness";
            public static readonly HoUrpIdentifier AovCurvature = "AOV.Curvature";
            public static readonly HoUrpIdentifier AovMaterialCustom0 = "AOV.MaterialCustom0";
            public static readonly HoUrpIdentifier AovMaterialCustom1 = "AOV.MaterialCustom1";
            public static readonly HoUrpIdentifier AovMaterialCustom2 = "AOV.MaterialCustom2";
            public static readonly HoUrpIdentifier AovMaterialCustom3 = "AOV.MaterialCustom3";
            public static readonly HoUrpIdentifier AovSssSource = "AOV.SssSource";
            public static readonly HoUrpIdentifier AovSssWeight = "AOV.SssWeight";
            public static readonly HoUrpIdentifier SssMask = "SSS.Mask";
            public static readonly HoUrpIdentifier SssSource = "SSS.Source";
            public static readonly HoUrpIdentifier SssDiffusion = "SSS.Diffusion";
            public static readonly HoUrpIdentifier SssCompositeWeight = "SSS.CompositeWeight";
            public static readonly HoUrpIdentifier SssProfileId = "SSS.ProfileId";
            public static readonly HoUrpIdentifier SssThickness = "SSS.Thickness";
            public static readonly HoUrpIdentifier SssCurvature = "SSS.Curvature";
            public static readonly HoUrpIdentifier OitAccumulation = "OIT.Accumulation";
            public static readonly HoUrpIdentifier OitRevealage = "OIT.Revealage";
            public static readonly HoUrpIdentifier ShadowCastAtlas = "ShadowCast.Atlas";
            public static readonly HoUrpIdentifier ShadowCastSecondDirectionalAtlas = "ShadowCast.SecondDirectionalAtlas";
            public static readonly HoUrpIdentifier ShadowCastAttenuation = "ShadowCast.Attenuation";
        }

        public static class Capabilities
        {
            public static readonly HoUrpIdentifier WritesAov = "WritesAov";
            public static readonly HoUrpIdentifier WritesObjectCustom = "WritesObjectCustom";
            public static readonly HoUrpIdentifier ReceivesSemanticPost = "ReceivesSemanticPost";
            public static readonly HoUrpIdentifier RequiresAov = "RequiresAov";
            public static readonly HoUrpIdentifier SupportsDebugView = "SupportsDebugView";
            public static readonly HoUrpIdentifier SupportsOit = "SupportsOit";
            public static readonly HoUrpIdentifier ParticipatesOit = "ParticipatesOit";
        }

        public static class ShaderPasses
        {
            public static readonly HoUrpIdentifier UniversalForward = "UniversalForward";
            public static readonly HoUrpIdentifier HoUrpAovOutput = "HoUrpAovOutput";
            public static readonly HoUrpIdentifier HoUrpOitAccumulation = "HoUrpOitAccumulation";
        }
    }
}
