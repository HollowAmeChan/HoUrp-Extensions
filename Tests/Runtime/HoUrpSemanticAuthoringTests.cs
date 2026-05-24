using System.Reflection;
using HoUrp.Extensions.Semantic;
using NUnit.Framework;
using UnityEngine;

namespace HoUrp.Extensions.Tests.Runtime
{
    public sealed class HoUrpSemanticAuthoringTests
    {
        [Test]
        public void ObjectSemanticPresetMapsToExpectedCustomMask()
        {
            Assert.That(ObjectSemanticAuthoring.GetPresetObjectCustomMask(ObjectSemanticPreset.Subject), Is.EqualTo(1));
            Assert.That(ObjectSemanticAuthoring.GetPresetObjectCustomMask(ObjectSemanticPreset.Face), Is.EqualTo(3));
            Assert.That(ObjectSemanticAuthoring.GetPresetObjectCustomMask(ObjectSemanticPreset.Hair), Is.EqualTo(5));
            Assert.That(ObjectSemanticAuthoring.GetPresetObjectCustomMask(ObjectSemanticPreset.Eye), Is.EqualTo(9));
            Assert.That(ObjectSemanticAuthoring.GetPresetObjectCustomMask(ObjectSemanticPreset.Accessory), Is.EqualTo(17));
            Assert.That(ObjectSemanticAuthoring.GetPresetObjectCustomMask(ObjectSemanticPreset.Cloth), Is.EqualTo(33));
            Assert.That(ObjectSemanticAuthoring.GetPresetObjectCustomMask(ObjectSemanticPreset.Prop), Is.EqualTo(64));
            Assert.That(ObjectSemanticAuthoring.GetPresetObjectCustomMask(ObjectSemanticPreset.Clear), Is.EqualTo(0));
        }

        [Test]
        public void ObjectSemanticPropertiesClampToContractRanges()
        {
            var gameObject = new GameObject("Object Semantic Test");
            try
            {
                ObjectSemanticAuthoring authoring = gameObject.AddComponent<ObjectSemanticAuthoring>();

                authoring.MaskWeight = 2.0f;
                authoring.ObjectCustomMask = 512;
                authoring.ObjectId = 512;
                authoring.GroupId = -4;
                authoring.Flags = 999;

                Assert.That(authoring.MaskWeight, Is.EqualTo(1.0f));
                Assert.That(authoring.ObjectCustomMask, Is.EqualTo(255));
                Assert.That(authoring.ObjectId, Is.EqualTo(RendererStaticSemanticValue.V1MaxObjectId));
                Assert.That(authoring.GroupId, Is.EqualTo(0));
                Assert.That(authoring.Flags, Is.EqualTo(RendererStaticSemanticValue.V1MaxObjectFeatureFlags & ~RendererStaticSemanticValue.V1ReservedFeatureBitMask));
                Assert.That(authoring.EffectiveFlags, Is.EqualTo(254));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ObjectSemanticApplyPresetSetsParticipationAndMask()
        {
            var gameObject = new GameObject("Object Semantic Preset Test");
            try
            {
                ObjectSemanticAuthoring authoring = gameObject.AddComponent<ObjectSemanticAuthoring>();

                authoring.ApplyPreset(ObjectSemanticPreset.Hair);

                Assert.That(authoring.WritesAov, Is.True);
                Assert.That(authoring.ReceivesSemanticPost, Is.True);
                Assert.That(authoring.MaskWeight, Is.EqualTo(1.0f));
                Assert.That(authoring.ObjectCustomMask, Is.EqualTo(5));
                Assert.That(authoring.EffectiveFlags & ObjectSemanticAuthoring.SemanticPostReceiverFlag, Is.Not.Zero);

                authoring.ApplyPreset(ObjectSemanticPreset.Clear);

                Assert.That(authoring.WritesAov, Is.False);
                Assert.That(authoring.ReceivesSemanticPost, Is.False);
                Assert.That(authoring.MaskWeight, Is.EqualTo(0.0f));
                Assert.That(authoring.ObjectCustomMask, Is.EqualTo(0));
                Assert.That(authoring.EffectiveFlags & ObjectSemanticAuthoring.SemanticPostReceiverFlag, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ObjectSemanticResetAllowsAllFeatureFlagsOff()
        {
            var gameObject = new GameObject("Object Semantic Feature Flags Test");
            try
            {
                ObjectSemanticAuthoring authoring = gameObject.AddComponent<ObjectSemanticAuthoring>();

                authoring.ResetObjectSemantics();

                Assert.That(authoring.ReceivesSemanticPost, Is.False);
                Assert.That(authoring.Flags, Is.EqualTo(0));
                Assert.That(authoring.EffectiveFlags, Is.EqualTo(0));
                Assert.That(authoring.ObjectFeatureFlags, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ObjectSemanticCustomBitsCanReturnToZeroAfterBeingSet()
        {
            var gameObject = new GameObject("Object Semantic Custom Bits Clear Test");
            try
            {
                ObjectSemanticAuthoring authoring = gameObject.AddComponent<ObjectSemanticAuthoring>();

                authoring.ObjectCustomMask = ObjectSemanticAuthoring.GetPresetObjectCustomMask(ObjectSemanticPreset.Hair);
                authoring.ObjectCustomMask = 0;

                Assert.That(authoring.ObjectCustomMask, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ObjectSemanticCustomInspectorBitsOverrideStaleMaskValue()
        {
            var gameObject = new GameObject("Object Semantic Custom Inspector Clear Test");
            try
            {
                ObjectSemanticAuthoring authoring = gameObject.AddComponent<ObjectSemanticAuthoring>();
                authoring.ObjectCustomMask = ObjectSemanticAuthoring.GetPresetObjectCustomMask(ObjectSemanticPreset.Hair);

                SetPrivateBool(authoring, "custom0Subject", false);
                SetPrivateBool(authoring, "custom2Hair", false);
                InvokeOnValidate(authoring);

                Assert.That(authoring.ObjectCustomMask, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ObjectSemanticFeatureFlagsCanReturnToZeroAfterBeingSet()
        {
            var gameObject = new GameObject("Object Semantic Feature Flags Clear Test");
            try
            {
                ObjectSemanticAuthoring authoring = gameObject.AddComponent<ObjectSemanticAuthoring>();

                authoring.Flags = ObjectSemanticAuthoring.SemanticPostReceiverFlag;
                authoring.Flags = 0;

                Assert.That(authoring.ReceivesSemanticPost, Is.False);
                Assert.That(authoring.Flags, Is.EqualTo(0));
                Assert.That(authoring.EffectiveFlags, Is.EqualTo(0));
                Assert.That(authoring.ObjectFeatureFlags, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ObjectSemanticPostReceiverDoesNotClearObjectSemantics()
        {
            var gameObject = new GameObject("Object Semantic Post Gate Test");
            try
            {
                ObjectSemanticAuthoring authoring = gameObject.AddComponent<ObjectSemanticAuthoring>();

                authoring.ApplyPreset(ObjectSemanticPreset.Hair);
                authoring.ReceivesSemanticPost = false;

                Assert.That(authoring.ObjectCustomMask, Is.EqualTo(5));
                Assert.That(authoring.EffectiveFlags & ObjectSemanticAuthoring.SemanticPostReceiverFlag, Is.Zero);

                authoring.ReceivesSemanticPost = true;

                Assert.That(authoring.ObjectCustomMask, Is.EqualTo(5));
                Assert.That(authoring.EffectiveFlags & ObjectSemanticAuthoring.SemanticPostReceiverFlag, Is.Not.Zero);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void MaterialSemanticPropertiesClampToExpectedRanges()
        {
            var gameObject = new GameObject("Material Semantic Test");
            try
            {
                MaterialSemanticAuthoring authoring = gameObject.AddComponent<MaterialSemanticAuthoring>();

                authoring.MaterialClass = 999;
                authoring.SssProfile = -3;
                authoring.Thickness = 2.0f;
                authoring.Curvature = -5.0f;
                authoring.MaterialCustom0_3 = new Vector4(-1.0f, 0.5f, 2.0f, 1.5f);

                Assert.That(authoring.MaterialClass, Is.EqualTo(255));
                Assert.That(authoring.SssProfile, Is.EqualTo(0));
                Assert.That(authoring.Thickness, Is.EqualTo(1.0f));
                Assert.That(authoring.Curvature, Is.EqualTo(-1.0f));
                Assert.That(authoring.MaterialCustom0_3, Is.EqualTo(new Vector4(0.0f, 0.5f, 1.0f, 1.0f)));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void MaterialSemanticSkinPresetSetsSssInputs()
        {
            var gameObject = new GameObject("Material Semantic Preset Test");
            try
            {
                MaterialSemanticAuthoring authoring = gameObject.AddComponent<MaterialSemanticAuthoring>();

                authoring.ApplyPreset(MaterialSemanticPreset.SkinSss);

                Assert.That(authoring.MaterialClass, Is.EqualTo(1));
                Assert.That(authoring.SssProfile, Is.EqualTo(1));
                Assert.That(authoring.Thickness, Is.GreaterThan(0.0f));
                Assert.That(authoring.MaterialCustom0_3.x, Is.EqualTo(1.0f));

                authoring.ApplyPreset(MaterialSemanticPreset.Clear);

                Assert.That(authoring.MaterialClass, Is.EqualTo(0));
                Assert.That(authoring.SssProfile, Is.EqualTo(0));
                Assert.That(authoring.Thickness, Is.EqualTo(0.0f));
                Assert.That(authoring.MaterialCustom0_3, Is.EqualTo(Vector4.zero));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static void SetPrivateBool(ObjectSemanticAuthoring authoring, string fieldName, bool value)
        {
            FieldInfo field = typeof(ObjectSemanticAuthoring).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            field.SetValue(authoring, value);
        }

        private static void InvokeOnValidate(ObjectSemanticAuthoring authoring)
        {
            MethodInfo onValidate = typeof(ObjectSemanticAuthoring).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(onValidate, Is.Not.Null);
            onValidate.Invoke(authoring, null);
        }
    }
}
