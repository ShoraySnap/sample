using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using TrudeCommon.DataTransfer;
using TrudeCommon.Events;
using TrudeCommon.Logging;

namespace SnaptrudeManagerUI
{
    public class Program
    {
        public static DataTransferManager TransferManager;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [STAThread]
        public static void Main(string[] args)
        {
            IntPtr hWnd = FindWindow(null, "SnaptrudeManagerUI");
            if (hWnd == IntPtr.Zero)
            {
                var application = new App();
                application.InitializeComponent();
                application.Run();
            }
            else
            {
                LogsConfig.Initialize("DummyManagerUI");
                TransferManager = new DataTransferManager();
                string arguments = string.Join(" ", args);
                TrudeEventEmitter.EmitEventWithStringData(TRUDE_EVENT.BROWSER_LOGIN_CREDENTIALS, arguments, TransferManager);
                Thread.Sleep(10000);
                LogsConfig.Shutdown();
            }
        }
    }
}
