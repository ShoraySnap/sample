using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace TrudeImporter
{
    public class BeamProperties
    {
        [JsonProperty("height")]
        public float Height { get; set; }

        //[JsonProperty("width")]
        //public float Width { get; set; }

        [JsonProperty("lenght")]
        public float Lenght { get; set; } 

        [JsonProperty("faceVertices")]
        public List<XYZ> FaceVertices { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("centerPosition")]
        public string CenterPosition { get; set; }
    }
}