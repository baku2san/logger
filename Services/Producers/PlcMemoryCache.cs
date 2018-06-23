using System;
using static loggerApp.CppWrapper.ClkLibConstants;

namespace loggerApp.Producers
{
    public class PlcMemoryCache
    {
        public EventMemory MemoryType { get; set; }
        public UInt32 ReadOffset { get; set; }
        public AccessSize AccessSize { get { return AccessSize.WORD; } }
        public UInt32 Length { get; set; }

        public UInt32 EndOffset { get { return ReadOffset + Length - 1; } }
        public UInt16[] Values { get; set; }

        public PlcMemoryCache(EventMemory memoryType, UInt32 readOffset, UInt32 length)
        {
            MemoryType = memoryType;
            ReadOffset = readOffset;
            Length = length;
        }
        public bool WithinValue(UInt32 readOffset)
        {
            return (ReadOffset <= readOffset && readOffset <= (EndOffset));
        }
        public bool WithinValue(EventMemory memoryType, UInt32 readOffset)
        {
            return MemoryType == memoryType && WithinValue(readOffset);
        }

    }
}
