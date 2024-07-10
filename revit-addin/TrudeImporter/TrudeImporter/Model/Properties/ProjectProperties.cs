using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace TrudeImporter
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum UnitEnum
    {
        Imperial,
        Metric,
    } 
    public class ProjectProperties
    {
        [JsonProperty("units")]
        public UnitEnum Unit { get; set; }
    }
}