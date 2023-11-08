using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace TrudeImporter
{
    public class MassProperties
    {
        [JsonProperty("storey")]
        public int Storey { get; set; }

        [JsonProperty("uniqueId")]
        public int UniqueId { get; set; }

        [JsonProperty("existingElementId")]
        public int? ExistingElementId { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("allFaceVertices")]
        public List<List<XYZ>> AllFaceVertices { get; set; }
    }
}