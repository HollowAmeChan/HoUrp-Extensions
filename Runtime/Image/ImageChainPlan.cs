using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.Image
{
    public readonly struct ImageChainPlan
    {
        public ImageChainPlan(
            HoUrpIdentifier source,
            HoUrpIdentifier workA,
            HoUrpIdentifier workB,
            HoUrpIdentifier finalOutput,
            bool originalSourceRequested,
            ReadOnlyArray<ImageChainStep> steps)
        {
            Source = source;
            WorkA = workA;
            WorkB = workB;
            FinalOutput = finalOutput;
            OriginalSourceRequested = originalSourceRequested;
            Steps = steps;
        }

        public HoUrpIdentifier Source { get; }
        public HoUrpIdentifier WorkA { get; }
        public HoUrpIdentifier WorkB { get; }
        public HoUrpIdentifier FinalOutput { get; }
        public bool OriginalSourceRequested { get; }
        public ReadOnlyArray<ImageChainStep> Steps { get; }
    }
}
