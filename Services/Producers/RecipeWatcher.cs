using loggerApp.Extensions;
using loggerApp.Models;
using loggerApp.Queue;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reactive.Linq;
using System.Text;

namespace loggerApp.Producers
{
    class RecipeWatcher : IDisposable
    {
        enum RecipeCsvIndex : int
        {
            WroteTime,
            Count,
            DatFileName,
            WaferId1,
            FoupId,
            LotNo,
            WaferId2,
            DeviceNumber,
            DeviceVersion,
            LayerNumber,
            ProductName,
            IntegratedRecipe,
            MapName,
            UnUsed1,
            ModuleStatus,
            UnUsed2,
            UnUsed3,
            ProcessRecipe,
        }
        private BlockingCollection<IQueueingData> BlockingCollection;
        private FileSystemWatcher RecipeFileWatcher = null;
        private string WatchingPath;
        private string LineName { get; set; }

        private long PreviousLength { get; set; }

        public RecipeWatcher(BlockingCollection<IQueueingData> bc, string lineNo, string path) : this(path)
        {
            BlockingCollection = bc;
            LineName = lineNo;
        }
        private RecipeWatcher(string path)
        {
            WatchingPath = path;
            RecipeFileWatcher = new FileSystemWatcher()
            {                
                Path = WatchingPath,
                NotifyFilter =
                    (
                    //    System.IO.NotifyFilters.LastAccess
                    //    System.IO.NotifyFilters.FileName,
                    //    System.IO.NotifyFilters.DirectoryName,
                        NotifyFilters.CreationTime |
                        NotifyFilters.LastWrite ),
                Filter = "",    // 全て
                // SynchronizingObject = this   // UIのスレッドにマーシャリングする時に必要 /コンソールアプリケーションでの使用では必要ない
                IncludeSubdirectories = false
            };
            //イベントハンドラの追加
            // Rxを使用することで、イベント集約
            RecipeFileWatcher.ChangedAsObservable()
                .Throttle(TimeSpan.FromSeconds(1))      // 一秒内のイベントに集約→一秒内に複数回イベントがあっても一回のイベントに。（実際に複数あったら消えるが、あり得ないと判断）
                .Subscribe(e => DetectDifference(e));

        }
        public void Start()
        {
            try
            {
                //監視を開始する
                Log.Information("監視を開始します。{0}", WatchingPath);
                RecipeFileWatcher.EnableRaisingEvents = true;
            }
            catch(Exception ex) 
            {
                Log.Error(ex, "Recipe watcher can not start.");
            }
        }
        public void Stop()
        {
            RecipeFileWatcher.EnableRaisingEvents = false;
        }
        public void Dispose()
        {
            RecipeFileWatcher.EnableRaisingEvents = false;    // とりあえず停止しておいてからにする
            ((IDisposable)RecipeFileWatcher).Dispose();
        }

        private void DetectDifference(FileSystemEventArgs eventArgs)
        {
            var fileInfo = new FileInfo(eventArgs.FullPath);
            if ((fileInfo.Attributes & FileAttributes.Directory) != FileAttributes.Directory
                && fileInfo.Extension == ".csv")   // SubFolderに*.dat ファイルが入ってくるのでこれを無視する為とその他に例外があるかもなので
            {
                if (eventArgs.ChangeType == WatcherChangeTypes.Created)
                {
                    // 新規作成検知時は、前回Byte位置はClear
                    PreviousLength = 0;
                    // 同時に複数ファイルは出来てこない前提。
                }
                if (PreviousLength != fileInfo.Length)
                {
                    var recipeList = new RecipeList();
                    using (var fileStream = fileInfo.OpenRead())
                    {
                        fileStream.Seek(PreviousLength, SeekOrigin.Begin);
                        using (var streamReader = new StreamReader(fileStream, Encoding.GetEncoding("Shift_JIS"), true))
                        {
                            var currentTime = DateTime.Now;
                            while (!streamReader.EndOfStream)
                            {
                                var line = streamReader.ReadLine();
                                var columns = line.Split(',');
                                var newRecipe = new Recipe()
                                {
                                    WroteTime = DateTime.TryParse(columns[(int)RecipeCsvIndex.WroteTime], out DateTime wroteTime) ? wroteTime : System.Data.SqlTypes.SqlDateTime.MinValue.Value,
                                    Count = short.TryParse(columns[(int)RecipeCsvIndex.Count], out short count) ? count : (short)0,
                                    DatFileName = columns[(int)RecipeCsvIndex.DatFileName],
                                    WaferNo = (short)(int.TryParse(columns[(int)RecipeCsvIndex.WaferId1], out int waferNo) ? waferNo % 1000 : 0),
                                    CassetteId = columns[(int)RecipeCsvIndex.FoupId],
                                    LotNo = columns[(int)RecipeCsvIndex.LotNo],
                                    WaferId = columns[(int)RecipeCsvIndex.WaferId2],
                                    DeviceNumber = columns[(int)RecipeCsvIndex.DeviceNumber],
                                    DeviceVersion = columns[(int)RecipeCsvIndex.DeviceVersion],
                                    LayerNumber = columns[(int)RecipeCsvIndex.LayerNumber],
                                    ProductName = columns[(int)RecipeCsvIndex.ProductName],
                                    IntegratedRecipe = columns[(int)RecipeCsvIndex.IntegratedRecipe],
                                    MapName = columns[(int)RecipeCsvIndex.MapName],
                                    ModuleStatus = columns[(int)RecipeCsvIndex.ModuleStatus],
                                    ProcessRecipe = columns[(int)RecipeCsvIndex.ProcessRecipe],
                                    LineName = LineName,
                                    Created = currentTime,
                                };
                                recipeList.Recipes.Add(newRecipe);
                            }
                        }
                    }
                    PreviousLength = fileInfo.Length;
                    BlockingCollection.Add(recipeList);
                }
            }
            else
            {
                Log.Information("Detected but do nothing at {ChangingPath} as {ChangeType}.", eventArgs.FullPath, eventArgs.ChangeType);
            }
        }
    }
}