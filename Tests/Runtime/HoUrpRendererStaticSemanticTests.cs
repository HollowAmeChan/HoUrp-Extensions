using HoUrp.Extensions.Semantic;
using NUnit.Framework;

namespace HoUrp.Extensions.Tests.Runtime
{
    public sealed class HoUrpRendererStaticSemanticTests
    {
        [Test]
        public void PackAndUnpackPreservesByteFields()
        {
            uint packed = RendererStaticSemanticValue.Pack(1, 2, 3, 4);

            RendererStaticSemanticValue value = RendererStaticSemanticValue.FromPacked(packed);

            Assert.That(value.ObjectCustomMask, Is.EqualTo(1));
            Assert.That(value.GroupId, Is.EqualTo(2));
            Assert.That(value.ObjectId, Is.EqualTo(3));
            Assert.That(value.Flags, Is.EqualTo(4));
            Assert.That(value.PackedValue, Is.EqualTo(packed));
        }

        [Test]
        public void PackClampsFieldsToByteRange()
        {
            RendererStaticSemanticValue value = RendererStaticSemanticValue.FromPacked(
                RendererStaticSemanticValue.Pack(-1, 300, 999, 4));

            Assert.That(value.ObjectCustomMask, Is.EqualTo(0));
            Assert.That(value.GroupId, Is.EqualTo(255));
            Assert.That(value.ObjectId, Is.EqualTo(255));
            Assert.That(value.Flags, Is.EqualTo(4));
        }

        [Test]
        public void ObjectPresetCanBuildRendererStaticSemantic()
        {
            RendererStaticSemanticValue value = new RendererStaticSemanticValue(
                ObjectSemanticAuthoring.GetPresetObjectCustomMask(ObjectSemanticPreset.Hair),
                7,
                9,
                ObjectSemanticAuthoring.SemanticPostReceiverFlag);

            Assert.That(value.ObjectCustomMask, Is.EqualTo(5));
            Assert.That(value.GroupId, Is.EqualTo(7));
            Assert.That(value.ObjectId, Is.EqualTo(9));
            Assert.That(value.Flags & ObjectSemanticAuthoring.SemanticPostReceiverFlag, Is.Not.Zero);
        }

        [Test]
        public void PackV1PreservesCompactFields()
        {
            bool packed = RendererStaticSemanticValue.TryPackV1(5, 0x1FFF, 15, 7, out uint packedValue);

            Assert.That(packed, Is.True);
            Assert.That(RendererStaticSemanticValue.TryUnpackV1(packedValue, out RendererStaticSemanticValue value), Is.True);
            Assert.That(value.IsV1, Is.True);
            Assert.That(value.ObjectCustomMask, Is.EqualTo(5));
            Assert.That(value.ObjectFeatureFlags, Is.EqualTo(0x1FFE));
            Assert.That(value.ObjectId, Is.EqualTo(15));
            Assert.That(value.GroupId, Is.EqualTo(7));
            Assert.That(value.Flags, Is.EqualTo(254));
            Assert.That(value.PackedValue, Is.EqualTo(packedValue));
        }

        [Test]
        public void PackV1ClampsRegionAndFeatureFlagsButRejectsLargeIds()
        {
            Assert.That(RendererStaticSemanticValue.TryPackV1(999, 99999, 15, 7, out uint packedValue), Is.True);
            Assert.That(RendererStaticSemanticValue.TryUnpackV1(packedValue, out RendererStaticSemanticValue value), Is.True);
            Assert.That(value.ObjectCustomMask, Is.EqualTo(255));
            Assert.That(value.ObjectFeatureFlags, Is.EqualTo(RendererStaticSemanticValue.V1MaxObjectFeatureFlags & ~RendererStaticSemanticValue.V1ReservedFeatureBitMask));

            Assert.That(RendererStaticSemanticValue.TryPackV1(1, 0, 16, 0, out _), Is.False);
            Assert.That(RendererStaticSemanticValue.TryPackV1(1, 0, 0, 8, out _), Is.False);
            Assert.That(RendererStaticSemanticValue.TryPackV1(1, 0, -1, 0, out _), Is.False);
            Assert.That(RendererStaticSemanticValue.TryPackV1(1, 0, 0, -1, out _), Is.False);
        }

        [Test]
        public void FeatureFlagsMapToCurrentAovFlags()
        {
            int featureFlags = (1 << 0) | (1 << 1) | (1 << 2) | (1 << 8) | (1 << 12);

            byte aovFlags = RendererStaticSemanticValue.FeatureFlagsToAovFlags(featureFlags);

            Assert.That((aovFlags & (1 << 0)), Is.Zero);
            Assert.That((aovFlags & (1 << 1)), Is.Not.Zero);
            Assert.That((aovFlags & (1 << 7)), Is.Not.Zero);
            Assert.That((aovFlags & (1 << 2)), Is.Not.Zero);
        }

        [Test]
        public void ObjectAuthoringPackedValueTracksV1SemanticPostReceiverFlag()
        {
            var gameObject = new UnityEngine.GameObject("Renderer Static Semantic Authoring Test");
            try
            {
                ObjectSemanticAuthoring authoring = gameObject.AddComponent<ObjectSemanticAuthoring>();
                authoring.ApplyPreset(ObjectSemanticPreset.Hair);
                authoring.GroupId = 7;
                authoring.ObjectId = 9;

                Assert.That(RendererStaticSemanticValue.TryUnpackV1(
                    authoring.PackedRendererStaticSemantic,
                    out RendererStaticSemanticValue enabledValue), Is.True);

                Assert.That(enabledValue.ObjectCustomMask, Is.EqualTo(5));
                Assert.That(enabledValue.GroupId, Is.EqualTo(7));
                Assert.That(enabledValue.ObjectId, Is.EqualTo(9));
                Assert.That(enabledValue.Flags & (1 << 0), Is.Zero);
                Assert.That(enabledValue.Flags & (1 << 1), Is.Not.Zero);
                Assert.That(enabledValue.ObjectFeatureFlags & (1 << 0), Is.Zero);
                Assert.That(enabledValue.ObjectFeatureFlags & (1 << 1), Is.Not.Zero);

                authoring.ReceivesSemanticPost = false;
                Assert.That(RendererStaticSemanticValue.TryUnpackV1(
                    authoring.PackedRendererStaticSemantic,
                    out RendererStaticSemanticValue disabledValue), Is.True);

                Assert.That(disabledValue.ObjectCustomMask, Is.EqualTo(5));
                Assert.That(disabledValue.Flags & (1 << 1), Is.Zero);
                Assert.That(disabledValue.ObjectFeatureFlags & (1 << 1), Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ObjectAuthoringClampsCompactIdsBeforePacking()
        {
            var gameObject = new UnityEngine.GameObject("Renderer Static Semantic Compact Id Clamp Test");
            try
            {
                ObjectSemanticAuthoring authoring = gameObject.AddComponent<ObjectSemanticAuthoring>();
                authoring.ApplyPreset(ObjectSemanticPreset.Subject);
                authoring.ObjectId = RendererStaticSemanticValue.V1MaxObjectId + 1;
                authoring.GroupId = RendererStaticSemanticValue.V1MaxGroupId + 1;

                Assert.That(authoring.ObjectId, Is.EqualTo(RendererStaticSemanticValue.V1MaxObjectId));
                Assert.That(authoring.GroupId, Is.EqualTo(RendererStaticSemanticValue.V1MaxGroupId));
                Assert.That(RendererStaticSemanticValue.TryUnpackV1(
                    authoring.PackedRendererStaticSemantic,
                    out RendererStaticSemanticValue value), Is.True);
                Assert.That(value.ObjectId, Is.EqualTo(RendererStaticSemanticValue.V1MaxObjectId));
                Assert.That(value.GroupId, Is.EqualTo(RendererStaticSemanticValue.V1MaxGroupId));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PackedZeroIsReservedForNoRendererStaticSemanticOverride()
        {
            RendererStaticSemanticValue value = RendererStaticSemanticValue.FromPacked(0u);

            Assert.That(value.ObjectCustomMask, Is.EqualTo(0));
            Assert.That(value.GroupId, Is.EqualTo(0));
            Assert.That(value.ObjectId, Is.EqualTo(0));
            Assert.That(value.Flags, Is.EqualTo(0));
            Assert.That(value.PackedValue, Is.EqualTo(0u));
        }
    }
}
