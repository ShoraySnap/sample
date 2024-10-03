using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NLog;
using System.Diagnostics;


namespace SnaptrudeManagerAddin.Launcher
{
    [Transaction(TransactionMode.Manual)]
    public class LauncherCommand : IExternalCommand
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            View currentView = commandData.Application.ActiveUIDocument.ActiveView;
            Application.Instance.IsAnyDocumentOpened = true;
            if (!Application.Instance.IsViewActivatedSubscribed)
                Application.UIControlledApplication.ViewActivated += Application.Instance.OnViewActivated;
            Application.UIControlledApplication.Idling += Application.Instance.OnRevitIdling;
            LaunchProcess.StartProcess(new string[] 
            { 
                Process.GetCurrentProcess().Id.ToString(),
                (currentView is View3D).ToString(),
                (!commandData.Application.ActiveUIDocument.Document.IsFamilyDocument).ToString(),
                commandData.Application.ActiveUIDocument.Document.Title.Replace(".rfa","")
            }, false);
            return Result.Succeeded;
        }

    }
}
