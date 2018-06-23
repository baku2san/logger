namespace loggerApp.AppSettings
{
    public class LoggerJsonSettings : JsonSettings<LoggerJsonSettings>
    {
        public LoggerSettings LoggerSettings { get; set; }

        public LoggerJsonSettings() {
            LoggerSettings = new LoggerSettings();
        }
    }
}
