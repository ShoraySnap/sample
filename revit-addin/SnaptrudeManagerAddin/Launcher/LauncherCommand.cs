using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using NLog;


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
            Application.UIControlledApplication.ViewActivated += Application.Instance.OnViewActivated;
            LaunchProcess.StartProcess(new string[] 
            { 
                (currentView is View3D).ToString(),
                (!commandData.Application.ActiveUIDocument.Document.IsFamilyDocument).ToString()
            });
            LaunchProcess.process.WaitForInputIdle();
            Application.UpdateButtonState(currentView is View3D);
            Application.UpdateNameAndFiletype(commandData.Application.ActiveUIDocument.Document.Title, commandData.Application.ActiveUIDocument.Document.IsFamilyDocument ? "rfa" : "rvt");
            return Result.Succeeded;
        }

    }
}
