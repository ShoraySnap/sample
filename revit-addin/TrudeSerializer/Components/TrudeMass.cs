using Autodesk.Revit.DB;
using System;
using TrudeSerializer.Importer;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Components
{
    internal class TrudeMass : TrudeComponent
    {
        public string subCategory;
        public TransformObject transform;

        public TrudeMass
            (
                string elementId,
                string family,
                string level,
                string subCategory,
                TransformObject transform
            ) : base(elementId, "Mass", family, level)
        {
            this.subCategory = subCategory;
            this.transform = transform;
        }

        static public TrudeMass GetSerializedComponent(SerializedTrudeData importData, Element element)
        {
            string elementId = element.Id.ToString();
            string family = InstanceUtility.GetFamily(element);
            string levelName = TrudeLevel.GetLevelName(element);

            string subCategory = element.GetType().Name;
            double[] center = new Double[] { 0, 0, 0 };

            TransformObject transform = new TransformObject(center); 

            TrudeMass serializedMass = new TrudeMass(elementId, family, levelName, subCategory, transform);
            return serializedMass;
        }
    }
}