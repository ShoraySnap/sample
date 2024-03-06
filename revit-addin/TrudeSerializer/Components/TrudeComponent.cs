using Autodesk.Revit.DB;
using System.Collections.Generic;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Components
{
    internal class TrudeComponent
    {
        public string elementId;
        public string category;
        public string family;
        public string level;
        public bool isParametric;
        public bool isInstance;
        public Dictionary<string, TrudeGeometry> geometries;

        public TrudeComponent(string elementId, string category, string family, string level)
        {
            this.elementId = elementId;
            this.category = category;
            this.family = family;
            this.level = level;
            this.isParametric = false;
            this.isInstance = false;
            this.geometries = new Dictionary<string, TrudeGeometry>();
        }

        public static TrudeComponent GetDefaultComponent()
        {
            return new TrudeComponent("-1", "", "", "");
        }

        public bool IsParametric()
        {
            return this.isParametric;
        }

        public void SetVertices(string materialId, double x, double y, double z)
        {
            if (!geometries.ContainsKey(materialId))
            {
                geometries.Add(materialId, new TrudeGeometry());
            }
            double updatedX = UnitConversion.ConvertToSnaptrudeUnitsFromFeet(x);
            double updatedY = UnitConversion.ConvertToSnaptrudeUnitsFromFeet(y);
            double updatedZ = UnitConversion.ConvertToSnaptrudeUnitsFromFeet(z);
            geometries[materialId].AddVertices(updatedX, updatedY, updatedZ);
        }

        public void SetFaces(string materialId, long a, long b, long c)
        {
            if (!geometries.ContainsKey(materialId))
            {
                geometries.Add(materialId, new TrudeGeometry());
            }
            geometries[materialId].AddFaces(a, b, c);
        }

        public void SetUVs(string materialId, double u, double v)
        {
            if (!geometries.ContainsKey(materialId))
            {
                geometries.Add(materialId, new TrudeGeometry());
            }
            geometries[materialId].AddUVs(u, v);
        }

        public void SetMaterial(string materialId, TrudeMaterial material)
        {
            if (!geometries.ContainsKey(materialId))
            {
                geometries.Add(materialId, new TrudeGeometry());
            }
            geometries[materialId].SetMaterial(material);
        }

        static public TrudeComponent CurrentFamily { get; set; }
        static public void ClearCurrentFamily()
        {
            CurrentFamily = null;
        }

        static public double GetHeightFromBoundingBox(Element element)
        {
            BoundingBoxXYZ boundingBox = element.get_BoundingBox(null);
            double height = boundingBox.Max.Z - boundingBox.Min.Z;
            height = UnitConversion.ConvertToSnaptrudeUnitsFromFeet(height);
            return height;
        }
    }
}