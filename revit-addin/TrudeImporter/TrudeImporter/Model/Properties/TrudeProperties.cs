using Newtonsoft.Json;
using System.Collections.Generic;

namespace TrudeImporter
{
    public class TrudeProperties
    {
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
        public List<CeilingProperties> Ceilings { get; set; }

        [JsonProperty("slabs")]
        public List<SlabProperties> Slabs { get; set; }

        [JsonProperty("doors")]
        public List<DoorProperties> Doors { get; set; }

        [JsonProperty("windows")]
        public List<WindowProperties> Windows { get; set; }

        [JsonProperty("deletedElements")]
        public List<int> DeletedElements { get; set; }
    }
}
