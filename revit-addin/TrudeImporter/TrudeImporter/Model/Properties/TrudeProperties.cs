using Newtonsoft.Json;
using System.Collections.Generic;
using TrudeCommon.Analytics;

namespace TrudeImporter
{
    public class TrudeProperties
    {
        [JsonProperty("isRevitImport")]
        public bool IsRevitImport { get; set; }

        [JsonProperty("storeys")]
        public List<StoreyProperties> Storeys { get; set; }

        [JsonProperty("walls")]
        public List<WallProperties> Walls { get; set; }

        [JsonProperty("beams")]
        public List<BeamProperties> Beams { get; set; }

        [JsonProperty("columns")]
        public List<ColumnProperties> Columns { get; set; }

        [JsonProperty("floors")]
        public List<FloorProperties> Floors { get; set; }

        [JsonProperty("ceilings")]
        public List<FloorProperties> Ceilings { get; set; }

        [JsonProperty("slabs")]
        public List<SlabProperties> Slabs { get; set; }

        [JsonProperty("doors")]
        public List<DoorProperties> Doors { get; set; }

        [JsonProperty("windows")]
        public List<WindowProperties> Windows { get; set; }

        [JsonProperty("masses")]
        public List<MassProperties> Masses { get; set; }

        [JsonProperty("staircases")]
        public List<StairCaseProperties> Staircases { get; set; }

        [JsonProperty("furniture")]
        public List<FurnitureProperties> Furniture { get; set; }

        [JsonProperty("deletedElements")]
        public List<int> DeletedElements { get; set; }

        [JsonProperty("snaptrudeLog")]
        public ComponentLogData SnaptrudeLog { get; set; }

        [JsonProperty("errors")]
        public List<LogError> Errors { get; set; }

        [JsonProperty("trudeGeneration")]
        public ExportStatus TrudeGeneration { get; set; }

        [JsonProperty("identifier")]
        public ExportIdentifier Identifier { get; set; }
    }
}
