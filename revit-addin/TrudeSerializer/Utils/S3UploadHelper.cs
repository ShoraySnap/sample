using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrudeCommon.Utils;
using TrudeSerializer.Importer;
using SnaptrudeManagerAddin;
using TrudeSerializer.Debug;

namespace TrudeSerializer
{
    public static class S3UploadHelper
    {
        public static async void Upload(Dictionary<string, string> jsonData, string floorkey)
        {
            await S3helper.UploadAndRedirectToSnaptrude(jsonData);
            Application.Instance.FinishExportSuccess(floorkey);
        }
    }
}
