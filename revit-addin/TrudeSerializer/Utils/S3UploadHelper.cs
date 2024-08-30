using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrudeCommon.Utils;
using TrudeSerializer.Importer;
using SnaptrudeManagerAddin;
using TrudeSerializer.Debug;
using Newtonsoft.Json;

namespace TrudeSerializer
{
    public static class S3UploadHelper
    {
        public static Action<float, string> action;
        public static void SetProgressUpdate(Action<float,string> func)
        {
            action = func;
        }

        public static string SaveForUpload(Dictionary<string, string> jsonData)
        {
            string fileName = FileUtils.DATA_FNAME;
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(jsonData));
            FileUtils.SaveCommonTempFile(fileName, bytes);
            return fileName;
        }

        public static async void Upload(Dictionary<string, string> jsonData, string floorkey)
        {
            if (S3helper.OnUploadProgressUpdate != null) S3helper.OnUploadProgressUpdate = null;
            S3helper.OnUploadProgressUpdate += action;
            await S3helper.UploadAndRedirectToSnaptrude(jsonData);

            if (ExportToSnaptrudeEEH.IsImportAborted())
                Application.Instance.EmitAbortEvent();
            else
                Application.Instance.FinishExportSuccess(floorkey);
        }
    }
}
