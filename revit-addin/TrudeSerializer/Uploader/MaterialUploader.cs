using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Uploader
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

        public async void Upload()
        {
            List<string> keys = materials.Keys.ToList();
            Config config = Config.GetConfigObject();
            string userId = config.userId;
            string floorkey = config.floorKey;

            Dictionary<string, string> materialPaths = new Dictionary<string, string>();

            List<Task<HttpResponseMessage>> uploadTasks = new List<Task<HttpResponseMessage>>();

            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                string path = $"media/{userId}/revitImport/{floorkey}/materials/{key}";
                materialPaths[key] = path;
            }
            try
            {
                var presignedUrlResponse = await S3helper.GetPresignedURLs(materialPaths, config);
                var presignedUrlsResponseData = await presignedUrlResponse.Content.ReadAsStringAsync();
                Dictionary<string, PreSignedURLResponse> presignedURLs = JsonConvert.DeserializeObject<Dictionary<string, PreSignedURLResponse>>(presignedUrlsResponseData);
                foreach (KeyValuePair<string, string> entry in materials)
                {
                    string key = entry.Key;
                    byte[] imageData = File.ReadAllBytes(entry.Value);
                    var uploadTask = S3helper.UploadUsingPresignedURL(imageData, presignedURLs[key]);
                    uploadTasks.Add(uploadTask);
                }
                await Task.WhenAll(uploadTasks);
            }
            catch (Exception e)
            {
                Console.WriteLine(e?.Message);
            }
        }
    }
}