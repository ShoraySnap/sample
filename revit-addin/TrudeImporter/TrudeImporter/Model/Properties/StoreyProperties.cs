using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace TrudeImporter
{
    public class StoreyProperties
    {
        [JsonProperty("levelNumber")]
        public int LevelNumber { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("elevation")]
        public double Elevation { get; set; }

        [JsonProperty("lowerLevelElementId")]
        public int? LowerLevelElementId { get; set; }
    }
}
