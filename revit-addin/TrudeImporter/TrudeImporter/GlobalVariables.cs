using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Document = Autodesk.Revit.DB.Document;

namespace TrudeImporter
{
    public static class GlobalVariables
    {
        public static Transaction Transaction;
        public static StairsEditScope StairsEditScope;
        public static Document Document;
        public static Autodesk.Revit.ApplicationServices.Application RvtApp;
        
        public static bool ForForge = false;
        public static string TrudeFileName = "";

        public static IDictionary<int, ElementId> LevelIdByNumber = new Dictionary<int, ElementId>();
        public static IDictionary<int, ElementId> childUniqueIdToWallElementId = new Dictionary<int, ElementId>();
        public static IDictionary<int, ElementId> UniqueIdToElementId = new Dictionary<int, ElementId>();
        public static IDictionary<string, (bool IsChecked, int NumberOfElements, string path)> MissingDoorFamiliesCount = new Dictionary<string, (bool, int, string)>();
        public static IDictionary<string, (bool IsChecked, int NumberOfElements, string path)> MissingWindowFamiliesCount = new Dictionary<string, (bool, int, string)>();
        public static IDictionary<string, (bool IsChecked, int NumberOfElements, string path)> MissingFurnitureFamiliesCount = new Dictionary<string, (bool, int, string)>();
        public static IDictionary<ElementId, List<TrudeRoom>> CreatedFloorsByLevel = new Dictionary<ElementId, List<TrudeRoom>>();

        public static List<ElementId> WallElementIdsToRecreate = new List<ElementId>();

        public static JArray materials;
        public static JArray multiMaterials;

        public static Dictionary<String, Element> idToElement = new Dictionary<String, Element>();
        public static Dictionary<String, FamilySymbol> idToFamilySymbol = new Dictionary<String, FamilySymbol>();

        public static List<int> MissingDoorIndexes = new List<int>();
        public static List<int> MissingWindowIndexes = new List<int>();
        public static List<int> MissingFurnitureIndexes = new List<int>();


        public static void cleanGlobalVariables()
        {
            Transaction = null;
            Document = null;
            RvtApp = null;
            LevelIdByNumber = new Dictionary<int, ElementId>();
            childUniqueIdToWallElementId = new Dictionary<int, ElementId>();
            UniqueIdToElementId = new Dictionary<int, ElementId>();
            WallElementIdsToRecreate = new List<ElementId>();
            CreatedFloorsByLevel = new Dictionary<ElementId, List<TrudeRoom>>();

            materials = null;
            multiMaterials = null;

            idToElement = new Dictionary<String, Element>();
            idToFamilySymbol = new Dictionary<String, FamilySymbol>();

            MissingDoorFamiliesCount = new Dictionary<string, (bool, int, string)>();
            MissingWindowFamiliesCount = new Dictionary<string, (bool, int, string)>();
            MissingFurnitureFamiliesCount = new Dictionary<string, (bool, int, string)>();
            MissingDoorIndexes = new List<int>();
            MissingWindowIndexes = new List<int>();
            MissingFurnitureIndexes = new List<int>();
        }

        public static string sanitizeString(string str)
        {
            string invalidChars = "{}[]|;<>?`~-";
            foreach (var c in invalidChars)
            {
                str = str.Replace(c.ToString(), string.Empty);
            }
            str = str.ToLower();
            return str;
        }

    }
}