using System;
using Autodesk.Revit.DB;

namespace TrudeImporter
{
    static class ComponentIdentifier
    {
        public static bool IsValidWall(Element element)
        {
            if (!(element is Wall wall)) return false;
            if (wall.CurtainGrid != null) return false;
            if (wall.IsStackedWall) return false;
            return true;
        }

        public static bool IsColumnCategory(Element element)
        {
            string[] categoryList = new string[]
            {
                "Structural Columns",
                "Columns"
            };
            var category = element?.Category?.Name;
            return Array.Exists(categoryList, element.Category.Name.Contains);
        }

        public static bool IsBeamCategory(Element element)
        {
            string[] categoryList = new string[]
            {
                "Structural Beams",
                "Beams"
            };
            var category = element?.Category?.Name;
            return Array.Exists(categoryList, element.Category.Name.Contains);
        }

        public static bool IsDoor(Element element)
        {
            string category = element?.Category?.Name;
            if (category == null)
            {
                return false;
            }
            return element is FamilyInstance && category.Contains("Doors");
        }

        public static bool IsWindow(Element element)
        {
            string category = element?.Category?.Name;
            if (category == null)
            {
                return false;
            }
            return element is FamilyInstance && category.Contains("Windows");
        }

        public static bool IsFurnitureCategory(Element element)
        {
            string[] funitureSubCategories = new string[] {
                "Lighting Fixtures",
                "Communication Devices",
                "Casework",
                "Food Service Equipment",
                "Furniture Systems",
                "Planting",
                "Security Devices",
                "Electrical Fixtures",
                "Furniture",
                "Fire Alarm Devices",
                "Data Devices",
                "Lighting Devices",
                "Specialty Equipment",
                "Assemblies",
                "Model Groups",
                "Mechanical Equipment",
                "Plumbing Fixtures",
                "Electrical Equipment",
                "Sprinklers",
                "Telephone Devices",
            };
            string category = element?.Category?.Name;
            if (category == null)
            {
                return false;
            }

            return Array.Exists(funitureSubCategories, element.Category.Name.Contains);
        }

        public static bool IsValidFurnitureCategoryForCount(Element element)
        {
            // Skiping count of group and assemblies
            bool isFurnitureCategory = IsFurnitureCategory(element);
            if (!isFurnitureCategory) return false;
            if (element is Group || element is AssemblyInstance) return false;
            return isFurnitureCategory;
        }
    }
}
