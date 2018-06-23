using System;
using static loggerApp.CppWrapper.ClkLibConstants;

namespace loggerApp.Producers
{
    public class PlcMemoryInfo
    {
        /// <summary>
        /// Value に対する特殊な変換を行う場合の設定：あれ？Delegateやめて、単純にこれでSwitchでよかったんじゃね？ TODO: Refactoring出来そうだね
        /// </summary>
        public enum ConvertOption
        {
            None,
            Mod1000,
        }
        public string Name { get; set; }
        public string Description { get; set; }
        public EventMemory MemoryType { get; set; }
        public UInt32 ReadOffset { get; set; }
        public AccessSize AccessSize { get; set; }
        public BitPlace BitPlace { get; set; }
        public bool IsEnable { get; set; }
        /// <summary>
        /// 既定値の設定。無い場合はNullで、Read実施
        /// </summary>
        public Object ConstantValue { get; set; }
        internal Func<Object, Object> Converter { get; set; }     // Delegate で切り替えようとしたが、Serialize出来ないと設定（Converter)が消えちゃうので、Converter選択方式にしている
        public ConvertOption Convertion { get; set; } 

        public bool HasConvertDefinition { get; set; }  // TODO: DbTables を分離すればこんなのいらんのよね・・。Column@Table 単位での定義なので
        public double Slope { get; set; }
        public double Intercept { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public string Unit { get; set; }

        public PlcMemoryInfo()
        {
            HasConvertDefinition = true;
            Convertion = ConvertOption.None;
            Converter = null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="description"></param>
        /// <param name="memoryType">CIO はBitのみ</param>
        /// <param name="readOffset"></param>
        /// <param name="accessSize">CIO はBit限定</param>
        /// <param name="isEnable"></param>
        /// <param name="bitPlace"></param>
        public PlcMemoryInfo(string name, string description, EventMemory memoryType = EventMemory.DM, UInt32 readOffset = 0, AccessSize accessSize = AccessSize.WORD, BitPlace bitPlace = BitPlace.NOBIT, bool isEnable = true, Object constantValue = null) : this()
        {
            // 初期化
            Name = name;
            Description = description;
            MemoryType = memoryType;
            AccessSize = accessSize;
            IsEnable = isEnable;
            ReadOffset = readOffset;
            BitPlace = bitPlace;

            ConstantValue = constantValue;
        }
        internal void Initialize()
        {
            switch (Convertion)
            {
                case ConvertOption.Mod1000:
                    Converter = Mod1000;
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// TODO: 設定がJSONだったんで、short 等の維持が難しい・・Binaryも厳しい・・どうしたもんか
        /// </summary>
        /// <param name="value"></param>
        public void SetConstantValue(bool value)
        {
            ConstantValue = value;
        }
        public void SetConstantValue(byte value)
        {
            ConstantValue = value;
        }
        public void SetConstantValue(short value)
        {
            ConstantValue = value;
        }
        public void CheckAndFix()
        {

            if (MemoryType == EventMemory.CIO)  // CIO はBit限定
            {
                AccessSize = AccessSize.BIT;
            }
            if ((MemoryType == EventMemory.CIO) && (BitPlace == BitPlace.NOBIT))    // CIO はBit必須なので、Bit 省略＝Addressに含有と認識して解釈する
            {
                BitPlace = (BitPlace)(Math.Pow(2, ReadOffset % 100));
                ReadOffset = ReadOffset / 100;
            }
        }
        /// <summary>
        /// 下位三桁の抽出 as １０進
        /// </summary>
        /// <param name="input">int</param>
        /// <returns>short</returns>
        public Object Mod1000(Object input)
        {
            UInt16 inputMasked = (UInt16)((UInt16)input & 0x7FFF);  // Rohm江崎様「-101」みたいなのがあるかもなんで、絶対値でみてね。とのこと
            return (short)(inputMasked % 1000);
        }
    }
}
