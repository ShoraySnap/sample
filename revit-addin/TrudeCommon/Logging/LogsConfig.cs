using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace TrudeCommon.Logging
{
    static class LogsConfig
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public static void Initialize(string logFileName)
        {
            var configuration = new NLog.Config.LoggingConfiguration();
            string snaptrudeManagerPath = "snaptrude-manager";
            string prefixFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                snaptrudeManagerPath,
                "logs"
            );
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = prefixFolder + "\\" + logFileName + ".txt"};
            configuration.AddRule(LogLevel.Trace, LogLevel.Fatal, logfile);

            NLog.LogManager.Configuration = configuration;
        }

        public static void Shutdown()
        {
            LogManager.Flush();
            LogManager.Shutdown();
        }
    }
}
