using Autodesk.Revit.DB;
using System;
using TrudeSerializer.Importer;

namespace TrudeSerializer.Components
{
    internal class TrudeRevitLink : TrudeComponent
    {
        public string subCategory;
        public double[] center;

        private TrudeRevitLink
            (
                string elementId,
                string family,
                string level,
                string subCategory,
                double[] center
            ) : base(elementId, "RevitLink", family, level)
        {
            this.subCategory = subCategory;
            this.center = center;
        }

        static public TrudeRevitLink GetSerializedComponent(SerializedTrudeData importData, Element element)
        {
            string elementId = element.Id.ToString();
            string family = InstanceUtility.GetFamily(element);
            string levelName = TrudeLevel.GetLevelName(element);

            string subCategory = element.GetType().Name;
            double[] center = new Double[] { 0, 0, 0 };

            TrudeRevitLink serializedMass = new TrudeRevitLink(elementId, family, levelName, subCategory, center);
            return serializedMass;
        }
    }
}