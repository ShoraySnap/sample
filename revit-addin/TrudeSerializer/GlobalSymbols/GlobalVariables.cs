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
        public static bool isDirectImport;
        public static View customActiveView;

        public static void CleanGlobalVariables()
        {
            Document = null;
            RvtApp = null;
            CurrentDocument = null;
            isDirectImport = false;
            customActiveView = null;
        }
    }

    public static class GlobalConstants
    {
        public static double INCH_TO_MM = 25.4;
        public static double FEET_TO_MM = 304.8;
        public static double SQF_TO_SNAP_AREA = 1.44;
        public static double INCH_PER_SNAP = 10;
        public static double FEET_TO_SNAP = 1.2;
        public static double METER_TO_SNAP = 39.37 / 10;
        public static double CM_TO_SNAP = 25.4;
        public static double MM_TO_SNAP = 254;
    }
}