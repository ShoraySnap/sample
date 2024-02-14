using Autodesk.Revit.DB;
using System;
using TrudeSerializer.Importer;

namespace TrudeSerializer.Components
{
    internal class TrudeMass : TrudeComponent
    {
        public string subCategory;
        public double[] center;

        private TrudeMass
            (
                string elementId,
                string family,
                string level,
                string subCategory,
                double[] center
            ) : base(elementId, "Mass", family, level)
        {
            this.subCategory = subCategory;
            this.center = center;
        }

        static public TrudeMass GetSerializedComponent(SerializedTrudeData importData, Element element)
        {
            string elementId = element.Id.ToString();
            string family = InstanceUtility.GetFamily(element);
            string levelName = TrudeLevel.GetLevelName(element);

            string subCategory = element.GetType().Name;
            double[] center = new Double[] { 0, 0, 0 };

            TrudeMass serializedMass = new TrudeMass(elementId, family, levelName, subCategory, center);
            return serializedMass;
        }
    }
}