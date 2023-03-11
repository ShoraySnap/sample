using Newtonsoft.Json;
using System.Collections.Generic;

#pragma warning disable IDE1006 // Naming Styles
namespace TrudeImporter
{
    public class TrudeProperties
    {
        [JsonProperty("storeys")]
        public List<StoreyProperties> Storeys { get; set; }

        [JsonProperty("walls")]
        public List<WallProperties> walls { get; set; }
    }
}
#pragma warning restore IDE1006 // Naming Styles
