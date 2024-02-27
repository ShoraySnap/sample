using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;

namespace TrudeSerializer
{
    public static class GlobalVariables
    {
        // Document of revit project
        public static Document Document;
        // Current document of revit project (used in TrudeCustomExporter)
        public static Document CurrentDocument;
        public static Application RvtApp;

        public static void CleanGlobalVariables()
        {
            Document = null;
            RvtApp = null;
            CurrentDocument = null;
        }
    }
}