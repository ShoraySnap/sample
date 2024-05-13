using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NLog;

namespace SnaptrudeManagerAddin.IPC
{
    public class IPCManager 
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        public static Process process = null;
        public static NamedPipeServerStream pipeServer = null;

        private const string PIPE_NAME = "SnaptrudeManagerAddin_UI_PIPE";
        public static Process CheckProcessRunning()
        {
            string name = "SnaptrudeManagerUI";
            var processes = Process.GetProcessesByName(name);
            if(processes.Length == 1)
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
            var exe = Path.Combine(file.Directory.FullName, @"..\..\..\..\SnaptrudeManagerUI\bin\Debug\net6.0-windows\SnaptrudeManagerUI.exe");
            if (File.Exists(exe))
            {
                process = new Process();
                process.StartInfo.FileName = exe;
                process.StartInfo.Arguments = PIPE_NAME;
                process.Start();
                if(process != null)
                {
                    logger.Info("UI Process started successfully!");
                }
            }
            else
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                MessageBox.Show("Please use SnaptrudeManagerUI.exe Standalone App.", $"SnaptrudeManagerAddin {version}", MessageBoxButton.OK, MessageBoxImage.Information);
                logger.Error("UI Process could not start! Please start manually.");
            }



        }

        public static bool serverRunning = false;
        public static int totalReceives = 0;

        public static async Task RunServerLoop()
        {
            serverRunning = true;
            totalReceives = 0;
            while (serverRunning)
            {
                totalReceives += 1;
                if(pipeServer.IsConnected)
                {
                    logger.Info("Server already connected!");
                    byte[] buffer = new byte[4096]; // Adjust buffer size as needed
                    int bytesRead;
                    bytesRead = await pipeServer.ReadAsync(buffer, 0, buffer.Length);

                    // Convert the received bytes to a string
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    logger.Info("Received from client: " + message);


                    // Send response back to client
                    string response = "Message received.";
                    byte[] responseBuffer = Encoding.UTF8.GetBytes(response);
                    await pipeServer.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                    pipeServer.WaitForPipeDrain(); // Wait until the response is fully written
                    logger.Info("Response sent to client.");

                    continue;
                }

                logger.Info("Pipe server waiting for connection...");
                await pipeServer.WaitForConnectionAsync().ContinueWith(async _ => {
                    logger.Info("Server received connection!");
                    // Read all bytes sent from the client asynchronously
                    byte[] buffer = new byte[4096]; // Adjust buffer size as needed
                    int bytesRead;
                    bytesRead = await pipeServer.ReadAsync(buffer, 0, buffer.Length);

                    // Convert the received bytes to a string
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    logger.Info("Received from client: " + message);


                    // Send response back to client
                    string response = "Message received.";
                    byte[] responseBuffer = Encoding.UTF8.GetBytes(response);
                    await pipeServer.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                    pipeServer.WaitForPipeDrain(); // Wait until the response is fully written
                    logger.Info("Response sent to client.");
                });

            }
        }

        private static void ProcessExited(object sender, EventArgs e)
        {
            CleanUp();
        }

        public static void CleanUp()
        {
            if(process != null)
            {
                process.Kill();
            }
            if (pipeServer == null) return;

            pipeServer.Close();
            logger.Info("Pipe server closed.");
        }

        public static void CreatePipeServer()
        {
            if (pipeServer != null)
            {
                logger.Info("Pipe server already created.");
                return;
            }

            logger.Info("Creating piper server...");
            pipeServer = new NamedPipeServerStream(PIPE_NAME, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            if(pipeServer != null)
            {
                logger.Info("Pipe server created successfully!");
            }
            else
            {
                logger.Error("Pipe server couldn't be created.");
            }

        }
    }
}
