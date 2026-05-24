using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace HoUrp.Extensions.Tests.Runtime
{
    public sealed class HoUrpFilterContractTests
    {
        private static string PackageRoot
        {
            get
            {
                string packageRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../Packages/com.hollow.hourp-extensions"));
                if (Directory.Exists(packageRoot))
                {
                    return packageRoot;
                }

                return Path.GetFullPath(Path.Combine(Application.dataPath, "../HoUrp-Extensions"));
            }
        }

        [Test]
        public void FilterKitDoesNotDeclareSchedulingAbstractions()
        {
            string filterRoot = Path.Combine(PackageRoot, "Runtime/Filter");
            Assert.That(Directory.Exists(filterRoot), Is.True);

            foreach (string path in Directory.EnumerateFiles(filterRoot, "*.*", SearchOption.AllDirectories))
            {
                string extension = Path.GetExtension(path);
                if (extension != ".cs" && extension != ".hlsl" && extension != ".shader")
                {
                    continue;
                }

                string text = File.ReadAllText(path);
                Assert.That(text, Does.Not.Contain("FilterRequest"), path);
                Assert.That(text, Does.Not.Contain("class FilterGraph"), path);
                Assert.That(text.ToLowerInvariant(), Does.Not.Contain("scheduler"), path);
            }
        }

        [Test]
        public void CommonIncludeDoesNotBindTextures()
        {
            string text = ReadPackageFile("Runtime/Filter/Shaders/HoUrpFilterCommon.hlsl");

            Assert.That(text, Does.Not.Contain("TEXTURE2D"));
            Assert.That(text, Does.Not.Contain("_CameraDepthTexture"));
            Assert.That(text, Does.Not.Contain("_HoUrpAov"));
            Assert.That(text, Does.Not.Contain("_HoUrpSss"));
        }

        [Test]
        public void GenericBlurDoesNotReadAovOrSssAbi()
        {
            string text = ReadPackageFile("Runtime/Filter/Shaders/HoUrpFilterBlur.shader");

            Assert.That(text, Does.Not.Contain("_HoUrpAov"));
            Assert.That(text, Does.Not.Contain("_HoUrpSss"));
            Assert.That(text, Does.Contain("HoURP Filter Separable Blur"));
            Assert.That(text, Does.Contain("HoURP Filter Depth Normal Aware Blur"));
        }

        [Test]
        public void SssShaderUsesFilterKitIncludesAndAvoidsLegacyAbi()
        {
            string text = ReadPackageFile("Runtime/Shaders/Hidden/HoURP/SSS/SubsurfaceScattering.shader");

            Assert.That(text, Does.Contain("Runtime/Filter/SSS/HoUrpSssFilter.hlsl"));
            Assert.That(text, Does.Contain("Runtime/Filter/Shaders/HoUrpFilterBurleyDiffusion.hlsl"));
            string sssInclude = ReadPackageFile("Runtime/Filter/SSS/HoUrpSssFilter.hlsl");
            Assert.That(sssInclude, Does.Contain("Runtime/Filter/Shaders/HoUrpFilterDepthNormalGate.hlsl"));
            Assert.That(text, Does.Not.Contain("_lilHoSSS"));
            Assert.That(text, Does.Not.Contain("_lilHoAovSssTexture"));
            Assert.That(text, Does.Not.Contain("Transmission"));
            Assert.That(text, Does.Not.Contain("_HoUrpSssTransmission"));

            int diffusionPass = text.IndexOf("Name \"HoURP SSS Profile Diffusion\"", System.StringComparison.Ordinal);
            int compositePass = text.IndexOf("Name \"HoURP SSS Composite\"", System.StringComparison.Ordinal);
            Assert.That(diffusionPass, Is.GreaterThanOrEqualTo(0));
            Assert.That(compositePass, Is.GreaterThan(diffusionPass));
            string diffusionBlock = text.Substring(diffusionPass, compositePass - diffusionPass);
            Assert.That(diffusionBlock, Does.Not.Contain("SAMPLE_TEXTURE2D_X(_HoUrpAovDiffuseTexture"));
            Assert.That(diffusionBlock, Does.Contain("SAMPLE_TEXTURE2D_X(_HoUrpSssSourceTexture"));

            string compositeBlock = text.Substring(compositePass);
            Assert.That(compositeBlock, Does.Contain("TEXTURE2D_X(_HoUrpSssSourceTexture)"));
            Assert.That(compositeBlock, Does.Contain("TEXTURE2D_X(_HoUrpSssDiffusionTexture)"));
            Assert.That(compositeBlock, Does.Contain("half compositeWeight = saturate(diffusion.a * centerMask);"));
            Assert.That(compositeBlock, Does.Contain("int debugMode = (int)round(_HoUrpSssDebugMode);"));
            Assert.That(diffusionBlock, Does.Contain("int sampleCount = clamp((int)round(_HoUrpSssParams.z), 1, 24);"));
            Assert.That(diffusionBlock, Does.Contain("HoFilterBurleySampleDiffusionProfile"));
            Assert.That(diffusionBlock, Does.Contain("HoFilterBurleyProfileWeight"));
            Assert.That(diffusionBlock, Does.Contain("HoFilterDepthGate"));
            Assert.That(diffusionBlock, Does.Contain("HoFilterNormalGate"));
            Assert.That(ReadPackageFile("Runtime/Filter/Shaders/HoUrpFilterBurleyDiffusion.hlsl"), Does.Contain("HOURP_FILTER_BURLEY_FILTER_RADIUS 16.5585"));
        }

        [Test]
        public void FilterIdsExposePassIndices()
        {
            string text = ReadPackageFile("Runtime/Filter/HoUrpFilterIds.cs");

            Assert.That(text, Does.Contain("public const int CopyPass = 0;"));
            Assert.That(text, Does.Contain("public const int SeparableBlurPass = 1;"));
            Assert.That(text, Does.Contain("public const int DepthNormalAwareBlurPass = 2;"));
            Assert.That(text, Does.Contain("public const int SssSourcePreparePass = 0;"));
            Assert.That(text, Does.Contain("public const int SssDiffusionPass = 1;"));
            Assert.That(text, Does.Contain("public const int SssCompositePass = 2;"));
        }

        [Test]
        public void SssFeatureCompositesAfterTransparentsForVisibleValidation()
        {
            string text = ReadPackageFile("Runtime/Features/SubsurfaceScatteringRendererFeature.cs");

            Assert.That(text, Does.Contain("renderPassEvent = RenderPassEvent.AfterRenderingTransparents"));
            Assert.That(text, Does.Contain("RecordGlobalTextureBinding(renderGraph, sssSourceTexture"));
            Assert.That(text, Does.Contain("HoUrpFilterIds.SssCompositePass"));
            Assert.That(text, Does.Contain("private SssDebugMode debugMode = SssDebugMode.Off;"));
            Assert.That(text, Does.Contain("private int sampleCount = 16;"));
            Assert.That(text, Does.Contain("HoUrpShaderPropertyIds.SssParams"));
            Assert.That(text, Does.Contain("HoUrpShaderPropertyIds.SssDebugMode"));
            Assert.That(text, Does.Contain("ClampRadius(enabled ? profile.diffusionRadius : fallbackRadius, 24.0f)"));
            Assert.That(text, Does.Not.Contain("PackRadius("));
            Assert.That(text, Does.Not.Contain("transmission"));
        }

        private static string ReadPackageFile(string relativePath)
        {
            return File.ReadAllText(Path.Combine(PackageRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
