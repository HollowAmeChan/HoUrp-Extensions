using System.Collections.Generic;
using HoUrp.Extensions.Core;
using UnityEngine;

namespace HoUrp.Extensions.Semantic
{
    public enum MaterialSemanticPreset
    {
        DefaultOpaque = 0,
        SkinSss = 1,
        Hair = 2,
        Eye = 3,
        Cloth = 4,
        Metal = 5,
        Clear = 6
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class MaterialSemanticAuthoring : MonoBehaviour
    {
        private static readonly List<Renderer> RendererCache = new List<Renderer>();

        [SerializeField]
        private bool includeChildren = true;

        [SerializeField]
        [Range(0, 255)]
        private int materialClass;

        [SerializeField]
        [Range(0, 255)]
        private int sssProfile;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float thickness;

        [SerializeField]
        [Range(-1.0f, 1.0f)]
        private float curvature;

        [SerializeField]
        private Vector4 materialCustom0_3;

        [SerializeField]
        private Color sssSourceColor = Color.black;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float sssWeight;

        private MaterialPropertyBlock propertyBlock;

        public bool IncludeChildren
        {
            get => includeChildren;
            set
            {
                includeChildren = value;
                ApplyToRenderers();
            }
        }

        public int MaterialClass
        {
            get => materialClass;
            set
            {
                materialClass = Mathf.Clamp(value, 0, 255);
                ApplyToRenderers();
            }
        }

        public int SssProfile
        {
            get => sssProfile;
            set
            {
                sssProfile = Mathf.Clamp(value, 0, 255);
                ApplyToRenderers();
            }
        }

        public float Thickness
        {
            get => thickness;
            set
            {
                thickness = Mathf.Clamp01(value);
                ApplyToRenderers();
            }
        }

        public float Curvature
        {
            get => curvature;
            set
            {
                curvature = Mathf.Clamp(value, -1.0f, 1.0f);
                ApplyToRenderers();
            }
        }

        public Vector4 MaterialCustom0_3
        {
            get => materialCustom0_3;
            set
            {
                materialCustom0_3 = Clamp01(value);
                ApplyToRenderers();
            }
        }

        public Color SssSourceColor
        {
            get => sssSourceColor;
            set
            {
                sssSourceColor = value;
                ApplyToRenderers();
            }
        }

        public float SssWeight
        {
            get => sssWeight;
            set
            {
                sssWeight = Mathf.Clamp01(value);
                ApplyToRenderers();
            }
        }

        public void ApplyPreset(MaterialSemanticPreset preset)
        {
            switch (preset)
            {
                case MaterialSemanticPreset.SkinSss:
                    materialClass = 1;
                    sssProfile = 1;
                    thickness = 0.65f;
                    curvature = 0.25f;
                    materialCustom0_3 = new Vector4(1.0f, 0.25f, 0.0f, 0.0f);
                    sssSourceColor = new Color(1.0f, 0.35f, 0.22f, 1.0f);
                    sssWeight = 1.0f;
                    break;
                case MaterialSemanticPreset.Hair:
                    materialClass = 2;
                    sssProfile = 0;
                    thickness = 0.15f;
                    curvature = 0.35f;
                    materialCustom0_3 = new Vector4(0.65f, 1.0f, 0.0f, 0.0f);
                    sssSourceColor = Color.black;
                    sssWeight = 0.0f;
                    break;
                case MaterialSemanticPreset.Eye:
                    materialClass = 3;
                    sssProfile = 0;
                    thickness = 0.05f;
                    curvature = 0.0f;
                    materialCustom0_3 = new Vector4(0.5f, 0.25f, 1.0f, 0.0f);
                    sssSourceColor = Color.black;
                    sssWeight = 0.0f;
                    break;
                case MaterialSemanticPreset.Cloth:
                    materialClass = 4;
                    sssProfile = 0;
                    thickness = 0.25f;
                    curvature = 0.0f;
                    materialCustom0_3 = new Vector4(0.5f, 0.0f, 0.0f, 0.0f);
                    sssSourceColor = Color.black;
                    sssWeight = 0.0f;
                    break;
                case MaterialSemanticPreset.Metal:
                    materialClass = 5;
                    sssProfile = 0;
                    thickness = 0.0f;
                    curvature = 0.0f;
                    materialCustom0_3 = Vector4.zero;
                    sssSourceColor = Color.black;
                    sssWeight = 0.0f;
                    break;
                default:
                    ResetMaterialSemantics();
                    return;
            }

            ApplyToRenderers();
        }

        public void ResetMaterialSemantics()
        {
            materialClass = 0;
            sssProfile = 0;
            thickness = 0.0f;
            curvature = 0.0f;
            materialCustom0_3 = Vector4.zero;
            sssSourceColor = Color.black;
            sssWeight = 0.0f;
            ApplyToRenderers();
        }

        public void SetMaterialCustom(int channel, float value)
        {
            int clampedChannel = Mathf.Clamp(channel, 0, 3);
            materialCustom0_3[clampedChannel] = Mathf.Clamp01(value);
            ApplyToRenderers();
        }

        private void Reset()
        {
            ResetMaterialSemantics();
            ApplyToRenderers();
        }

        private void OnEnable()
        {
            ApplyToRenderers();
        }

        private void OnDisable()
        {
            ClearRenderers();
        }

        private void OnDestroy()
        {
            ClearRenderers();
        }

        private void OnValidate()
        {
            materialClass = Mathf.Clamp(materialClass, 0, 255);
            sssProfile = Mathf.Clamp(sssProfile, 0, 255);
            thickness = Mathf.Clamp01(thickness);
            curvature = Mathf.Clamp(curvature, -1.0f, 1.0f);
            materialCustom0_3 = Clamp01(materialCustom0_3);
            sssWeight = Mathf.Clamp01(sssWeight);
            if (isActiveAndEnabled)
            {
                ApplyToRenderers();
            }
        }

        public void ApplyToRenderers()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            EnsurePropertyBlock();
            CollectRenderers();
            for (int i = 0; i < RendererCache.Count; i++)
            {
                Renderer targetRenderer = RendererCache[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.MaterialClass, materialClass);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.MaterialSssProfile, sssProfile);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.MaterialThickness, thickness);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.MaterialCurvature, curvature);
                propertyBlock.SetVector(HoUrpShaderPropertyIds.MaterialCustom0_3, materialCustom0_3);
                propertyBlock.SetColor(HoUrpShaderPropertyIds.SssSourceColor, sssSourceColor);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.SssWeight, sssWeight);
                targetRenderer.SetPropertyBlock(propertyBlock);
            }

            RendererCache.Clear();
        }

        private void ClearRenderers()
        {
            EnsurePropertyBlock();
            CollectRenderers();
            for (int i = 0; i < RendererCache.Count; i++)
            {
                Renderer targetRenderer = RendererCache[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.MaterialClass, 0.0f);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.MaterialSssProfile, 0.0f);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.MaterialThickness, 0.0f);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.MaterialCurvature, 0.0f);
                propertyBlock.SetVector(HoUrpShaderPropertyIds.MaterialCustom0_3, Vector4.zero);
                propertyBlock.SetColor(HoUrpShaderPropertyIds.SssSourceColor, Color.black);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.SssWeight, 0.0f);
                targetRenderer.SetPropertyBlock(propertyBlock);
            }

            RendererCache.Clear();
        }

        private void CollectRenderers()
        {
            RendererCache.Clear();
            if (includeChildren)
            {
                GetComponentsInChildren(true, RendererCache);
                return;
            }

            if (TryGetComponent(out Renderer targetRenderer))
            {
                RendererCache.Add(targetRenderer);
            }
        }

        private void EnsurePropertyBlock()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }

        private static Vector4 Clamp01(Vector4 value)
        {
            return new Vector4(
                Mathf.Clamp01(value.x),
                Mathf.Clamp01(value.y),
                Mathf.Clamp01(value.z),
                Mathf.Clamp01(value.w));
        }
    }
}
