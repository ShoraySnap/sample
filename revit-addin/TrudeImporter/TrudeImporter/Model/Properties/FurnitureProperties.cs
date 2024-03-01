using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace TrudeImporter
{
    public class FurnitureProperties
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("storey")]
        public int Storey { get; set; }

        [JsonProperty("uniqueId")]
        public int UniqueId { get; set; }

        // Below is revitMetaData stuff

        [JsonProperty("boundingBoxCenter")]
        public XYZ BoundingBoxCenter { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("existingElementId")]
        public int? ExistingElementId { get; set; }

        [JsonProperty("elements")]
        public List<FurnitureInstanceProperties> Elements { get; set; }

        [JsonProperty("facingFlipped")]
        public bool? FacingFlipped { get; set; }

        [JsonProperty("family")]
        public string RevitFamilyName { get; set; }

        [JsonProperty("familyRotation")]
        public double? FamilyRotation { get; set; }

        [JsonProperty("centerPosition")]
        public XYZ CenterPosition { get; set; }

        [JsonProperty("offset")]
        public XYZ Offset { get; set; }

        [JsonProperty("scaling")]
        public XYZ Scaling { get; set; }

        [JsonProperty("revitOffset")]
        public double? RevitOffset { get; set; }

        [JsonProperty("worldBoundingBoxMin")]
        public XYZ WorldBoundingBoxMin { get; set; }

        [JsonProperty("rotation")]
        public XYZ Rotation { get; set; }

        [JsonProperty("type")]
        public string RevitFamilyType { get; set; }

        [JsonProperty("sourceElementId")]
        public int? SourceElementId { get; set; }

    }

    [Serializable]
    public class FurnitureInstanceProperties
    {
        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("family")]
        public string RevitFamilyName { get; set; }

        [JsonProperty("type")]
        public string RevitFamilyType { get; set; }

    }
}