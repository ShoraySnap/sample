using Autodesk.Revit.DB;
using Newtonsoft.Json;

namespace TrudeImporter
{
    public class SubMeshProperties
    {
        [JsonProperty("materialIndex")]
        public int MaterialIndex { get; set; }

        [JsonProperty("normal")]
        public XYZ Normal { get; set; }
    }
}
