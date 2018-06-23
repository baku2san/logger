using loggerApp.AppSettings;
using loggerApp.Consumers;
using loggerApp.CppWrapper;
using loggerApp.Producers;
using loggerApp.Queue;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace loggerApp
{
    /// <summary>
    /// Timmer 上で、Start/Stop にしたほうがいいかも・・
    /// </summary>
    public class LoggerService : IDisposable
    {
        private LoggerJsonSettings Settings;
        private CancellationTokenSource CancellationTokenSource;

        private IEnumerable<PlcWatcher> PlcWatchers;
        private RecipeWatcher RecipeWatcher;
        private DbConsumer DbConsumer;
        public LoggerService(LoggerJsonSettings settings)
        {
            Settings = settings;
        }
        public void Start() { 
            Settings.LoggerSettings.Initialize();

            CancellationTokenSource = new CancellationTokenSource();
            var storingData = new BlockingCollection<IQueueingData>();

            // データをDBへ突っ込む部分
            DbConsumer = new DbConsumer(storingData, CancellationTokenSource.Token);

            // Recipe監視
            RecipeWatcher = new RecipeWatcher(storingData, Settings.LoggerSettings.LineName, Settings.LoggerSettings.WatchingRecipeFolderPath);

            // Plc監視
            var factory = new ClkLibHelperFactory(Settings.LoggerSettings.NetworkNo, Settings.LoggerSettings.NodeNo, Settings.LoggerSettings.UnitNo);        // ClkLib のHandle番号管理。実HandleはC-Lib 内
            PlcWatchers = Settings.LoggerSettings.PlcAccesss.Where(w => w.IsEnable).Select(f => new PlcWatcher(storingData, CancellationTokenSource.Token, factory.GetHelper(), f));

            // start
            DbConsumer.Start();
            RecipeWatcher.Start();
            PlcWatchers.ToList().ForEach(f => f.Start());
        }
        public void Stop()
        {
            if (CancellationTokenSource != null)
            {
                CancellationTokenSource.Cancel();
                RecipeWatcher.Dispose();
                PlcWatchers.ToList().ForEach(f => f.Dispose());
                CancellationTokenSource.Dispose();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Settings = null;
                    CancellationTokenSource = null;
                    PlcWatchers = null;
                    RecipeWatcher = null;
                    DbConsumer = null;
                }

                // アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~LoggerService() {
        //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //   Dispose(false);
        // }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}
