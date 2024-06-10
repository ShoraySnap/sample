using System;
using System.Collections.Generic;
using TrudeSerializer.Utils;

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
            vertices.Add(Math.Round(x, Constants.ROUND_DIGITS));
            vertices.Add(Math.Round(y, Constants.ROUND_DIGITS));
            vertices.Add(Math.Round(z, Constants.ROUND_DIGITS));
        }

        public void AddFaces(long a, long b, long c)
        {
            indices.Add(a);
            indices.Add(b);
            indices.Add(c);
        }

        public void AddUVs(double u, double v)
        {
            uvs.Add(Math.Round(u, Constants.ROUND_DIGITS));
            uvs.Add(Math.Round(v, Constants.ROUND_DIGITS));
        }

        public void SetMaterial(TrudeMaterial material)
        {
            this.material = material;
        }
    }
}