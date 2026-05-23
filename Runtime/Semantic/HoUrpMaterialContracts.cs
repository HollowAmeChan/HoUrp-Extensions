using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.Semantic
{
    public static class HoUrpMaterialContracts
    {
        public static class Templates
        {
            public static readonly HoUrpIdentifier DebugLitMinimal = "MaterialTemplate.DebugLitMinimal";
        }

        public static class FeatureBlocks
        {
            public static readonly HoUrpIdentifier BaseColorConstant = "MaterialBlock.BaseColorConstant";
            public static readonly HoUrpIdentifier DebugNormal = "MaterialBlock.DebugNormal";
            public static readonly HoUrpIdentifier SkinSss = "MaterialBlock.SkinSss";
            public static readonly HoUrpIdentifier MaterialClass = "MaterialBlock.MaterialClass";
            public static readonly HoUrpIdentifier MaterialCustom = "MaterialBlock.MaterialCustom";
            public static readonly HoUrpIdentifier AovOutputStandard = "MaterialBlock.AovOutputStandard";
            public static readonly HoUrpIdentifier OitTransparent = "MaterialBlock.OitTransparent";
        }

        public static class Presets
        {
            public static readonly HoUrpIdentifier CharacterDebugLitSssOitReady = "MaterialPreset.Character_DebugLit_SSS_OITReady";
        }

        public static ReadOnlyArray<MaterialFeatureBlockDefinition> CreatePrototypeFeatureBlocks()
        {
            return new ReadOnlyArray<MaterialFeatureBlockDefinition>(
                new MaterialFeatureBlockDefinition(
                    FeatureBlocks.BaseColorConstant,
                    "Base Color Constant",
                    HoUrpDomain.Material,
                    new ReadOnlyArray<HoUrpIdentifier>(),
                    new ReadOnlyArray<HoUrpIdentifier>(
                        HoUrpBuiltInNames.Semantics.TransparentColor,
                        HoUrpBuiltInNames.Semantics.TransparentAlpha),
                    new ReadOnlyArray<string>("HoUrpMaterialSurface.hlsl"),
                    new ReadOnlyArray<HoUrpIdentifier>(Templates.DebugLitMinimal),
                    "Provides base color and alpha for the prototype material."),
                new MaterialFeatureBlockDefinition(
                    FeatureBlocks.DebugNormal,
                    "Debug Normal",
                    HoUrpDomain.Geometry,
                    new ReadOnlyArray<HoUrpIdentifier>(),
                    new ReadOnlyArray<HoUrpIdentifier>(HoUrpBuiltInNames.Semantics.GeometryWorldNormal),
                    new ReadOnlyArray<string>("HoUrpMaterialSurface.hlsl"),
                    new ReadOnlyArray<HoUrpIdentifier>(Templates.DebugLitMinimal),
                    "Provides a minimal world normal for forward and AOV output."),
                new MaterialFeatureBlockDefinition(
                    FeatureBlocks.SkinSss,
                    "Skin SSS",
                    HoUrpDomain.Shading,
                    new ReadOnlyArray<HoUrpIdentifier>(),
                    new ReadOnlyArray<HoUrpIdentifier>(
                        HoUrpBuiltInNames.Semantics.MaterialSssProfile,
                        HoUrpBuiltInNames.Semantics.MaterialThickness,
                        HoUrpBuiltInNames.Semantics.MaterialCurvature,
                        HoUrpBuiltInNames.Semantics.ShadingSssSourceColor,
                        HoUrpBuiltInNames.Semantics.ShadingSssWeight),
                    new ReadOnlyArray<string>("HoUrpObjectSemantic.hlsl", "HoUrpMaterialSurface.hlsl", "HoUrpMaterialAov.hlsl"),
                    new ReadOnlyArray<HoUrpIdentifier>(Templates.DebugLitMinimal),
                    "Provides minimal SSS semantics for AOV, SSS, and ScreenPost validation."),
                new MaterialFeatureBlockDefinition(
                    FeatureBlocks.MaterialClass,
                    "Material Class",
                    HoUrpDomain.Material,
                    new ReadOnlyArray<HoUrpIdentifier>(),
                    new ReadOnlyArray<HoUrpIdentifier>(HoUrpBuiltInNames.Semantics.MaterialClass),
                    new ReadOnlyArray<string>("HoUrpMaterialSurface.hlsl"),
                    new ReadOnlyArray<HoUrpIdentifier>(Templates.DebugLitMinimal),
                    "Provides Material.Class for generated material validation."),
                new MaterialFeatureBlockDefinition(
                    FeatureBlocks.MaterialCustom,
                    "Material Custom",
                    HoUrpDomain.Material,
                    new ReadOnlyArray<HoUrpIdentifier>(),
                    new ReadOnlyArray<HoUrpIdentifier>(
                        HoUrpBuiltInNames.Semantics.MaterialCustom0,
                        HoUrpBuiltInNames.Semantics.MaterialCustom1,
                        HoUrpBuiltInNames.Semantics.MaterialCustom2,
                        HoUrpBuiltInNames.Semantics.MaterialCustom3),
                    new ReadOnlyArray<string>("HoUrpMaterialSurface.hlsl"),
                    new ReadOnlyArray<HoUrpIdentifier>(Templates.DebugLitMinimal),
                    "Provides Material.Custom0-3 for generated material validation."),
                new MaterialFeatureBlockDefinition(
                    FeatureBlocks.AovOutputStandard,
                    "AOV Output Standard",
                    HoUrpDomain.Material,
                    new ReadOnlyArray<HoUrpIdentifier>(),
                    new ReadOnlyArray<HoUrpIdentifier>(
                        HoUrpBuiltInNames.Semantics.MaterialClass,
                        HoUrpBuiltInNames.Semantics.MaterialSssProfile,
                        HoUrpBuiltInNames.Semantics.MaterialThickness,
                        HoUrpBuiltInNames.Semantics.MaterialCurvature,
                        HoUrpBuiltInNames.Semantics.MaterialCustom0,
                        HoUrpBuiltInNames.Semantics.MaterialCustom1,
                        HoUrpBuiltInNames.Semantics.MaterialCustom2,
                        HoUrpBuiltInNames.Semantics.MaterialCustom3,
                        HoUrpBuiltInNames.Semantics.ShadingSssSourceColor,
                        HoUrpBuiltInNames.Semantics.ShadingSssWeight),
                    new ReadOnlyArray<string>("HoUrpObjectSemantic.hlsl", "HoUrpMaterialAov.hlsl"),
                    new ReadOnlyArray<HoUrpIdentifier>(Templates.DebugLitMinimal),
                    "Encodes material semantics into the existing AOV MRT layout."),
                new MaterialFeatureBlockDefinition(
                    FeatureBlocks.OitTransparent,
                    "OIT Transparent",
                    HoUrpDomain.Composite,
                    new ReadOnlyArray<HoUrpIdentifier>(),
                    new ReadOnlyArray<HoUrpIdentifier>(
                        HoUrpBuiltInNames.Semantics.TransparentColor,
                        HoUrpBuiltInNames.Semantics.TransparentAlpha,
                        HoUrpBuiltInNames.Semantics.TransparentCoverage,
                        HoUrpBuiltInNames.Semantics.OitAccumulationInput,
                        HoUrpBuiltInNames.Semantics.OitRevealageInput),
                    new ReadOnlyArray<string>("HoUrpMaterialOit.hlsl"),
                    new ReadOnlyArray<HoUrpIdentifier>(Templates.DebugLitMinimal),
                    "Provides an OIT-ready accumulation pass without creating OIT runtime resources."));
        }

        public static MaterialPresetDefinition CreatePrototypePreset()
        {
            return new MaterialPresetDefinition(
                Presets.CharacterDebugLitSssOitReady,
                "Character Debug Lit SSS OIT Ready",
                Templates.DebugLitMinimal,
                new ReadOnlyArray<HoUrpIdentifier>(
                    FeatureBlocks.BaseColorConstant,
                    FeatureBlocks.DebugNormal,
                    FeatureBlocks.SkinSss,
                    FeatureBlocks.MaterialClass,
                    FeatureBlocks.MaterialCustom,
                    FeatureBlocks.AovOutputStandard,
                    FeatureBlocks.OitTransparent),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Semantics.MaterialClass,
                    HoUrpBuiltInNames.Semantics.MaterialSssProfile,
                    HoUrpBuiltInNames.Semantics.MaterialThickness,
                    HoUrpBuiltInNames.Semantics.MaterialCurvature,
                    HoUrpBuiltInNames.Semantics.MaterialCustom0,
                    HoUrpBuiltInNames.Semantics.MaterialCustom1,
                    HoUrpBuiltInNames.Semantics.MaterialCustom2,
                    HoUrpBuiltInNames.Semantics.MaterialCustom3,
                    HoUrpBuiltInNames.Semantics.ShadingSssSourceColor,
                    HoUrpBuiltInNames.Semantics.ShadingSssWeight,
                    HoUrpBuiltInNames.Semantics.TransparentColor,
                    HoUrpBuiltInNames.Semantics.TransparentAlpha,
                    HoUrpBuiltInNames.Semantics.TransparentCoverage,
                    HoUrpBuiltInNames.Semantics.OitAccumulationInput,
                    HoUrpBuiltInNames.Semantics.OitRevealageInput),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Capabilities.SupportsOit,
                    HoUrpBuiltInNames.Capabilities.ParticipatesOit),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.ShaderPasses.UniversalForward,
                    HoUrpBuiltInNames.ShaderPasses.HoUrpAovOutput,
                    HoUrpBuiltInNames.ShaderPasses.HoUrpOitAccumulation),
                MaterialPhasePolicy.OitOnly,
                true,
                "Prototype generated-material contract for AOV, SSS, ScreenPost, and OIT-ready validation.");
        }
    }
}
