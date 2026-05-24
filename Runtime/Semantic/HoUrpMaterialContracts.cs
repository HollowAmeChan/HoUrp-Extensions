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
            public static readonly HoUrpIdentifier ScreenSpaceSssSourceProducer = "MaterialBlock.ScreenSpaceSssSourceProducer";
            public static readonly HoUrpIdentifier MaterialClass = "MaterialBlock.MaterialClass";
            public static readonly HoUrpIdentifier MaterialCustom = "MaterialBlock.MaterialCustom";
            public static readonly HoUrpIdentifier MaterialSemanticProducer = "MaterialBlock.MaterialSemanticProducer";
            public static readonly HoUrpIdentifier AovOutputStandard = "MaterialBlock.AovOutputStandard";
            public static readonly HoUrpIdentifier UrpMainLightInput = "MaterialBlock.UrpMainLightInput";
            public static readonly HoUrpIdentifier UrpAdditionalLightInput = "MaterialBlock.UrpAdditionalLightInput";
            public static readonly HoUrpIdentifier IndirectLightInput = "MaterialBlock.IndirectLightInput";
            public static readonly HoUrpIdentifier ScreenAoReceiver = "MaterialBlock.ScreenAoReceiver";
            public static readonly HoUrpIdentifier HoShadowReceiver = "MaterialBlock.HoShadowReceiver";
            public static readonly HoUrpIdentifier OitTransparent = "MaterialBlock.OitTransparent";
            public static readonly HoUrpIdentifier OitAccumulationOutput = "MaterialBlock.OitAccumulationOutput";
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
                    FeatureBlocks.ScreenSpaceSssSourceProducer,
                    "Screen Space SSS Source Producer",
                    HoUrpDomain.Shading,
                    new ReadOnlyArray<HoUrpIdentifier>(),
                    new ReadOnlyArray<HoUrpIdentifier>(
                        HoUrpBuiltInNames.Semantics.MaterialSssProfile,
                        HoUrpBuiltInNames.Semantics.MaterialThickness,
                        HoUrpBuiltInNames.Semantics.MaterialCurvature,
                        HoUrpBuiltInNames.Semantics.ShadingSssSourceColor),
                    new ReadOnlyArray<string>("HoUrpMaterialSurface.hlsl", "HoUrpMaterialAov.hlsl"),
                    new ReadOnlyArray<HoUrpIdentifier>(Templates.DebugLitMinimal),
                    "Public HoURP-facing material block for contributing Aov.Diffuse input consumed by screen-space SSS. SSS weight/control remains SSS runtime-owned."),
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
                    FeatureBlocks.MaterialSemanticProducer,
                    "Material Semantic Producer",
                    HoUrpDomain.Material,
                    new ReadOnlyArray<HoUrpIdentifier>(),
                    new ReadOnlyArray<HoUrpIdentifier>(
                        HoUrpBuiltInNames.Semantics.MaterialClass,
                        HoUrpBuiltInNames.Semantics.MaterialCustom0,
                        HoUrpBuiltInNames.Semantics.MaterialCustom1,
                        HoUrpBuiltInNames.Semantics.MaterialCustom2,
                        HoUrpBuiltInNames.Semantics.MaterialCustom3),
                    new ReadOnlyArray<string>("HoUrpMaterialSurface.hlsl"),
                    new ReadOnlyArray<HoUrpIdentifier>(Templates.DebugLitMinimal),
                    "Public HoURP-facing material semantic producer for AOV surface data."),
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
                        HoUrpBuiltInNames.Semantics.ShadingSssSourceColor),
                    new ReadOnlyArray<string>("HoUrpObjectSemantic.hlsl", "HoUrpMaterialAov.hlsl"),
                    new ReadOnlyArray<HoUrpIdentifier>(Templates.DebugLitMinimal),
                    "Encodes material semantics into the existing AOV MRT layout."),
                new MaterialFeatureBlockDefinition(
                    FeatureBlocks.UrpMainLightInput,
                    "URP Main Light Input",
                    HoUrpDomain.Lighting,
                    new ReadOnlyArray<HoUrpIdentifier>(),
                    new ReadOnlyArray<HoUrpIdentifier>(),
                    new ReadOnlyArray<string>("Lighting.hlsl"),
                    new ReadOnlyArray<HoUrpIdentifier>(Templates.DebugLitMinimal),
                    "Public material block contract for consuming URP main light data."),
                new MaterialFeatureBlockDefinition(
                    FeatureBlocks.UrpAdditionalLightInput,
                    "URP Additional Light Input",
                    HoUrpDomain.Lighting,
                    new ReadOnlyArray<HoUrpIdentifier>(),
                    new ReadOnlyArray<HoUrpIdentifier>(),
                    new ReadOnlyArray<string>("Lighting.hlsl"),
                    new ReadOnlyArray<HoUrpIdentifier>(Templates.DebugLitMinimal),
                    "Public material block contract for consuming URP additional light data."),
                new MaterialFeatureBlockDefinition(
                    FeatureBlocks.IndirectLightInput,
                    "Indirect Light Input",
                    HoUrpDomain.Lighting,
                    new ReadOnlyArray<HoUrpIdentifier>(),
                    new ReadOnlyArray<HoUrpIdentifier>(),
                    new ReadOnlyArray<string>("Lighting.hlsl"),
                    new ReadOnlyArray<HoUrpIdentifier>(Templates.DebugLitMinimal),
                    "Public material block contract for consuming URP indirect light data."),
                new MaterialFeatureBlockDefinition(
                    FeatureBlocks.ScreenAoReceiver,
                    "Screen AO Receiver",
                    HoUrpDomain.Lighting,
                    new ReadOnlyArray<HoUrpIdentifier>(),
                    new ReadOnlyArray<HoUrpIdentifier>(),
                    new ReadOnlyArray<string>("HoUrpMaterialSurface.hlsl"),
                    new ReadOnlyArray<HoUrpIdentifier>(Templates.DebugLitMinimal),
                    "Public material block contract for receiving screen-space AO attenuation."),
                new MaterialFeatureBlockDefinition(
                    FeatureBlocks.HoShadowReceiver,
                    "Ho Shadow Receiver",
                    HoUrpDomain.Lighting,
                    new ReadOnlyArray<HoUrpIdentifier>(),
                    new ReadOnlyArray<HoUrpIdentifier>(),
                    new ReadOnlyArray<string>("HoUrpShadowCastSampling.hlsl"),
                    new ReadOnlyArray<HoUrpIdentifier>(Templates.DebugLitMinimal),
                    "Public material block contract for receiving HoURP ShadowCast through the sampling ABI without writing URP main-light shadow."),
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
                    "Provides an OIT-ready accumulation pass without creating OIT runtime resources."),
                new MaterialFeatureBlockDefinition(
                    FeatureBlocks.OitAccumulationOutput,
                    "OIT Accumulation Output",
                    HoUrpDomain.Composite,
                    new ReadOnlyArray<HoUrpIdentifier>(
                        HoUrpBuiltInNames.Semantics.TransparentColor,
                        HoUrpBuiltInNames.Semantics.TransparentAlpha),
                    new ReadOnlyArray<HoUrpIdentifier>(
                        HoUrpBuiltInNames.Semantics.OitAccumulationInput,
                        HoUrpBuiltInNames.Semantics.OitRevealageInput),
                    new ReadOnlyArray<string>("HoUrpMaterialOit.hlsl"),
                    new ReadOnlyArray<HoUrpIdentifier>(Templates.DebugLitMinimal),
                    "Public material block contract for writing the HoUrpOitAccumulation pass payload."));
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
                    FeatureBlocks.ScreenSpaceSssSourceProducer,
                    FeatureBlocks.MaterialClass,
                    FeatureBlocks.MaterialCustom,
                    FeatureBlocks.MaterialSemanticProducer,
                    FeatureBlocks.AovOutputStandard,
                    FeatureBlocks.UrpMainLightInput,
                    FeatureBlocks.UrpAdditionalLightInput,
                    FeatureBlocks.IndirectLightInput,
                    FeatureBlocks.ScreenAoReceiver,
                    FeatureBlocks.HoShadowReceiver,
                    FeatureBlocks.OitTransparent,
                    FeatureBlocks.OitAccumulationOutput),
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
