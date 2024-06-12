using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace TrudeCommon.Logging
{
    static class LogsConfig
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public static void Initialize(string logFileName)
        {
            var configuration = new NLog.Config.LoggingConfiguration();
            string snaptrudeManagerPath = "snaptrude-manager";
            string prefixFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                snaptrudeManagerPath,
                "logs"
            );
            var logfile = new NLog.Targets.FileTarget("logfile") { 
                FileName = $"{prefixFolder}\\{logFileName}.txt",
                DeleteOldFileOnStartup = true,
                CreateDirs = true,
                //KeepFileOpen = true,
            };
            configuration.AddTarget(logfile);
            configuration.AddRule(LogLevel.Trace, LogLevel.Fatal, logfile);

            NLog.LogManager.Configuration = configuration;

            logger.Info("<<<STARTUP>>>");
            logger.Info("===================");
            logger.Info("Time : {0}", DateTime.Now.ToString());
            logger.Info("===================\n");
        }

        public static void Shutdown()
        {
            logger.Info("<<<SHUTDOWN>>>\n");
            logger.Info("===================");
            logger.Info("Time : {0}", DateTime.Now.ToString());
            logger.Info("===================\n");
            LogManager.Flush();
            LogManager.Shutdown();

            //TODO: Upload log file from UI
        }
    }
}
