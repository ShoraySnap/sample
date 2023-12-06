using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace TrudeImporter
{
    public class TextureProperties
    {
        public string TexturePath { get; set; }
        public double UScale { get; set; }
        public double VScale { get; set; }
        public double UOffset { get; set; }
        public double VOffset { get; set; }
        public double WAngle { get; set; }

        public TextureProperties(string texturePath, double uScale = 1, double vScale = 1, double uOffset = 0, double vOffset = 0, double wAngle = 0)
        {
            TexturePath = texturePath;
            UScale = uScale;
            VScale = vScale;
            UOffset = uOffset;
            VOffset = vOffset;
            WAngle = wAngle;
        }
    }
}
