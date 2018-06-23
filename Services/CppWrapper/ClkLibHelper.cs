using Serilog;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static loggerApp.CppWrapper.ClkLibConstants;

namespace loggerApp.CppWrapper
{
    public class ClkLibHelper : IDisposable
    {
        #region dllimport
        [DllImport("ClkLibrary.dll", EntryPoint = "Open", CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 ClkLib_Open(Int32 unitAdr, Byte networkNo, Byte nodeNo, Byte unitNo);
        
        [DllImport("ClkLibrary.dll", EntryPoint = "Close", CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 ClkLib_Close(Int32 unitAdr);

        [DllImport("ClkLibrary.dll", EntryPoint = "ReadAsWord", CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 ClkLib_ReadAsUInt16(Int32 unitAdr, EventMemory memoryType, UInt32 readOffset, 
                [ Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2, SizeParamIndex = 4)] UInt16[] result, UInt32 length);
        
        [DllImport("ClkLibrary.dll", EntryPoint = "GetLastError", CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 ClkLib_GetLastError(Int32 unitAdr);

        [DllImport("ClkLibrary.dll", EntryPoint = "ReadAsWordByFinsAtDM", CallingConvention = CallingConvention.Cdecl)]
        private static extern Int32 ClkLib_ReadAsUInt16ByFinsAtDM(Int32 unitAdr, UInt32 readOffset,
                [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U2, SizeParamIndex = 4)] UInt16[] result, UInt32 wordLength, UInt32 reciveWaitMs);
        #endregion dllimport

        public Int32 UnitAdr;
        public Byte NetworkNo { get; set; }
        public Byte NodeNo { get; set; }
        public Byte UnitNo { get; set; }
        /// <summary>
        /// 関数Wrapperとして、PCLKHANDLEに１ｖｓ１で紐付くInstanceを予定。
        /// 非同期タスク起動時に、MutipleAccessを上限として、unitAdr を紐づけてOpen/Read/Closeを行う。
        /// </summary>
        public ClkLibHelper(Int32 unitAdr, Byte networkNo, Byte nodeNo, Byte unitNo)
        {
            UnitAdr = unitAdr - 1;      // C++側では index:0～  だが、C#側では 1～ なので合わせる必要がある
            NetworkNo = networkNo;
            NodeNo = nodeNo;
            UnitNo = unitNo;
        }
        
        public CLK_ReturnCode Open()
            {
            return (CLK_ReturnCode)ClkLib_Open(UnitAdr, NetworkNo, NodeNo, UnitNo);
        }

        public CLK_ReturnCode Close()
        {
            return (CLK_ReturnCode)ClkLib_Close(UnitAdr);
        }
        /// <summary>
        /// 上下の8bit Swap
        /// </summary>
        /// <param name="source"></param>
        private static unsafe void SwapX2(UInt16[] source)
        {
            fixed (UInt16* pSource = &source[0])
            {
                Byte* bp = (Byte*)pSource;
                Byte* bp_stop = bp + source.Length;

                while (bp < bp_stop)
                {
                    *(UInt16*)bp = (UInt16)(*bp << 8 | *(bp + 1));
                    bp += 2;
                }
            }
        }
        /// <summary>
        /// PLCからのデータ取得
        /// 大量のReadが発生しないように、上位概念において連続AddressをMergeして高速化を図ることにしているので、これ以外のReadMethosは現状未使用となっている
        /// </summary>
        /// <param name="unitAdr"></param>
        /// <param name="acMem"></param>
        /// <param name="readOffset"></param>
        /// <param name="length"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public CLK_ReturnCode Read(EventMemory memoryType, UInt32 readOffset, out UInt16[] data, UInt32 wordLength = 1)
        {
            CLK_ReturnCode result = CLK_ReturnCode.CLK_SUCCESS;
            if (memoryType == EventMemory.DM && readOffset > ClkLibConstants.DataLinkEndAddressDM)
            {
                // DM で、DataLink対象外のMemoryは、FinsCommandで取得する。→送信＆受信待ちとなるので、取得時間が遅くなるけどね・・
                var wordLengthWithHeader = wordLength + ClkLibConstants.FinsReceiveHeaderSizeAsWord;
                data = new UInt16[wordLengthWithHeader];
                result = (CLK_ReturnCode)ClkLib_ReadAsUInt16ByFinsAtDM(UnitAdr, readOffset, data, wordLength, ClkLibConstants.FinsReceiveCommandWaitTime);
                data = data.Skip(ClkLibConstants.FinsReceiveHeaderSizeAsWord).ToArray();
                SwapX2(data);
                return result;
            }
            else
            {
                data = new UInt16[wordLength];
                result = (CLK_ReturnCode)ClkLib_ReadAsUInt16(UnitAdr, memoryType, readOffset, data, wordLength);
                // DWORD 時、ここにSwapX2(data)いるかも。未検証。データが結局なかったので
            }
            if (result != CLK_ReturnCode.CLK_SUCCESS)
            {
                var sb = new StringBuilder("Read error ");
                sb.Append(result).Append(":");
                sb.Append(memoryType).Append(",");
                sb.Append(readOffset).Append(",");
                sb.Append(wordLength).Append(",");
                data.ToList().ForEach(f => sb.Append(f));
                Log.Error(sb.ToString());
            }
            return result;
        }
        /// <summary>
        /// ビットの状態確認 obsoleted
        /// ・WordAccessしてデータ取得して、対象Bitを確認する。
        /// </summary>
        /// <param name="memoryType"></param>
        /// <param name="readOffset"></param>
        /// <param name="bitPlace">AND条件での指定</param>
        /// <returns></returns>
        private bool HasFlags_AND(EventMemory memoryType, UInt32 readOffset, BitPlace bitPlace)
        {
            UInt32 length = 1;
            var data = new UInt16[length];
            if (ClkLib_ReadAsUInt16(UnitAdr, memoryType, readOffset, data, length) == (int)CLK_ReturnCode.CLK_SUCCESS)
            {
                return ((BitPlace)data[0]).HasFlag(bitPlace);
            }
            else
            {
                return false;   // エラー時は 0 
            }
        }
        /// <summary> 
        /// ビットの状態確認 obsoleted
        /// ・WordAccessしてデータ取得して、対象Bitを確認する。
        /// </summary>
        /// <param name="memoryType"></param>
        /// <param name="readOffset"></param>
        /// <param name="bitPlace">OR条件での指定</param>
        /// <returns></returns>
        private bool HasFlags_OR(EventMemory memoryType, UInt32 readOffset, BitPlace bitPlace)
        {
            UInt32 length = 1;
            var data = new UInt16[length];
            if (ClkLib_ReadAsUInt16(UnitAdr, memoryType, readOffset, data, length) == (int)CLK_ReturnCode.CLK_SUCCESS)
            {
                return ((BitPlace)data[0] & bitPlace) != BitPlace.NOBIT;
            }
            else
            {
                return false;   // エラー時は 0 
            }
        }
        /// <summary>
        /// WORD2SmallInt 未対応。obsoleted
        /// </summary>
        /// <param name="memoryType"></param>
        /// <param name="readOffset"></param>
        /// <param name="accessSize"></param>
        /// <param name="bitPlace"></param>
        /// <returns></returns>
        private string ReadAsString(EventMemory memoryType, UInt32 readOffset, AccessSize accessSize, BitPlace bitPlace = BitPlace.NOBIT)
        {
            switch (accessSize)
            {
                case AccessSize.BIT:
                    return HasFlags_AND(memoryType, readOffset, bitPlace).ToString();
                case AccessSize.BYTE:
                    throw new NotImplementedException("Word Access Address しか無い場合、Byte指定はあり得ない。上位/下位のどちらかが取得不能なので");
                case AccessSize.WORD:
                    return ReadAsUInt16(memoryType, readOffset).ToString();
                case AccessSize.DWORD:
                    return ReadAsUInt16(memoryType, readOffset).ToString() + ReadAsUInt16(memoryType, readOffset + 1).ToString();
                default:
                    return "illegal parameter";
            }
        }
        // obsoleted
        private UInt32 ReadAsUInt32(EventMemory memoryType, UInt32 readOffset, AccessSize accessSize, BitPlace bitPlace = BitPlace.NOBIT)
        {
            switch (accessSize)
            {
                case AccessSize.BIT:
                    return (UInt16) (HasFlags_AND(memoryType, readOffset, bitPlace) ? 0 : 1);
                case AccessSize.BYTE:
                    throw new NotImplementedException("Word Access Address しか無い場合、Byte指定はあり得ない。上位/下位のどちらかが取得不能なので");
                case AccessSize.WORD:
                    return ReadAsUInt16(memoryType, readOffset);
                case AccessSize.DWORD:
                    return (UInt32)(ReadAsUInt16(memoryType, readOffset) << 16) + ReadAsUInt16(memoryType, (UInt32)readOffset + 1);
                default:
                    return UInt32.MinValue;
            }
        }
        // obsoleted
        private UInt16 ReadAsUInt16(EventMemory memoryType, UInt32 readOffset)
        {
            UInt32 length = 1;
            var data = new UInt16[length];
            if (ClkLib_ReadAsUInt16(UnitAdr, memoryType, readOffset, data, length) == (int)CLK_ReturnCode.CLK_SUCCESS)
            {
                return data[0];
            }
            else
            {
                return 0;   // エラー時は 0 
            }
        }

        // obsoleted
        private CLK_ReturnCode GetLastError()
        {
            return (CLK_ReturnCode)ClkLib_GetLastError(UnitAdr);
        }

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // マネージ状態を破棄します (マネージ オブジェクト)。
                }

                // アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // 大きなフィールドを null に設定します。
                ClkLib_Close(UnitAdr);

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        ~ClkLibHelper()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(false);
        }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
