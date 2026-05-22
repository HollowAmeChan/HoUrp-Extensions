using System.Collections.Generic;
using HoUrp.Extensions.Core;
using UnityEngine;

namespace HoUrp.Extensions.Semantic
{
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

        private MaterialPropertyBlock propertyBlock;

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
                materialCustom0_3 = value;
                ApplyToRenderers();
            }
        }

        private void Reset()
        {
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
    }
}
