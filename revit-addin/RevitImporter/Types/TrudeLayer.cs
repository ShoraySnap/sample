using RevitImporter.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace RevitImporter.Types
{
    internal class TrudeLayer
    {
        public double width;
        public String function;
        public TrudeMaterial material;

        public TrudeLayer(double width, String function, TrudeMaterial material)
        {
            this.width = width;
            this.function = function;
            this.material = material;
        }
    }
}
  