using Autodesk.Revit.DB;
using Newtonsoft.Json;

namespace TrudeImporter
{
    public class StaircaseBlockProperties
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("translation")]
        public double[] Translation { get; set; }

        [JsonProperty("rotation")]
        public double[] Rotation { get; set; }

        [JsonProperty("extrudeHeight")]
        public double ExtrudeHeight { get; set; }

        [JsonProperty("startPoint")]
        public XYZ StartPoint { get; set; }

        [JsonProperty("riser")]
        public double Riser { get; set; }

        [JsonProperty("steps")]
        public int Steps { get; set; }

        [JsonProperty("tread")]
        public double Tread { get; set; }

        [JsonProperty("depth")]
        public double Depth { get; set; }

        [JsonProperty("landingWidth")]
        public double LandingWidth { get; set; }

        [JsonProperty("maxDepthForBaseCutoff")]

        public double MaxDepthForBaseCutoff { get; set; }

        [JsonProperty("startLandingWidth")]
        public double StartLandingWidth { get; set; }

        [JsonProperty("endLandingWidth")]
        public double EndLandingWidth { get; set; }

        [JsonProperty("cutoffProtrusions")]
        public bool CutoffProtrusions { get; set; }
    }
}