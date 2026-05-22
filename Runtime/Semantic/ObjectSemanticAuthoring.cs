using System.Collections.Generic;
using HoUrp.Extensions.Core;
using UnityEngine;

namespace HoUrp.Extensions.Semantic
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ObjectSemanticAuthoring : MonoBehaviour
    {
        private static readonly List<Renderer> RendererCache = new List<Renderer>();

        [SerializeField]
        private bool includeChildren = true;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float maskWeight = 1.0f;

        [SerializeField]
        [Range(0, 255)]
        private int objectCustomMask;

        private MaterialPropertyBlock propertyBlock;

        public float MaskWeight
        {
            get => maskWeight;
            set
            {
                maskWeight = Mathf.Clamp01(value);
                ApplyToRenderers();
            }
        }

        public int ObjectCustomMask
        {
            get => objectCustomMask;
            set
            {
                objectCustomMask = Mathf.Clamp(value, 0, 255);
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
            maskWeight = Mathf.Clamp01(maskWeight);
            objectCustomMask = Mathf.Clamp(objectCustomMask, 0, 255);
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
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.AovMaskWeight, maskWeight);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.ObjectCustomMask, objectCustomMask);
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
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.AovMaskWeight, 1.0f);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.ObjectCustomMask, 0.0f);
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
