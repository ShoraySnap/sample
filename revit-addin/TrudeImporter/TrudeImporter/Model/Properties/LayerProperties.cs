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

        [JsonProperty("thickness")]
        public double Thickness { get; set; }

        [JsonProperty("core")]
        public bool IsCore { get; set; }

        public TrudeLayer ToTrudeLayer(string type = "DefaultType")
        {
            return new TrudeLayer(type, Name, Thickness, IsCore);
        }
    }
}
