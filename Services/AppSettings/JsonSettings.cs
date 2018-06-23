using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Web.Script.Serialization;

namespace loggerApp.AppSettings
{
    /// <summary>
    /// https://stackoverflow.com/questions/453161/best-practice-to-save-application-settings-in-a-windows-forms-application
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JsonSettings<T> where T : new()
    {
        public string DefaultPath { get; set; }

        public JsonSettings()
        {
            DefaultPath = Path.Combine(new loggerConstants().ApplicationDataPath, "loggerSettings.json");
        }
        public void Save()
        {
            File.WriteAllText(DefaultPath, (new JavaScriptSerializer()).Serialize(this));
        }

        public void Save(T pSettings)
        {
            File.WriteAllText(DefaultPath, (new JavaScriptSerializer()).Serialize(pSettings));
        }

        public T Load()
        {
            T t = new T();
            try
            {
                if (File.Exists(DefaultPath))
                    t = (new JavaScriptSerializer()).Deserialize<T>(File.ReadAllText(DefaultPath));
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Load settings");
            }
            return t;
        }
    }
}
