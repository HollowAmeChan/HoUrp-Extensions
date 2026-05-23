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
        private const string ShadowCastSamplingPath = PackageRoot + "/Runtime/Shaders/ShaderLibrary/HoUrpShadowCastSampling.hlsl";
        private const string ShadowCastDebugPath = PackageRoot + "/Runtime/Shaders/Hidden/HoURP/ShadowCast/Debug.shader";

        [Test]
        public void MaterialShaderAbiIncludeFilesExist()
        {
            Assert.That(File.Exists(ToAbsolutePath(PackageRoot + "/Runtime/Shaders/ShaderLibrary/HoUrpMaterialSurface.hlsl")), Is.True);
            Assert.That(File.Exists(ToAbsolutePath(PackageRoot + "/Runtime/Shaders/ShaderLibrary/HoUrpMaterialAov.hlsl")), Is.True);
            Assert.That(File.Exists(ToAbsolutePath(PackageRoot + "/Runtime/Shaders/ShaderLibrary/HoUrpMaterialOit.hlsl")), Is.True);
            Assert.That(File.Exists(ToAbsolutePath(PackageRoot + "/Runtime/Shaders/ShaderLibrary/HoUrpObjectSemantic.hlsl")), Is.True);
            Assert.That(File.Exists(ToAbsolutePath(ShadowCastSamplingPath)), Is.True);
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
            Assert.That(shaderText, Does.Contain("SV_Target6"));
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
            Assert.That(aovAbiText, Does.Contain("data.sssSource = half4(max(semantic.sssSourceColor, 0.0h) * coverage"));
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
        public void ShadowCastSamplingShaderUsesNewRuntimeAbiOnly()
        {
            string samplingText = ReadPackageText(ShadowCastSamplingPath);
            string debugText = ReadPackageText(ShadowCastDebugPath);

            Assert.That(samplingText, Does.Contain("HoUrpSampleShadowCastAttenuation"));
            Assert.That(samplingText, Does.Contain("_HoUrpShadowCastAtlas"));
            Assert.That(samplingText, Does.Contain("_HoUrpShadowCastSecondDirectionalAtlas"));
            Assert.That(samplingText, Does.Contain("HoUrpSampleShadowCastPunctual"));
            Assert.That(samplingText, Does.Contain("HoUrpSampleShadowCastSecondDirectional"));
            Assert.That(samplingText, Does.Not.Contain("_HoShadowCast"));
            Assert.That(debugText, Does.Contain("Shader \"Hidden/HoURP/ShadowCast/Debug\""));
        }

        [Test]
        public void GeneratedDebugLitShaderCanBeResolvedByAssetDatabase()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);

            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.name, Is.EqualTo("HoURP/Generated/HoUrpDebugLitMinimal"));
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

            string path = assetPath;
            return Path.GetFullPath(path);
        }
    }
}
