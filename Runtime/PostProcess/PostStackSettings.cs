using System;
using HoUrp.Extensions.Core;
using UnityEngine;

namespace HoUrp.Extensions.PostProcess
{
    [Serializable]
    public sealed class ScreenPostLayerSettings
    {
        [SerializeField]
        public bool enabled = true;

        [SerializeField]
        public string name = "ScreenPost Layer";

        [SerializeField]
        public ScreenPostBlendMode blendMode = ScreenPostBlendMode.Alpha;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        public float opacity = 0.5f;

        [SerializeField]
        public Color color = new Color(0.05f, 0.85f, 1.0f, 1.0f);

        [SerializeField]
        public ScreenPostRuleSet ruleSet = new ScreenPostRuleSet();

        public static ScreenPostLayerSettings CreateDefault(string name = "Cyan Mask Tint")
        {
            return new ScreenPostLayerSettings
            {
                enabled = true,
                name = name,
                blendMode = ScreenPostBlendMode.Alpha,
                opacity = 0.5f,
                color = new Color(0.05f, 0.85f, 1.0f, 1.0f),
                ruleSet = new ScreenPostRuleSet()
            };
        }

        public static ScreenPostLayerSettings CreateDisabled(string name = "ScreenPost Layer")
        {
            return new ScreenPostLayerSettings
            {
                enabled = false,
                name = name,
                opacity = 0.0f,
                color = Color.white,
                ruleSet = new ScreenPostRuleSet()
            };
        }

        public void Ensure()
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "ScreenPost Layer";
            }

            if (ruleSet == null)
            {
                ruleSet = new ScreenPostRuleSet();
            }

            ruleSet.EnsureRules();
            opacity = Mathf.Clamp01(opacity);
        }

        public PostLayerDefinition ToPostLayerDefinition(int index)
        {
            Ensure();
            HoUrpIdentifier layerId = HoUrpIdentifier.From("ScreenPost.Layer." + index.ToString("00"));
            return new PostLayerDefinition(
                layerId,
                PostEffectDomain.ScreenPost,
                enabled && opacity > 0.0001f && ruleSet.enabled,
                new ReadOnlyArray<HoUrpIdentifier>(HoUrpBuiltInNames.PostEffects.ScreenPostRuleMaskPrototype),
                ruleSet.BuildResourceRequests(layerId));
        }
    }

    [Serializable]
    public sealed class ImagePostFilterSettings
    {
        [SerializeField]
        public bool enabled = true;

        [SerializeField]
        public string name = "ImagePost Color Adjust";

        [SerializeField]
        public Color colorTint = Color.white;

        [SerializeField]
        [Range(0.0f, 2.0f)]
        public float brightness = 1.0f;

        [SerializeField]
        [Range(0.0f, 2.0f)]
        public float contrast = 1.0f;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        public float tintStrength = 0.15f;

        [SerializeField]
        public bool blurEnabled = false;

        [SerializeField]
        [Range(0.0f, 8.0f)]
        public float blurRadius = 0.0f;

        [SerializeField]
        [Range(1, 13)]
        public int blurSampleCount = 5;

        public static ImagePostFilterSettings CreateDefault(string name = "Color Adjust")
        {
            return new ImagePostFilterSettings
            {
                enabled = true,
                name = name,
                colorTint = Color.white,
                brightness = 1.0f,
                contrast = 1.0f,
                tintStrength = 0.15f,
                blurEnabled = false,
                blurRadius = 0.0f,
                blurSampleCount = 5
            };
        }

        public void Ensure()
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "ImagePost Color Adjust";
            }

            brightness = Mathf.Max(0.0f, brightness);
            contrast = Mathf.Max(0.0f, contrast);
            tintStrength = Mathf.Clamp01(tintStrength);
            blurRadius = Mathf.Max(0.0f, blurRadius);
            blurSampleCount = Mathf.Clamp(blurSampleCount, 1, 13);
        }

        public PostLayerDefinition ToPostLayerDefinition(int index)
        {
            Ensure();
            return new PostLayerDefinition(
                HoUrpIdentifier.From("ImagePost.Layer." + index.ToString("00")),
                PostEffectDomain.ImagePost,
                enabled,
                new ReadOnlyArray<HoUrpIdentifier>(HoUrpBuiltInNames.PostEffects.ImagePostColorAdjustPrototype));
        }
    }

    public static class PostStackSettings
    {
        public static PostStackDefinition BuildScreenPostStack(ScreenPostLayerSettings[] layers)
        {
            if (layers == null || layers.Length == 0)
            {
                return new PostStackDefinition(new ReadOnlyArray<PostLayerDefinition>());
            }

            PostLayerDefinition[] definitions = new PostLayerDefinition[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                ScreenPostLayerSettings layer = layers[i] ?? ScreenPostLayerSettings.CreateDisabled();
                definitions[i] = layer.ToPostLayerDefinition(i);
            }

            return new PostStackDefinition(new ReadOnlyArray<PostLayerDefinition>(definitions));
        }

        public static PostStackDefinition BuildImagePostStack(ImagePostFilterSettings[] filters)
        {
            if (filters == null || filters.Length == 0)
            {
                return new PostStackDefinition(new ReadOnlyArray<PostLayerDefinition>());
            }

            PostLayerDefinition[] definitions = new PostLayerDefinition[filters.Length];
            for (int i = 0; i < filters.Length; i++)
            {
                ImagePostFilterSettings filter = filters[i] ?? ImagePostFilterSettings.CreateDefault();
                definitions[i] = filter.ToPostLayerDefinition(i);
            }

            return new PostStackDefinition(new ReadOnlyArray<PostLayerDefinition>(definitions));
        }
    }
}
