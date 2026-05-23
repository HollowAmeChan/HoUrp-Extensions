using HoUrp.Extensions.Core;

namespace HoUrp.Extensions.Image
{
    public readonly struct ImageChainStep
    {
        public ImageChainStep(
            ImagePassDescriptor pass,
            HoUrpIdentifier readResource,
            HoUrpIdentifier writeResource,
            int passIndex)
        {
            Pass = pass;
            ReadResource = readResource;
            WriteResource = writeResource;
            PassIndex = passIndex;
        }

        public ImagePassDescriptor Pass { get; }
        public HoUrpIdentifier ReadResource { get; }
        public HoUrpIdentifier WriteResource { get; }
        public int PassIndex { get; }
    }
}
