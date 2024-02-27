using System;
using System.Collections.Generic;
using System.Text;

namespace TrudeSerializer.Uploader
{
    internal class PreSignedURLResponse
    {
        public string url { get; set; }
        public Dictionary<string, string> fields { get; set; }
        
    }

}
