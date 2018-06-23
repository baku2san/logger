using System;
using System.IO;

namespace loggerApp.AppSettings
{
    public class loggerConstants
    {
        public string ApplicationName = System.Reflection.Assembly.GetExecutingAssembly().Location;
        public string ApplicationDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"shima\logger");
        public const string ServiceDescription = "shima Logger. OMRON PLC からのデータ取得";
        public const string ServiceDsiplayName = "shima Logger";
        public const string ServiceServiceName = "shimaLoggerService";        // space 入れないでね
        public const int DbConsummerWarningQueueCount = 100;
    }
}
