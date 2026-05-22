using HoUrp.Extensions.Capability;
using HoUrp.Extensions.Debugging;
using HoUrp.Extensions.Features;
using HoUrp.Extensions.Resources;
using HoUrp.Extensions.Semantic;

namespace HoUrp.Extensions.Core
{
    public static class HoUrpBuiltInContracts
    {
        public static HoUrpContractRegistry CreateMinimalAovRegistry()
        {
            var registry = new HoUrpContractRegistry();
            RegisterMinimalAovContracts(registry);
            return registry;
        }

        public static void RegisterMinimalAovContracts(HoUrpContractRegistry registry)
        {
            if (registry == null)
            {
                throw new System.ArgumentNullException(nameof(registry));
            }

            RegisterCapabilities(registry);
            RegisterDebugViews(registry);
            RegisterSemantics(registry);
            RegisterResources(registry);
            RegisterFeatures(registry);
        }

        private static void RegisterCapabilities(HoUrpContractRegistry registry)
        {
            registry.Capabilities.Register(new CapabilityDefinition(
                HoUrpBuiltInNames.Capabilities.WritesAov,
                HoUrpDomain.Object,
                CapabilityOwnerKind.Object,
                "Object capability component",
                "AovOutput participation",
                true,
                "Object Capability UI",
                "HoAovSubject",
                "Allows an object to write the first AOV semantic set."));

            registry.Capabilities.Register(new CapabilityDefinition(
                HoUrpBuiltInNames.Capabilities.SupportsDebugView,
                HoUrpDomain.Debug,
                CapabilityOwnerKind.Feature,
                "FeatureDescriptor",
                "DebugView registration",
                true,
                "Feature Inspector / Debug Panel",
                "old per-feature debug modes",
                "Requires a feature to expose at least one queryable debug view."));

            registry.Capabilities.Register(new CapabilityDefinition(
                HoUrpBuiltInNames.Capabilities.RequiresAov,
                HoUrpDomain.Capability,
                CapabilityOwnerKind.Feature,
                "FeatureDescriptor",
                "AOV resource reads",
                true,
                "Feature Inspector / Debug Panel",
                "HoPost AOV rules",
                "Marks a feature as a consumer of registered AOV resources."));
        }

        private static void RegisterDebugViews(HoUrpContractRegistry registry)
        {
            registry.DebugViews.Register(new DebugViewDefinition(
                HoUrpBuiltInNames.DebugViews.AovMask,
                HoUrpDomain.Debug,
                HoUrpBuiltInNames.Resources.AovMaskId,
                HoUrpBuiltInNames.Semantics.ObjectMaskWeight,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<DebugDisplayMode>(
                    DebugDisplayMode.Replace,
                    DebugDisplayMode.Overlay,
                    DebugDisplayMode.ChannelInspect),
                DebugValueRange.ZeroToOne,
                HoUrpBuiltInNames.Features.AovOutput,
                "HoAovDebugMode.Mask",
                "Displays the AOV coverage mask."));

            registry.DebugViews.Register(new DebugViewDefinition(
                HoUrpBuiltInNames.DebugViews.AovObjectId,
                HoUrpDomain.Debug,
                HoUrpBuiltInNames.Resources.AovMaskId,
                HoUrpBuiltInNames.Semantics.ObjectId,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<DebugDisplayMode>(
                    DebugDisplayMode.Replace,
                    DebugDisplayMode.Heatmap),
                DebugValueRange.Byte,
                HoUrpBuiltInNames.Features.AovOutput,
                "HoAovDebugMode.Id",
                "Displays object ids from Aov.MaskId."));

            registry.DebugViews.Register(new DebugViewDefinition(
                HoUrpBuiltInNames.DebugViews.AovLinearDepth,
                HoUrpDomain.Debug,
                HoUrpBuiltInNames.Resources.AovNormalDepth,
                HoUrpBuiltInNames.Semantics.GeometryLinearDepth,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<DebugDisplayMode>(
                    DebugDisplayMode.Replace,
                    DebugDisplayMode.Heatmap),
                DebugValueRange.Depth,
                HoUrpBuiltInNames.Features.AovOutput,
                "HoAovDebugMode.LinearDepth",
                "Displays linear depth from Aov.NormalDepth."));

            registry.DebugViews.Register(new DebugViewDefinition(
                HoUrpBuiltInNames.DebugViews.AovWorldNormal,
                HoUrpDomain.Debug,
                HoUrpBuiltInNames.Resources.AovNormalDepth,
                HoUrpBuiltInNames.Semantics.GeometryWorldNormal,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<DebugDisplayMode>(DebugDisplayMode.Replace),
                DebugValueRange.MinusOneToOne,
                HoUrpBuiltInNames.Features.AovOutput,
                "HoAovDebugMode.WorldNormal",
                "Displays world-space normal from Aov.NormalDepth."));
        }

        private static void RegisterSemantics(HoUrpContractRegistry registry)
        {
            HoUrpIdentifier[] consumers =
            {
                HoUrpBuiltInNames.Features.SemanticPostProcess,
                HoUrpBuiltInNames.Features.DebugComposite
            };

            registry.Semantics.Register(new SemanticDefinition(
                HoUrpBuiltInNames.Semantics.ObjectMaskWeight,
                HoUrpDomain.Object,
                SemanticFormat.NormalizedFloat,
                HoUrpPassStage.GeometrySemanticAov,
                HoUrpLifetime.PerCamera,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<HoUrpIdentifier>(consumers),
                HoUrpBuiltInNames.DebugViews.AovMask,
                HoUrpMigrationDecision.KeepConceptRename,
                "_HoAovMaskWeight",
                "Object coverage mask written into Aov.MaskId."));

            registry.Semantics.Register(new SemanticDefinition(
                HoUrpBuiltInNames.Semantics.ObjectId,
                HoUrpDomain.Object,
                SemanticFormat.Byte,
                HoUrpPassStage.GeometrySemanticAov,
                HoUrpLifetime.PerCamera,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<HoUrpIdentifier>(consumers),
                HoUrpBuiltInNames.DebugViews.AovObjectId,
                HoUrpMigrationDecision.KeepConceptRename,
                "_HoAovObjectId",
                "Object id written into Aov.MaskId."));

            registry.Semantics.Register(new SemanticDefinition(
                HoUrpBuiltInNames.Semantics.ObjectGroupId,
                HoUrpDomain.Object,
                SemanticFormat.Byte,
                HoUrpPassStage.GeometrySemanticAov,
                HoUrpLifetime.PerCamera,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<HoUrpIdentifier>(consumers),
                HoUrpBuiltInNames.DebugViews.AovObjectId,
                HoUrpMigrationDecision.KeepConceptRename,
                "_HoAovGroupId",
                "Group id written into Aov.MaskId."));

            registry.Semantics.Register(new SemanticDefinition(
                HoUrpBuiltInNames.Semantics.ObjectFlags,
                HoUrpDomain.Object,
                SemanticFormat.ByteFlags,
                HoUrpPassStage.GeometrySemanticAov,
                HoUrpLifetime.PerCamera,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<HoUrpIdentifier>(consumers),
                HoUrpBuiltInNames.DebugViews.AovObjectId,
                HoUrpMigrationDecision.KeepConceptRename,
                "_HoAovFlags",
                "Object flags written into Aov.MaskId."));

            registry.Semantics.Register(new SemanticDefinition(
                HoUrpBuiltInNames.Semantics.GeometryWorldNormal,
                HoUrpDomain.Geometry,
                SemanticFormat.Float3,
                HoUrpPassStage.GeometrySemanticAov,
                HoUrpLifetime.PerCamera,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<HoUrpIdentifier>(consumers),
                HoUrpBuiltInNames.DebugViews.AovWorldNormal,
                HoUrpMigrationDecision.KeepConceptRename,
                "_lilHoAovNormalDepthTexture",
                "World normal encoded into Aov.NormalDepth."));

            registry.Semantics.Register(new SemanticDefinition(
                HoUrpBuiltInNames.Semantics.GeometryLinearDepth,
                HoUrpDomain.Geometry,
                SemanticFormat.Depth,
                HoUrpPassStage.GeometrySemanticAov,
                HoUrpLifetime.PerCamera,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<HoUrpIdentifier>(consumers),
                HoUrpBuiltInNames.DebugViews.AovLinearDepth,
                HoUrpMigrationDecision.KeepConceptRename,
                "_lilHoAovNormalDepthTexture",
                "Linear depth encoded into Aov.NormalDepth."));
        }

        private static void RegisterResources(HoUrpContractRegistry registry)
        {
            registry.Resources.Register(new ResourceDefinition(
                HoUrpBuiltInNames.Resources.AovMaskId,
                ResourceKind.Texture2D,
                HoUrpBuiltInNames.Semantics.ObjectMaskWeight,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Features.SemanticPostProcess,
                    HoUrpBuiltInNames.Features.DebugComposite),
                ResourceFormatHint.MaskRgba8,
                ResourceScale.Full,
                HoUrpLifetime.PerCamera,
                ResourceClearPolicy.ClearZero,
                HoUrpBuiltInNames.DebugViews.AovMask,
                "_lilHoAovMaskIdTexture",
                "Packed mask/id/group/flags AOV resource."));

            registry.Resources.Register(new ResourceDefinition(
                HoUrpBuiltInNames.Resources.AovNormalDepth,
                ResourceKind.Texture2D,
                HoUrpBuiltInNames.Semantics.GeometryWorldNormal,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Features.SemanticPostProcess,
                    HoUrpBuiltInNames.Features.DebugComposite),
                ResourceFormatHint.HighPrecisionRgba16Float,
                ResourceScale.Full,
                HoUrpLifetime.PerCamera,
                ResourceClearPolicy.ClearNeutralNormal,
                HoUrpBuiltInNames.DebugViews.AovWorldNormal,
                "_lilHoAovNormalDepthTexture",
                "High precision normal/depth AOV resource."));
        }

        private static void RegisterFeatures(HoUrpContractRegistry registry)
        {
            registry.Features.Register(new FeatureDescriptor(
                HoUrpBuiltInNames.Features.AovOutput,
                HoUrpDomain.Geometry,
                HoUrpPassStage.GeometrySemanticAov,
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Resources.AovMaskId,
                    HoUrpBuiltInNames.Resources.AovNormalDepth),
                new ReadOnlyArray<HoUrpIdentifier>(),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Semantics.ObjectMaskWeight,
                    HoUrpBuiltInNames.Semantics.ObjectId,
                    HoUrpBuiltInNames.Semantics.ObjectGroupId,
                    HoUrpBuiltInNames.Semantics.ObjectFlags,
                    HoUrpBuiltInNames.Semantics.GeometryWorldNormal,
                    HoUrpBuiltInNames.Semantics.GeometryLinearDepth),
                new ReadOnlyArray<HoUrpIdentifier>(),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Capabilities.WritesAov,
                    HoUrpBuiltInNames.Capabilities.SupportsDebugView),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.DebugViews.AovMask,
                    HoUrpBuiltInNames.DebugViews.AovObjectId,
                    HoUrpBuiltInNames.DebugViews.AovLinearDepth,
                    HoUrpBuiltInNames.DebugViews.AovWorldNormal),
                HoUrpMigrationDecision.KeepConceptRename,
                "Runtime/AOV/HoAovRendererFeature.cs",
                "Minimum AOV contract for mask/id and normal/depth resources."));

            registry.Features.Register(new FeatureDescriptor(
                HoUrpBuiltInNames.Features.SemanticPostProcess,
                HoUrpDomain.Composite,
                HoUrpPassStage.SemanticPost,
                new ReadOnlyArray<HoUrpIdentifier>(),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Resources.AovMaskId,
                    HoUrpBuiltInNames.Resources.AovNormalDepth),
                new ReadOnlyArray<HoUrpIdentifier>(),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Semantics.ObjectMaskWeight,
                    HoUrpBuiltInNames.Semantics.ObjectId,
                    HoUrpBuiltInNames.Semantics.ObjectGroupId,
                    HoUrpBuiltInNames.Semantics.ObjectFlags,
                    HoUrpBuiltInNames.Semantics.GeometryWorldNormal,
                    HoUrpBuiltInNames.Semantics.GeometryLinearDepth),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Capabilities.RequiresAov,
                    HoUrpBuiltInNames.Capabilities.SupportsDebugView),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.DebugViews.AovMask,
                    HoUrpBuiltInNames.DebugViews.AovObjectId,
                    HoUrpBuiltInNames.DebugViews.AovLinearDepth,
                    HoUrpBuiltInNames.DebugViews.AovWorldNormal),
                HoUrpMigrationDecision.KeepConceptRename,
                "Runtime/HoPostProcessing/HoPostProcessRendererFeature.cs",
                "Minimum read-only AOV consumer for semantic post processing."));

            registry.Features.Register(new FeatureDescriptor(
                HoUrpBuiltInNames.Features.DebugComposite,
                HoUrpDomain.Debug,
                HoUrpPassStage.DebugComposite,
                new ReadOnlyArray<HoUrpIdentifier>(),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Resources.AovMaskId,
                    HoUrpBuiltInNames.Resources.AovNormalDepth),
                new ReadOnlyArray<HoUrpIdentifier>(),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Semantics.ObjectMaskWeight,
                    HoUrpBuiltInNames.Semantics.ObjectId,
                    HoUrpBuiltInNames.Semantics.GeometryWorldNormal,
                    HoUrpBuiltInNames.Semantics.GeometryLinearDepth),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Capabilities.SupportsDebugView),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.DebugViews.AovMask,
                    HoUrpBuiltInNames.DebugViews.AovObjectId,
                    HoUrpBuiltInNames.DebugViews.AovLinearDepth,
                    HoUrpBuiltInNames.DebugViews.AovWorldNormal),
                HoUrpMigrationDecision.Replace,
                "old per-feature debug passes",
                "Central debug composite owner for registered debug views."));
        }
    }
}
