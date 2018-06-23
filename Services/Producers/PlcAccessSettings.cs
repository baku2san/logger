using loggerApp.Consumers;
using System;
using System.Collections.Generic;
using System.Data.Entity.Design.PluralizationServices;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;

namespace loggerApp.Producers
{
    /// <summary>
    /// ❏Cache実装
    /// APIasC は16bit アクセスなので、そこをTargetとする
    /// ・16bitアクセスで、OffsetとLengthを一覧化
    /// ・Watcherで上記を読込、読込結果を保持 to Cached**
    /// ・対象Memoryに対し、8/16/32 でのOffsetReadIFを用意
    /// ・各Trigger/Target 別
    /// ・Trigger/Target で、Cacheを読み込ませて結果を保持し、DBへ
    /// ❏Class概要
    /// PlcAccessSettings
    ///     ・周期相違でのRead PLC, Store DB を実現
    ///     ・TableNameを同一にすれば、同一DBに挿入可能。（構造上問題ないことが前提）
    ///   PlcmemoryInfoGroups
    ///   　・同一Tableに対して、相違設定での読込を行う場合に、複数Groupにする。周期同一の場合、Casheを利用できるので速度向上に貢献
    /// </summary>
    public class PlcAccessSettings
    {
        /// <summary>
        /// Table Name @ DB
        /// </summary>
        private string _TableName { get; set; }
        public string TableName
        {
            get { return this._TableName; }
            set { this._TableName = PluralizationService.CreateService(new CultureInfo("en-US")).Pluralize(value); }
        }
        public bool IsEnable { get; set; }
        public bool AsUpdateSQL { get; set; }

        /// <summary>
        /// 設定変更・保存用
        /// 同一Tableに対して、複数の設定群を割り付ける必要が生じたので、InfoGroup を定義してList化した
        /// </summary>
        public List<PlcMemoryInfoGroup> InfoGroups;

        /// <summary>
        /// DB への登録情報。同一構造がList化されている前提なので、First で取得可能。
        /// ConvertInfoと、MemoryInfoを分離しようかなと、Targets のGrouping化になったんで
        /// </summary>
        public IEnumerable<PlcMemoryInfo> DbTargets
        {
            get
            {
                return InfoGroups.FirstOrDefault()?.RawTargets.FirstOrDefault().Where(w => w.IsEnable && w.HasConvertDefinition);
            }
        }
        internal IEnumerable<PlcMemoryInfo> InsertTargets
        {
            get
            {
                return InfoGroups.FirstOrDefault()?.RawTargets.FirstOrDefault().Where(w => w.IsEnable);
            }
        }

        internal SqlCommand SqlCommand;
        internal PlcMemoryCaches CachesBySettings;
        public UInt16 CycleTimeMs { get; set; }

        public PlcAccessSettings() 
        {
            IsEnable = true;
            AsUpdateSQL = false;
            InfoGroups = new List<PlcMemoryInfoGroup>();
        }
        public PlcAccessSettings(UInt16 cycleTimeMs = 100, string tableName = null) : this()
        {
            CycleTimeMs = cycleTimeMs;
            TableName = tableName ?? TableName;
        }
        internal void Initialize(string lineName)
        {
            // 個々のInitialize
            InfoGroups.ForEach(f => f.Initialize());
            CachesBySettings = new PlcMemoryCaches(InfoGroups.Select(s => s.OptimizedTriggers).SelectMany(sm => sm));
            InfoGroups.ForEach(f => f.PostInitialize(CachesBySettings));

            // 速度向上用
            if (AsUpdateSQL)
            {
                //SqlCommand = DbConsumer.GetSqlCommandAsUpdateOrInsert(TableName, lineName, InsertTargets);
                SqlCommand = DbConsumer.GetSqlCommandAsUpdate(TableName, lineName, InsertTargets);  // MergeだとLockの可能性があるのでUpdateOnlyとした
            }
            else
            {
                SqlCommand = DbConsumer.GetSqlCommandAsInsert(TableName, lineName, InsertTargets);
            }
        }

        internal IEnumerable<PlcMemoryInfoGroup> ExecutableTargets()
        {
            return InfoGroups.Where(w => w.IsExecutable());
        }
    }
}
