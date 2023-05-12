using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace TrudeImporter
{
    public class WallProperties
    {
        [JsonProperty("storey")]
        public int? Storey { get; set; }

        [JsonProperty("uniqueId")]
        public int? UniqueId { get; set; }

        [JsonProperty("profile")]
        public List<XYZ> ProfilePoints { get; set; }

        [JsonProperty("normal")]
        XYZ Normal { get; set; }

        [JsonProperty("holes")]
        public List<List<XYZ>> Holes { get; set; }

        [JsonProperty("layers")]
        public List<LayerProperties> Layers { get; set; }

        [JsonProperty("thicknessInMm")]
        public double? ThicknessInMm { get; set; }

        [JsonProperty("baseHeight")]
        public double BaseHeight { get; set; }

        [JsonProperty("height")]
        public double Height { get; set; }

        [JsonProperty("materialName")]
        public string MaterialName { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        // Below is revitMetaData stuff

        [JsonProperty("isStackedWall")]
        public bool IsStackedWall { get; set; }

        [JsonProperty("isStackedWallParent")]
        public bool IsStackedWallParent { get; set; }

        [JsonProperty("existingElementId")]
        public string ExistingElementId { get; set; }

        [JsonProperty("sourceElementId")]
        public string SourceElementId { get; set; }

        [JsonProperty("subMeshes")]
        public List<SubMeshProperties> SubMeshes { get; set; }

        [JsonProperty("revitFamily")]
        public string RevitFamily { get; set; }

        [JsonProperty("childrenUniqueIds")]
        public List<int> ChildrenUniqueIds { get; set; }
    }
}
