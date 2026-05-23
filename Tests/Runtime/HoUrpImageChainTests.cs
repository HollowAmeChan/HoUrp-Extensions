using HoUrp.Extensions.Core;
using HoUrp.Extensions.Image;
using NUnit.Framework;

namespace HoUrp.Extensions.Tests.Runtime
{
    public sealed class HoUrpImageChainTests
    {
        [Test]
        public void ImageChainUsesTwoWorkTexturesForMultiplePasses()
        {
            var chain = new ImageChain();
            chain.Begin(HoUrpBuiltInNames.PostFrameResources.ImagePrimary);

            ImageChainStep first = chain.AddPass(new ImagePassDescriptor(
                "ImagePass.First",
                "Layer.First",
                HoUrpBuiltInNames.PostEffects.ImagePostColorAdjustPrototype));
            ImageChainStep second = chain.AddPass(new ImagePassDescriptor(
                "ImagePass.Second",
                "Layer.Second",
                HoUrpBuiltInNames.PostEffects.ImagePostColorAdjustPrototype));
            ImageChainPlan plan = chain.End();

            Assert.That(plan.WorkA, Is.EqualTo(HoUrpBuiltInNames.PostFrameResources.ImageWorkA));
            Assert.That(plan.WorkB, Is.EqualTo(HoUrpBuiltInNames.PostFrameResources.ImageWorkB));
            Assert.That(first.ReadResource, Is.EqualTo(HoUrpBuiltInNames.PostFrameResources.ImageWorkA));
            Assert.That(first.WriteResource, Is.EqualTo(HoUrpBuiltInNames.PostFrameResources.ImageWorkB));
            Assert.That(second.ReadResource, Is.EqualTo(HoUrpBuiltInNames.PostFrameResources.ImageWorkB));
            Assert.That(second.WriteResource, Is.EqualTo(HoUrpBuiltInNames.PostFrameResources.ImageWorkA));
            Assert.That(plan.FinalOutput, Is.EqualTo(HoUrpBuiltInNames.PostFrameResources.ImageWorkA));
            Assert.That(plan.Steps.Count, Is.EqualTo(2));
        }

        [Test]
        public void ImageChainDoesNotAllocateOriginalSourceByDefault()
        {
            var chain = new ImageChain();
            chain.Begin(HoUrpBuiltInNames.PostFrameResources.ImagePrimary);
            chain.AddPass(new ImagePassDescriptor(
                "ImagePass.Color",
                "Layer.Color",
                HoUrpBuiltInNames.PostEffects.ImagePostColorAdjustPrototype));

            ImageChainPlan plan = chain.End();

            Assert.That(plan.OriginalSourceRequested, Is.False);
        }

        [Test]
        public void ImageChainRequestsOriginalSourceOnlyWhenPassNeedsIt()
        {
            var chain = new ImageChain();
            chain.Begin(HoUrpBuiltInNames.PostFrameResources.ImagePrimary);
            chain.AddPass(new ImagePassDescriptor(
                "ImagePass.OriginalMix",
                "Layer.OriginalMix",
                HoUrpBuiltInNames.PostEffects.ImagePostColorAdjustPrototype,
                true));

            ImageChainPlan plan = chain.End();

            Assert.That(plan.OriginalSourceRequested, Is.True);
        }

        [Test]
        public void ImageChainWithNoPassReturnsSourceAsFinalOutput()
        {
            var chain = new ImageChain();
            chain.Begin(HoUrpBuiltInNames.PostFrameResources.ImagePrimary);

            ImageChainPlan plan = chain.End();

            Assert.That(plan.Steps.Count, Is.EqualTo(0));
            Assert.That(plan.FinalOutput, Is.EqualTo(HoUrpBuiltInNames.PostFrameResources.ImagePrimary));
        }

        [Test]
        public void ImageChainRejectsPassBeforeBegin()
        {
            var chain = new ImageChain();

            Assert.Throws<System.InvalidOperationException>(() =>
                chain.AddPass(new ImagePassDescriptor(
                    "ImagePass.Invalid",
                    "Layer.Invalid",
                    HoUrpBuiltInNames.PostEffects.ImagePostColorAdjustPrototype)));
        }
    }
}
