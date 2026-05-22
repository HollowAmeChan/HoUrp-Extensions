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
        public const int SemanticPostReceiverFlag = 1 << 1;
        public const int RendererStaticSemanticPostReceiverFeatureFlag = SemanticPostReceiverFlag;

        private static readonly List<ObjectSemanticAuthoring> ActiveAuthorings = new List<ObjectSemanticAuthoring>();
        private static readonly List<Renderer> RendererCache = new List<Renderer>();
        private static readonly List<Renderer> SceneRendererCache = new List<Renderer>();

        [SerializeField]
        private bool includeChildren = true;

        [SerializeField]
        private bool writesAov = true;

        [SerializeField]
        private bool receivesSemanticPost;

        [SerializeField]
        private RendererStaticSemanticBindingMode rendererStaticBindingMode = RendererStaticSemanticBindingMode.PreferRendererUserValue;

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
        [Range(0, RendererStaticSemanticValue.V1MaxObjectId)]
        private int objectId = 1;

        [SerializeField]
        [Range(0, RendererStaticSemanticValue.V1MaxGroupId)]
        private int groupId;

        [SerializeField]
        [Range(0, RendererStaticSemanticValue.V1MaxObjectFeatureFlags)]
        [HideInInspector]
        private int flags;

        [SerializeField]
        private bool featureFlag1ReceivesSemanticPost;

        [SerializeField]
        private bool featureFlag2ReceivesSss;

        [SerializeField]
        private bool featureFlag3ReceivesCharacterComposite;

        [SerializeField]
        private bool featureFlag4ReceivesOutline;

        [SerializeField]
        private bool featureFlag5ReceivesDropShadow;

        [SerializeField]
        private bool featureFlag6ReceivesStylizedShadow;

        [SerializeField]
        private bool featureFlag7ReceivesSelectiveImagePost;

        [SerializeField]
        private bool featureFlag8ReceivesHoShadow;

        [SerializeField]
        private bool featureFlag9CastsHoShadow;

        [SerializeField]
        private bool featureFlag10ParticipatesOit;

        [SerializeField]
        private bool featureFlag11Reserved;

        [SerializeField]
        private bool featureFlag12Reserved;

        private MaterialPropertyBlock propertyBlock;
        private int lastBindingTargetCount;
        private int lastRendererUserValueBindingCount;
        private int lastMaterialPropertyBlockSourceCount;

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
            get => featureFlag1ReceivesSemanticPost;
            set
            {
                featureFlag1ReceivesSemanticPost = value;
                receivesSemanticPost = value;
                ApplyToRenderers();
            }
        }

        public RendererStaticSemanticBindingMode RendererStaticBindingMode
        {
            get => rendererStaticBindingMode;
            set
            {
                rendererStaticBindingMode = value;
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
                objectId = Mathf.Clamp(value, 0, RendererStaticSemanticValue.V1MaxObjectId);
                ApplyToRenderers();
            }
        }

        public int GroupId
        {
            get => groupId;
            set
            {
                groupId = Mathf.Clamp(value, 0, RendererStaticSemanticValue.V1MaxGroupId);
                ApplyToRenderers();
            }
        }

        public int Flags
        {
            get => flags;
            set
            {
                flags = Mathf.Clamp(value, 0, RendererStaticSemanticValue.V1MaxObjectFeatureFlags)
                    & ~RendererStaticSemanticValue.V1ReservedFeatureBitMask;
                UnpackFlags();
                ApplyToRenderers();
            }
        }

        public int EffectiveFlags => BuildEffectiveFlags();

        public uint PackedRendererStaticSemantic => TryBuildRendererStaticSemanticV1(out uint packed) ? packed : 0u;

        public int ObjectFeatureFlags => BuildObjectFeatureFlags();

        public int LastBindingTargetCount => lastBindingTargetCount;

        public int LastRendererUserValueBindingCount => lastRendererUserValueBindingCount;

        public int LastMaterialPropertyBlockSourceCount => lastMaterialPropertyBlockSourceCount;

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
            featureFlag1ReceivesSemanticPost = !clear && preset != ObjectSemanticPreset.Prop;
            receivesSemanticPost = featureFlag1ReceivesSemanticPost;
            maskWeight = clear ? 0.0f : 1.0f;
            objectCustomMask = GetPresetObjectCustomMask(preset);
            UnpackObjectCustomMask();
            ApplyToRenderers();
        }

        public void ResetObjectSemantics()
        {
            writesAov = true;
            receivesSemanticPost = false;
            featureFlag1ReceivesSemanticPost = false;
            maskWeight = 1.0f;
            objectCustomMask = 0;
            UnpackObjectCustomMask();
            objectId = 1;
            groupId = 0;
            flags = 0;
            UnpackFlags();
            rendererStaticBindingMode = RendererStaticSemanticBindingMode.PreferRendererUserValue;
            ApplyToRenderers();
        }

        public void SetObjectCustomBit(int index, bool enabled)
        {
            int bit = 1 << Mathf.Clamp(index, 0, 7);
            objectCustomMask = enabled ? objectCustomMask | bit : objectCustomMask & ~bit;
            objectCustomMask = Mathf.Clamp(objectCustomMask, 0, 255);
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
            if (!ActiveAuthorings.Contains(this))
            {
                ActiveAuthorings.Add(this);
            }

            ApplyToRenderers();
        }

        private void OnDisable()
        {
            ActiveAuthorings.Remove(this);
            ClearRenderers();
            RefreshRendererStaticSemantics();
        }

        private void OnDestroy()
        {
            ActiveAuthorings.Remove(this);
            ClearRenderers();
            RefreshRendererStaticSemantics();
        }

        private void OnValidate()
        {
            maskWeight = Mathf.Clamp01(maskWeight);

            objectCustomMask = PackObjectCustomMask();
            objectId = Mathf.Clamp(objectId, 0, RendererStaticSemanticValue.V1MaxObjectId);
            groupId = Mathf.Clamp(groupId, 0, RendererStaticSemanticValue.V1MaxGroupId);

            flags = PackFlags();
            receivesSemanticPost = featureFlag1ReceivesSemanticPost;
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

            EnsureActiveAuthoringRegistered();
            EnsurePropertyBlock();
            objectCustomMask = PackObjectCustomMask();
            flags = PackFlags();
            receivesSemanticPost = featureFlag1ReceivesSemanticPost;
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
            RefreshRendererStaticSemantics();
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

        public static void RefreshRendererStaticSemantics()
        {
            ClearSceneRendererStaticSemantics();
            for (int i = 0; i < ActiveAuthorings.Count; i++)
            {
                ObjectSemanticAuthoring authoring = ActiveAuthorings[i];
                if (authoring == null || !authoring.isActiveAndEnabled)
                {
                    continue;
                }

                authoring.ApplyRendererStaticSemanticToTargets();
            }
        }

        private static void ClearSceneRendererStaticSemantics()
        {
            SceneRendererCache.Clear();
            SceneRendererCache.AddRange(Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None));

            for (int i = 0; i < SceneRendererCache.Count; i++)
            {
                Renderer targetRenderer = SceneRendererCache[i];
                if (targetRenderer == null || !targetRenderer.gameObject.scene.IsValid())
                {
                    continue;
                }

                ClearRendererStaticSemantic(targetRenderer);
            }

            SceneRendererCache.Clear();
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

        private void EnsureActiveAuthoringRegistered()
        {
            if (!ActiveAuthorings.Contains(this))
            {
                ActiveAuthorings.Add(this);
            }
        }

        private RendererStaticSemanticValue BuildRendererStaticSemantic()
        {
            return new RendererStaticSemanticValue(objectCustomMask, groupId, objectId, BuildEffectiveFlags());
        }

        private bool TryBuildRendererStaticSemanticV1(out uint packed)
        {
            return RendererStaticSemanticValue.TryPackV1(
                objectCustomMask,
                BuildObjectFeatureFlags(),
                objectId,
                groupId,
                out packed);
        }

        private void ApplyRendererStaticSemanticToTargets()
        {
            lastBindingTargetCount = 0;
            lastRendererUserValueBindingCount = 0;
            lastMaterialPropertyBlockSourceCount = 0;

            if (rendererStaticBindingMode == RendererStaticSemanticBindingMode.Disabled
                || rendererStaticBindingMode == RendererStaticSemanticBindingMode.MaterialPropertyBlockOnly)
            {
                CollectRenderers();
                lastBindingTargetCount = RendererCache.Count;
                lastMaterialPropertyBlockSourceCount = RendererCache.Count;
                RendererCache.Clear();
                return;
            }

            objectCustomMask = PackObjectCustomMask();
            flags = PackFlags();
            receivesSemanticPost = featureFlag1ReceivesSemanticPost;
            bool hasPacked = TryBuildRendererStaticSemanticV1(out uint packed);
            CollectRenderers();
            for (int i = 0; i < RendererCache.Count; i++)
            {
                Renderer targetRenderer = RendererCache[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                lastBindingTargetCount++;
                if (hasPacked && TrySetRendererUserValue(targetRenderer, packed))
                {
                    lastRendererUserValueBindingCount++;
                }
                else
                {
                    ClearRendererStaticSemantic(targetRenderer);
                    lastMaterialPropertyBlockSourceCount++;
                }
            }

            RendererCache.Clear();
        }

        private static void ClearRendererStaticSemantic(Renderer targetRenderer)
        {
            TrySetRendererUserValue(targetRenderer, 0u);
        }

        private static bool TrySetRendererUserValue(Renderer targetRenderer, uint value)
        {
            if (targetRenderer is MeshRenderer meshRenderer)
            {
                meshRenderer.SetShaderUserValue(value);
                return true;
            }

            if (targetRenderer is SkinnedMeshRenderer skinnedMeshRenderer)
            {
                skinnedMeshRenderer.SetShaderUserValue(value);
                return true;
            }

            return false;
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

        private int PackFlags()
        {
            int packed = 0;
            if (featureFlag1ReceivesSemanticPost) packed |= 1 << 1;
            if (featureFlag2ReceivesSss) packed |= 1 << 2;
            if (featureFlag3ReceivesCharacterComposite) packed |= 1 << 3;
            if (featureFlag4ReceivesOutline) packed |= 1 << 4;
            if (featureFlag5ReceivesDropShadow) packed |= 1 << 5;
            if (featureFlag6ReceivesStylizedShadow) packed |= 1 << 6;
            if (featureFlag7ReceivesSelectiveImagePost) packed |= 1 << 7;
            if (featureFlag8ReceivesHoShadow) packed |= 1 << 8;
            if (featureFlag9CastsHoShadow) packed |= 1 << 9;
            if (featureFlag10ParticipatesOit) packed |= 1 << 10;
            if (featureFlag11Reserved) packed |= 1 << 11;
            if (featureFlag12Reserved) packed |= 1 << 12;
            return packed;
        }

        private int BuildEffectiveFlags()
        {
            return RendererStaticSemanticValue.FeatureFlagsToAovFlags(PackFlags());
        }

        private int BuildObjectFeatureFlags()
        {
            return Mathf.Clamp(PackFlags(), 0, RendererStaticSemanticValue.V1MaxObjectFeatureFlags)
                & ~RendererStaticSemanticValue.V1ReservedFeatureBitMask;
        }

        private void UnpackFlags()
        {
            flags &= ~RendererStaticSemanticValue.V1ReservedFeatureBitMask;
            featureFlag1ReceivesSemanticPost = HasBit(flags, 1);
            receivesSemanticPost = featureFlag1ReceivesSemanticPost;
            featureFlag2ReceivesSss = HasBit(flags, 2);
            featureFlag3ReceivesCharacterComposite = HasBit(flags, 3);
            featureFlag4ReceivesOutline = HasBit(flags, 4);
            featureFlag5ReceivesDropShadow = HasBit(flags, 5);
            featureFlag6ReceivesStylizedShadow = HasBit(flags, 6);
            featureFlag7ReceivesSelectiveImagePost = HasBit(flags, 7);
            featureFlag8ReceivesHoShadow = HasBit(flags, 8);
            featureFlag9CastsHoShadow = HasBit(flags, 9);
            featureFlag10ParticipatesOit = HasBit(flags, 10);
            featureFlag11Reserved = HasBit(flags, 11);
            featureFlag12Reserved = HasBit(flags, 12);
        }

        private static bool HasBit(int mask, int index)
        {
            return (mask & (1 << Mathf.Clamp(index, 0, 12))) != 0;
        }

        private static void SetBit(ref int mask, int index, bool enabled)
        {
            int bit = 1 << Mathf.Clamp(index, 0, 12);
            mask = enabled ? mask | bit : mask & ~bit;
            mask = Mathf.Clamp(mask, 0, RendererStaticSemanticValue.V1MaxObjectFeatureFlags)
                & ~RendererStaticSemanticValue.V1ReservedFeatureBitMask;
        }
    }
}
