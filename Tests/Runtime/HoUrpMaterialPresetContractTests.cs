using HoUrp.Extensions.Core;
using HoUrp.Extensions.Semantic;
using NUnit.Framework;

namespace HoUrp.Extensions.Tests.Runtime
{
    public sealed class HoUrpMaterialPresetContractTests
    {
        [Test]
        public void PrototypeFeatureBlocksExposeOitReadyMaterialAbi()
        {
            ReadOnlyArray<MaterialFeatureBlockDefinition> blocks = HoUrpMaterialContracts.CreatePrototypeFeatureBlocks();

            Assert.That(blocks.Count, Is.EqualTo(14));
            Assert.That(FindBlock(blocks, HoUrpMaterialContracts.FeatureBlocks.BaseColorConstant).Id, Is.EqualTo(HoUrpMaterialContracts.FeatureBlocks.BaseColorConstant));

            MaterialFeatureBlockDefinition screenSss = FindBlock(blocks, HoUrpMaterialContracts.FeatureBlocks.ScreenSpaceSssSourceProducer);
            Assert.That(screenSss.Domain, Is.EqualTo(HoUrpDomain.Shading));
            Assert.That(screenSss.ProducedFields, Contains.Item(HoUrpBuiltInNames.Semantics.ShadingSssSourceColor));
            Assert.That(screenSss.ProducedFields, Does.Not.Contain(HoUrpBuiltInNames.Semantics.ShadingSssWeight));
            Assert.That(screenSss.RequiredIncludes, Contains.Item("HoUrpMaterialAov.hlsl"));

            MaterialFeatureBlockDefinition aovBlock = FindBlock(blocks, HoUrpMaterialContracts.FeatureBlocks.AovOutputStandard);
            Assert.That(aovBlock.ProducedFields, Contains.Item(HoUrpBuiltInNames.Semantics.ShadingSssSourceColor));
            Assert.That(aovBlock.ProducedFields, Does.Not.Contain(HoUrpBuiltInNames.Semantics.ShadingSssWeight));
            Assert.That(aovBlock.RequiredIncludes, Contains.Item("HoUrpMaterialAov.hlsl"));

            MaterialFeatureBlockDefinition oitBlock = FindBlock(blocks, HoUrpMaterialContracts.FeatureBlocks.OitTransparent);
            Assert.That(oitBlock.Id, Is.EqualTo(HoUrpMaterialContracts.FeatureBlocks.OitTransparent));
            Assert.That(oitBlock.Domain, Is.EqualTo(HoUrpDomain.Composite));
            Assert.That(oitBlock.ProducedFields, Contains.Item(HoUrpBuiltInNames.Semantics.TransparentColor));
            Assert.That(oitBlock.ProducedFields, Contains.Item(HoUrpBuiltInNames.Semantics.TransparentAlpha));
            Assert.That(oitBlock.ProducedFields, Contains.Item(HoUrpBuiltInNames.Semantics.TransparentCoverage));
            Assert.That(oitBlock.ProducedFields, Contains.Item(HoUrpBuiltInNames.Semantics.OitAccumulationInput));
            Assert.That(oitBlock.ProducedFields, Contains.Item(HoUrpBuiltInNames.Semantics.OitRevealageInput));
            Assert.That(oitBlock.RequiredIncludes, Contains.Item("HoUrpMaterialOit.hlsl"));
            Assert.That(oitBlock.CompatibleTemplates, Contains.Item(HoUrpMaterialContracts.Templates.DebugLitMinimal));
        }

        [Test]
        public void PublicFeatureBlocksExposeHoNprFacingHoUrpContractsOnly()
        {
            ReadOnlyArray<MaterialFeatureBlockDefinition> blocks = HoUrpMaterialContracts.CreatePrototypeFeatureBlocks();

            Assert.That(FindBlock(blocks, HoUrpMaterialContracts.FeatureBlocks.MaterialSemanticProducer).Domain, Is.EqualTo(HoUrpDomain.Material));
            Assert.That(FindBlock(blocks, HoUrpMaterialContracts.FeatureBlocks.UrpMainLightInput).Domain, Is.EqualTo(HoUrpDomain.Lighting));
            Assert.That(FindBlock(blocks, HoUrpMaterialContracts.FeatureBlocks.UrpAdditionalLightInput).Domain, Is.EqualTo(HoUrpDomain.Lighting));
            Assert.That(FindBlock(blocks, HoUrpMaterialContracts.FeatureBlocks.IndirectLightInput).Domain, Is.EqualTo(HoUrpDomain.Lighting));
            Assert.That(FindBlock(blocks, HoUrpMaterialContracts.FeatureBlocks.ScreenAoReceiver).Domain, Is.EqualTo(HoUrpDomain.Lighting));

            MaterialFeatureBlockDefinition hoShadow = FindBlock(blocks, HoUrpMaterialContracts.FeatureBlocks.HoShadowReceiver);
            Assert.That(hoShadow.Domain, Is.EqualTo(HoUrpDomain.Lighting));
            Assert.That(hoShadow.RequiredIncludes, Contains.Item("HoUrpShadowCastSampling.hlsl"));
            Assert.That(hoShadow.Description, Does.Contain("without writing URP main-light shadow"));

            MaterialFeatureBlockDefinition oitAccumulation = FindBlock(blocks, HoUrpMaterialContracts.FeatureBlocks.OitAccumulationOutput);
            Assert.That(oitAccumulation.Domain, Is.EqualTo(HoUrpDomain.Composite));
            Assert.That(oitAccumulation.ProducedFields, Contains.Item(HoUrpBuiltInNames.Semantics.OitAccumulationInput));
            Assert.That(oitAccumulation.ProducedFields, Contains.Item(HoUrpBuiltInNames.Semantics.OitRevealageInput));

            foreach (MaterialFeatureBlockDefinition block in blocks)
            {
                Assert.That(block.Id.ToString(), Does.Not.Contain("LilToon"));
                Assert.That(block.Id.ToString(), Does.Not.Contain("LilPbr"));
                Assert.That(block.Id.ToString(), Does.Not.Contain("Hair"));
                Assert.That(block.Id.ToString(), Does.Not.Contain("Outline"));
            }
        }

        [Test]
        public void PrototypePresetDeclaresAovSssAndOitReadyPasses()
        {
            MaterialPresetDefinition preset = HoUrpMaterialContracts.CreatePrototypePreset();

            Assert.That(preset.Id, Is.EqualTo(HoUrpMaterialContracts.Presets.CharacterDebugLitSssOitReady));
            Assert.That(preset.Template, Is.EqualTo(HoUrpMaterialContracts.Templates.DebugLitMinimal));
            Assert.That(preset.IsPrototype, Is.True);
            Assert.That(preset.PhasePolicy, Is.EqualTo(MaterialPhasePolicy.OitOnly));
            Assert.That(preset.FeatureBlocks, Contains.Item(HoUrpMaterialContracts.FeatureBlocks.AovOutputStandard));
            Assert.That(preset.FeatureBlocks, Contains.Item(HoUrpMaterialContracts.FeatureBlocks.ScreenSpaceSssSourceProducer));
            Assert.That(preset.FeatureBlocks, Contains.Item(HoUrpMaterialContracts.FeatureBlocks.HoShadowReceiver));
            Assert.That(preset.FeatureBlocks, Contains.Item(HoUrpMaterialContracts.FeatureBlocks.OitAccumulationOutput));
            Assert.That(preset.FeatureBlocks, Contains.Item(HoUrpMaterialContracts.FeatureBlocks.OitTransparent));
            Assert.That(preset.ProducedSemantics, Contains.Item(HoUrpBuiltInNames.Semantics.MaterialClass));
            Assert.That(preset.ProducedSemantics, Contains.Item(HoUrpBuiltInNames.Semantics.ShadingSssSourceColor));
            Assert.That(preset.ProducedSemantics, Does.Not.Contain(HoUrpBuiltInNames.Semantics.ShadingSssWeight));
            Assert.That(preset.ProducedSemantics, Contains.Item(HoUrpBuiltInNames.Semantics.TransparentAlpha));
            Assert.That(preset.ProducedSemantics, Contains.Item(HoUrpBuiltInNames.Semantics.OitAccumulationInput));
            Assert.That(preset.RequiredCapabilities, Contains.Item(HoUrpBuiltInNames.Capabilities.SupportsOit));
            Assert.That(preset.RequiredCapabilities, Contains.Item(HoUrpBuiltInNames.Capabilities.ParticipatesOit));
            Assert.That(preset.SupportedPasses, Contains.Item(HoUrpBuiltInNames.ShaderPasses.UniversalForward));
            Assert.That(preset.SupportedPasses, Contains.Item(HoUrpBuiltInNames.ShaderPasses.HoUrpAovOutput));
            Assert.That(preset.SupportedPasses, Contains.Item(HoUrpBuiltInNames.ShaderPasses.HoUrpOitAccumulation));
        }

        private static MaterialFeatureBlockDefinition FindBlock(
            ReadOnlyArray<MaterialFeatureBlockDefinition> blocks,
            HoUrpIdentifier id)
        {
            foreach (MaterialFeatureBlockDefinition block in blocks)
            {
                if (block.Id == id)
                {
                    return block;
                }
            }

            Assert.Fail("Missing material feature block: " + id);
            return default;
        }
    }
}
