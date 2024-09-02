using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TrudeCommon.Utils;
using TrudeSerializer.Uploader;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Utils
{
    public class MaterialUploader
    {
        Dictionary<string, string> materials = new Dictionary<string, string> { };
        Dictionary<string, string> failedMaterials = new Dictionary<string, string> { };

        private static MaterialUploader instance = null;

        public MaterialUploader()
        {
        }

        public static MaterialUploader Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MaterialUploader();
                }
                return instance;
            }
        }

        public void AddFailedMaterial(string key, string material)
        {
            failedMaterials[key] = material;
        }

        public void AddMaterial(string key, string material)
        {
            materials[key] = material;
        }

        public void ClearMaterial()
        {
            materials.Clear();
        }
        public string SaveForUpload()
        {
            string fileName = FileUtils.MATERIAL_FNAME;

            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(materials));
            FileUtils.SaveCommonTempFile(fileName, bytes);
            return fileName;
        }
    }
}