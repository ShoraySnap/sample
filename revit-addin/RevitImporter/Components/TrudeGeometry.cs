using System.Collections.Generic;

namespace TrudeSerializer.Components
{
    class TrudeGeometry
    {
        public List<double> Vertices;
        public List<long> Faces;
        public List<double> UVs;
        public TrudeMaterial material;

        public TrudeGeometry()
        {
            Vertices = new List<double>();
            Faces = new List<long>();
            UVs = new List<double>();
        }

        public void AddVertices(double x, double y, double z)
        {
            Vertices.Add(x);
            Vertices.Add(y);
            Vertices.Add(z);
        }

        public void AddFaces(long a, long b, long c)
        {
            Faces.Add(a);
            Faces.Add(b);
            Faces.Add(c);
        }

        public void AddUVs(double u, double v)
        {
            UVs.Add(u);
            UVs.Add(v);
        }

        public void SetMaterial(TrudeMaterial material)
        {
            this.material = material;
        }
    }
}