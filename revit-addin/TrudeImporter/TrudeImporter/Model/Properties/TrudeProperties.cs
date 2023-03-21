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

        [JsonProperty("deletedElements")]
        public List<int> DeletedElements { get; set; }
    }
}
