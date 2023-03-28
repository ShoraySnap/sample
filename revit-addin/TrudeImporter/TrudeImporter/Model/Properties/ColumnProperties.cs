using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace TrudeImporter
{
    public class ColumnProperties
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("storey")]
        public int Storey { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; }

        [JsonProperty("existingElementId")]
        public int? ExistingElementId { get; set; }

        // these face vertices are the vertices of the face that was drawn first to extrude the beam from
        [JsonProperty("faceVertices")]
        public List<XYZ> FaceVertices { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("centerPosition")]
        public XYZ CenterPosition { get; set; }
    }
}