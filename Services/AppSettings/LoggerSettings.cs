using loggerApp.Producers;
using System;
using System.Collections.Generic;

namespace loggerApp.AppSettings
{
    public class LoggerSettings
    {
        /// <summary>
        /// 装置名称　or PC名称
        /// </summary>
        public string LineName { get; set; }
        public Byte NetworkNo { get; set; }
        public Byte NodeNo { get; set; }
        public Byte UnitNo { get; set; }
        public string WatchingRecipeFolderPath { get; set; }

        public List<PlcAccessSettings> PlcAccesss { get; set; }
        // TODO: 設定で記憶している事は設定ファイルを外部に持ち出せたら問題になるね。このユーザーのみのInstallとしているので問題は少ないとの判断
        public string ServiceUserName { get; set; }
        public string ServicePassword { get; set; }

        public LoggerSettings()
        {
            PlcAccesss = new List<PlcAccessSettings>();
        }
        public void Initialize()
        {
            PlcAccesss.ForEach(f => f.Initialize(LineName));
        }
    }
}
