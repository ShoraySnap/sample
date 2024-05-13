using System;
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
    [Transaction(TransactionMode.Manual)]
    public class IPCCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            IPCManager.CreatePipeServer();
            IPCManager.StartProcess();

            IPCManager.RunServerLoop();
            return Result.Succeeded;
        }
    }
}
