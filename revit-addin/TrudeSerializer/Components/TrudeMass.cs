using Autodesk.Revit.DB;
using System.Collections.Generic;
using TrudeSerializer.Importer;

namespace TrudeSerializer.Components
{
    internal class TrudeMass : TrudeComponent
    {
        public string subCategory;
        public string subType;
        List<double> center;

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

        static public TrudeMass GetSerializedComponent(SerializedTrudeData importData, Element element)
        {
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