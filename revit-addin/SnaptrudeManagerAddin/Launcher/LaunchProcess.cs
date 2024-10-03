using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;
using Autodesk.Revit.DB;
using NLog;
using TrudeCommon.Events;

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
                logger.Warn("Process is already running with name: {0} and id: {1}", name, process.Id);
                return process;
            }
            else
            {
                return null;
            }
        }

        public static void StartProcess(string[] args, bool update)
        {
            if (update)
            {
                FileInfo file = new FileInfo(Assembly.GetExecutingAssembly().Location);
                var exe = file.Directory.FullName.Contains(@"revit-addin") ?
                    Path.Combine(file.Directory.FullName, @"..\..\..\..\SnaptrudeManagerUI\bin\Debug\net48\SnaptrudeManagerUI.exe") :
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"SnaptrudeManager\UI\SnaptrudeManagerUI.exe");
                if (File.Exists(exe))
                {
                    process = new Process();
                    process.StartInfo.FileName = exe;
                    process.StartInfo.Arguments = "update";
                    process.Start();
                }
            }
            if (!update)
            {
                process = CheckProcessRunning();
                if (process != null)
                {
                    logger.Warn("UI Process already running!");
                    HandshakeManager.SetHandshakeName(Process.GetCurrentProcess().Id.ToString(), process.Id.ToString());
                    Application.UpdateButtonState(bool.Parse(args[1]));
                    Application.UpdateNameAndFiletype(args[3], bool.Parse(args[2]) ? "rvt" : "rfa");
                    TrudeEventEmitter.EmitEventWithStringData(TRUDE_EVENT.REVIT_PLUGIN_UPDATE_REVIT_PROCESS_ID, Process.GetCurrentProcess().Id.ToString(), Application.TransferManager);
                    return;
                }

                logger.Info("Trying to start UI process...");
                FileInfo file = new FileInfo(Assembly.GetExecutingAssembly().Location);
                var exe = file.Directory.FullName.Contains(@"revit-addin") ?
                    Path.Combine(file.Directory.FullName, @"..\..\..\..\SnaptrudeManagerUI\bin\Debug\net48\SnaptrudeManagerUI.exe") :
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"SnaptrudeManager\UI\SnaptrudeManagerUI.exe");
                if (File.Exists(exe))
                {
                    process = new Process();
                    process.StartInfo.FileName = exe;
                    string quotedArguments = string.Join(" ", args.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg));
                    process.StartInfo.Arguments = quotedArguments;
                    process.Start();
                    if (process != null)
                    {
                        logger.Info("UI Process started successfully!");
                        HandshakeManager.SetHandshakeName(Process.GetCurrentProcess().Id.ToString(), process.Id.ToString());
                        TrudeEventEmitter.EmitEventWithStringData(TRUDE_EVENT.REVIT_PLUGIN_UPDATE_REVIT_PROCESS_ID, Process.GetCurrentProcess().Id.ToString(), Application.TransferManager);
                    }
                }
                else
                {
                    var version = Assembly.GetExecutingAssembly().GetName().Version;
                    MessageBox.Show("Please check installation of SnaptrudeManager! UI Application not found.", $"SnaptrudeManagerAddin {version}", MessageBoxButton.OK, MessageBoxImage.Information);
                    logger.Error("UI Process could not start! Please start manually.");
                }
            }
        }

        public static void Kill()
        {
            if (process != null && !process.HasExited)
            {
                process.Kill();
            }
            process = null;
        }

    }
}
