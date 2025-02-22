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

        [JsonProperty("roomType")]
        public string RoomType { get; set; }

        [JsonProperty("allFaceVertices")]
        public List<List<XYZ>> AllFaceVertices { get; set; }

        [JsonProperty("bottomFaceVertices")]
        public List<XYZ> BottomFaceVertices { get; set; }

        [JsonProperty("faceMaterialIds")]
        public List<int> FaceMaterialIds { get; set; }

        [JsonProperty("materialName")]
        public string MaterialName { get; set; }

        [JsonProperty("materialHex", NullValueHandling = NullValueHandling.Ignore)]
        public string MaterialHex { get; set; }
    }
}