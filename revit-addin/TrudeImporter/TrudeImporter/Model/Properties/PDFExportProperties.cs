using Newtonsoft.Json;

namespace TrudeImporter
{
    public class PDFExportProperties
    {
        [JsonProperty("company")]
        public string CompanyName;

        [JsonProperty("project")]
        public string ProjectName;

        [JsonProperty("merge")]
        public bool MergePDFs;
    }
}