using UnityEngine;
using UnityEngine.Rendering;

namespace HoUrp.Extensions.Filter
{
    internal static class HoUrpFilterUtils
    {
        public static Vector4 CreateBlurParams(float radius, int sampleCount, float depthTolerance, float normalTolerance)
        {
            return new Vector4(
                Mathf.Max(0.0f, radius),
                Mathf.Clamp(sampleCount, 1, 13),
                Mathf.Max(0.0001f, depthTolerance),
                Mathf.Clamp01(normalTolerance));
        }

        public static Vector4 CreateTexelSize(RenderTextureDescriptor descriptor)
        {
            int width = Mathf.Max(1, descriptor.width);
            int height = Mathf.Max(1, descriptor.height);
            return new Vector4(1.0f / width, 1.0f / height, width, height);
        }

        public static Vector4 NormalizeDirection(float x, float y)
        {
            var direction = new Vector2(x, y);
            if (direction.sqrMagnitude <= 0.000001f)
            {
                direction = Vector2.right;
            }

            direction.Normalize();
            return new Vector4(direction.x, direction.y, 0.0f, 0.0f);
        }
    }
}
