using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace TrudeImporter
{
    public class FloorProperties
    {
        [JsonProperty("thickness")]
        public float Thickness { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; }

        [JsonProperty("storey")]
        public int Storey { get; set; }

        [JsonProperty("uniqueId")]
        public int UniqueId { get; set; }

        [JsonProperty("existingElementId")]
        public int? ExistingElementId { get; set; }

        // these face vertices are the vertices of the face that was drawn first to extrude the beam from
        [JsonProperty("faceVertices")]
        public List<XYZ> FaceVertices { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("centerPosition")]
        public XYZ CenterPosition { get; set; }

        [JsonProperty("layers")]
        public List<LayerProperties> Layers { get; set; }

        [JsonProperty("baseType")]
        public string BaseType { get; set; }

        [JsonProperty("holes")]
        public List<List<XYZ>> Holes { get; set; }

        [JsonProperty("subMeshes")]
        public List<SubMeshProperties> SubMeshes { get; set; }

        [JsonProperty("materialName")]
        public string MaterialName { get; set; }

        [JsonProperty("allFaceVertices")]
        public List<List<XYZ>> AllFaceVertices { get; set; }

        [JsonProperty("faceMaterialIds")]
        public List<int> FaceMaterialIds { get; set; }
    }
}