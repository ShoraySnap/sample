using Autodesk.Revit.DB;
using System;
using TrudeSerializer.Importer;
using TrudeSerializer.Types;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Components
{
    internal class TrudeWall : TrudeComponent
    {
        public string type;
        public double width;
        public double height;
        public bool function;
        public double[] orientation;
        public double[][] endpoints;
        public double[][] wallBottomProfile;
        public bool isCurvedWall;

        private TrudeWall(string elementId, string level, string wallType, double width, double height, string family, bool function, double[] orientation, double[][] endpoints, Boolean isCurvedWall) : base(elementId, "Walls", family, level)
        {
            this.type = wallType;
            this.width = width;
            this.height = height;
            this.function = function;
            this.orientation = orientation;
            this.endpoints = endpoints;
            this.isCurvedWall = isCurvedWall;
            this.wallBottomProfile = endpoints;
        }

        static public TrudeWall GetSerializedComponent(SerializedTrudeData importData, Element element)
        {
            Wall wall = element as Wall;

            string elementId = element.Id.ToString();
            Curve baseLine = GetBaseLine(element);
            string levelName = TrudeLevel.GetLevelName(element);
            string wallType = element.Name;
            double width = wall.Width;
            string family = wall.WallType.FamilyName;
            bool function = wall.WallType.Function == WallFunction.Exterior;
            double[] orientation = new Double[] { wall.Orientation.X, wall.Orientation.Z, wall.Orientation.Y };
            double[][] endpoints = GetEndPoints(baseLine);
            bool isCurvedWall = baseLine is Arc;
            double height = GetWallHeight(element);
            SetWallType(importData, wall);

            TrudeWall serializedWall = new TrudeWall(elementId, levelName, wallType, width, height, family, function, orientation, endpoints, isCurvedWall);
            serializedWall.SetIsParametric();
            return serializedWall;
        }
        static public double GetWallHeight(Element element)
        {
            Wall wall = element as Wall;
            double height = UnitConversion.ConvertToMillimeterForRevit2021AndAbove(wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM).AsDouble(), UnitTypeId.Feet);
            return height;
        }
        static Curve GetBaseLine(Element element)
        {
            Location location = element.Location;
            LocationCurve locationCurve = location as LocationCurve;
            Curve curve = locationCurve.Curve;
            return curve;
        }

        static double[][] GetEndPoints(Curve curve)
        {
            XYZ startPoint = curve.GetEndPoint(0);
            XYZ endPoint = curve.GetEndPoint(1);
            double[][] endpoints = new Double[2][];

            endpoints.SetValue(new double[] { startPoint.X * 304.8, startPoint.Y * 304.8, startPoint.Z * 304.8 }, 0);
            endpoints.SetValue(new double[] { endPoint.X * 304.8, endPoint.Y * 304.8, endPoint.Z * 304.8 }, 1);

            return endpoints;
        }

        private void SetIsParametric()
        {
            if (this.isCurvedWall)
            {
                this.isParametric = false;
            }
            else
            {
                this.isParametric = true;
            }
        }

        static public void SetWallType(SerializedTrudeData importData, Wall wall)
        {
            string name = wall.WallType.Name;
            if (importData.FamilyTypes.HasWallType(name)) return;

            TrudeWallType snaptrudeWallType = TrudeWallType.GetLayersData(wall);
            importData.FamilyTypes.AddWallType(name, snaptrudeWallType);
        }
    }
}