using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace TrudeImporter
{
    public class LayerProperties
    {

        [JsonProperty("value")]
        public string Name { get; set; }

        [JsonProperty("thicknessInMm")]
        public double ThicknessInMm { get; set; }

        [JsonProperty("isCore")]
        public bool IsCore { get; set; }

        public TrudeLayer ToTrudeLayer(string type = "DefaultType")
        {
            return new TrudeLayer(type, Name, ThicknessInMm, IsCore);
        }
    }
}
