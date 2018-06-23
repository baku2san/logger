using loggerApp.CppWrapper;
using loggerApp.Queue;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static loggerApp.CppWrapper.ClkLibConstants;

namespace loggerApp.Producers
{
    public class PlcWatcher : IDisposable
    {
        private CancellationToken Token;
        /// <summary>
        /// _UnitAdr    DLL 側のHandle管理番号：号機アドレスとは別物。タスクNoみたいなもの
        /// </summary>
        private ClkLibHelper ClkLibHelper;
        private PlcAccessSettings PlcAccessSettings;
        private BlockingCollection<IQueueingData> BlockingCollection;

        public PlcWatcher(BlockingCollection<IQueueingData> bc, CancellationToken token, ClkLibHelper clkLibHelper, PlcAccessSettings plcAccessSettings)
        {
            BlockingCollection = bc;
            Token = token;
            ClkLibHelper = clkLibHelper;
            PlcAccessSettings = plcAccessSettings;
        }

        public void Dispose()
        {
            ((IDisposable)ClkLibHelper)?.Dispose();
        }

        /// <summary>
        /// Open > Read .. Read > Close までの一連の処理をTask＆Sleep(周期－実行時間）で実施
        /// ・ClkLibに関することは長くなってもここに実装。
        /// </summary>
        public void Start()
        {
            var result = ClkLibHelper.Open();
            if (result == CLK_ReturnCode.CLK_SUCCESS)
            {
                try
                {
                    var period = PlcAccessSettings.CycleTimeMs;
                    Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            Log.Information("begin ClkLib." + PlcAccessSettings.TableName);
                            long previousCycle = 0;
                            long beginCycle = 0;
                            long endCycle = 0;
                            long executeTick = 0;       // 処理時間分、Sleepを短くする為に使用
                            int cycle = period;
                            while (true)
                            {
                                previousCycle = beginCycle;
                                beginCycle = Environment.TickCount;

                                // 実処理
                                // 実行判断処理：
                                // Burst read
                                PlcAccessSettings.CachesBySettings.Caches.ForEach(f => { ClkLibHelper.Read(f.MemoryType, f.ReadOffset, out ushort[] data, f.Length); f.Values = data; });

                                var valueList = new PlcMemoryValueList(PlcAccessSettings.TableName, PlcAccessSettings.SqlCommand);
                                PlcAccessSettings.ExecutableTargets().ToList()
                                    .ForEach(ig =>
                                    {
                                    // Cache更新
                                    ig.CachedTargets.Caches.ForEach(f => { ClkLibHelper.Read(f.MemoryType, f.ReadOffset, out ushort[] data, f.Length); f.Values = data; });
                                    // データ取得＆更新データ生成
                                    ig.ReadValuesFromCaches().ForEach(f => valueList.PlcMemoryValuesGroups.Add(f));
                                    });
                                if (valueList.PlcMemoryValuesGroups.Count > 0)
                                {
                                    BlockingCollection.Add(valueList);
                                }
                                // キャンセル処理
                                if (Token.IsCancellationRequested)
                                {
                                    break;
                                }

                                endCycle = Environment.TickCount;
                                executeTick = endCycle - beginCycle + 1 + 2;   // +1 は最低差分。+2 は調整用・・。理論上ではなく、実際動かした際の
                                if (endCycle < beginCycle)   // TickCount will jump to Int32.MinValue from Int32.MaxValue: approximately 49.7 days
                                {
                                    executeTick += (long)UInt32.MaxValue + 1; // int の値域分を足し込むことで、差分補正を行う
                                }
                                cycle = (int)(period - executeTick);
                                if (cycle <= 0)     // periodを超える処理が発生することがあった場合
                                {
                                    // 初回起動は重いよ
                                    Log.Warning("PlcWatcher is slow.");
                                    while (cycle <= 0)
                                    {   // period 単位でのWaitにしておく
                                        cycle += period;
                                    }
                                }
                                Thread.Sleep(cycle - 1);
                            }
                        }catch(Exception ex)
                        {
                            // TODO: なんらかの理由でOpen継続できなかった場合（Open自体も？）、Retry監視が必要かも
                            Log.Error(ex, "Exception at ClkLib task.");
                        }
                        Log.Information("end ClkLib.");
                        ClkLibHelper.Close();
                    });
                }
                catch (Exception ex)
                {
                    ClkLibHelper.Close();
                    // TODO: なんらかの理由でOpen継続できなかった場合（Open自体も？）、Retry監視が必要かも
                    Log.Error(ex, "Exception at ClkLib task beginning.");
                }
                finally
                {
                }
            }
            else
            {
                Log.Error("can not open ClkLib as {0} at {1}", result, PlcAccessSettings.TableName);
            }
        }
    }
}
