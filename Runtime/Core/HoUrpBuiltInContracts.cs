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

            RegisterObjectCustomDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom0,
                HoUrpBuiltInNames.Resources.AovObjectCustom0_3,
                HoUrpBuiltInNames.Semantics.ObjectCustom0,
                "HoAovDebugMode.ObjectCustom0",
                "Displays Object.Custom0 from Aov.ObjectCustom0_3.");

            RegisterObjectCustomDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom1,
                HoUrpBuiltInNames.Resources.AovObjectCustom0_3,
                HoUrpBuiltInNames.Semantics.ObjectCustom1,
                "HoAovDebugMode.ObjectCustom1",
                "Displays Object.Custom1 from Aov.ObjectCustom0_3.");

            RegisterObjectCustomDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom2,
                HoUrpBuiltInNames.Resources.AovObjectCustom0_3,
                HoUrpBuiltInNames.Semantics.ObjectCustom2,
                "HoAovDebugMode.ObjectCustom2",
                "Displays Object.Custom2 from Aov.ObjectCustom0_3.");

            RegisterObjectCustomDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom3,
                HoUrpBuiltInNames.Resources.AovObjectCustom0_3,
                HoUrpBuiltInNames.Semantics.ObjectCustom3,
                "HoAovDebugMode.ObjectCustom3",
                "Displays Object.Custom3 from Aov.ObjectCustom0_3.");

            RegisterObjectCustomDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom4,
                HoUrpBuiltInNames.Resources.AovObjectCustom4_7,
                HoUrpBuiltInNames.Semantics.ObjectCustom4,
                "HoAovDebugMode.ObjectCustom4",
                "Displays Object.Custom4 from Aov.ObjectCustom4_7.");

            RegisterObjectCustomDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom5,
                HoUrpBuiltInNames.Resources.AovObjectCustom4_7,
                HoUrpBuiltInNames.Semantics.ObjectCustom5,
                "HoAovDebugMode.ObjectCustom5",
                "Displays Object.Custom5 from Aov.ObjectCustom4_7.");

            RegisterObjectCustomDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom6,
                HoUrpBuiltInNames.Resources.AovObjectCustom4_7,
                HoUrpBuiltInNames.Semantics.ObjectCustom6,
                "HoAovDebugMode.ObjectCustom6",
                "Displays Object.Custom6 from Aov.ObjectCustom4_7.");

            RegisterObjectCustomDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom7,
                HoUrpBuiltInNames.Resources.AovObjectCustom4_7,
                HoUrpBuiltInNames.Semantics.ObjectCustom7,
                "HoAovDebugMode.ObjectCustom7",
                "Displays Object.Custom7 from Aov.ObjectCustom4_7.");

            RegisterMaterialDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovMaterialClass,
                HoUrpBuiltInNames.Resources.AovSurfaceData,
                HoUrpBuiltInNames.Semantics.MaterialClass,
                DebugValueRange.Byte,
                "HoAovDebugMode.MaterialClass",
                "Displays Material.Class from Aov.SurfaceData.");

            RegisterMaterialDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovSssProfile,
                HoUrpBuiltInNames.Resources.AovSurfaceData,
                HoUrpBuiltInNames.Semantics.MaterialSssProfile,
                DebugValueRange.Byte,
                "HoAovDebugMode.SssProfile",
                "Displays Material.SssProfile from Aov.SurfaceData.");

            RegisterMaterialDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovThickness,
                HoUrpBuiltInNames.Resources.AovSurfaceData,
                HoUrpBuiltInNames.Semantics.MaterialThickness,
                DebugValueRange.ZeroToOne,
                "HoAovDebugMode.Thickness",
                "Displays Material.Thickness from Aov.SurfaceData.");

            RegisterMaterialDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovCurvature,
                HoUrpBuiltInNames.Resources.AovSurfaceData,
                HoUrpBuiltInNames.Semantics.MaterialCurvature,
                DebugValueRange.MinusOneToOne,
                "HoAovDebugMode.Curvature",
                "Displays Material.Curvature from Aov.SurfaceData.");

            RegisterMaterialDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovMaterialCustom0,
                HoUrpBuiltInNames.Resources.AovMaterialCustom0_3,
                HoUrpBuiltInNames.Semantics.MaterialCustom0,
                DebugValueRange.ZeroToOne,
                "HoAovDebugMode.MaterialCustom0",
                "Displays Material.Custom0 from Aov.MaterialCustom0_3.");

            RegisterMaterialDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovMaterialCustom1,
                HoUrpBuiltInNames.Resources.AovMaterialCustom0_3,
                HoUrpBuiltInNames.Semantics.MaterialCustom1,
                DebugValueRange.ZeroToOne,
                "HoAovDebugMode.MaterialCustom1",
                "Displays Material.Custom1 from Aov.MaterialCustom0_3.");

            RegisterMaterialDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovMaterialCustom2,
                HoUrpBuiltInNames.Resources.AovMaterialCustom0_3,
                HoUrpBuiltInNames.Semantics.MaterialCustom2,
                DebugValueRange.ZeroToOne,
                "HoAovDebugMode.MaterialCustom2",
                "Displays Material.Custom2 from Aov.MaterialCustom0_3.");

            RegisterMaterialDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovMaterialCustom3,
                HoUrpBuiltInNames.Resources.AovMaterialCustom0_3,
                HoUrpBuiltInNames.Semantics.MaterialCustom3,
                DebugValueRange.ZeroToOne,
                "HoAovDebugMode.MaterialCustom3",
                "Displays Material.Custom3 from Aov.MaterialCustom0_3.");
        }

        private static void RegisterObjectCustomDebugView(
            HoUrpContractRegistry registry,
            HoUrpIdentifier debugView,
            HoUrpIdentifier resource,
            HoUrpIdentifier semantic,
            string legacyReference,
            string description)
        {
            registry.DebugViews.Register(new DebugViewDefinition(
                debugView,
                HoUrpDomain.Debug,
                resource,
                semantic,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<DebugDisplayMode>(DebugDisplayMode.Replace),
                DebugValueRange.ZeroToOne,
                HoUrpBuiltInNames.Features.AovOutput,
                legacyReference,
                description));
        }

        private static void RegisterMaterialDebugView(
            HoUrpContractRegistry registry,
            HoUrpIdentifier debugView,
            HoUrpIdentifier resource,
            HoUrpIdentifier semantic,
            DebugValueRange range,
            string legacyReference,
            string description)
        {
            registry.DebugViews.Register(new DebugViewDefinition(
                debugView,
                HoUrpDomain.Debug,
                resource,
                semantic,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<DebugDisplayMode>(DebugDisplayMode.Replace),
                range,
                HoUrpBuiltInNames.Features.AovOutput,
                legacyReference,
                description));
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

            RegisterObjectCustomSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.ObjectCustom0,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom0,
                "_lilHoAovObjectCustom0_3Texture.r",
                "Object custom semantic channel 0.");

            RegisterObjectCustomSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.ObjectCustom1,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom1,
                "_lilHoAovObjectCustom0_3Texture.g",
                "Object custom semantic channel 1.");

            RegisterObjectCustomSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.ObjectCustom2,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom2,
                "_lilHoAovObjectCustom0_3Texture.b",
                "Object custom semantic channel 2.");

            RegisterObjectCustomSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.ObjectCustom3,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom3,
                "_lilHoAovObjectCustom0_3Texture.a",
                "Object custom semantic channel 3.");

            RegisterObjectCustomSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.ObjectCustom4,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom4,
                "_lilHoAovObjectCustom4_7Texture.r",
                "Object custom semantic channel 4.");

            RegisterObjectCustomSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.ObjectCustom5,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom5,
                "_lilHoAovObjectCustom4_7Texture.g",
                "Object custom semantic channel 5.");

            RegisterObjectCustomSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.ObjectCustom6,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom6,
                "_lilHoAovObjectCustom4_7Texture.b",
                "Object custom semantic channel 6.");

            RegisterObjectCustomSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.ObjectCustom7,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom7,
                "_lilHoAovObjectCustom4_7Texture.a",
                "Object custom semantic channel 7.");

            RegisterMaterialSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.MaterialClass,
                SemanticFormat.Byte,
                HoUrpBuiltInNames.DebugViews.AovMaterialClass,
                "_HoAovMaterialClass",
                "Material class written into Aov.SurfaceData.");

            RegisterMaterialSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.MaterialSssProfile,
                SemanticFormat.Byte,
                HoUrpBuiltInNames.DebugViews.AovSssProfile,
                "_HoSSSProfileId",
                "SSS profile id written into Aov.SurfaceData.");

            RegisterMaterialSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.MaterialThickness,
                SemanticFormat.NormalizedFloat,
                HoUrpBuiltInNames.DebugViews.AovThickness,
                "_HoAovThickness",
                "Material thickness written into Aov.SurfaceData.");

            RegisterMaterialSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.MaterialCurvature,
                SemanticFormat.SignedNormalizedFloat,
                HoUrpBuiltInNames.DebugViews.AovCurvature,
                "_HoAovCurvature",
                "Material curvature written into Aov.SurfaceData.");

            RegisterMaterialSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.MaterialUtility,
                SemanticFormat.NormalizedFloat,
                HoUrpBuiltInNames.DebugViews.None,
                "_HoAovUtility",
                "Registered material utility semantic. Phase four does not produce a stable resource channel for it.");

            RegisterMaterialSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.MaterialCustom0,
                SemanticFormat.NormalizedFloat,
                HoUrpBuiltInNames.DebugViews.AovMaterialCustom0,
                "_HoAovCustomValues0.x",
                "Material custom semantic channel 0.");

            RegisterMaterialSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.MaterialCustom1,
                SemanticFormat.NormalizedFloat,
                HoUrpBuiltInNames.DebugViews.AovMaterialCustom1,
                "_HoAovCustomValues0.y",
                "Material custom semantic channel 1.");

            RegisterMaterialSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.MaterialCustom2,
                SemanticFormat.NormalizedFloat,
                HoUrpBuiltInNames.DebugViews.AovMaterialCustom2,
                "_HoAovCustomValues0.z",
                "Material custom semantic channel 2.");

            RegisterMaterialSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.MaterialCustom3,
                SemanticFormat.NormalizedFloat,
                HoUrpBuiltInNames.DebugViews.AovMaterialCustom3,
                "_HoAovCustomValues0.w",
                "Material custom semantic channel 3.");
        }

        private static void RegisterObjectCustomSemantic(
            HoUrpContractRegistry registry,
            HoUrpIdentifier semantic,
            HoUrpIdentifier debugView,
            string legacySource,
            string description)
        {
            HoUrpIdentifier[] consumers =
            {
                HoUrpBuiltInNames.Features.SemanticPostProcess,
                HoUrpBuiltInNames.Features.DebugComposite
            };

            registry.Semantics.Register(new SemanticDefinition(
                semantic,
                HoUrpDomain.Object,
                SemanticFormat.NormalizedFloat,
                HoUrpPassStage.GeometrySemanticAov,
                HoUrpLifetime.PerCamera,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<HoUrpIdentifier>(consumers),
                debugView,
                HoUrpMigrationDecision.KeepConceptRename,
                legacySource,
                description));
        }

        private static void RegisterMaterialSemantic(
            HoUrpContractRegistry registry,
            HoUrpIdentifier semantic,
            SemanticFormat format,
            HoUrpIdentifier debugView,
            string legacySource,
            string description)
        {
            HoUrpIdentifier[] consumers =
            {
                HoUrpBuiltInNames.Features.SemanticPostProcess,
                HoUrpBuiltInNames.Features.DebugComposite
            };

            registry.Semantics.Register(new SemanticDefinition(
                semantic,
                HoUrpDomain.Material,
                format,
                HoUrpPassStage.MaterialShadingSemanticAov,
                HoUrpLifetime.PerCamera,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<HoUrpIdentifier>(consumers),
                debugView,
                HoUrpMigrationDecision.KeepConceptRename,
                legacySource,
                description));
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
                ResourceClearPolicy.ClearInvalidNormalFarDepth,
                HoUrpBuiltInNames.DebugViews.AovWorldNormal,
                "_lilHoAovNormalDepthTexture",
                "High precision normal/depth AOV resource. Clear is invalid normal with far depth."));

            registry.Resources.Register(new ResourceDefinition(
                HoUrpBuiltInNames.Resources.AovObjectCustom0_3,
                ResourceKind.Texture2D,
                HoUrpBuiltInNames.Semantics.ObjectCustom0,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Features.SemanticPostProcess,
                    HoUrpBuiltInNames.Features.DebugComposite),
                ResourceFormatHint.MaskRgba8,
                ResourceScale.Full,
                HoUrpLifetime.PerCamera,
                ResourceClearPolicy.ClearZero,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom0,
                "_lilHoAovObjectCustom0_3Texture",
                "Packed object custom channels 0-3."));

            registry.Resources.Register(new ResourceDefinition(
                HoUrpBuiltInNames.Resources.AovObjectCustom4_7,
                ResourceKind.Texture2D,
                HoUrpBuiltInNames.Semantics.ObjectCustom4,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Features.SemanticPostProcess,
                    HoUrpBuiltInNames.Features.DebugComposite),
                ResourceFormatHint.MaskRgba8,
                ResourceScale.Full,
                HoUrpLifetime.PerCamera,
                ResourceClearPolicy.ClearZero,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom4,
                "_lilHoAovObjectCustom4_7Texture",
                "Packed object custom channels 4-7."));

            registry.Resources.Register(new ResourceDefinition(
                HoUrpBuiltInNames.Resources.AovSurfaceData,
                ResourceKind.Texture2D,
                HoUrpBuiltInNames.Semantics.MaterialClass,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Features.SemanticPostProcess,
                    HoUrpBuiltInNames.Features.DebugComposite),
                ResourceFormatHint.HighPrecisionRgba16Float,
                ResourceScale.Full,
                HoUrpLifetime.PerCamera,
                ResourceClearPolicy.ClearZero,
                HoUrpBuiltInNames.DebugViews.AovMaterialClass,
                "_lilHoAovSurfaceDataTexture",
                "Packed material class/profile/thickness/curvature AOV resource."));

            registry.Resources.Register(new ResourceDefinition(
                HoUrpBuiltInNames.Resources.AovMaterialCustom0_3,
                ResourceKind.Texture2D,
                HoUrpBuiltInNames.Semantics.MaterialCustom0,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Features.SemanticPostProcess,
                    HoUrpBuiltInNames.Features.DebugComposite),
                ResourceFormatHint.HighPrecisionRgba16Float,
                ResourceScale.Full,
                HoUrpLifetime.PerCamera,
                ResourceClearPolicy.ClearZero,
                HoUrpBuiltInNames.DebugViews.AovMaterialCustom0,
                "_lilHoAovCustom0_3Texture",
                "Packed material custom channels 0-3."));
        }

        private static void RegisterFeatures(HoUrpContractRegistry registry)
        {
            registry.Features.Register(new FeatureDescriptor(
                HoUrpBuiltInNames.Features.AovOutput,
                HoUrpDomain.Geometry,
                HoUrpPassStage.GeometrySemanticAov,
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Resources.AovMaskId,
                    HoUrpBuiltInNames.Resources.AovNormalDepth,
                    HoUrpBuiltInNames.Resources.AovObjectCustom0_3,
                    HoUrpBuiltInNames.Resources.AovObjectCustom4_7,
                    HoUrpBuiltInNames.Resources.AovSurfaceData,
                    HoUrpBuiltInNames.Resources.AovMaterialCustom0_3),
                new ReadOnlyArray<HoUrpIdentifier>(),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Semantics.ObjectMaskWeight,
                    HoUrpBuiltInNames.Semantics.ObjectId,
                    HoUrpBuiltInNames.Semantics.ObjectGroupId,
                    HoUrpBuiltInNames.Semantics.ObjectFlags,
                    HoUrpBuiltInNames.Semantics.ObjectCustom0,
                    HoUrpBuiltInNames.Semantics.ObjectCustom1,
                    HoUrpBuiltInNames.Semantics.ObjectCustom2,
                    HoUrpBuiltInNames.Semantics.ObjectCustom3,
                    HoUrpBuiltInNames.Semantics.ObjectCustom4,
                    HoUrpBuiltInNames.Semantics.ObjectCustom5,
                    HoUrpBuiltInNames.Semantics.ObjectCustom6,
                    HoUrpBuiltInNames.Semantics.ObjectCustom7,
                    HoUrpBuiltInNames.Semantics.MaterialClass,
                    HoUrpBuiltInNames.Semantics.MaterialSssProfile,
                    HoUrpBuiltInNames.Semantics.MaterialThickness,
                    HoUrpBuiltInNames.Semantics.MaterialCurvature,
                    HoUrpBuiltInNames.Semantics.MaterialCustom0,
                    HoUrpBuiltInNames.Semantics.MaterialCustom1,
                    HoUrpBuiltInNames.Semantics.MaterialCustom2,
                    HoUrpBuiltInNames.Semantics.MaterialCustom3,
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
                    HoUrpBuiltInNames.DebugViews.AovWorldNormal,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom0,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom1,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom2,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom3,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom4,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom5,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom6,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom7,
                    HoUrpBuiltInNames.DebugViews.AovMaterialClass,
                    HoUrpBuiltInNames.DebugViews.AovSssProfile,
                    HoUrpBuiltInNames.DebugViews.AovThickness,
                    HoUrpBuiltInNames.DebugViews.AovCurvature,
                    HoUrpBuiltInNames.DebugViews.AovMaterialCustom0,
                    HoUrpBuiltInNames.DebugViews.AovMaterialCustom1,
                    HoUrpBuiltInNames.DebugViews.AovMaterialCustom2,
                    HoUrpBuiltInNames.DebugViews.AovMaterialCustom3),
                HoUrpMigrationDecision.KeepConceptRename,
                "Runtime/AOV/HoAovRendererFeature.cs",
                "Minimum AOV contract for object, geometry, and phase-four material semantic resources."));

            registry.Features.Register(new FeatureDescriptor(
                HoUrpBuiltInNames.Features.SemanticPostProcess,
                HoUrpDomain.Composite,
                HoUrpPassStage.SemanticPost,
                new ReadOnlyArray<HoUrpIdentifier>(),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Resources.AovMaskId,
                    HoUrpBuiltInNames.Resources.AovNormalDepth,
                    HoUrpBuiltInNames.Resources.AovObjectCustom0_3,
                    HoUrpBuiltInNames.Resources.AovObjectCustom4_7,
                    HoUrpBuiltInNames.Resources.AovSurfaceData,
                    HoUrpBuiltInNames.Resources.AovMaterialCustom0_3),
                new ReadOnlyArray<HoUrpIdentifier>(),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Semantics.ObjectMaskWeight,
                    HoUrpBuiltInNames.Semantics.ObjectId,
                    HoUrpBuiltInNames.Semantics.ObjectGroupId,
                    HoUrpBuiltInNames.Semantics.ObjectFlags,
                    HoUrpBuiltInNames.Semantics.ObjectCustom0,
                    HoUrpBuiltInNames.Semantics.ObjectCustom1,
                    HoUrpBuiltInNames.Semantics.ObjectCustom2,
                    HoUrpBuiltInNames.Semantics.ObjectCustom3,
                    HoUrpBuiltInNames.Semantics.ObjectCustom4,
                    HoUrpBuiltInNames.Semantics.ObjectCustom5,
                    HoUrpBuiltInNames.Semantics.ObjectCustom6,
                    HoUrpBuiltInNames.Semantics.ObjectCustom7,
                    HoUrpBuiltInNames.Semantics.MaterialClass,
                    HoUrpBuiltInNames.Semantics.MaterialSssProfile,
                    HoUrpBuiltInNames.Semantics.MaterialThickness,
                    HoUrpBuiltInNames.Semantics.MaterialCurvature,
                    HoUrpBuiltInNames.Semantics.MaterialCustom0,
                    HoUrpBuiltInNames.Semantics.MaterialCustom1,
                    HoUrpBuiltInNames.Semantics.MaterialCustom2,
                    HoUrpBuiltInNames.Semantics.MaterialCustom3,
                    HoUrpBuiltInNames.Semantics.GeometryWorldNormal,
                    HoUrpBuiltInNames.Semantics.GeometryLinearDepth),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Capabilities.RequiresAov,
                    HoUrpBuiltInNames.Capabilities.SupportsDebugView),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.DebugViews.AovMask,
                    HoUrpBuiltInNames.DebugViews.AovObjectId,
                    HoUrpBuiltInNames.DebugViews.AovLinearDepth,
                    HoUrpBuiltInNames.DebugViews.AovWorldNormal,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom0,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom1,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom2,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom3,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom4,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom5,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom6,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom7,
                    HoUrpBuiltInNames.DebugViews.AovMaterialClass,
                    HoUrpBuiltInNames.DebugViews.AovSssProfile,
                    HoUrpBuiltInNames.DebugViews.AovThickness,
                    HoUrpBuiltInNames.DebugViews.AovCurvature,
                    HoUrpBuiltInNames.DebugViews.AovMaterialCustom0,
                    HoUrpBuiltInNames.DebugViews.AovMaterialCustom1,
                    HoUrpBuiltInNames.DebugViews.AovMaterialCustom2,
                    HoUrpBuiltInNames.DebugViews.AovMaterialCustom3),
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
                    HoUrpBuiltInNames.Resources.AovNormalDepth,
                    HoUrpBuiltInNames.Resources.AovObjectCustom0_3,
                    HoUrpBuiltInNames.Resources.AovObjectCustom4_7,
                    HoUrpBuiltInNames.Resources.AovSurfaceData,
                    HoUrpBuiltInNames.Resources.AovMaterialCustom0_3),
                new ReadOnlyArray<HoUrpIdentifier>(),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Semantics.ObjectMaskWeight,
                    HoUrpBuiltInNames.Semantics.ObjectId,
                    HoUrpBuiltInNames.Semantics.ObjectCustom0,
                    HoUrpBuiltInNames.Semantics.ObjectCustom1,
                    HoUrpBuiltInNames.Semantics.ObjectCustom2,
                    HoUrpBuiltInNames.Semantics.ObjectCustom3,
                    HoUrpBuiltInNames.Semantics.ObjectCustom4,
                    HoUrpBuiltInNames.Semantics.ObjectCustom5,
                    HoUrpBuiltInNames.Semantics.ObjectCustom6,
                    HoUrpBuiltInNames.Semantics.ObjectCustom7,
                    HoUrpBuiltInNames.Semantics.MaterialClass,
                    HoUrpBuiltInNames.Semantics.MaterialSssProfile,
                    HoUrpBuiltInNames.Semantics.MaterialThickness,
                    HoUrpBuiltInNames.Semantics.MaterialCurvature,
                    HoUrpBuiltInNames.Semantics.MaterialCustom0,
                    HoUrpBuiltInNames.Semantics.MaterialCustom1,
                    HoUrpBuiltInNames.Semantics.MaterialCustom2,
                    HoUrpBuiltInNames.Semantics.MaterialCustom3,
                    HoUrpBuiltInNames.Semantics.GeometryWorldNormal,
                    HoUrpBuiltInNames.Semantics.GeometryLinearDepth),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Capabilities.SupportsDebugView),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.DebugViews.AovMask,
                    HoUrpBuiltInNames.DebugViews.AovObjectId,
                    HoUrpBuiltInNames.DebugViews.AovLinearDepth,
                    HoUrpBuiltInNames.DebugViews.AovWorldNormal,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom0,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom1,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom2,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom3,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom4,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom5,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom6,
                    HoUrpBuiltInNames.DebugViews.AovObjectCustom7,
                    HoUrpBuiltInNames.DebugViews.AovMaterialClass,
                    HoUrpBuiltInNames.DebugViews.AovSssProfile,
                    HoUrpBuiltInNames.DebugViews.AovThickness,
                    HoUrpBuiltInNames.DebugViews.AovCurvature,
                    HoUrpBuiltInNames.DebugViews.AovMaterialCustom0,
                    HoUrpBuiltInNames.DebugViews.AovMaterialCustom1,
                    HoUrpBuiltInNames.DebugViews.AovMaterialCustom2,
                    HoUrpBuiltInNames.DebugViews.AovMaterialCustom3),
                HoUrpMigrationDecision.Replace,
                "old per-feature debug passes",
                "Central debug composite owner for registered debug views."));
        }
    }
}
