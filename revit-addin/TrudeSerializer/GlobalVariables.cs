using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;

namespace TrudeSerializer
{
    public static class GlobalVariables
    {
        public static Document Document;
        public static Document CurrentDocument;
        public static Application RvtApp;

        public static void CleanGlobalVariables()
        {
            Document = null;
            RvtApp = null;
        }
    }
}