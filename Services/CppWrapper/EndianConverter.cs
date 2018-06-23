using System;

namespace loggerApp.CppWrapper
{
    static public class EndianConverter
    {
        /// int 2 bytes
        static public byte[] Int2Bytes(UInt16 intValue)
        {
            byte[] intBytes = BitConverter.GetBytes(intValue);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(intBytes);
            }
            return intBytes;
        }
        static public byte[] UInt2BytesAsMiddleEndian(UInt32 intValue)
        {
            byte[] intBytes = BitConverter.GetBytes(intValue);
            return new Byte[4] { intBytes[1], intBytes[0], intBytes[3], intBytes[2] };
        }

    }
}
