using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using TrudeSerializer.Importer;

namespace TrudeSerializer.Components
{
    internal class TrudeMass : TrudeComponent
    {
        public string subCategory;
        public string subType;
        public List<double> center;

        readonly static string[] ignoreCategories = new string[] { "Cameras", "Levels" };
        public TrudeMass
            (
                string elementId,
                string family,
                string level,
                string subCategory,
                string subType,
                List<double> center

            ) : base(elementId, "Mass", family, level)
        {
            this.subCategory = subCategory;
            this.center = center;
            this.subType = subType;
        }

        private TrudeMass() : base("-1", "Mass", "", "")
        {
            this.elementId = "-1";
        }

        public static bool ToIgnoreCategory(Element element)
        {
            string category = element?.Category?.Name;
            if (category == null)
            {
                return false;
            }
            return Array.Exists(ignoreCategories, element.Category.Name.Contains);
        }

        static public TrudeMass GetSerializedComponent(SerializedTrudeData importData, Element element)
        {
            if (ToIgnoreCategory(element))
            {
                return new TrudeMass();
            }
            string elementId = element.Id.ToString();
            string family = InstanceUtility.GetFamily(element);
            string levelName = TrudeLevel.GetLevelName(element);

            string subCategory = element.GetType().Name;
            string subType = element?.Category?.Name;
            List<double> center = InstanceUtility.GetCustomCenterPoint(element);

            TrudeMass serializedMass = new TrudeMass(elementId, family, levelName, subCategory, subType, center);
            return serializedMass;
        }
    }
}