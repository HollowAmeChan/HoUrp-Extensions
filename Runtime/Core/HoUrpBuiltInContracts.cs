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
                HoUrpBuiltInNames.Capabilities.WritesObjectCustom,
                HoUrpDomain.Object,
                CapabilityOwnerKind.Object,
                "Object capability component",
                "Object.Custom0-7",
                false,
                "Object Capability UI",
                "HoAovGroup.objectCustom0-7",
                "Allows an object to write object custom region bits."));

            registry.Capabilities.Register(new CapabilityDefinition(
                HoUrpBuiltInNames.Capabilities.ReceivesSemanticPost,
                HoUrpDomain.Object,
                CapabilityOwnerKind.Object,
                "Object capability component",
                "SemanticPostProcess masks",
                true,
                "Object Capability UI",
                "HoPost AOV rules",
                "Allows an object to participate in semantic post process rules through Object.FeatureFlags bit 1."));

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

            registry.Capabilities.Register(new CapabilityDefinition(
                HoUrpBuiltInNames.Capabilities.SupportsOit,
                HoUrpDomain.Material,
                CapabilityOwnerKind.Material,
                "MaterialPreset",
                "HoUrpOitAccumulation pass",
                false,
                "Material preset",
                "_lilOITEnabled",
                "Marks a generated material preset as capable of producing OIT accumulation input."));

            registry.Capabilities.Register(new CapabilityDefinition(
                HoUrpBuiltInNames.Capabilities.ParticipatesOit,
                HoUrpDomain.Material,
                CapabilityOwnerKind.Material,
                "MaterialPreset / material instance",
                "Weighted OIT runtime draw list",
                false,
                "Material preset",
                "_lilOITEnabled",
                "Marks a generated material instance as participating in OIT accumulation. Runtime support is deferred to the OIT stage."));
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

            RegisterObjectFlagDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectFlag0,
                "HoAovDebugMode.ObjectFlag0",
                "Displays Object.FeatureFlags bit 0 as written to Aov.MaskId.a; this bit is reserved empty in RSUV v1 and should stay zero.");

            RegisterObjectFlagDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectFlag1,
                "HoAovDebugMode.PostReceiver",
                "Displays Object.FeatureFlags bit 1 as written to Aov.MaskId.a; this is the SemanticPost receiver gate.");

            RegisterObjectFlagDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectFlag2,
                "HoAovDebugMode.ObjectFlag2",
                "Displays Object.FeatureFlags bit 2 as written to Aov.MaskId.a.");

            RegisterObjectFlagDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectFlag3,
                "HoAovDebugMode.ObjectFlag3",
                "Displays Object.FeatureFlags bit 3 as written to Aov.MaskId.a.");

            RegisterObjectFlagDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectFlag4,
                "HoAovDebugMode.ObjectFlag4",
                "Displays Object.FeatureFlags bit 4 as written to Aov.MaskId.a.");

            RegisterObjectFlagDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectFlag5,
                "HoAovDebugMode.ObjectFlag5",
                "Displays Object.FeatureFlags bit 5 as written to Aov.MaskId.a.");

            RegisterObjectFlagDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectFlag6,
                "HoAovDebugMode.ObjectFlag6",
                "Displays Object.FeatureFlags bit 6 as written to Aov.MaskId.a.");

            RegisterObjectFlagDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectFlag7,
                "HoAovDebugMode.ObjectFlag7",
                "Displays Object.FeatureFlags bit 7 as written to Aov.MaskId.a.");

            RegisterHighObjectFlagDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectFlag8,
                "HoAovDebugMode.ObjectFlag8",
                "Displays Object.FeatureFlags bit 8 as packed into Aov.MaskId.b bit 3.");

            RegisterHighObjectFlagDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectFlag9,
                "HoAovDebugMode.ObjectFlag9",
                "Displays Object.FeatureFlags bit 9 as packed into Aov.MaskId.b bit 4.");

            RegisterHighObjectFlagDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectFlag10,
                "HoAovDebugMode.ObjectFlag10",
                "Displays Object.FeatureFlags bit 10 as packed into Aov.MaskId.b bit 5.");

            RegisterHighObjectFlagDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectFlag11,
                "HoAovDebugMode.ObjectFlag11",
                "Displays Object.FeatureFlags bit 11 as packed into Aov.MaskId.b bit 6.");

            RegisterHighObjectFlagDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectFlag12,
                "HoAovDebugMode.ObjectFlag12",
                "Displays Object.FeatureFlags bit 12 as packed into Aov.MaskId.b bit 7.");

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
                "HoAovDebugMode.Subject",
                "Displays the Subject object semantic bit from Aov.ObjectCustom0_3.");

            RegisterObjectCustomDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom1,
                HoUrpBuiltInNames.Resources.AovObjectCustom0_3,
                HoUrpBuiltInNames.Semantics.ObjectCustom1,
                "HoAovDebugMode.Face",
                "Displays the Face object semantic bit from Aov.ObjectCustom0_3.");

            RegisterObjectCustomDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom2,
                HoUrpBuiltInNames.Resources.AovObjectCustom0_3,
                HoUrpBuiltInNames.Semantics.ObjectCustom2,
                "HoAovDebugMode.Hair",
                "Displays the Hair object semantic bit from Aov.ObjectCustom0_3.");

            RegisterObjectCustomDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom3,
                HoUrpBuiltInNames.Resources.AovObjectCustom0_3,
                HoUrpBuiltInNames.Semantics.ObjectCustom3,
                "HoAovDebugMode.Eye",
                "Displays the Eye object semantic bit from Aov.ObjectCustom0_3.");

            RegisterObjectCustomDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom4,
                HoUrpBuiltInNames.Resources.AovObjectCustom4_7,
                HoUrpBuiltInNames.Semantics.ObjectCustom4,
                "HoAovDebugMode.Accessory",
                "Displays the Accessory object semantic bit from Aov.ObjectCustom4_7.");

            RegisterObjectCustomDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom5,
                HoUrpBuiltInNames.Resources.AovObjectCustom4_7,
                HoUrpBuiltInNames.Semantics.ObjectCustom5,
                "HoAovDebugMode.Cloth",
                "Displays the Cloth object semantic bit from Aov.ObjectCustom4_7.");

            RegisterObjectCustomDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom6,
                HoUrpBuiltInNames.Resources.AovObjectCustom4_7,
                HoUrpBuiltInNames.Semantics.ObjectCustom6,
                "HoAovDebugMode.Prop",
                "Displays the Prop object semantic bit from Aov.ObjectCustom4_7.");

            RegisterObjectCustomDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovObjectCustom7,
                HoUrpBuiltInNames.Resources.AovObjectCustom4_7,
                HoUrpBuiltInNames.Semantics.ObjectCustom7,
                "HoAovDebugMode.Reserved",
                "Displays the reserved object semantic bit from Aov.ObjectCustom4_7.");

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

            RegisterShadingDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovSssSource,
                HoUrpBuiltInNames.Resources.AovSssSource,
                HoUrpBuiltInNames.Semantics.ShadingSssSourceColor,
                DebugValueRange.HdrColor,
                "HoAovDebugMode.SssSource",
                "Displays Shading.SssSourceColor from Aov.SssSource.rgb.");

            RegisterShadingDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.AovSssWeight,
                HoUrpBuiltInNames.Resources.AovSssSource,
                HoUrpBuiltInNames.Semantics.ShadingSssWeight,
                DebugValueRange.ZeroToOne,
                "HoAovDebugMode.SssWeight",
                "Displays Shading.SssWeight from Aov.SssSource.a.");

            registry.DebugViews.Register(new DebugViewDefinition(
                HoUrpBuiltInNames.DebugViews.SssMask,
                HoUrpDomain.Debug,
                HoUrpBuiltInNames.Resources.SssSource,
                HoUrpBuiltInNames.Semantics.ShadingSssWeight,
                HoUrpBuiltInNames.Features.SubsurfaceScattering,
                new ReadOnlyArray<DebugDisplayMode>(DebugDisplayMode.Replace),
                DebugValueRange.ZeroToOne,
                HoUrpBuiltInNames.Features.SubsurfaceScattering,
                "HoSSS.Debug.Mask",
                "Displays final SSS participation mask from Sss.Source.a."));

            registry.DebugViews.Register(new DebugViewDefinition(
                HoUrpBuiltInNames.DebugViews.SssSource,
                HoUrpDomain.Debug,
                HoUrpBuiltInNames.Resources.SssSource,
                HoUrpBuiltInNames.Semantics.ShadingSssSourceColor,
                HoUrpBuiltInNames.Features.SubsurfaceScattering,
                new ReadOnlyArray<DebugDisplayMode>(DebugDisplayMode.Replace),
                DebugValueRange.HdrColor,
                HoUrpBuiltInNames.Features.SubsurfaceScattering,
                "HoSSS.Debug.Source",
                "Displays SSS source color prepared for diffusion."));

            registry.DebugViews.Register(new DebugViewDefinition(
                HoUrpBuiltInNames.DebugViews.SssDiffusion,
                HoUrpDomain.Debug,
                HoUrpBuiltInNames.Resources.SssDiffusion,
                HoUrpBuiltInNames.Semantics.ShadingSssDiffusionColor,
                HoUrpBuiltInNames.Features.SubsurfaceScattering,
                new ReadOnlyArray<DebugDisplayMode>(DebugDisplayMode.Replace),
                DebugValueRange.HdrColor,
                HoUrpBuiltInNames.Features.SubsurfaceScattering,
                "HoSSS.Debug.Diffusion",
                "Displays diffused SSS color before camera composite."));

            registry.DebugViews.Register(new DebugViewDefinition(
                HoUrpBuiltInNames.DebugViews.SssCompositeWeight,
                HoUrpDomain.Debug,
                HoUrpBuiltInNames.Resources.SssDiffusion,
                HoUrpBuiltInNames.Semantics.ShadingSssCompositeWeight,
                HoUrpBuiltInNames.Features.SubsurfaceScattering,
                new ReadOnlyArray<DebugDisplayMode>(DebugDisplayMode.Replace),
                DebugValueRange.ZeroToOne,
                HoUrpBuiltInNames.Features.SubsurfaceScattering,
                "HoSSS.Debug.CompositeWeight",
                "Displays SSS composite weight from Sss.Diffusion.a."));

            registry.DebugViews.Register(new DebugViewDefinition(
                HoUrpBuiltInNames.DebugViews.SemanticPostMask,
                HoUrpDomain.Debug,
                HoUrpBuiltInNames.Resources.SemanticPostMask,
                HoUrpBuiltInNames.Semantics.CompositeSemanticPostMask,
                HoUrpBuiltInNames.Features.SemanticPostProcess,
                new ReadOnlyArray<DebugDisplayMode>(
                    DebugDisplayMode.Replace,
                    DebugDisplayMode.ChannelInspect),
                DebugValueRange.ZeroToOne,
                HoUrpBuiltInNames.Features.SemanticPostProcess,
                "HoPost.Debug.Mask",
                "Displays SemanticPost layer masks packed into SemanticPost.Mask."));

            RegisterMaterialDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.SssProfileId,
                HoUrpBuiltInNames.Resources.AovSurfaceData,
                HoUrpBuiltInNames.Semantics.MaterialSssProfile,
                DebugValueRange.Byte,
                "HoAovDebugMode.SSS.ProfileId",
                "SSS-oriented alias for Material.SssProfile from Aov.SurfaceData.");

            RegisterMaterialDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.SssThickness,
                HoUrpBuiltInNames.Resources.AovSurfaceData,
                HoUrpBuiltInNames.Semantics.MaterialThickness,
                DebugValueRange.ZeroToOne,
                "HoAovDebugMode.SSS.Thickness",
                "SSS-oriented alias for Material.Thickness from Aov.SurfaceData.");

            RegisterMaterialDebugView(
                registry,
                HoUrpBuiltInNames.DebugViews.SssCurvature,
                HoUrpBuiltInNames.Resources.AovSurfaceData,
                HoUrpBuiltInNames.Semantics.MaterialCurvature,
                DebugValueRange.MinusOneToOne,
                "HoAovDebugMode.SSS.Curvature",
                "SSS-oriented alias for Material.Curvature from Aov.SurfaceData.");
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

        private static void RegisterObjectFlagDebugView(
            HoUrpContractRegistry registry,
            HoUrpIdentifier debugView,
            string legacyReference,
            string description)
        {
            registry.DebugViews.Register(new DebugViewDefinition(
                debugView,
                HoUrpDomain.Debug,
                HoUrpBuiltInNames.Resources.AovMaskId,
                HoUrpBuiltInNames.Semantics.ObjectFlags,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<DebugDisplayMode>(DebugDisplayMode.Replace),
                DebugValueRange.ZeroToOne,
                HoUrpBuiltInNames.Features.AovOutput,
                legacyReference,
                description));
        }

        private static void RegisterHighObjectFlagDebugView(
            HoUrpContractRegistry registry,
            HoUrpIdentifier debugView,
            string legacyReference,
            string description)
        {
            registry.DebugViews.Register(new DebugViewDefinition(
                debugView,
                HoUrpDomain.Debug,
                HoUrpBuiltInNames.Resources.AovMaskId,
                HoUrpBuiltInNames.Semantics.ObjectFlags,
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

        private static void RegisterShadingDebugView(
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

            HoUrpIdentifier[] sssInputConsumers =
            {
                HoUrpBuiltInNames.Features.SubsurfaceScattering,
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
                new ReadOnlyArray<HoUrpIdentifier>(sssInputConsumers),
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
                "Object feature flags written into Aov.MaskId. Bit 0 is reserved empty; bit 1 is the SemanticPost receiver gate."));

            registry.Semantics.Register(new SemanticDefinition(
                HoUrpBuiltInNames.Semantics.GeometryWorldNormal,
                HoUrpDomain.Geometry,
                SemanticFormat.Float3,
                HoUrpPassStage.GeometrySemanticAov,
                HoUrpLifetime.PerCamera,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<HoUrpIdentifier>(sssInputConsumers),
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
                new ReadOnlyArray<HoUrpIdentifier>(sssInputConsumers),
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
                "Material class written into Aov.SurfaceData.",
                false);

            RegisterMaterialSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.MaterialSssProfile,
                SemanticFormat.Byte,
                HoUrpBuiltInNames.DebugViews.AovSssProfile,
                "_HoSSSProfileId",
                "SSS profile id written into Aov.SurfaceData.",
                true);

            RegisterMaterialSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.MaterialThickness,
                SemanticFormat.NormalizedFloat,
                HoUrpBuiltInNames.DebugViews.AovThickness,
                "_HoAovThickness",
                "Material thickness written into Aov.SurfaceData.",
                true);

            RegisterMaterialSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.MaterialCurvature,
                SemanticFormat.SignedNormalizedFloat,
                HoUrpBuiltInNames.DebugViews.AovCurvature,
                "_HoAovCurvature",
                "Material curvature written into Aov.SurfaceData.",
                true);

            RegisterMaterialSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.MaterialUtility,
                SemanticFormat.NormalizedFloat,
                HoUrpBuiltInNames.DebugViews.None,
                "_HoAovUtility",
                "Registered material utility semantic. Phase four does not produce a stable resource channel for it.",
                false);

            RegisterMaterialSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.MaterialCustom0,
                SemanticFormat.NormalizedFloat,
                HoUrpBuiltInNames.DebugViews.AovMaterialCustom0,
                "_HoAovCustomValues0.x",
                "Material custom semantic channel 0.",
                false);

            RegisterMaterialSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.MaterialCustom1,
                SemanticFormat.NormalizedFloat,
                HoUrpBuiltInNames.DebugViews.AovMaterialCustom1,
                "_HoAovCustomValues0.y",
                "Material custom semantic channel 1.",
                false);

            RegisterMaterialSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.MaterialCustom2,
                SemanticFormat.NormalizedFloat,
                HoUrpBuiltInNames.DebugViews.AovMaterialCustom2,
                "_HoAovCustomValues0.z",
                "Material custom semantic channel 2.",
                false);

            RegisterMaterialSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.MaterialCustom3,
                SemanticFormat.NormalizedFloat,
                HoUrpBuiltInNames.DebugViews.AovMaterialCustom3,
                "_HoAovCustomValues0.w",
                "Material custom semantic channel 3.",
                false);

            RegisterShadingSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.ShadingSssSourceColor,
                SemanticFormat.Float3,
                HoUrpBuiltInNames.DebugViews.AovSssSource,
                "_lilHoAovSssTexture.rgb",
                "SSS source color encoded into Aov.SssSource.rgb.");

            RegisterShadingSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.ShadingSssWeight,
                SemanticFormat.NormalizedFloat,
                HoUrpBuiltInNames.DebugViews.AovSssWeight,
                "_lilHoAovSssTexture.a",
                "SSS participation weight encoded into Aov.SssSource.a.");

            RegisterGeneratedMaterialSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.TransparentColor,
                HoUrpDomain.Material,
                SemanticFormat.Float3,
                "SurfaceData.baseColor",
                "Transparent color emitted by generated material presets for forward and OIT-ready passes.");

            RegisterGeneratedMaterialSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.TransparentAlpha,
                HoUrpDomain.Material,
                SemanticFormat.NormalizedFloat,
                "SurfaceData.alpha",
                "Transparent alpha shared by forward, AOV, and OIT-ready passes.");

            RegisterGeneratedMaterialSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.TransparentCoverage,
                HoUrpDomain.Material,
                SemanticFormat.NormalizedFloat,
                "TransparentOutputData.coverage",
                "Coverage after alpha clip or dithering. The stage-ten prototype may mirror alpha.");

            RegisterGeneratedMaterialSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.OitAccumulationInput,
                HoUrpDomain.Composite,
                SemanticFormat.Float4,
                "OitAccumulationData.weightedColor",
                "OIT-ready weighted color/alpha input produced by the material pass. OIT runtime resources are deferred.");

            RegisterGeneratedMaterialSemantic(
                registry,
                HoUrpBuiltInNames.Semantics.OitRevealageInput,
                HoUrpDomain.Composite,
                SemanticFormat.NormalizedFloat,
                "OitAccumulationData.revealage",
                "OIT-ready revealage input produced by the material pass. OIT runtime resources are deferred.");

            registry.Semantics.Register(new SemanticDefinition(
                HoUrpBuiltInNames.Semantics.ShadingSssDiffusionColor,
                HoUrpDomain.Shading,
                SemanticFormat.Float3,
                HoUrpPassStage.ScreenSss,
                HoUrpLifetime.PerCamera,
                HoUrpBuiltInNames.Features.SubsurfaceScattering,
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Features.SemanticPostProcess,
                    HoUrpBuiltInNames.Features.DebugComposite),
                HoUrpBuiltInNames.DebugViews.SssDiffusion,
                HoUrpMigrationDecision.KeepConceptRename,
                "_lilHoSSSDiffusedTexture.rgb",
                "Diffused SSS color encoded into Sss.Diffusion.rgb."));

            registry.Semantics.Register(new SemanticDefinition(
                HoUrpBuiltInNames.Semantics.ShadingSssCompositeWeight,
                HoUrpDomain.Shading,
                SemanticFormat.NormalizedFloat,
                HoUrpPassStage.ScreenSss,
                HoUrpLifetime.PerCamera,
                HoUrpBuiltInNames.Features.SubsurfaceScattering,
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Features.SemanticPostProcess,
                    HoUrpBuiltInNames.Features.DebugComposite),
                HoUrpBuiltInNames.DebugViews.SssCompositeWeight,
                HoUrpMigrationDecision.KeepConceptRename,
                "_lilHoSSSDiffusedTexture.a",
                "SSS composite weight encoded into Sss.Diffusion.a."));

            registry.Semantics.Register(new SemanticDefinition(
                HoUrpBuiltInNames.Semantics.CompositeSemanticPostMask,
                HoUrpDomain.Composite,
                SemanticFormat.NormalizedFloat,
                HoUrpPassStage.SemanticPost,
                HoUrpLifetime.PerCamera,
                HoUrpBuiltInNames.Features.SemanticPostProcess,
                new ReadOnlyArray<HoUrpIdentifier>(HoUrpBuiltInNames.Features.DebugComposite),
                HoUrpBuiltInNames.DebugViews.SemanticPostMask,
                HoUrpMigrationDecision.KeepConceptRename,
                "HoPostAovMask",
                "SemanticPost layer mask generated from registered AOV and SSS resources."));
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
            string description,
            bool consumedBySss)
        {
            HoUrpIdentifier[] consumers = consumedBySss
                ? new[]
                {
                    HoUrpBuiltInNames.Features.SubsurfaceScattering,
                    HoUrpBuiltInNames.Features.SemanticPostProcess,
                    HoUrpBuiltInNames.Features.DebugComposite
                }
                : new[]
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

        private static void RegisterShadingSemantic(
            HoUrpContractRegistry registry,
            HoUrpIdentifier semantic,
            SemanticFormat format,
            HoUrpIdentifier debugView,
            string legacySource,
            string description)
        {
            HoUrpIdentifier[] consumers =
            {
                HoUrpBuiltInNames.Features.SubsurfaceScattering,
                HoUrpBuiltInNames.Features.SemanticPostProcess,
                HoUrpBuiltInNames.Features.DebugComposite
            };

            registry.Semantics.Register(new SemanticDefinition(
                semantic,
                HoUrpDomain.Shading,
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

        private static void RegisterGeneratedMaterialSemantic(
            HoUrpContractRegistry registry,
            HoUrpIdentifier semantic,
            HoUrpDomain domain,
            SemanticFormat format,
            string source,
            string description)
        {
            registry.Semantics.Register(new SemanticDefinition(
                semantic,
                domain,
                format,
                HoUrpPassStage.MaterialShadingSemanticAov,
                HoUrpLifetime.PerMaterial,
                HoUrpBuiltInNames.Features.GeneratedMaterial,
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Features.AovOutput,
                    HoUrpBuiltInNames.Features.DebugComposite),
                HoUrpBuiltInNames.DebugViews.None,
                HoUrpMigrationDecision.Replace,
                source,
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
                    HoUrpBuiltInNames.Features.SubsurfaceScattering,
                    HoUrpBuiltInNames.Features.SemanticPostProcess,
                    HoUrpBuiltInNames.Features.ScreenPost,
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
                    HoUrpBuiltInNames.Features.SubsurfaceScattering,
                    HoUrpBuiltInNames.Features.SemanticPostProcess,
                    HoUrpBuiltInNames.Features.ScreenPost,
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
                    HoUrpBuiltInNames.Features.SubsurfaceScattering,
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

            registry.Resources.Register(new ResourceDefinition(
                HoUrpBuiltInNames.Resources.AovSssSource,
                ResourceKind.Texture2D,
                HoUrpBuiltInNames.Semantics.ShadingSssSourceColor,
                HoUrpBuiltInNames.Features.AovOutput,
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Features.SubsurfaceScattering,
                    HoUrpBuiltInNames.Features.SemanticPostProcess,
                    HoUrpBuiltInNames.Features.DebugComposite),
                ResourceFormatHint.HighPrecisionRgba16Float,
                ResourceScale.Full,
                HoUrpLifetime.PerCamera,
                ResourceClearPolicy.ClearZero,
                HoUrpBuiltInNames.DebugViews.AovSssSource,
                "_lilHoAovSssTexture",
                "Packed SSS input source color and participation weight."));

            registry.Resources.Register(new ResourceDefinition(
                HoUrpBuiltInNames.Resources.SssSource,
                ResourceKind.Texture2D,
                HoUrpBuiltInNames.Semantics.ShadingSssSourceColor,
                HoUrpBuiltInNames.Features.SubsurfaceScattering,
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Features.SemanticPostProcess,
                    HoUrpBuiltInNames.Features.DebugComposite),
                ResourceFormatHint.HighPrecisionRgba16Float,
                ResourceScale.Full,
                HoUrpLifetime.PerCamera,
                ResourceClearPolicy.ClearZero,
                HoUrpBuiltInNames.DebugViews.SssSource,
                "_lilHoSSSSourceTexture",
                "Prepared SSS source color and participation mask."));

            registry.Resources.Register(new ResourceDefinition(
                HoUrpBuiltInNames.Resources.SssDiffusion,
                ResourceKind.Texture2D,
                HoUrpBuiltInNames.Semantics.ShadingSssDiffusionColor,
                HoUrpBuiltInNames.Features.SubsurfaceScattering,
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Features.SemanticPostProcess,
                    HoUrpBuiltInNames.Features.DebugComposite),
                ResourceFormatHint.HighPrecisionRgba16Float,
                ResourceScale.Full,
                HoUrpLifetime.PerCamera,
                ResourceClearPolicy.ClearZero,
                HoUrpBuiltInNames.DebugViews.SssDiffusion,
                "_lilHoSSSDiffusedTexture",
                "Diffused SSS color and composite weight."));

            registry.Resources.Register(new ResourceDefinition(
                HoUrpBuiltInNames.Resources.SemanticPostMask,
                ResourceKind.Texture2D,
                HoUrpBuiltInNames.Semantics.CompositeSemanticPostMask,
                HoUrpBuiltInNames.Features.SemanticPostProcess,
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Features.DebugComposite),
                ResourceFormatHint.MaskRgba8,
                ResourceScale.Full,
                HoUrpLifetime.PerCamera,
                ResourceClearPolicy.ClearZero,
                HoUrpBuiltInNames.DebugViews.SemanticPostMask,
                "HoPostAovMask",
                "Packed SemanticPost layer masks."));
        }

        private static void RegisterFeatures(HoUrpContractRegistry registry)
        {
            registry.Features.Register(new FeatureDescriptor(
                HoUrpBuiltInNames.Features.GeneratedMaterial,
                HoUrpDomain.Material,
                HoUrpPassStage.MaterialShadingSemanticAov,
                new ReadOnlyArray<HoUrpIdentifier>(),
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
                    HoUrpBuiltInNames.Semantics.ShadingSssWeight,
                    HoUrpBuiltInNames.Semantics.TransparentColor,
                    HoUrpBuiltInNames.Semantics.TransparentAlpha,
                    HoUrpBuiltInNames.Semantics.TransparentCoverage,
                    HoUrpBuiltInNames.Semantics.OitAccumulationInput,
                    HoUrpBuiltInNames.Semantics.OitRevealageInput),
                new ReadOnlyArray<HoUrpIdentifier>(),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Capabilities.SupportsOit,
                    HoUrpBuiltInNames.Capabilities.ParticipatesOit),
                new ReadOnlyArray<HoUrpIdentifier>(),
                HoUrpMigrationDecision.Replace,
                "future generated material system",
                "Stage-ten declaration for generated material shader ABI. It produces material and OIT-ready semantics but owns no RenderGraph resources."));

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
                    HoUrpBuiltInNames.Resources.AovMaterialCustom0_3,
                    HoUrpBuiltInNames.Resources.AovSssSource),
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
                    HoUrpBuiltInNames.Semantics.ShadingSssSourceColor,
                    HoUrpBuiltInNames.Semantics.ShadingSssWeight,
                    HoUrpBuiltInNames.Semantics.GeometryWorldNormal,
                    HoUrpBuiltInNames.Semantics.GeometryLinearDepth),
                new ReadOnlyArray<HoUrpIdentifier>(),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Capabilities.WritesAov,
                    HoUrpBuiltInNames.Capabilities.SupportsDebugView),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.DebugViews.AovMask,
                    HoUrpBuiltInNames.DebugViews.AovObjectId,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag0,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag1,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag2,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag3,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag4,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag5,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag6,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag7,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag8,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag9,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag10,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag11,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag12,
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
                    HoUrpBuiltInNames.DebugViews.AovMaterialCustom3,
                    HoUrpBuiltInNames.DebugViews.AovSssSource,
                    HoUrpBuiltInNames.DebugViews.AovSssWeight,
                    HoUrpBuiltInNames.DebugViews.SssProfileId,
                    HoUrpBuiltInNames.DebugViews.SssThickness,
                    HoUrpBuiltInNames.DebugViews.SssCurvature),
                HoUrpMigrationDecision.KeepConceptRename,
                "Runtime/AOV/HoAovRendererFeature.cs",
                "Minimum AOV contract for object, geometry, material, and phase-five SSS input resources."));

            registry.Features.Register(new FeatureDescriptor(
                HoUrpBuiltInNames.Features.SemanticPostProcess,
                HoUrpDomain.Composite,
                HoUrpPassStage.SemanticPost,
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Resources.SemanticPostMask),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Resources.AovMaskId,
                    HoUrpBuiltInNames.Resources.AovNormalDepth,
                    HoUrpBuiltInNames.Resources.AovObjectCustom0_3,
                    HoUrpBuiltInNames.Resources.AovObjectCustom4_7,
                    HoUrpBuiltInNames.Resources.AovSurfaceData,
                    HoUrpBuiltInNames.Resources.AovMaterialCustom0_3,
                    HoUrpBuiltInNames.Resources.AovSssSource,
                    HoUrpBuiltInNames.Resources.SssSource,
                    HoUrpBuiltInNames.Resources.SssDiffusion),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Semantics.CompositeSemanticPostMask),
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
                    HoUrpBuiltInNames.Semantics.ShadingSssSourceColor,
                    HoUrpBuiltInNames.Semantics.ShadingSssWeight,
                    HoUrpBuiltInNames.Semantics.ShadingSssDiffusionColor,
                    HoUrpBuiltInNames.Semantics.ShadingSssCompositeWeight,
                    HoUrpBuiltInNames.Semantics.GeometryWorldNormal,
                    HoUrpBuiltInNames.Semantics.GeometryLinearDepth),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Capabilities.RequiresAov,
                    HoUrpBuiltInNames.Capabilities.SupportsDebugView),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.DebugViews.AovMask,
                    HoUrpBuiltInNames.DebugViews.AovObjectId,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag0,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag1,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag2,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag3,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag4,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag5,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag6,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag7,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag8,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag9,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag10,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag11,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag12,
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
                    HoUrpBuiltInNames.DebugViews.AovMaterialCustom3,
                    HoUrpBuiltInNames.DebugViews.AovSssSource,
                    HoUrpBuiltInNames.DebugViews.AovSssWeight,
                    HoUrpBuiltInNames.DebugViews.SssProfileId,
                    HoUrpBuiltInNames.DebugViews.SssThickness,
                    HoUrpBuiltInNames.DebugViews.SssCurvature,
                    HoUrpBuiltInNames.DebugViews.SemanticPostMask),
                HoUrpMigrationDecision.KeepConceptRename,
                "Runtime/HoPostProcessing/HoPostProcessRendererFeature.cs",
                "Minimum read-only AOV consumer for semantic post processing."));

            registry.Features.Register(new FeatureDescriptor(
                HoUrpBuiltInNames.Features.ScreenPost,
                HoUrpDomain.Composite,
                HoUrpPassStage.SemanticPost,
                new ReadOnlyArray<HoUrpIdentifier>(),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Resources.AovMaskId,
                    HoUrpBuiltInNames.Resources.AovNormalDepth),
                new ReadOnlyArray<HoUrpIdentifier>(),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Semantics.ObjectMaskWeight,
                    HoUrpBuiltInNames.Semantics.GeometryLinearDepth),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Capabilities.RequiresAov),
                new ReadOnlyArray<HoUrpIdentifier>(),
                HoUrpMigrationDecision.KeepConceptRename,
                "HoPost rule-mask prototype",
                "Stage-eleven ScreenPost prototype. It consumes explicitly declared AOV inputs and writes only to the current frame color target."));

            registry.Features.Register(new FeatureDescriptor(
                HoUrpBuiltInNames.Features.ImagePost,
                HoUrpDomain.Image,
                HoUrpPassStage.ImagePost,
                new ReadOnlyArray<HoUrpIdentifier>(),
                new ReadOnlyArray<HoUrpIdentifier>(),
                new ReadOnlyArray<HoUrpIdentifier>(),
                new ReadOnlyArray<HoUrpIdentifier>(),
                new ReadOnlyArray<HoUrpIdentifier>(),
                new ReadOnlyArray<HoUrpIdentifier>(),
                HoUrpMigrationDecision.KeepConceptRename,
                "Shoost image-chain prototype",
                "Stage-eleven ImagePost prototype. It uses frame-local Image.WorkA/Image.WorkB ping-pong textures without publishing them as public semantic resources."));

            registry.Features.Register(new FeatureDescriptor(
                HoUrpBuiltInNames.Features.SubsurfaceScattering,
                HoUrpDomain.Shading,
                HoUrpPassStage.ScreenSss,
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Resources.SssSource,
                    HoUrpBuiltInNames.Resources.SssDiffusion),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Resources.AovMaskId,
                    HoUrpBuiltInNames.Resources.AovNormalDepth,
                    HoUrpBuiltInNames.Resources.AovSurfaceData,
                    HoUrpBuiltInNames.Resources.AovSssSource),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Semantics.ShadingSssDiffusionColor,
                    HoUrpBuiltInNames.Semantics.ShadingSssCompositeWeight),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Semantics.ObjectMaskWeight,
                    HoUrpBuiltInNames.Semantics.GeometryWorldNormal,
                    HoUrpBuiltInNames.Semantics.GeometryLinearDepth,
                    HoUrpBuiltInNames.Semantics.MaterialSssProfile,
                    HoUrpBuiltInNames.Semantics.MaterialThickness,
                    HoUrpBuiltInNames.Semantics.MaterialCurvature,
                    HoUrpBuiltInNames.Semantics.ShadingSssSourceColor,
                    HoUrpBuiltInNames.Semantics.ShadingSssWeight),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Capabilities.RequiresAov,
                    HoUrpBuiltInNames.Capabilities.SupportsDebugView),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.DebugViews.SssMask,
                    HoUrpBuiltInNames.DebugViews.SssSource,
                    HoUrpBuiltInNames.DebugViews.SssDiffusion,
                    HoUrpBuiltInNames.DebugViews.SssCompositeWeight),
                HoUrpMigrationDecision.KeepConceptRename,
                "Runtime/SubsurfaceScattering/HoSubsurfaceScatteringRendererFeature.cs",
                "Minimum screen-space SSS source, diffusion, and composite feature."));

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
                    HoUrpBuiltInNames.Resources.AovMaterialCustom0_3,
                    HoUrpBuiltInNames.Resources.AovSssSource,
                    HoUrpBuiltInNames.Resources.SssSource,
                    HoUrpBuiltInNames.Resources.SssDiffusion,
                    HoUrpBuiltInNames.Resources.SemanticPostMask),
                new ReadOnlyArray<HoUrpIdentifier>(),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Semantics.ObjectMaskWeight,
                    HoUrpBuiltInNames.Semantics.ObjectId,
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
                    HoUrpBuiltInNames.Semantics.ShadingSssSourceColor,
                    HoUrpBuiltInNames.Semantics.ShadingSssWeight,
                    HoUrpBuiltInNames.Semantics.ShadingSssDiffusionColor,
                    HoUrpBuiltInNames.Semantics.ShadingSssCompositeWeight,
                    HoUrpBuiltInNames.Semantics.CompositeSemanticPostMask,
                    HoUrpBuiltInNames.Semantics.GeometryWorldNormal,
                    HoUrpBuiltInNames.Semantics.GeometryLinearDepth),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Capabilities.SupportsDebugView),
                new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.DebugViews.AovMask,
                    HoUrpBuiltInNames.DebugViews.AovObjectId,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag0,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag1,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag2,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag3,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag4,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag5,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag6,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag7,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag8,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag9,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag10,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag11,
                    HoUrpBuiltInNames.DebugViews.AovObjectFlag12,
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
                    HoUrpBuiltInNames.DebugViews.AovMaterialCustom3,
                    HoUrpBuiltInNames.DebugViews.AovSssSource,
                    HoUrpBuiltInNames.DebugViews.AovSssWeight,
                    HoUrpBuiltInNames.DebugViews.SssMask,
                    HoUrpBuiltInNames.DebugViews.SssSource,
                    HoUrpBuiltInNames.DebugViews.SssDiffusion,
                    HoUrpBuiltInNames.DebugViews.SssCompositeWeight,
                    HoUrpBuiltInNames.DebugViews.SssProfileId,
                    HoUrpBuiltInNames.DebugViews.SssThickness,
                    HoUrpBuiltInNames.DebugViews.SssCurvature,
                    HoUrpBuiltInNames.DebugViews.SemanticPostMask),
                HoUrpMigrationDecision.Replace,
                "old per-feature debug passes",
                "Central debug composite owner for registered debug views."));
        }
    }
}
