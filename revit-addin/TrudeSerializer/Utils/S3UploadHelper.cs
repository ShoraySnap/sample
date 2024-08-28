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
        public static Action<float, string> action;
        public static void SetProgressUpdate(Action<float,string> func)
        {
            action = func;
        }
        public static async void Upload(Dictionary<string, string> jsonData, string floorkey)
        {
            if (S3helper.OnUploadProgressUpdate != null) S3helper.OnUploadProgressUpdate = null;
            S3helper.OnUploadProgressUpdate += action;
            await S3helper.UploadAndRedirectToSnaptrude(jsonData);
            Application.Instance.FinishExportSuccess(floorkey);
        }
    }
}
