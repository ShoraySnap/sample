using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Text;
using TrudeSerializer.Importer;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Components
{
    internal class TrudeCurtainWall : TrudeComponent
    {
        public string subType;
        public double[] center;

        public static bool IsCurtainWallComponent(Element element)
        {
            string[] SUPPORTED_CATEGORIES = { "Curtain Panels", "Curtain Wall Mullions" };
            string category = element?.Category?.Name;
            if (category == null)
            {
                return false;
            }

            return Array.Exists(SUPPORTED_CATEGORIES, element.Category.Name.Contains);
        }

        public TrudeCurtainWall(string elementId, string family, string level, string subType, double[] center) : base(elementId, "Curtain Wall", family, level)
        {
            this.subType = subType;
            this.center = center;
        }

        public static TrudeCurtainWall GetSerializedComponent(SerializedTrudeData serializedData, Element element)
        {
            string elementId = element.Id.ToString();
            string family = element.Name;
            string level = TrudeLevel.GetLevelName(element);
            double[] center = UnitConversion.ConvertToSnaptrudeUnitsFromFeet(GetCenterFromBoundingBox(element));

            string subType = element?.Category?.Name;

            TrudeCurtainWall trudeCurtainWall = new TrudeCurtainWall(elementId, family, level, subType, center);

            return trudeCurtainWall;
        }
    }
}