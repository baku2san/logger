using loggerApp.Extensions;
using System;
using static loggerApp.CppWrapper.ClkLibConstants;

namespace loggerApp.Producers
{
    public class PlcMemoryInfoTrigger : PlcMemoryInfo
    {
        public enum TriggerTypes
        {
            Status,
            ChangeTo,
            ChangeFrom,
        }
        /// <summary>
        /// TODO: Value系は、AccessSize設定時か、Initial時に、型合わせをしないとまずいよね。現状全ての型に対応していないので機能向上時は注意しましょう
        /// </summary>
        public Object TriggerValue { get; set; }
        /// <summary>
        /// TriggerがWORD/DWORD時、比較対象と同一型であるTriggerValueもByte[]にしておく為のもの。正直・・BYTE[]形式でのTrigger自体やめたほうがいい・・かな
        /// </summary>
        private Byte[] TriggerArrayValue { get; set; }      // 現状使ってないので削除したいけど、確認方法が現地しかないので、このまま残しておく・・ごめんね
        private Object PreviousValue { get; set; }
        public TriggerTypes TriggerType { get; set; }

        /// <summary>
        /// for SettingsJson
        /// </summary>
        public PlcMemoryInfoTrigger()
        {

        }
        public PlcMemoryInfoTrigger(Object triggerValue, TriggerTypes type = TriggerTypes.Status) : base()
        {
            PreviousValue = null;
            TriggerValue = triggerValue;
            TriggerType = type;
        }
        internal PlcMemoryInfoTrigger(Object triggerValue, TriggerTypes type, string name, string description, EventMemory memoryType = EventMemory.DM,
            UInt32 readOffset = 0, AccessSize accessSize = AccessSize.WORD, BitPlace bitPlace = BitPlace.NOBIT,
            bool isEnable = true, Object constantValue = null)
            : base(name, description, memoryType, readOffset, accessSize, bitPlace, isEnable, constantValue)
        {
            TriggerValue = triggerValue;
            TriggerType = type;
        }

        internal new void Initialize() 
        {
            base.Initialize();
            switch (base.AccessSize)
            {
                case AccessSize.WORD:
                    var obj = BitConverter.GetBytes((UInt16)(Int32)TriggerValue);
                    TriggerArrayValue = obj;
                    break;
                case AccessSize.BIT:
                    break;
                case AccessSize.WORD2SmallInt:
                    TriggerValue = (UInt16)(Int32)TriggerValue;    // JSON 設定保存∴Int→short への変換必要。Object格納している為、Equal(）使用するため
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
        internal bool IsValid(Object currentValue)
        {
            var result = false;
            if (TriggerArrayValue == null)
            {
                switch (TriggerType)
                {
                    case TriggerTypes.Status:
                        result = TriggerValue.Equals(currentValue);
                        break;
                    case TriggerTypes.ChangeFrom:
                        result = (TriggerValue.Equals(PreviousValue) && !TriggerValue.Equals(currentValue));
                        PreviousValue = currentValue;
                        break;
                    case TriggerTypes.ChangeTo:
                        result = (!TriggerValue.Equals(PreviousValue) && TriggerValue.Equals(currentValue));
                        PreviousValue = currentValue;
                        break;
                    default:
                        break;
                }
            }
            else
            {
                switch (TriggerType)
                {
                    case TriggerTypes.Status:
                        result = TriggerArrayValue.SequenceEqualWithNull(currentValue);
                        break;
                    case TriggerTypes.ChangeFrom:
                        result = (TriggerArrayValue.SequenceEqualWithNull(PreviousValue) && !TriggerArrayValue.SequenceEqualWithNull(currentValue));
                        PreviousValue = currentValue;
                        break;
                    case TriggerTypes.ChangeTo:
                        result = (!TriggerArrayValue.SequenceEqualWithNull(PreviousValue) && TriggerArrayValue.SequenceEqualWithNull(currentValue));
                        PreviousValue = currentValue;
                        break;
                    default:
                        break;
                }
            }
            return result;
        }
    }
}
