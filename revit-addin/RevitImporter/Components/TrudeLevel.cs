using Autodesk.Revit.DB;
using RevitImporter.Importer;
using RevitImporter.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace RevitImporter.Components
{
    internal class SnaptrudeLevel
    {
        public String elementId;
        public String name;
        public double elevation;

        private SnaptrudeLevel(string elementId, string name, double elevation) {
            this.elementId = elementId;
            this.name = name;
            this.elevation = elevation;
        }
        public static String GetLevelName(Element element)
        {
            Element level = GlobalVariables.Document.GetElement(element.LevelId);
            return level.Name;
        }
        public static void SetImportData(ImportData importData, Element element)
        {
            importData.AddLevel(GetImportData(element));
        }
        public static SnaptrudeLevel GetImportData(Element element)
        {
            String elementId = element.Id.ToString();
            String name = element.Name;
            double elevation = UnitConversion.ConvertToMillimeterForRevit2021AndAbove((element as Level).Elevation, UnitTypeId.Feet);
            return new SnaptrudeLevel(elementId, name, elevation);
        }
    }
}
