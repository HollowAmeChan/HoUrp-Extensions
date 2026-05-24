using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace HoUrp.Extensions.Tests.Runtime
{
    public sealed class HoUrpMaterialShaderAbiTests
    {
        private const string PackageRoot = "Packages/com.hollow.hourp-extensions";
        private const string ShaderPath = PackageRoot + "/Runtime/Shaders/Generated/HoUrpDebugLitMinimal.shader";
        private const string OitCompositeShaderPath = PackageRoot + "/Runtime/Shaders/Hidden/HoURP/OIT/WeightedComposite.shader";
        private const string SubsurfaceScatteringShaderPath = PackageRoot + "/Runtime/Shaders/Hidden/HoURP/SSS/SubsurfaceScattering.shader";
        private const string ShadowCastSamplingPath = PackageRoot + "/Runtime/Shaders/ShaderLibrary/HoUrpShadowCastSampling.hlsl";
        private const string ShadowCastDebugPath = PackageRoot + "/Runtime/Shaders/Hidden/HoURP/ShadowCast/Debug.shader";
        private const string ShadowCastReceiverDebugPath = PackageRoot + "/Runtime/Shaders/Generated/HoUrpShadowCastReceiverDebug.shader";
        private const string HoNprPackageRoot = "Packages/com.hollow.honpr";

        [Test]
        public void MaterialShaderAbiIncludeFilesExist()
        {
            Assert.That(File.Exists(ToAbsolutePath(PackageRoot + "/Runtime/Shaders/ShaderLibrary/HoUrpMaterialSurface.hlsl")), Is.True);
            Assert.That(File.Exists(ToAbsolutePath(PackageRoot + "/Runtime/Shaders/ShaderLibrary/HoUrpMaterialAov.hlsl")), Is.True);
            Assert.That(File.Exists(ToAbsolutePath(PackageRoot + "/Runtime/Shaders/ShaderLibrary/HoUrpMaterialOit.hlsl")), Is.True);
            Assert.That(File.Exists(ToAbsolutePath(PackageRoot + "/Runtime/Shaders/ShaderLibrary/HoUrpObjectSemantic.hlsl")), Is.True);
            Assert.That(File.Exists(ToAbsolutePath(ShadowCastSamplingPath)), Is.True);
            Assert.That(File.Exists(ToAbsolutePath(ShadowCastReceiverDebugPath)), Is.True);
        }

        [Test]
        public void GeneratedDebugLitShaderDeclaresAovAndOitReadyPasses()
        {
            string shaderText = ReadPackageText(ShaderPath);

            Assert.That(shaderText, Does.Contain("Shader \"HoURP/Generated/HoUrpDebugLitMinimal\""));
            Assert.That(shaderText, Does.Contain("Name \"UniversalForward\""));
            Assert.That(shaderText, Does.Contain("\"LightMode\" = \"UniversalForward\""));
            Assert.That(shaderText, Does.Contain("Name \"HoUrpAovOutput\""));
            Assert.That(shaderText, Does.Contain("\"LightMode\" = \"HoUrpAovOutput\""));
            Assert.That(shaderText, Does.Contain("Name \"HoUrpOitAccumulation\""));
            Assert.That(shaderText, Does.Contain("\"LightMode\" = \"HoUrpOitAccumulation\""));
            Assert.That(shaderText, Does.Contain("Name \"ShadowCaster\""));
            Assert.That(shaderText, Does.Contain("\"LightMode\" = \"ShadowCaster\""));
            Assert.That(shaderText, Does.Contain("HoUrpShadowCastSampling.hlsl"));
            Assert.That(shaderText, Does.Contain("HoUrpSampleShadowCastAttenuation"));
            Assert.That(shaderText, Does.Contain("HoUrpTransparentOutputData"));
            Assert.That(shaderText, Does.Contain("HoUrpOitAccumulationData"));
            Assert.That(shaderText, Does.Contain("transparentData.color = surface.baseColor * (0.25h + 0.75h * ndotl) * shadowAttenuation;"));
            Assert.That(ReadPackageText(PackageRoot + "/Runtime/Shaders/ShaderLibrary/HoUrpMaterialOit.hlsl"), Does.Contain("weightedColor"));
            Assert.That(ReadPackageText(PackageRoot + "/Runtime/Shaders/ShaderLibrary/HoUrpMaterialOit.hlsl"), Does.Contain("weightedAlpha"));
            Assert.That(ReadPackageText(PackageRoot + "/Runtime/Shaders/ShaderLibrary/HoUrpMaterialOit.hlsl"), Does.Contain("_HoUrpOitWeight"));
            Assert.That(ReadPackageText(PackageRoot + "/Runtime/Shaders/ShaderLibrary/HoUrpMaterialOit.hlsl"), Does.Contain("_HoUrpOitAlphaClipThreshold"));
            Assert.That(shaderText, Does.Contain("_HoUrpOitActive"));
            Assert.That(shaderText, Does.Contain("clip(0.5h - half(_HoUrpOitActive * _HoUrpSupportsOit * _HoUrpParticipatesOit))"));
            Assert.That(shaderText, Does.Contain("_HoUrpGeneratedMaterialClass"));
            Assert.That(shaderText, Does.Not.Contain("float _HoUrpMaterialClass;"));
            Assert.That(shaderText, Does.Not.Contain("_HoUrpAovMaskWeight(\""));
            Assert.That(shaderText, Does.Not.Contain("_HoUrpObjectId(\""));
            Assert.That(shaderText, Does.Not.Contain("_HoUrpObjectGroupId(\""));
            Assert.That(shaderText, Does.Not.Contain("_HoUrpObjectFlags(\""));
            Assert.That(shaderText, Does.Not.Contain("_HoUrpObjectCustomMask(\""));
            Assert.That(shaderText, Does.Contain("HoUrpResolveObjectSemanticData"));
            Assert.That(shaderText, Does.Contain("SV_Target5"));
            Assert.That(shaderText, Does.Contain("SV_Target6"));
            Assert.That(shaderText, Does.Contain("half4 diffuse : SV_Target6"));
            Assert.That(shaderText, Does.Not.Contain("SV_Target7"));
        }

        [Test]
        public void MaterialAovAbiDoesNotScaleMaterialSemanticIdsByCoverage()
        {
            string aovAbiText = ReadPackageText(PackageRoot + "/Runtime/Shaders/ShaderLibrary/HoUrpMaterialAov.hlsl");

            Assert.That(aovAbiText, Does.Contain("semanticGate = coverage > 0.0h ? 1.0h : 0.0h"));
            Assert.That(aovAbiText, Does.Contain("data.surfaceData = half4("));
            Assert.That(aovAbiText, Does.Contain(") * semanticGate;"));
            Assert.That(aovAbiText, Does.Not.Contain("data.materialCustom0_3 = saturate(semantic.materialCustom0_3) * coverage;"));
            Assert.That(aovAbiText, Does.Not.Contain("sssSource"));
            Assert.That(aovAbiText, Does.Not.Contain("semantic.sssWeight"));
            Assert.That(aovAbiText, Does.Contain("data.diffuse = half4(max(semantic.sssSourceColor, 0.0h) * coverage, 0.0h);"));
            Assert.That(aovAbiText, Does.Contain("diffuse"));
        }

        [Test]
        public void GeneratedDebugLitShaderDoesNotExposeOldOitAbi()
        {
            string shaderText = ReadPackageText(ShaderPath);
            string materialOitText = ReadPackageText(PackageRoot + "/Runtime/Shaders/ShaderLibrary/HoUrpMaterialOit.hlsl");

            Assert.That(shaderText, Does.Not.Contain("lilToonOIT"));
            Assert.That(shaderText, Does.Not.Contain("_lilOITEnabled"));
            Assert.That(shaderText, Does.Not.Contain("_lilOITActive"));
            Assert.That(shaderText, Does.Not.Contain("_lilHoAov"));
            Assert.That(shaderText, Does.Not.Contain("_HoAov"));
            Assert.That(materialOitText, Does.Not.Contain("_lilOIT"));
            Assert.That(materialOitText, Does.Not.Contain("lilWeightedOIT"));
        }

        [Test]
        public void WeightedOitCompositeShaderUsesNewRuntimeAbiOnly()
        {
            string shaderText = ReadPackageText(OitCompositeShaderPath);

            Assert.That(shaderText, Does.Contain("Shader \"Hidden/HoURP/OIT/WeightedComposite\""));
            Assert.That(shaderText, Does.Contain("_HoUrpOitCompositeSourceTexture"));
            Assert.That(shaderText, Does.Contain("_HoUrpOitAccumulationTexture"));
            Assert.That(shaderText, Does.Contain("_HoUrpOitRevealageTexture"));
            Assert.That(shaderText, Does.Not.Contain("_lilOIT"));
            Assert.That(shaderText, Does.Not.Contain("Hidden/lilToon/URP/WeightedOITComposite"));
        }

        [Test]
        public void SubsurfaceScatteringShaderUsesObjectReceiverFlagAsRuntimeGate()
        {
            string shaderText = ReadPackageText(SubsurfaceScatteringShaderPath);

            Assert.That(shaderText, Does.Contain("TEXTURE2D_X(_HoUrpAovMaskIdTexture)"));
            Assert.That(shaderText, Does.Contain("PickLowObjectFlag(maskId.a, 4.0h)"));
            Assert.That(shaderText, Does.Contain("maskId.r * receivesSss * validNormal * thicknessGate * profileGate"));
            Assert.That(shaderText, Does.Contain("diffuse.rgb * receivesSss"));
            Assert.That(shaderText, Does.Contain("if (centerSource.a <= 0.0001h)"));
            Assert.That(shaderText, Does.Contain("return half4(centerSceneColor, 0.0h);"));
        }

        [Test]
        public void ShadowCastSamplingShaderUsesNewRuntimeAbiOnly()
        {
            string samplingText = ReadPackageText(ShadowCastSamplingPath);
            string debugText = ReadPackageText(ShadowCastDebugPath);
            string receiverDebugText = ReadPackageText(ShadowCastReceiverDebugPath);

            Assert.That(samplingText, Does.Contain("HoUrpSampleShadowCastAttenuation"));
            Assert.That(samplingText, Does.Contain("_HoUrpShadowCastAtlas"));
            Assert.That(samplingText, Does.Contain("_HoUrpShadowCastSecondDirectionalAtlas"));
            Assert.That(samplingText, Does.Contain("HoUrpSampleShadowCastPunctual"));
            Assert.That(samplingText, Does.Contain("HoUrpSampleShadowCastSecondDirectional"));
            Assert.That(samplingText, Does.Not.Contain("_HoShadowCast"));
            Assert.That(debugText, Does.Contain("Shader \"Hidden/HoURP/ShadowCast/Debug\""));
            Assert.That(receiverDebugText, Does.Contain("Shader \"HoURP/Generated/HoUrpShadowCastReceiverDebug\""));
            Assert.That(receiverDebugText, Does.Contain("\"LightMode\" = \"UniversalForward\""));
            Assert.That(receiverDebugText, Does.Contain("\"LightMode\" = \"ShadowCaster\""));
            Assert.That(receiverDebugText, Does.Contain("HoUrpShadowCastSampling.hlsl"));
            Assert.That(receiverDebugText, Does.Contain("HoUrpSampleShadowCastPunctual"));
            Assert.That(receiverDebugText, Does.Contain("HoUrpSampleShadowCastSecondDirectional"));
            Assert.That(receiverDebugText, Does.Contain("HoUrpSampleShadowCastAttenuation"));
        }

        [Test]
        public void HoNprSourceDeclarationsExposeScreenSpaceSssProducer()
        {
            string blockText = ReadPackageText(HoNprPackageRoot + "/ShaderSystem/Features/Subsurface/ScreenSpaceSssSourceProducer/Block.honprblock");
            string paramsText = ReadPackageText(HoNprPackageRoot + "/ShaderSystem/Features/Subsurface/ScreenSpaceSssSourceProducer/Parameters.honprparams");
            string presetText = ReadPackageText(HoNprPackageRoot + "/ShaderSystem/Presets/Character/Character_LilToon_Skin_SSS.honprpreset");
            string fsssPresetText = ReadPackageText(HoNprPackageRoot + "/ShaderSystem/Presets/Character/Character_LilToon_Skin_fSSS.honprpreset");
            string fsssUiText = ReadPackageText(HoNprPackageRoot + "/ShaderSystem/Features/PresetUi/Character/Character_LilToon_Skin_fSSS.honprui");
            string sssUiText = ReadPackageText(HoNprPackageRoot + "/ShaderSystem/Features/PresetUi/Character/Character_LilToon_Skin_SSS.honprui");
            string sourceInlineTemplateText = ReadPackageText(HoNprPackageRoot + "/ShaderSystem/Templates/Character/CharacterLilToonSourceInline.hlsl.template");
            string generatedShaderText = ReadPackageText(HoNprPackageRoot + "/Shaders/Generated/LilToon/Skin_SSS.shader");
            string generatedFsssShaderText = ReadPackageText(HoNprPackageRoot + "/Shaders/Generated/LilToon/Skin_fSSS.shader");

            Assert.That(blockText, Does.Contain("block MaterialBlock.ScreenSpaceSssSourceProducer"));
            Assert.That(blockText, Does.Contain("produces Material.SssProfile Material.Thickness Material.Curvature Shading.SssSourceColor Aov.Diffuse"));
            Assert.That(blockText, Does.Not.Contain("Shading.SssWeight"));
            Assert.That(blockText, Does.Contain("HONPR_HAS_SCREEN_SPACE_SSS_SOURCE"));
            Assert.That(paramsText, Does.Contain("_HoUrpGeneratedMaterialSssProfile"));
            Assert.That(paramsText, Does.Contain("_HoUrpGeneratedMaterialThickness"));
            Assert.That(paramsText, Does.Contain("_HoUrpGeneratedMaterialCurvature"));
            Assert.That(paramsText, Does.Contain("_HoUrpGeneratedSssSourceColor"));
            Assert.That(paramsText, Does.Not.Contain("_HoUrpGeneratedSssWeight"));
            Assert.That(presetText, Does.Contain("preset MaterialPreset.Character_LilToon_Skin_SSS"));
            Assert.That(presetText, Does.Contain("shaderName \"HoNpr/Character_LilToon_Skin_SSS\""));
            Assert.That(presetText, Does.Contain("MaterialBlock.ScreenSpaceSssSourceProducer"));
            Assert.That(presetText, Does.Contain("HoUrpAovOutput"));
            Assert.That(presetText, Does.Contain("Aov.Diffuse"));
            Assert.That(sssUiText, Does.Contain("ui MaterialUi.Character_LilToon_Skin_SSS for MaterialPreset.Character_LilToon_Skin_SSS"));
            Assert.That(sssUiText, Does.Contain("_HoUrpGeneratedSssSourceColor"));
            Assert.That(sssUiText, Does.Contain("_HoUrpGeneratedMaterialSssProfile"));
            Assert.That(sssUiText, Does.Contain("_HoUrpGeneratedMaterialThickness"));
            Assert.That(sssUiText, Does.Not.Contain("_HoNprForwardThinSss"));
            Assert.That(sssUiText, Does.Not.Contain("_HoUrpGeneratedSssWeight"));
            Assert.That(sourceInlineTemplateText, Does.Contain("PRESET_Character_LilToon_Skin_SSS"));
            Assert.That(sourceInlineTemplateText, Does.Contain("HoNprCharacterLilToonSkinSSS.hlsl"));
            Assert.That(generatedShaderText, Does.Contain("Shader \"HoNpr/Character_LilToon_Skin_SSS\""));
            Assert.That(generatedShaderText, Does.Contain("MaterialBlock.ScreenSpaceSssSourceProducer"));
            Assert.That(generatedShaderText, Does.Contain("HoNprCharacterLilToonSkinSSS.hlsl"));
            Assert.That(generatedShaderText, Does.Contain("Name \"HoUrpAovOutput\""));
            Assert.That(generatedShaderText, Does.Contain("_HoUrpGeneratedSssSourceColor"));
            Assert.That(generatedShaderText, Does.Not.Contain("MaterialBlock.ForwardThinSss"));
            Assert.That(generatedShaderText, Does.Not.Contain("_HoNprForwardThinSss"));
            Assert.That(generatedShaderText, Does.Not.Contain("_HoUrpGeneratedSssWeight"));
            Assert.That(presetText, Does.Not.Contain("MaterialBlock.ForwardThinSss"));
            Assert.That(fsssPresetText, Does.Contain("MaterialBlock.ForwardThinSss"));
            Assert.That(fsssPresetText, Does.Not.Contain("MaterialBlock.SssSourceProducer"));
            Assert.That(fsssPresetText, Does.Not.Contain("MaterialBlock.ScreenSpaceSssSourceProducer"));
            Assert.That(fsssPresetText, Does.Not.Contain("Shading.SssSourceColor"));
            Assert.That(fsssPresetText, Does.Not.Contain("Shading.SssWeight"));
            Assert.That(fsssPresetText, Does.Not.Contain("Material.SssProfile"));
            Assert.That(fsssPresetText, Does.Not.Contain("Material.Thickness"));
            Assert.That(fsssPresetText, Does.Not.Contain("Material.Curvature"));
            Assert.That(fsssUiText, Does.Contain("_HoNprForwardThinSssColor"));
            Assert.That(fsssUiText, Does.Contain("_HoNprForwardThinSssThickness"));
            Assert.That(fsssUiText, Does.Contain("_HoNprForwardThinSssWeight"));
            Assert.That(fsssUiText, Does.Not.Contain("_HoUrpGeneratedMaterialSssProfile"));
            Assert.That(fsssUiText, Does.Not.Contain("_HoUrpGeneratedMaterialThickness"));
            Assert.That(fsssUiText, Does.Not.Contain("_HoUrpGeneratedMaterialCurvature"));
            Assert.That(generatedFsssShaderText, Does.Contain("MaterialBlock.ForwardThinSss"));
            Assert.That(generatedFsssShaderText, Does.Not.Contain("MaterialBlock.ScreenSpaceSssSourceProducer"));
            Assert.That(generatedFsssShaderText, Does.Not.Contain("_HoUrpGeneratedMaterialSssProfile"));
            Assert.That(generatedFsssShaderText, Does.Not.Contain("_HoUrpGeneratedMaterialThickness"));
            Assert.That(generatedFsssShaderText, Does.Not.Contain("_HoUrpGeneratedMaterialCurvature"));
            Assert.That(generatedFsssShaderText, Does.Not.Contain("_HoUrpGeneratedSssSourceColor"));
        }

        [Test]
        public void HoNprAssembliesUseHoUrpShadowCastSamplingThroughWrapper()
        {
            string includeRegistryText = ReadPackageText(HoNprPackageRoot + "/ShaderSystem/Includes/INCLUDE_REGISTRY.honprinclude");
            string blockText = ReadPackageText(HoNprPackageRoot + "/ShaderSystem/Features/Lighting/HoShadowReceiver/Block.honprblock");
            string wrapperText = ReadPackageText(HoNprPackageRoot + "/Shaders/ShaderLibrary/Lighting/HoNprHoUrpShadowReceiver.hlsl");
            string characterAssemblyText = ReadPackageText(HoNprPackageRoot + "/Shaders/ShaderLibrary/Assemblies/CharacterLilToon/HoNprCharacterLilToonShared.hlsl");
            string environmentAssemblyText = ReadPackageText(HoNprPackageRoot + "/Shaders/ShaderLibrary/Assemblies/EnvironmentLilPbr/HoNprEnvironmentLilPbr.hlsl");

            Assert.That(includeRegistryText, Does.Contain("include HoNpr.HoUrpShadowReceiver"));
            Assert.That(blockText, Does.Contain("requires include HoNpr.LightingInput HoNpr.HoUrpShadowReceiver"));
            Assert.That(wrapperText, Does.Contain("HoUrpShadowCastSampling.hlsl"));
            Assert.That(wrapperText, Does.Contain("HoUrpSampleShadowCastAttenuation"));
            Assert.That(wrapperText, Does.Not.Contain("_HoUrpShadowCastAtlas"));
            Assert.That(characterAssemblyText, Does.Contain("HoNprHoUrpShadowReceiver.hlsl"));
            Assert.That(characterAssemblyText, Does.Contain("HoNprSampleHoUrpShadowReceiver(input.positionWS, surface.normalWS)"));
            Assert.That(characterAssemblyText, Does.Not.Contain("HoNprResolveHoShadowReceiver(lighting, 1.0h)"));
            Assert.That(environmentAssemblyText, Does.Contain("HoNprHoUrpShadowReceiver.hlsl"));
            Assert.That(environmentAssemblyText, Does.Contain("HoNprSampleHoUrpShadowReceiver(input.positionWS, surface.normalWS)"));
            Assert.That(environmentAssemblyText, Does.Not.Contain("HoNprResolveHoShadowReceiver(lighting, 1.0h)"));
            Assert.That(characterAssemblyText, Does.Not.Contain("mainLightShadow ="));
            Assert.That(environmentAssemblyText, Does.Not.Contain("mainLightShadow ="));
        }

        [Test]
        public void GeneratedDebugLitShaderCanBeResolvedByAssetDatabase()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);

            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.name, Is.EqualTo("HoURP/Generated/HoUrpDebugLitMinimal"));
        }

        [Test]
        public void ShadowCastReceiverDebugShaderCanBeResolvedByAssetDatabase()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShadowCastReceiverDebugPath);

            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.name, Is.EqualTo("HoURP/Generated/HoUrpShadowCastReceiverDebug"));
        }

        private static string ReadPackageText(string assetPath)
        {
            string resolvedPath = AssetDatabase.GUIDToAssetPath(AssetDatabase.AssetPathToGUID(assetPath));
            if (string.IsNullOrEmpty(resolvedPath))
            {
                resolvedPath = assetPath;
            }

            return File.ReadAllText(ToAbsolutePath(resolvedPath));
        }

        private static string ToAbsolutePath(string assetPath)
        {
            if (!assetPath.StartsWith("Packages/"))
            {
                return Path.GetFullPath(assetPath);
            }

            PackageInfo packageInfo = PackageInfo.FindForAssetPath(assetPath);
            if (packageInfo != null && !string.IsNullOrEmpty(packageInfo.resolvedPath))
            {
                string packagePrefix = "Packages/" + packageInfo.name + "/";
                if (assetPath.StartsWith(packagePrefix))
                {
                    string relativePath = assetPath.Substring(packagePrefix.Length).Replace('/', Path.DirectorySeparatorChar);
                    return Path.Combine(packageInfo.resolvedPath, relativePath);
                }
            }

            string siblingPackagePath = TryResolveSiblingPackagePath(assetPath);
            if (!string.IsNullOrEmpty(siblingPackagePath))
            {
                return siblingPackagePath;
            }

            string path = assetPath;
            return Path.GetFullPath(path);
        }

        private static string TryResolveSiblingPackagePath(string assetPath)
        {
            string packageName = null;
            string directoryName = null;
            if (assetPath.StartsWith("Packages/com.hollow.honpr/"))
            {
                packageName = "com.hollow.honpr";
                directoryName = "HoNpr";
            }
            else if (assetPath.StartsWith("Packages/com.hollow.hourp-extensions/"))
            {
                packageName = "com.hollow.hourp-extensions";
                directoryName = "HoUrp-Extensions";
            }

            if (string.IsNullOrEmpty(packageName))
            {
                return string.Empty;
            }

            PackageInfo currentPackage = PackageInfo.FindForAssetPath(PackageRoot);
            if (currentPackage == null || string.IsNullOrEmpty(currentPackage.resolvedPath))
            {
                return string.Empty;
            }

            DirectoryInfo parent = Directory.GetParent(currentPackage.resolvedPath);
            if (parent == null)
            {
                return string.Empty;
            }

            string relativePath = assetPath.Substring(("Packages/" + packageName + "/").Length).Replace('/', Path.DirectorySeparatorChar);
            string candidatePath = Path.Combine(parent.FullName, directoryName, relativePath);
            return File.Exists(candidatePath) ? candidatePath : string.Empty;
        }
    }
}
