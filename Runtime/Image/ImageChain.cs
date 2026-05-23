using System.Collections.Generic;
using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.Image
{
    public sealed class ImageChain
    {
        private readonly List<ImageChainStep> steps = new List<ImageChainStep>();
        private HoUrpIdentifier source;
        private HoUrpIdentifier current;
        private HoUrpIdentifier alternate;
        private bool originalSourceRequested;
        private bool begun;

        public void Begin(HoUrpIdentifier sourceResource)
        {
            source = sourceResource;
            current = HoUrpBuiltInNames.PostFrameResources.ImageWorkA;
            alternate = HoUrpBuiltInNames.PostFrameResources.ImageWorkB;
            originalSourceRequested = false;
            steps.Clear();
            begun = true;
        }

        public ImageChainStep AddPass(ImagePassDescriptor pass)
        {
            if (!begun)
            {
                throw new System.InvalidOperationException("ImageChain.Begin must be called before adding passes.");
            }

            if (current == alternate)
            {
                throw new System.InvalidOperationException("ImageChain read and write resources must be different.");
            }

            if (pass.NeedsOriginalSource)
            {
                originalSourceRequested = true;
            }

            var step = new ImageChainStep(pass, current, alternate, steps.Count);
            steps.Add(step);
            Swap();
            return step;
        }

        public ImageChainPlan End()
        {
            if (!begun)
            {
                throw new System.InvalidOperationException("ImageChain.Begin must be called before ending the chain.");
            }

            HoUrpIdentifier finalOutput = steps.Count == 0 ? source : current;
            begun = false;
            return new ImageChainPlan(
                source,
                HoUrpBuiltInNames.PostFrameResources.ImageWorkA,
                HoUrpBuiltInNames.PostFrameResources.ImageWorkB,
                finalOutput,
                originalSourceRequested,
                new ReadOnlyArray<ImageChainStep>(steps.ToArray()));
        }

        private void Swap()
        {
            HoUrpIdentifier temp = current;
            current = alternate;
            alternate = temp;
        }
    }
}
