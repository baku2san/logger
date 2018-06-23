using System;
using System.Collections.Generic;
using System.Linq;
using static loggerApp.CppWrapper.ClkLibConstants;

namespace loggerApp.Producers
{
    /// <summary>
    ///  UInt16でのAccessに標準化し、Burst読込を行わせるためのCache用Class
    ///  Csache 対象なので、Readを行うものだけ
    /// </summary>
    public class PlcMemoryCaches
    {
        public List<PlcMemoryCache> Caches { get; set; }

        public PlcMemoryCaches()
        {
            Caches = new List<PlcMemoryCache>();
        }
        public PlcMemoryCaches(IEnumerable<PlcMemoryInfo> infos) : this()
        {
            if (infos.Count() == 0) { return; }
            
            // AccessをUInt16 に統一し、同一MemoryType & Addressを統合。基準要素が必要なので最小Offsetを取得出来るようにOrderByしておく
            var standardizedInfos= infos
                                    .Where(w=> w.IsEnable && w.ConstantValue == null)
                                    .Select(s => StandardizeAccess(s))
                                    .GroupBy(g => new {  g.MemoryType,  g.ReadOffset })
                                    .Select(s => new PlcMemoryCache(s.Key.MemoryType, s.Key.ReadOffset, s.ToList().Max(m => m.Length)))
                                    .OrderBy(o => o.ReadOffset);

            var first = standardizedInfos.FirstOrDefault();

            var pre = new PlcMemoryCache(first.MemoryType, first.ReadOffset, first.Length);
            Caches.Add(pre);

            // 最初の要素を基準として、読み込み範囲の重複を除去＆結合していく
            foreach (var item in standardizedInfos.Skip(1))
            {
                if ((pre.EndOffset + 1) >= item.ReadOffset)     // 連続時も結合する為、End+1
                {
                    if (pre.EndOffset >= item.EndOffset)
                    {
                        // item は pre に含有で処理不要
                    }
                    else
                    {
                        pre.Length += item.EndOffset - pre.EndOffset;
                    }
                }
                else
                {
                    pre = new PlcMemoryCache(item.MemoryType, item.ReadOffset, item.Length);
                    Caches.Add(pre);
                }
            }
        }
        /// <summary>
        /// word access のみ、ということなので、長さ対応だけをここでやることになった。DWORDだと2words 読み込む必要がある、ってことで
        /// </summary>
        private Func<PlcMemoryInfo, PlcMemoryCache> StandardizeAccess = (input) =>
        {
            switch (input.AccessSize)
            {
                case AccessSize.BIT:
                case AccessSize.BYTE:
                case AccessSize.WORD:
                case AccessSize.WORD2SmallInt:
                    return new PlcMemoryCache(input.MemoryType, input.ReadOffset, 1);
                case AccessSize.DWORD:
                default:
                    return new PlcMemoryCache(input.MemoryType, input.ReadOffset, 2);
            }
        };
        public PlcMemoryValue ReadAsValue(PlcMemoryInfo plcMemoryInfo)
        {
            // 固定値の場合即座に返す
            if (plcMemoryInfo.ConstantValue != null) {
                return new PlcMemoryValue(plcMemoryInfo, plcMemoryInfo.ConstantValue);
            }

            Object value;
            // Offset/Length のCache情報への置換
            var readCache = StandardizeAccess(plcMemoryInfo);
            // 対象Cacheの取得
            var targetCache = Caches.First(w => w.WithinValue(readCache.ReadOffset));
            // 対象値の取得
            var beginOffset = (int)(readCache.ReadOffset - targetCache.ReadOffset);
            var chacedValues = targetCache.Values.Skip(beginOffset).Take((int)readCache.Length);
            switch (plcMemoryInfo.AccessSize)
            {
                case AccessSize.BIT:
                    value = ((BitPlace)chacedValues.FirstOrDefault() & plcMemoryInfo.BitPlace) != BitPlace.NOBIT;
                    break;
                case AccessSize.WORD:
                    var valueWord = BitConverter.GetBytes(chacedValues.FirstOrDefault());
                    value = new Byte[2] { valueWord[1], valueWord[0] };
                    break;
                case AccessSize.WORD2SmallInt:
                    value = chacedValues.FirstOrDefault();
                    //value = (UInt16) (((valueWord & 0xFF00) >> 8) | ((valueWord & 0x00FF) << 8));
                    break;
                case AccessSize.BYTE:
                    value = (Byte)(chacedValues.FirstOrDefault());
                    break;
                case AccessSize.DWORD:
                    var values = chacedValues.Select(s => BitConverter.GetBytes(s)).ToArray();
                    value = new Byte[4] { values[1][1], values[1][0], values[0][1], values[0][0] };
                    break;
                default:
                    value = null;
                    break;
            }
            return new PlcMemoryValue(plcMemoryInfo, value);
        }
        // Endianの問題が生じた時の、前後置換用。テスト用に使うべきで、実装を変えるべきだよ、勿論
        static private byte[] ByteReverse(Byte[] bytes)
        {
            return new Byte[4] { bytes[1], bytes[0], bytes[3], bytes[2] };
        }
    }
}
