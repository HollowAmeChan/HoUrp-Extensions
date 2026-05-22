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

            Assert.That(blocks.Count, Is.EqualTo(7));
            Assert.That(blocks[0].Id, Is.EqualTo(HoUrpMaterialContracts.FeatureBlocks.BaseColorConstant));
            Assert.That(blocks[5].Id, Is.EqualTo(HoUrpMaterialContracts.FeatureBlocks.AovOutputStandard));
            Assert.That(blocks[5].ProducedFields, Contains.Item(HoUrpBuiltInNames.Semantics.ShadingSssSourceColor));
            Assert.That(blocks[5].RequiredIncludes, Contains.Item("HoUrpMaterialAov.hlsl"));

            MaterialFeatureBlockDefinition oitBlock = blocks[6];
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
        public void PrototypePresetDeclaresAovSssAndOitReadyPasses()
        {
            MaterialPresetDefinition preset = HoUrpMaterialContracts.CreatePrototypePreset();

            Assert.That(preset.Id, Is.EqualTo(HoUrpMaterialContracts.Presets.CharacterDebugLitSssOitReady));
            Assert.That(preset.Template, Is.EqualTo(HoUrpMaterialContracts.Templates.DebugLitMinimal));
            Assert.That(preset.IsPrototype, Is.True);
            Assert.That(preset.PhasePolicy, Is.EqualTo(MaterialPhasePolicy.OitOnly));
            Assert.That(preset.FeatureBlocks, Contains.Item(HoUrpMaterialContracts.FeatureBlocks.AovOutputStandard));
            Assert.That(preset.FeatureBlocks, Contains.Item(HoUrpMaterialContracts.FeatureBlocks.OitTransparent));
            Assert.That(preset.ProducedSemantics, Contains.Item(HoUrpBuiltInNames.Semantics.MaterialClass));
            Assert.That(preset.ProducedSemantics, Contains.Item(HoUrpBuiltInNames.Semantics.ShadingSssWeight));
            Assert.That(preset.ProducedSemantics, Contains.Item(HoUrpBuiltInNames.Semantics.TransparentAlpha));
            Assert.That(preset.ProducedSemantics, Contains.Item(HoUrpBuiltInNames.Semantics.OitAccumulationInput));
            Assert.That(preset.RequiredCapabilities, Contains.Item(HoUrpBuiltInNames.Capabilities.SupportsOit));
            Assert.That(preset.RequiredCapabilities, Contains.Item(HoUrpBuiltInNames.Capabilities.ParticipatesOit));
            Assert.That(preset.SupportedPasses, Contains.Item(HoUrpBuiltInNames.ShaderPasses.UniversalForward));
            Assert.That(preset.SupportedPasses, Contains.Item(HoUrpBuiltInNames.ShaderPasses.HoUrpAovOutput));
            Assert.That(preset.SupportedPasses, Contains.Item(HoUrpBuiltInNames.ShaderPasses.HoUrpOitAccumulation));
        }
    }
}
