using Autodesk.Revit.DB;
using System.Collections.Generic;
using TrudeSerializer.Importer;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Components
{
    internal class TrudeMass : TrudeComponent
    {
        public string subCategory;
        List<double> center;

        public TrudeMass
            (
                string elementId,
                string family,
                string level,
                string subCategory,
                List<double> center


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
            List<double> center = InstanceUtility.GetCustomCenterPoint(element);


            TrudeMass serializedMass = new TrudeMass(elementId, family, levelName, subCategory, center);
            return serializedMass;
        }
    }
}