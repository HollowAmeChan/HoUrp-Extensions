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

        [Test]
        public void MaterialShaderAbiIncludeFilesExist()
        {
            Assert.That(File.Exists(ToAbsolutePath(PackageRoot + "/Runtime/Shaders/ShaderLibrary/HoUrpMaterialSurface.hlsl")), Is.True);
            Assert.That(File.Exists(ToAbsolutePath(PackageRoot + "/Runtime/Shaders/ShaderLibrary/HoUrpMaterialAov.hlsl")), Is.True);
            Assert.That(File.Exists(ToAbsolutePath(PackageRoot + "/Runtime/Shaders/ShaderLibrary/HoUrpMaterialOit.hlsl")), Is.True);
            Assert.That(File.Exists(ToAbsolutePath(PackageRoot + "/Runtime/Shaders/ShaderLibrary/HoUrpObjectSemantic.hlsl")), Is.True);
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
            Assert.That(shaderText, Does.Contain("HoUrpTransparentOutputData"));
            Assert.That(shaderText, Does.Contain("HoUrpOitAccumulationData"));
            Assert.That(ReadPackageText(PackageRoot + "/Runtime/Shaders/ShaderLibrary/HoUrpMaterialOit.hlsl"), Does.Contain("weightedColor"));
            Assert.That(ReadPackageText(PackageRoot + "/Runtime/Shaders/ShaderLibrary/HoUrpMaterialOit.hlsl"), Does.Contain("weightedAlpha"));
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

            Assert.That(shaderText, Does.Not.Contain("lilToonOIT"));
            Assert.That(shaderText, Does.Not.Contain("_lilOITEnabled"));
            Assert.That(shaderText, Does.Not.Contain("_lilOITActive"));
            Assert.That(shaderText, Does.Not.Contain("_lilHoAov"));
            Assert.That(shaderText, Does.Not.Contain("_HoAov"));
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
