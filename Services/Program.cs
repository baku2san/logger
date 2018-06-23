using loggerApp.AppSettings;
using Serilog;
using System;
using Topshelf;

namespace loggerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            TopshelfExitCode exitCode = TopshelfExitCode.Ok;
            try
            {
                // https://github.com/serilog/serilog/wiki/AppSettings
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.AppSettings()
                    .CreateLogger();

                Log.Information("*** Begin Application. ***");

                exitCode = HostFactory.Run(x =>
                {
                    var settings = new LoggerJsonSettings().Load();
                    Log.Information("Loaded settings at {0}", settings.DefaultPath);
                    x.Service<LoggerService>(s =>
                    {
                        s.ConstructUsing(name => new LoggerService(settings));
                        s.WhenStarted(tc => tc.Start());
                        s.WhenStopped(tc => tc.Stop());

                    });

                    x.EnableServiceRecovery(r =>
                    {
                        r.OnCrashOnly();
                        r.RestartService(1); //first
                    r.RestartService(1); //second
                    r.RestartService(1); //subsequents
                });
                    //Windowsサービスの設定
                    x.RunAs(settings.LoggerSettings.ServiceUserName, settings.LoggerSettings.ServicePassword);

                    x.SetDescription(loggerConstants.ServiceDescription);
                    x.SetDisplayName(loggerConstants.ServiceDsiplayName);
                    x.SetServiceName(loggerConstants.ServiceServiceName);
                    x.StartAutomaticallyDelayed();
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Topshelf down?");
            }
            finally
            {
                Log.Information("*** Topshelf exit code is {0} ***", exitCode);
                Log.CloseAndFlush();
            }
        }
    }
}
