using System;
using System.Collections.Generic;
using System.Text;

namespace TrudeImporter
{
    public class StoreyProperties
    {
        public int LevelNumber { get; set; }
        public string Name { get; set; }
        public double Elevation { get; set; }
        public int RevitElementId { get; set; }
    }
}
