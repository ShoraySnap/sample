using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Text;
using RevitImporter.Types;
using RevitImporter.Importer;
using RevitImporter.Utils;

namespace RevitImporter.Components
{
    internal class TrudeWall
    {
        public String elementId;
        public String level;
        public String type;
        public double width;
        public double height;
        public String family;
        public Boolean function;
        public Double[] orientation;
        public double[][] endpoints;
        public double[][] wallBottomProfile;
        public Boolean isCurvedWall;

        private TrudeWall(string elementId, string levelName, string wallType, double width, double height, string family, bool function, double[] orientation, double[][] endpoints, Boolean isCurvedWall)
        {
            this.elementId = elementId;
            this.level = levelName;
            this.type = wallType;
            this.width = width;
            this.height = height;
            this.family = family;
            this.function = function;
            this.orientation = orientation;
            this.endpoints = endpoints;
            this.isCurvedWall = isCurvedWall;
            this.wallBottomProfile = endpoints;
        }


        static public void SetImportData(ImportData importData, Element element)
        {
            Wall wall = element as Wall;
            TrudeWall snaptrudeWall = GetImportData(element);
            importData.AddWall(snaptrudeWall);
            TrudeWall.SetWallType(importData, wall);
        }

        static public TrudeWall GetImportData(Element element)
        {

            Wall wall = element as Wall;

            String elementId = element.Id.ToString();
            Curve baseLine = getBaseLine(element);
            String levelName = SnaptrudeLevel.GetLevelName(element);
            String wallType = element.Name;
            double width = wall.Width;
            String family = wall.WallType.FamilyName;
            Boolean function = wall.WallType.Function == WallFunction.Exterior;
            Double[] orientation = new Double[] { wall.Orientation.X, wall.Orientation.Z, wall.Orientation.Y };
            double[][] endpoints = getEndPoints(baseLine);
            Boolean isCurvedWall = baseLine is Arc;
            double height = GetWallHeight(element);


            return new TrudeWall(elementId, levelName, wallType, width, height, family, function, orientation, endpoints, isCurvedWall);

        }
        static public double GetWallHeight(Element element)
        {
            Wall wall = element as Wall;
            double height = UnitConversion.ConvertToMillimeterForRevit2021AndAbove(wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsDouble(), UnitTypeId.Feet);
            return height;
        }
        static Curve getBaseLine(Element element)
        {
            Location location = element.Location;
            LocationCurve locationCurve = location as LocationCurve;
            Curve curve = locationCurve.Curve;
            return curve;

        }

        static double[][] getEndPoints(Curve curve)
        {
            XYZ startPoint = curve.GetEndPoint(0);
            XYZ endPoint = curve.GetEndPoint(1);
            double[][] endpoints = new Double[2][];

            endpoints.SetValue(new double[] { startPoint.X * 304.8, startPoint.Y * 304.8, startPoint.Z * 304.8 }, 0);
            endpoints.SetValue(new double[] { endPoint.X * 304.8, endPoint.Y * 304.8, endPoint.Z * 304.8 }, 1);


            return endpoints;

        }

        static public void SetWallType(ImportData importData, Wall wall)
        {
            String name = wall.WallType.Name;
            if (importData.familyTypes.HasWallType(name)) return;


            TrudeWallType snaptrudeWallType = TrudeWallType.GetLayersData(wall);
            importData.familyTypes.AddWallType(name, snaptrudeWallType);
        }
    }
}
