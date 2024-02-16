using System.Collections.Generic;

namespace TrudeSerializer.Components
{
    class TrudeGeometry
    {
        public List<double> vertices;
        public List<long> indices;
        public List<double> uvs;
        public TrudeMaterial material;

        public TrudeGeometry()
        {
            vertices = new List<double>();
            indices = new List<long>();
            uvs = new List<double>();
        }

        public void AddVertices(double x, double y, double z)
        {
            vertices.Add(x);
            vertices.Add(y);
            vertices.Add(z);
        }

        public void AddFaces(long a, long b, long c)
        {
            indices.Add(a);
            indices.Add(b);
            indices.Add(c);
        }

        public void AddUVs(double u, double v)
        {
            uvs.Add(u);
            uvs.Add(v);
        }

        public void SetMaterial(TrudeMaterial material)
        {
            this.material = material;
        }
    }
}