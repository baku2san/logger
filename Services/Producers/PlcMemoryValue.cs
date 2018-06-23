using System;

namespace loggerApp.Producers
{
    public class PlcMemoryValue
    {
        /// <summary>
        /// Object として、各型でそのまま入れ、DBへもそのまま流すようにする
        /// </summary>
        public Object Value {get; set;}

        public PlcMemoryInfo PlcMemoryInfo { get; set; }

        public PlcMemoryValue(PlcMemoryInfo plcMemoryInfo, Object value)
        {
            PlcMemoryInfo = plcMemoryInfo;
            if (PlcMemoryInfo.Converter != null)
            {
                Value = PlcMemoryInfo.Converter(value);
            }
            else
            {
                Value = value;
            }
        }
    }
}
