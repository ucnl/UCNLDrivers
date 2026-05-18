using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCNLDrivers
{
    public static class BinUtils
    {
        public static int ReadInt32LittleEndian(byte[] buffer, int offset)
        {
            if (buffer == null || offset < 0 || offset + 3 >= buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            return buffer[offset] |
                   (buffer[offset + 1] << 8) |
                   (buffer[offset + 2] << 16) |
                   (buffer[offset + 3] << 24);
        }     

        public static int SwapInt32(int value)
        {
            uint uvalue = (uint)value;
            uint swapped = ((uvalue >> 24) & 0x000000FF) |
                           ((uvalue >> 8)  & 0x0000FF00) |
                           ((uvalue << 8)  & 0x00FF0000) |
                           ((uvalue << 24) & 0xFF000000);
            return (int)swapped;
        }
    }
}
