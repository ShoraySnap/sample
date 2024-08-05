using System;
using System.Collections.Generic;
using System.Windows.Markup;
using Autodesk.Revit.DB;
using Newtonsoft.Json;

namespace TrudeImporter
{
    internal static class TrudeExportLoggerHelper
    {
        public const string BASIC_WALL_KEY = "BasicWall";
        public const string BASIC_FLOOR_KEY = "BasicFloor";
        public const string BASIC_CEILING_KEY = "BasicCeiling";
        public const string BASIC_COLUMN_KEY = "BasicColumn";
        public const string BASIC_BEAM_KEY = "BasicBeam";
        public const string BASIC_DOOR_KEY = "BasicDoor";
        public const string BASIC_WINDOW_KEY = "BasicWindow";
        public const string BASIC_FURNITURE_KEY = "BasicFurniture";
        public const string MULLIONS_KEY = "mullions";
        public const string PANELS_KEY = "panels";
        public const string MASSES_KEY = "masses";
        public const string GENERIC_MODELS_KEY = "generic";
        public const string BASIC_ROOF_KEY = "BasicRoof";
        public const string BASIC_SLAB_KEY = "BasicSlab";

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
            // else if (element is TrudeSlab) TrudeExportLogger.Instance.CountOutputElements(BASIC_SLAB_KEY, true, "deleted");
            else if (element is RoofBase) TrudeExportLogger.Instance.CountOutputElements(BASIC_ROOF_KEY, true, "deleted");
            else TrudeExportLogger.Instance.CountOutputElements(MASSES_KEY, true, "deleted");
        }
    }
}