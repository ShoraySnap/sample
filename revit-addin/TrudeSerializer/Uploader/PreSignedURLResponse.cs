using System.Collections.Generic;

namespace TrudeSerializer.Uploader
{
    internal class PreSignedURLResponse
    {
        public string url { get; set; }
        public Dictionary<string, string> fields { get; set; }
    }
}