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
        public void ObjectSemanticPropertiesClampToByteAndNormalizedRanges()
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
                Assert.That(authoring.ObjectId, Is.EqualTo(255));
                Assert.That(authoring.GroupId, Is.EqualTo(0));
                Assert.That(authoring.Flags, Is.EqualTo(255));
                Assert.That(authoring.EffectiveFlags, Is.EqualTo(255));
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
                authoring.SssWeight = 2.0f;
                authoring.MaterialCustom0_3 = new Vector4(-1.0f, 0.5f, 2.0f, 1.5f);

                Assert.That(authoring.MaterialClass, Is.EqualTo(255));
                Assert.That(authoring.SssProfile, Is.EqualTo(0));
                Assert.That(authoring.Thickness, Is.EqualTo(1.0f));
                Assert.That(authoring.Curvature, Is.EqualTo(-1.0f));
                Assert.That(authoring.SssWeight, Is.EqualTo(1.0f));
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
                Assert.That(authoring.SssWeight, Is.EqualTo(1.0f));
                Assert.That(authoring.MaterialCustom0_3.x, Is.EqualTo(1.0f));

                authoring.ApplyPreset(MaterialSemanticPreset.Clear);

                Assert.That(authoring.MaterialClass, Is.EqualTo(0));
                Assert.That(authoring.SssProfile, Is.EqualTo(0));
                Assert.That(authoring.Thickness, Is.EqualTo(0.0f));
                Assert.That(authoring.SssWeight, Is.EqualTo(0.0f));
                Assert.That(authoring.MaterialCustom0_3, Is.EqualTo(Vector4.zero));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
