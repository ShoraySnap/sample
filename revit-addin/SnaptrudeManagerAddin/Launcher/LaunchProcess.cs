﻿using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using NLog;

namespace SnaptrudeManagerAddin.Launcher
{
    public class LaunchProcess
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        public static Process process = null;
        public static Process CheckProcessRunning()
        {
            string name = "SnaptrudeManagerUI";
            var processes = Process.GetProcessesByName(name);
            if (processes.Length == 1)
            {
                process = processes[0];
                logger.Info("Process is already running with name: {0} and id: {1}", name, process.Id);
                return process;
            }
            else
            {
                return null;
            }
        }

        public static void StartProcess()
        {
            process = CheckProcessRunning();
            if (process != null)
            {
                logger.Warn("UI Process already running!");
                return;
            }

            logger.Info("Trying to start UI process...");
            FileInfo file = new FileInfo(Assembly.GetExecutingAssembly().Location);
            // TODO: Different paths for debug
            var exe = Path.Combine(file.Directory.FullName, @"..\..\..\..\SnaptrudeManagerUI\bin\Debug\net6.0-windows\SnaptrudeManagerUI.exe");
            if (File.Exists(exe))
            {
                process = new Process();
                process.StartInfo.FileName = exe;
                process.Start();
                if (process != null)
                {
                    logger.Info("UI Process started successfully!");
                }
            }
            else
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                MessageBox.Show("Please check installation of SnaptrudeManager! UI Application not found.", $"SnaptrudeManagerAddin {version}", MessageBoxButton.OK, MessageBoxImage.Information);
                logger.Error("UI Process could not start! Please start manually.");
            }



        }

        public static void CleanUp()
        {
            if (process != null && !process.HasExited)
            {
                process.Kill();
            }
        }

    }
}
