using System.Collections.Generic;

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
            geometries[materialId].AddVertices(x, y, z);
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
    }
}