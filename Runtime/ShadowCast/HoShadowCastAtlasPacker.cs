using UnityEngine;

namespace HoUrp.Extensions.ShadowCast
{
    public struct HoShadowCastAtlasPacker
    {
        private readonly int atlasSize;
        private int cursorX;
        private int cursorY;
        private int rowHeight;

        public HoShadowCastAtlasPacker(int atlasSize)
        {
            this.atlasSize = Mathf.Max(1, atlasSize);
            cursorX = 0;
            cursorY = 0;
            rowHeight = 0;
        }

        public bool TryAllocate(int size, out int offsetX, out int offsetY)
        {
            return TryAllocate(size, size, out offsetX, out offsetY);
        }

        public bool TryAllocate(int width, int height, out int offsetX, out int offsetY)
        {
            width = Mathf.Clamp(width, 1, atlasSize);
            height = Mathf.Clamp(height, 1, atlasSize);
            if (cursorX + width > atlasSize)
            {
                cursorX = 0;
                cursorY += rowHeight;
                rowHeight = 0;
            }

            if (cursorY + height > atlasSize)
            {
                offsetX = 0;
                offsetY = 0;
                return false;
            }

            offsetX = cursorX;
            offsetY = cursorY;
            cursorX += width;
            rowHeight = Mathf.Max(rowHeight, height);
            return true;
        }
    }
}
