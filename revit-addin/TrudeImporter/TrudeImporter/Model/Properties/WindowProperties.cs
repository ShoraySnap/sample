using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace TrudeImporter
{
    public class WindowProperties
    {
        [JsonProperty("height")]
        public float Height { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("storey")]
        public int Storey { get; set; }

        [JsonProperty("width")]
        public float Width { get; set; }

        [JsonProperty("direction")]
        public XYZ Direction { get; set; }

        [JsonProperty("existingElementId")]
        public int? ExistingElementId { get; set; }

        [JsonProperty("centerPosition")]
        public XYZ CenterPosition { get; set; }

        [JsonProperty("revitFamilyName")]
        public string RevitFamilyName { get; set; }

        [JsonProperty("uniqueId")]
        public int UniqueId { get; set; }
    }
}