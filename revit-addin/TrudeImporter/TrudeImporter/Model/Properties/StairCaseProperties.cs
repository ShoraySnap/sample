using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace TrudeImporter
{
    public class StairCaseProperties
    {
        [JsonProperty("storey")]
        public int Storey { get; set; }

        [JsonProperty("uniqueId")]
        public int UniqueId { get; set; }

        [JsonProperty("height")]
        public double Height { get; set; }

        [JsonProperty("width")]
        public double Width { get; set; }

        [JsonProperty("tread")]
        public double Tread { get; set; }

        [JsonProperty("riser")]
        public double Riser { get; set; }

        [JsonProperty("landingWidth")]
        public double LandingWidth { get; set; }

        [JsonProperty("stairThickness")]
        public double StairThickness { get; set; }

        [JsonProperty("baseOffset")]
        public double BaseOffset { get; set; }

        [JsonProperty("steps")]
        public int Steps { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("centerPosition")]
        public XYZ CenterPosition { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("staircaseType")]
        public string StaircaseType { get; set; }

        [JsonProperty("staircasePreset")]
        public string StaircasePreset { get; set; }

        [JsonProperty("layers")]
        public List<LayerProperties> Layers { get; set; }

        // Below is revitMetaData stuff

        [JsonProperty("isStackedWall")]
        public bool IsStackedWall { get; set; }

        [JsonProperty("isStackedWallParent")]
        public bool IsStackedWallParent { get; set; }

        [JsonProperty("existingElementId")]
        public int? ExistingElementId { get; set; }

        [JsonProperty("sourceElementId")]
        public string SourceElementId { get; set; }

        [JsonProperty("subMeshes")]
        public List<SubMeshProperties> SubMeshes { get; set; }

        [JsonProperty("revitFamily")]
        public string RevitFamily { get; set; }

        [JsonProperty("childrenUniqueIds")]
        public List<int> ChildrenUniqueIds { get; set; }

        [JsonProperty("allFaceVertices")]
        public List<List<XYZ>> AllFaceVertices { get; set; }

        [JsonProperty("faceMaterialIds")]
        public List<int> FaceMaterialIds { get; set; }

    }
}
