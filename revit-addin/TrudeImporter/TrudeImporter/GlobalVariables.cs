using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TrudeImporter
{
    public static class GlobalVariables
    {
        public static Document Document;
        public static Application RvtApp;

        public static IDictionary<int, ElementId> LevelIdByNumber = new Dictionary<int, ElementId>();
        public static IDictionary<int, ElementId> childUniqueIdToWallElementId = new Dictionary<int, ElementId>();

        public static JArray materials;
        public static JArray multiMaterials;

        public static Dictionary<String, Element> idToElement = new Dictionary<String, Element>();
        public static Dictionary<String, FamilySymbol> idToFamilySymbol = new Dictionary<String, FamilySymbol>();

        public static void cleanGlobalVariables()
        {
            Document = null;
            RvtApp = null;
            LevelIdByNumber = new Dictionary<int, ElementId>();
            childUniqueIdToWallElementId = new Dictionary<int, ElementId>();

            materials = null;
            multiMaterials = null;

            idToElement = new Dictionary<String, Element>();
            idToFamilySymbol = new Dictionary<String, FamilySymbol>();
        }
    }
}