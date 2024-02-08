using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RevitImporter
{
    public static class GlobalVariables
    {
        public static Document Document;
        public static Application RvtApp;

        public static void cleanGlobalVariables()
        {
            Document = null;
            RvtApp = null;
        }
    }
}