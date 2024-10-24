using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrudeCommon.Utils;
using TrudeSerializer.Importer;
using TrudeSerializer.Debug;
using Newtonsoft.Json;

namespace TrudeSerializer
{
    public static class S3UploadHelper
    {
        public static string SaveForUpload(Dictionary<string, string> jsonData)
        {
            string fileName = FileUtils.DATA_FNAME;
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(jsonData));
            FileUtils.SaveCommonTempFile(fileName, bytes);
            return fileName;
        }
    }
}
