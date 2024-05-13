using NLog;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnaptrudeManagerUI.IPC
{
    internal class IPCManager
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        private const string PLUGIN_PIPE_NAME = "SnaptrudeManagerAddin_UI_PIPE";

        private static NamedPipeClientStream? pipeClient = null;

        public static void Init()
        {
            pipeClient = new NamedPipeClientStream(".", PLUGIN_PIPE_NAME, PipeDirection.InOut);
            logger.Info("IPC Manager Initialized!");
        }
        public static void PostMessage()
        {
            if(pipeClient == null)
            {
                logger.Error("Pipe client is null here");
                return;
            }
            if(pipeClient.IsConnected)
            {
                PassMessage("Hello again!");
                return;
            }
            logger.Info("Trying to connect to pipe " + PLUGIN_PIPE_NAME);
            pipeClient?.ConnectAsync().ContinueWith(async _ =>
            {
                logger.Info("Pipe client connected to: " + PLUGIN_PIPE_NAME);
                logger.Info($"Pipe Async : {pipeClient.IsAsync}");
                logger.Info($"Pipe Transmission Mode : {pipeClient.TransmissionMode}");
                logger.Info($"Pipe Seek Access : {pipeClient.CanSeek}");
                logger.Info($"Pipe Read Access : {pipeClient.CanRead}");
                logger.Info($"Pipe Write Access : {pipeClient.CanWrite}");


                PassMessage("Hello for the first time!");
            });
        }

        private static async void PassMessage(string message)
        {
            if(pipeClient == null)
            {
                logger.Error("Pipe client is null here");
                return;
            }
            byte[] messageBuffer = Encoding.UTF8.GetBytes(message);
            await pipeClient.WriteAsync(messageBuffer, 0, messageBuffer.Length).ContinueWith( t => { 
                if(t.IsFaulted)
                {
                    logger.Error("Pipe faulted");
                }
                if(t.Exception != null)
                {
                    logger.Info(t.Exception);
                }
            });
            pipeClient.WaitForPipeDrain(); // Wait until the message is fully written

            // Read response from server
            byte[] responseBuffer = new byte[4096]; // Adjust buffer size as needed
            int bytesRead;
            bytesRead = await pipeClient.ReadAsync(responseBuffer, 0, responseBuffer.Length);
            pipeClient.WaitForPipeDrain(); // Wait until the message is fully written


            // Convert the received bytes to a string
            string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);
            logger.Info("Received from server: " + response);
        }

        public void Close()
        {
            pipeClient?.Close();
            logger.Info("IPC Manager Closed!");
        }

    }
}
