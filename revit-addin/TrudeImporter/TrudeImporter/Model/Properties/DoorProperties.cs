using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace TrudeImporter
{
    public class DoorProperties
    {
        [JsonProperty("height")]
        public float Height { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("storey")]
        public int Storey { get; set; }

        [JsonProperty("uniqueId")]
        public int UniqueId { get; set; }

        [JsonProperty("width")]
        public float Width { get; set; }

        [JsonProperty("direction")]
        public XYZ Direction { get; set; }

        [JsonProperty("handFlipped")]
        public bool HandFlipped { get; set; }

        [JsonProperty("existingElementId")]
        public int? ExistingElementId { get; set; }

        [JsonProperty("centerPosition")]
        public XYZ CenterPosition { get; set; }

        [JsonProperty("revitFamilyName")]
        public string RevitFamilyName { get; set; }
    }
}