using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace TrudeImporter
{
    public class WallProperties
    {
        [JsonProperty("profile")]
        public List<XYZ> ProfilePoints { get; set; }

        [JsonProperty("normal")]
        int Normal { get; set; }

        [JsonProperty("holes")]
        public List<List<XYZ>> Holes { get; set; }

        [JsonProperty("layers")]
        public List<LayerProperties> Layers { get; set; }

        [JsonProperty("thickness")]
        public double Thickness { get; set; }

        [JsonProperty("baseHeight")]
        public int BaseHeight { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

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
    }
}
