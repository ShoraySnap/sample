using System;
using System.Collections.Generic;
using System.Windows.Markup;
using Autodesk.Revit.DB;
using Newtonsoft.Json;

namespace TrudeImporter
{
    internal static class TrudeExportLoggerHelper
    {
        public const string BASIC_WALL_KEY = "basic wall";
        public const string BASIC_FLOOR_KEY = "basic floor";
        public const string BASIC_CEILING_KEY = "basic ceiling";
        public const string BASIC_COLUMN_KEY = "basic column";
        public const string BASIC_BEAM_KEY = "basic beam";
        public const string BASIC_DOOR_KEY = "basic door";
        public const string BASIC_WINDOW_KEY = "basic window";
        public const string BASIC_FURNITURE_KEY = "basic furniture";
        public const string BASIC_ROOF_KEY = "basic roof";
        public const string MASSES_KEY = "masses";
        public const string GENERIC_MODELS_KEY = "generic";

        public const string BASIC_SLAB_KEY = "basic slab";
        public const string BASIC_STAIRCASE_KEY = "basic staircase";

        public static void DeleteCountLogger(Element element)
        {
            if (ComponentIdentifier.IsValidWall(element)) TrudeExportLogger.Instance.CountOutputElements(BASIC_WALL_KEY, true, "deleted");
            else if (element is Floor) TrudeExportLogger.Instance.CountOutputElements(BASIC_FLOOR_KEY, true, "deleted");
            else if (element is Ceiling) TrudeExportLogger.Instance.CountOutputElements(BASIC_CEILING_KEY, true, "deleted");
            else if (ComponentIdentifier.IsColumnCategory(element)) TrudeExportLogger.Instance.CountOutputElements(BASIC_COLUMN_KEY, true, "deleted");
            else if (ComponentIdentifier.IsBeamCategory(element)) TrudeExportLogger.Instance.CountOutputElements(BASIC_BEAM_KEY, true, "deleted");
            else if (ComponentIdentifier.IsDoor(element)) TrudeExportLogger.Instance.CountOutputElements(BASIC_DOOR_KEY, true, "deleted");
            else if (ComponentIdentifier.IsWindow(element)) TrudeExportLogger.Instance.CountOutputElements(BASIC_WINDOW_KEY, true, "deleted");
            else if (ComponentIdentifier.IsValidFurnitureCategoryForCount(element)) TrudeExportLogger.Instance.CountOutputElements(BASIC_FURNITURE_KEY, true, "deleted");
            else if (element is RoofBase) TrudeExportLogger.Instance.CountOutputElements(BASIC_ROOF_KEY, true, "deleted");
            // else if (element is SLAB) TrudeExportLogger.Instance.CountOutputElements(BASIC_SLAB_KEY, true, "deleted");
            // else if (element is STAIRCASE) TrudeExportLogger.Instance.CountOutputElements(BASIC_STAIRCASE_KEY, true, "deleted");
            else TrudeExportLogger.Instance.CountOutputElements(MASSES_KEY, true, "deleted");
        }
    }
}