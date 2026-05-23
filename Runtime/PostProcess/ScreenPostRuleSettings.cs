using System;
using System.Collections.Generic;
using HoUrp.Extensions.Core;
using HoUrp.Extensions.Resources;
using UnityEngine;

namespace HoUrp.Extensions.PostProcess
{
    public enum ScreenPostRuleSource
    {
        Always = 0,
        MaskWeight = 1,
        ObjectId = 2,
        GroupId = 3,
        Flags = 4,
        ObjectCustom0 = 10,
        ObjectCustom1 = 11,
        ObjectCustom2 = 12,
        ObjectCustom3 = 13,
        ObjectCustom4 = 14,
        ObjectCustom5 = 15,
        ObjectCustom6 = 16,
        ObjectCustom7 = 17,
        MaterialClass = 20,
        Thickness = 22,
        Curvature = 23,
        LinearDepth = 50,
        WorldNormalFacing = 51
    }

    public enum ScreenPostRuleOperator
    {
        Always = 0,
        Greater = 1,
        Less = 2,
        Range = 3,
        EqualByte = 4,
        FlagsAny = 5,
        FlagsAll = 6
    }

    public enum ScreenPostRuleCombine
    {
        Replace = 0,
        Or = 1,
        And = 2,
        Subtract = 3,
        Multiply = 4
    }

    public enum ScreenPostBlendMode
    {
        Alpha = 0,
        Add = 1,
        Multiply = 2,
        Screen = 3
    }

    [Serializable]
    public sealed class ScreenPostRule
    {
        [SerializeField]
        public bool enabled = true;

        [SerializeField]
        public ScreenPostRuleSource source = ScreenPostRuleSource.MaskWeight;

        [SerializeField]
        public ScreenPostRuleOperator op = ScreenPostRuleOperator.Greater;

        [SerializeField]
        public ScreenPostRuleCombine combine = ScreenPostRuleCombine.Replace;

        [SerializeField]
        public Vector2 range = new Vector2(0.01f, 1.0f);

        [SerializeField]
        [Range(0, 255)]
        public int byteValue;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        public float weight = 1.0f;
    }

    [Serializable]
    public sealed class ScreenPostRuleSet
    {
        public const int MaxRuleCount = 4;

        [SerializeField]
        public bool enabled = true;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        public float defaultValue;

        [SerializeField]
        public bool invert;

        [SerializeField]
        public string debugName = "ScreenPost.RuleSet";

        [SerializeField]
        public ScreenPostRule[] rules = CreateDefaultRules();

        public static ScreenPostRule[] CreateDefaultRules()
        {
            return new[]
            {
                new ScreenPostRule(),
                new ScreenPostRule { enabled = false },
                new ScreenPostRule { enabled = false },
                new ScreenPostRule { enabled = false }
            };
        }

        public void EnsureRules()
        {
            if (rules == null || rules.Length != MaxRuleCount)
            {
                ScreenPostRule[] defaults = CreateDefaultRules();
                if (rules != null)
                {
                    int copyCount = Mathf.Min(rules.Length, defaults.Length);
                    for (int i = 0; i < copyCount; i++)
                    {
                        if (rules[i] != null)
                        {
                            defaults[i] = rules[i];
                        }
                    }
                }

                rules = defaults;
            }

            for (int i = 0; i < rules.Length; i++)
            {
                if (rules[i] == null)
                {
                    rules[i] = new ScreenPostRule { enabled = false };
                }
            }
        }

        public ReadOnlyArray<PostResourceRequest> BuildResourceRequests(HoUrpIdentifier ownerLayer)
        {
            EnsureRules();

            if (!enabled)
            {
                return new ReadOnlyArray<PostResourceRequest>();
            }

            var requests = new List<PostResourceRequest>();
            bool needsMaskId = false;
            bool needsObjectCustom0 = false;
            bool needsObjectCustom1 = false;
            bool needsSurfaceData = false;
            bool needsNormalDepth = false;

            for (int i = 0; i < rules.Length; i++)
            {
                ScreenPostRule rule = rules[i];
                if (rule == null || !rule.enabled)
                {
                    continue;
                }

                switch (rule.source)
                {
                    case ScreenPostRuleSource.MaskWeight:
                    case ScreenPostRuleSource.ObjectId:
                    case ScreenPostRuleSource.GroupId:
                    case ScreenPostRuleSource.Flags:
                        needsMaskId = true;
                        break;
                    case ScreenPostRuleSource.ObjectCustom0:
                    case ScreenPostRuleSource.ObjectCustom1:
                    case ScreenPostRuleSource.ObjectCustom2:
                    case ScreenPostRuleSource.ObjectCustom3:
                        needsObjectCustom0 = true;
                        break;
                    case ScreenPostRuleSource.ObjectCustom4:
                    case ScreenPostRuleSource.ObjectCustom5:
                    case ScreenPostRuleSource.ObjectCustom6:
                    case ScreenPostRuleSource.ObjectCustom7:
                        needsObjectCustom1 = true;
                        break;
                    case ScreenPostRuleSource.MaterialClass:
                    case ScreenPostRuleSource.Thickness:
                    case ScreenPostRuleSource.Curvature:
                        needsSurfaceData = true;
                        break;
                    case ScreenPostRuleSource.LinearDepth:
                    case ScreenPostRuleSource.WorldNormalFacing:
                        needsNormalDepth = true;
                        break;
                }
            }

            if (needsMaskId)
            {
                requests.Add(CreateSemanticInputRequest(ownerLayer, HoUrpBuiltInNames.Resources.AovMaskId, "ScreenPost.RuleMask.AovMaskId", MaskIdSemantics()));
            }

            if (needsObjectCustom0)
            {
                requests.Add(CreateSemanticInputRequest(ownerLayer, HoUrpBuiltInNames.Resources.AovObjectCustom0_3, "ScreenPost.RuleMask.AovObjectCustom0_3", ObjectCustomSemantics(0)));
            }

            if (needsObjectCustom1)
            {
                requests.Add(CreateSemanticInputRequest(ownerLayer, HoUrpBuiltInNames.Resources.AovObjectCustom4_7, "ScreenPost.RuleMask.AovObjectCustom4_7", ObjectCustomSemantics(4)));
            }

            if (needsSurfaceData)
            {
                requests.Add(CreateSemanticInputRequest(ownerLayer, HoUrpBuiltInNames.Resources.AovSurfaceData, "ScreenPost.RuleMask.AovSurfaceData", SurfaceDataSemantics()));
            }

            if (needsNormalDepth)
            {
                requests.Add(CreateSemanticInputRequest(ownerLayer, HoUrpBuiltInNames.Resources.AovNormalDepth, "ScreenPost.RuleMask.AovNormalDepth", NormalDepthSemantics()));
            }

            return new ReadOnlyArray<PostResourceRequest>(requests.ToArray());
        }

        private static PostResourceRequest CreateSemanticInputRequest(
            HoUrpIdentifier ownerLayer,
            HoUrpIdentifier resourceId,
            string debugName,
            ReadOnlyArray<HoUrpIdentifier> readSemantics)
        {
            return new PostResourceRequest(
                HoUrpIdentifier.From(ownerLayer.Value + "." + resourceId.Value + ".Request"),
                HoUrpBuiltInNames.Features.ScreenPost,
                ownerLayer,
                HoUrpBuiltInNames.PostEffects.ScreenPostRuleMaskPrototype,
                PostResourceRequestKind.SemanticInput,
                resourceId,
                ResourceFormatHint.External,
                ResourceScale.External,
                HoUrpLifetime.Imported,
                ResourceClearPolicy.External,
                readSemantics,
                HoUrpBuiltInNames.PostInputs.None,
                debugName);
        }

        private static ReadOnlyArray<HoUrpIdentifier> MaskIdSemantics()
        {
            return new ReadOnlyArray<HoUrpIdentifier>(
                HoUrpBuiltInNames.Semantics.ObjectMaskWeight,
                HoUrpBuiltInNames.Semantics.ObjectId,
                HoUrpBuiltInNames.Semantics.ObjectGroupId,
                HoUrpBuiltInNames.Semantics.ObjectFlags);
        }

        private static ReadOnlyArray<HoUrpIdentifier> ObjectCustomSemantics(int first)
        {
            return first == 0
                ? new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Semantics.ObjectCustom0,
                    HoUrpBuiltInNames.Semantics.ObjectCustom1,
                    HoUrpBuiltInNames.Semantics.ObjectCustom2,
                    HoUrpBuiltInNames.Semantics.ObjectCustom3)
                : new ReadOnlyArray<HoUrpIdentifier>(
                    HoUrpBuiltInNames.Semantics.ObjectCustom4,
                    HoUrpBuiltInNames.Semantics.ObjectCustom5,
                    HoUrpBuiltInNames.Semantics.ObjectCustom6,
                    HoUrpBuiltInNames.Semantics.ObjectCustom7);
        }

        private static ReadOnlyArray<HoUrpIdentifier> SurfaceDataSemantics()
        {
            return new ReadOnlyArray<HoUrpIdentifier>(
                HoUrpBuiltInNames.Semantics.MaterialClass,
                HoUrpBuiltInNames.Semantics.MaterialThickness,
                HoUrpBuiltInNames.Semantics.MaterialCurvature);
        }

        private static ReadOnlyArray<HoUrpIdentifier> NormalDepthSemantics()
        {
            return new ReadOnlyArray<HoUrpIdentifier>(
                HoUrpBuiltInNames.Semantics.GeometryLinearDepth,
                HoUrpBuiltInNames.Semantics.GeometryWorldNormal);
        }
    }
}
