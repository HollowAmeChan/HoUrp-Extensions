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
        public void ObjectAuthoringPackedValueTracksSemanticPostReceiverFlag()
        {
            var gameObject = new UnityEngine.GameObject("Renderer Static Semantic Authoring Test");
            try
            {
                ObjectSemanticAuthoring authoring = gameObject.AddComponent<ObjectSemanticAuthoring>();
                authoring.ApplyPreset(ObjectSemanticPreset.Hair);
                authoring.GroupId = 7;
                authoring.ObjectId = 9;

                RendererStaticSemanticValue enabledValue = RendererStaticSemanticValue.FromPacked(
                    authoring.PackedRendererStaticSemantic);

                Assert.That(enabledValue.ObjectCustomMask, Is.EqualTo(5));
                Assert.That(enabledValue.GroupId, Is.EqualTo(7));
                Assert.That(enabledValue.ObjectId, Is.EqualTo(9));
                Assert.That(enabledValue.Flags & ObjectSemanticAuthoring.SemanticPostReceiverFlag, Is.Not.Zero);

                authoring.ReceivesSemanticPost = false;
                RendererStaticSemanticValue disabledValue = RendererStaticSemanticValue.FromPacked(
                    authoring.PackedRendererStaticSemantic);

                Assert.That(disabledValue.ObjectCustomMask, Is.EqualTo(5));
                Assert.That(disabledValue.Flags & ObjectSemanticAuthoring.SemanticPostReceiverFlag, Is.Zero);
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
