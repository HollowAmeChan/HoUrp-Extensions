using System.Collections.Generic;
using HoUrp.Extensions.Core;
using UnityEngine;

namespace HoUrp.Extensions.Semantic
{
    public enum ObjectSemanticPreset
    {
        Subject = 0,
        Face = 1,
        Hair = 2,
        Eye = 3,
        Accessory = 4,
        Cloth = 5,
        Prop = 6,
        Clear = 7
    }

    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ObjectSemanticAuthoring : MonoBehaviour
    {
        public const int SemanticPostReceiverFlag = 1 << 0;

        private static readonly List<Renderer> RendererCache = new List<Renderer>();

        [SerializeField]
        private bool includeChildren = true;

        [SerializeField]
        private bool writesAov = true;

        [SerializeField]
        private bool receivesSemanticPost = true;

        [SerializeField]
        [Range(0.0f, 1.0f)]
        private float maskWeight = 1.0f;

        [SerializeField]
        [Range(0, 255)]
        [HideInInspector]
        private int objectCustomMask;

        [SerializeField]
        private bool custom0Subject;

        [SerializeField]
        private bool custom1Face;

        [SerializeField]
        private bool custom2Hair;

        [SerializeField]
        private bool custom3Eye;

        [SerializeField]
        private bool custom4Accessory;

        [SerializeField]
        private bool custom5Cloth;

        [SerializeField]
        private bool custom6Prop;

        [SerializeField]
        private bool custom7Reserved;

        [SerializeField]
        [Range(0, 255)]
        private int objectId = 1;

        [SerializeField]
        [Range(0, 255)]
        private int groupId;

        [SerializeField]
        [Range(0, 255)]
        [HideInInspector]
        private int flags;

        [SerializeField]
        private bool flag0;

        [SerializeField]
        private bool flag1;

        [SerializeField]
        private bool flag2;

        [SerializeField]
        private bool flag3;

        [SerializeField]
        private bool flag4;

        [SerializeField]
        private bool flag5;

        [SerializeField]
        private bool flag6;

        [SerializeField]
        private bool flag7;

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

        public bool WritesAov
        {
            get => writesAov;
            set
            {
                writesAov = value;
                ApplyToRenderers();
            }
        }

        public bool ReceivesSemanticPost
        {
            get => receivesSemanticPost;
            set
            {
                receivesSemanticPost = value;
                ApplyToRenderers();
            }
        }

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
                UnpackObjectCustomMask();
                ApplyToRenderers();
            }
        }

        public int ObjectId
        {
            get => objectId;
            set
            {
                objectId = Mathf.Clamp(value, 0, 255);
                ApplyToRenderers();
            }
        }

        public int GroupId
        {
            get => groupId;
            set
            {
                groupId = Mathf.Clamp(value, 0, 255);
                ApplyToRenderers();
            }
        }

        public int Flags
        {
            get => flags;
            set
            {
                flags = Mathf.Clamp(value, 0, 255);
                UnpackFlags();
                ApplyToRenderers();
            }
        }

        public int EffectiveFlags => BuildEffectiveFlags();

        public static int GetPresetObjectCustomMask(ObjectSemanticPreset preset)
        {
            switch (preset)
            {
                case ObjectSemanticPreset.Face:
                    return (1 << 0) | (1 << 1);
                case ObjectSemanticPreset.Hair:
                    return (1 << 0) | (1 << 2);
                case ObjectSemanticPreset.Eye:
                    return (1 << 0) | (1 << 3);
                case ObjectSemanticPreset.Accessory:
                    return (1 << 0) | (1 << 4);
                case ObjectSemanticPreset.Cloth:
                    return (1 << 0) | (1 << 5);
                case ObjectSemanticPreset.Prop:
                    return 1 << 6;
                case ObjectSemanticPreset.Clear:
                    return 0;
                default:
                    return 1 << 0;
            }
        }

        public void ApplyPreset(ObjectSemanticPreset preset)
        {
            bool clear = preset == ObjectSemanticPreset.Clear;
            writesAov = !clear;
            receivesSemanticPost = !clear && preset != ObjectSemanticPreset.Prop;
            maskWeight = clear ? 0.0f : 1.0f;
            objectCustomMask = GetPresetObjectCustomMask(preset);
            UnpackObjectCustomMask();
            ApplyToRenderers();
        }

        public void ResetObjectSemantics()
        {
            writesAov = true;
            receivesSemanticPost = true;
            maskWeight = 1.0f;
            objectCustomMask = 0;
            UnpackObjectCustomMask();
            objectId = 1;
            groupId = 0;
            flags = 0;
            UnpackFlags();
            ApplyToRenderers();
        }

        public void SetObjectCustomBit(int index, bool enabled)
        {
            SetBit(ref objectCustomMask, index, enabled);
            UnpackObjectCustomMask();
            ApplyToRenderers();
        }

        private void Reset()
        {
            ResetObjectSemantics();
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
            if (objectCustomMask != 0 && !HasAnyObjectCustomBit())
            {
                UnpackObjectCustomMask();
            }

            objectCustomMask = PackObjectCustomMask();
            objectId = Mathf.Clamp(objectId, 0, 255);
            groupId = Mathf.Clamp(groupId, 0, 255);
            if (flags != 0 && !HasAnyFlagBit())
            {
                UnpackFlags();
            }

            flags = PackFlags();
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
            objectCustomMask = PackObjectCustomMask();
            flags = PackFlags();
            int effectiveFlags = BuildEffectiveFlags();
            CollectRenderers();
            for (int i = 0; i < RendererCache.Count; i++)
            {
                Renderer targetRenderer = RendererCache[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.AovMaskWeight, writesAov ? maskWeight : 0.0f);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.ObjectCustomMask, objectCustomMask);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.ObjectId, objectId);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.ObjectGroupId, groupId);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.ObjectFlags, effectiveFlags);
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
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.ObjectId, 1.0f);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.ObjectGroupId, 0.0f);
                propertyBlock.SetFloat(HoUrpShaderPropertyIds.ObjectFlags, 0.0f);
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

        private int PackObjectCustomMask()
        {
            int mask = 0;
            if (custom0Subject) mask |= 1 << 0;
            if (custom1Face) mask |= 1 << 1;
            if (custom2Hair) mask |= 1 << 2;
            if (custom3Eye) mask |= 1 << 3;
            if (custom4Accessory) mask |= 1 << 4;
            if (custom5Cloth) mask |= 1 << 5;
            if (custom6Prop) mask |= 1 << 6;
            if (custom7Reserved) mask |= 1 << 7;
            return mask;
        }

        private void UnpackObjectCustomMask()
        {
            custom0Subject = HasBit(objectCustomMask, 0);
            custom1Face = HasBit(objectCustomMask, 1);
            custom2Hair = HasBit(objectCustomMask, 2);
            custom3Eye = HasBit(objectCustomMask, 3);
            custom4Accessory = HasBit(objectCustomMask, 4);
            custom5Cloth = HasBit(objectCustomMask, 5);
            custom6Prop = HasBit(objectCustomMask, 6);
            custom7Reserved = HasBit(objectCustomMask, 7);
        }

        private bool HasAnyObjectCustomBit()
        {
            return custom0Subject
                || custom1Face
                || custom2Hair
                || custom3Eye
                || custom4Accessory
                || custom5Cloth
                || custom6Prop
                || custom7Reserved;
        }

        private int PackFlags()
        {
            int packed = 0;
            if (flag0) packed |= 1 << 0;
            if (flag1) packed |= 1 << 1;
            if (flag2) packed |= 1 << 2;
            if (flag3) packed |= 1 << 3;
            if (flag4) packed |= 1 << 4;
            if (flag5) packed |= 1 << 5;
            if (flag6) packed |= 1 << 6;
            if (flag7) packed |= 1 << 7;
            return packed;
        }

        private int BuildEffectiveFlags()
        {
            int packed = PackFlags() & ~SemanticPostReceiverFlag;
            return receivesSemanticPost ? packed | SemanticPostReceiverFlag : packed;
        }

        private void UnpackFlags()
        {
            flag0 = HasBit(flags, 0);
            flag1 = HasBit(flags, 1);
            flag2 = HasBit(flags, 2);
            flag3 = HasBit(flags, 3);
            flag4 = HasBit(flags, 4);
            flag5 = HasBit(flags, 5);
            flag6 = HasBit(flags, 6);
            flag7 = HasBit(flags, 7);
        }

        private bool HasAnyFlagBit()
        {
            return flag0 || flag1 || flag2 || flag3 || flag4 || flag5 || flag6 || flag7;
        }

        private static bool HasBit(int mask, int index)
        {
            return (mask & (1 << Mathf.Clamp(index, 0, 7))) != 0;
        }

        private static void SetBit(ref int mask, int index, bool enabled)
        {
            int bit = 1 << Mathf.Clamp(index, 0, 7);
            mask = enabled ? mask | bit : mask & ~bit;
            mask = Mathf.Clamp(mask, 0, 255);
        }
    }
}
