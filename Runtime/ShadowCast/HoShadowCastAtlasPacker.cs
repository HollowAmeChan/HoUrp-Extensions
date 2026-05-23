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
            size = Mathf.Clamp(size, 1, atlasSize);
            if (cursorX + size > atlasSize)
            {
                cursorX = 0;
                cursorY += rowHeight;
                rowHeight = 0;
            }

            if (cursorY + size > atlasSize)
            {
                offsetX = 0;
                offsetY = 0;
                return false;
            }

            offsetX = cursorX;
            offsetY = cursorY;
            cursorX += size;
            rowHeight = Mathf.Max(rowHeight, size);
            return true;
        }
    }
}
