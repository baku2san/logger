using System.Collections.Generic;
using System.Linq;
using static loggerApp.CppWrapper.ClkLibConstants;

namespace loggerApp.Producers
{
    /// <summary>
    /// PlcMemoryInfoGroup
    /// ・Cache 単位での制御のまとまり
    /// ・RawTriggers：複数Triggerのまとまりこの条件でTarget確認
    /// ・RawTargets：複数Targetsのまとまりの　Group(外側のList）
    /// 　・Group単位で、同一Tableに対する、周期同一の、読み込み設定相違を吸収
    /// </summary>
    public class PlcMemoryInfoGroup
    {
        /// <summary>
        /// trigger 未登録時のDefault動作。True:稼働, False:停止
        /// PlcMemoryInfo.ConstantValue を登録することで、PLCアクセス無しで固定で稼働／停止を決めることも可能
        /// </summary>
        public bool IsExecutableWithoutTrigger { get; set; }
        private bool IsExecutableAsConstants { get; set; }

        /// <summary>
        /// 設定変更・保存用
        /// </summary>
        public List<PlcMemoryInfoTrigger> RawTriggers;
        public List<List<PlcMemoryInfo>> RawTargets;
        /// <summary>
        /// 実使用する参照用
        /// </summary>
        internal IEnumerable<PlcMemoryInfoTrigger> OptimizedTriggers;
        internal IEnumerable<IEnumerable<PlcMemoryInfo>> OptimizedTargets;

        /// <summary>
        /// Burst読み込みCache：ここに読み込んで、Targetsの内容をValueList化する
        /// </summary>
        internal PlcMemoryCaches CachedTriggers;
        internal PlcMemoryCaches CachedTargets;

        public PlcMemoryInfoGroup()
        {
            RawTriggers = new List<PlcMemoryInfoTrigger>();
            RawTargets = new List<List<PlcMemoryInfo>>();
        }
        internal void Initialize()
        {
            // 個々のInitialize
            RawTriggers.ForEach(f => f.Initialize());
            RawTargets.ForEach(f => f.ForEach(cf=>cf.Initialize()));

            // Trigger は単純に全ての　稼働条件を、Constantの是非で集約して、IsExecutable()で利用出来るようにするだけ。
            var bitOptimized = RawTriggers
                .Where(w => w.IsEnable && (w.AccessSize == AccessSize.BIT) && (w.ConstantValue == null))
                .GroupBy(g => new { readOffset = g.ReadOffset, accessSize = g.AccessSize, triggerValue = g.TriggerValue, triggerType = g.TriggerType })
                .Select(s => new PlcMemoryInfoTrigger(s.Key.triggerValue, s.Key.triggerType, "optimizedBit", "optimizedBit", s.First().MemoryType, s.Key.readOffset, s.Key.accessSize, s.Select(v => v.BitPlace).Aggregate(BitPlace.NOBIT, (x, y) => x | y), true));
            var byteOptimized = RawTriggers
                .Where(w => w.IsEnable && (w.AccessSize != AccessSize.BIT) && (w.ConstantValue == null));
            OptimizedTriggers = bitOptimized.Union(byteOptimized);

            // constant をここで事前処理 with IsExecutableWithoutTrigger
            IsExecutableAsConstants = RawTriggers
                .Where(w => w.IsEnable && (w.AccessSize == AccessSize.BIT) && (w.ConstantValue != null))
                    .Select(s => new PlcMemoryValue(s, s.ConstantValue))
                    .Aggregate(IsExecutableWithoutTrigger, (x, y) => x | ((bool)y.Value));

            OptimizedTargets = RawTargets.Select(ps=>ps.Where(w => w.IsEnable));

            CachedTargets = new PlcMemoryCaches(OptimizedTargets.SelectMany(sm=>sm));
        }
        internal void PostInitialize(PlcMemoryCaches cachesBySettings)
        {
            CachedTriggers = cachesBySettings;
        }
        internal bool IsExecutable()
        {
            return OptimizedTriggers
                .Select(s => CachedTriggers.ReadAsValue(s))
                .Aggregate(IsExecutableAsConstants, (x, y) => x | ((y.PlcMemoryInfo as PlcMemoryInfoTrigger).IsValid(y.Value)));
        }

        internal List<List<PlcMemoryValue>> ReadValuesFromCaches()
        {
            return OptimizedTargets.Select(ps => ps.Select(s=> CachedTargets.ReadAsValue(s)).ToList()).ToList();
        }
    }
}
