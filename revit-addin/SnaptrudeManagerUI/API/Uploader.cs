using Newtonsoft.Json;
using NLog;
using SnaptrudeManagerUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TrudeCommon.Analytics;
using TrudeCommon.Events;
using TrudeCommon.Utils;
using static SnaptrudeManagerUI.ViewModels.ProgressViewModel;

namespace SnaptrudeManagerUI.API
{
    internal class PreSignedURLResponse
    {
        public string url { get; set; }
        public Dictionary<string, string> fields { get; set; }
    }

    internal class Uploader
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly string GET_PRESIGNED_URL = "/s3/presigned-url/upload/";
        private static readonly string GET_PRESIGNED_URLS = "/s3/presigned-urls/upload/";

        public static volatile bool abortFlag = false;
        public static async void Upload(ProgressViewType progressViewType)
        {
            abortFlag = false;

            if (progressViewType != ProgressViewType.ExportRFAExisting)
            {
                try
                {
                    UpdateUploadProgressValues(60, "Creating Snaptrude Project...");
                    logger.Info("Creating Snaptrude Project!");
                    string floorkey = await SnaptrudeRepo.CreateProjectAsync();
                    Store.Set("floorkey", floorkey);
                    Store.Save();
                }
                catch (Exception ex)
                {
                    logger.Error("Error on upload to Snaptrude" + ex.StackTrace); 
                    App.OnUploadIssue.Invoke("Error creating the project:" + ex.Message);
                    return;
                }
            }

            try
            {
                logger.Info("Uploading to snaptrude!");

                string processId = App.TransferManager.ReadString(TRUDE_EVENT.REVIT_PLUGIN_REQUEST_UPLOAD_TO_SNAPTRUDE);

                var data = FileUtils.GetCommonTempFile(FileUtils.DATA_FNAME);
                var stringData = Encoding.UTF8.GetString(data);
                var deserializedData = JsonConvert.DeserializeObject<Dictionary<string, string>>(stringData);

                logger.Info("Uploading data to snaptrude!");
                await UploadAndRedirectToSnaptrude(deserializedData);

                var matData = FileUtils.GetCommonTempFile(FileUtils.MATERIAL_FNAME);
                var matDataStr = Encoding.UTF8.GetString(matData);
                var materials = JsonConvert.DeserializeObject<Dictionary<string, string>>(matDataStr);

                logger.Info("Uploading materials to snaptrude!");
                await UploadMaterials(materials);

                var logData = FileUtils.GetCommonTempFile(FileUtils.LOG_FNAME);
                var logDataStr = Encoding.UTF8.GetString(logData);

                logger.Info("Uploading log to snaptrude!");
                UpdateUploadProgressValues(98, "Finalizing Process...");
                await UploadLog(logDataStr, processId);

                var analyticsData = FileUtils.GetCommonTempFile(FileUtils.ANALYTICS_FNAME);
                var aDataStr = Encoding.UTF8.GetString(analyticsData);

                string floorkey = Store.Get("floorkey").ToString();
                logger.Info("Uploading analytics to snaptrude!");
                UpdateUploadProgressValues(99, "Finalizing Process...");
                await UploadAnalytics(floorkey, aDataStr, processId);

                logger.Info("Export finished, opening browser.");
                await MainWindowViewModel.Instance.ProgressViewModel.FinishExport(floorkey);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, App.OnUploadIssue);
                try
                {
                    if (progressViewType == ProgressViewType.ExportProjectNew || progressViewType == ProgressViewType.ExportRFANew)
                    {
                        logger.Info("Deleting Snaptrude project...");
                        await SnaptrudeRepo.DeleteProjectAsync();
                    }
                }
                catch (Exception deleteEx)
                {
                    logger.Info("Error Deleting Snaptrude project..." + deleteEx.Message);
                }
            }
        }

        public static void UpdateUploadProgressValues(int progress, string message)
        {
            App.OnProgressUpdate?.Invoke(progress, message);
        }

        public static async Task UploadAndRedirectToSnaptrude(Dictionary<string, string> jsonData)
        {
            Dictionary<string, byte[]> compressedJsonData = new Dictionary<string, byte[]>();

            Config config = Config.GetConfigObject();

            string userId = config.userId;
            string projectFloorKey = config.floorKey;

            List<Task> uploadTasks = new List<Task>();
            Dictionary<string, string> paths = new Dictionary<string, string>();

            foreach (KeyValuePair<string, string> entry in jsonData)
            {
                byte[] compressedString = Compressor.CompressString(entry.Value);
                compressedJsonData.Add(entry.Key, compressedString);

                string path = $"media/{userId}/revitImport/{projectFloorKey}/{entry.Key}.json";
                paths.Add(entry.Key, path);
            }

            var presignedUrlsResponse = await GetPresignedURLs(paths, config);
            var presignedUrlsResponseData = await presignedUrlsResponse.Content.ReadAsStringAsync();
            Dictionary<string, PreSignedURLResponse> presignedURLs = JsonConvert.DeserializeObject<Dictionary<string, PreSignedURLResponse>>(presignedUrlsResponseData);

            int uploadTasksDone = 0;
            foreach (KeyValuePair<string, PreSignedURLResponse> presignedURL in presignedURLs)
            {
                var task = UploadUsingPresignedURL(compressedJsonData[presignedURL.Key], presignedURL.Value).ContinueWith((a) =>
                    {
                        uploadTasksDone++;
                        float p = uploadTasksDone / (float)uploadTasks.Count;
                        int progress = (int)Math.Round(70.0 + p * 20.0);
                        UpdateUploadProgressValues(progress, $"Uploading Serialized Data... {uploadTasksDone} / {uploadTasks.Count}");
                    });
                uploadTasks.Add(task);
            }
            UpdateUploadProgressValues(70, $"Uploading Serialized Data... {uploadTasksDone} / {uploadTasks.Count}");
            await Task.WhenAll(uploadTasks);
        }

        public static async Task<HttpResponseMessage> UploadUsingPresignedURL(byte[] compressedString, PreSignedURLResponse preSignedURLResponse)
        {
            if (abortFlag) return new HttpResponseMessage(System.Net.HttpStatusCode.Ambiguous);
            var url = preSignedURLResponse.url;
            var formData = new MultipartFormDataContent();
            var client = new HttpClient();
            // Add form fields from presigned POST
            foreach (var key in preSignedURLResponse.fields)
            {
                formData.Add(new StringContent((string)key.Value), (string)key.Key);
            }

            // Add file to be uploaded
            formData.Add(new ByteArrayContent(compressedString), "file");

            // Perform the upload
            var uploadResponse = await client.PostAsync(url, formData);
            uploadResponse.EnsureSuccessStatusCode();
            return uploadResponse;
        }

        public static async Task<HttpResponseMessage> GetPresignedURLs(Dictionary<string, string> fileNames, Config config)
        {
            var client = new HttpClient();

            string snaptrudeDjangoUrl = URLsConfig.GetSnaptrudeDjangoUrl();

            string url = snaptrudeDjangoUrl + GET_PRESIGNED_URLS;

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            string accessToken = "Bearer " + config.accessToken;
            request.Headers.Add("Auth", accessToken);

            string serializedFileNames = JsonConvert.SerializeObject(fileNames);

            var content = new MultipartFormDataContent
            {
                { new StringContent(serializedFileNames), "object_names" }
            };
            request.Content = content;
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseData = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(responseData);
            if (result.ContainsKey("error") && result["message"] == "Invalid Access Token.")
            {
                throw new InvalidTokenException("Invalid Access Token");
            }

            return response;
        }

        public static async Task<HttpResponseMessage> GetPresignedURL(string fileName, Config config)
        {
            var client = new HttpClient();

            string snaptrudeDjangoUrl = URLsConfig.GetSnaptrudeDjangoUrl();

            string url = snaptrudeDjangoUrl + GET_PRESIGNED_URL;

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            string accessToken = "Bearer " + config.accessToken;
            request.Headers.Add("Auth", accessToken);

            var content = new MultipartFormDataContent
            {
                { new StringContent(fileName), "object_name" }
            };
            request.Content = content;
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return response;
        }

        public static async Task UploadLog(string jsonData, string processId)
        {
            Config config = Config.GetConfigObject();

            string userId = config.userId;
            string projectFloorKey = config.floorKey;

            Task<HttpResponseMessage> uploadTask;

            byte[] data = Encoding.UTF8.GetBytes(jsonData.ToString());
            string path = $"media/{userId}/revitImport/{projectFloorKey}/logs/{processId}_log.json";

            var presignedUrlResponse = await GetPresignedURL(path, config);
            var presignedUrlResponseData = await presignedUrlResponse.Content.ReadAsStringAsync();
            PreSignedURLResponse presignedURL = JsonConvert.DeserializeObject<PreSignedURLResponse>(presignedUrlResponseData);
            uploadTask = UploadUsingPresignedURL(data, presignedURL);

            await uploadTask;
        }

        public static async Task UploadAnalytics(string floorkey, string analyticsData, string processId, string folder = "revitImport")
        {
            var uploadData = JsonConvert.DeserializeObject<UploadData>(analyticsData);
            uploadData.identifier.floorkey = floorkey;

            var identifier = uploadData.identifier;
            string userId = identifier.userId;
            string projectFloorKey = identifier.floorkey;


            var jsonData = JsonConvert.SerializeObject(uploadData);
            Task<HttpResponseMessage> uploadTask;

            byte[] data = Encoding.UTF8.GetBytes(jsonData.ToString());
            string path = $"media/{userId}/{folder}/{projectFloorKey}/analytics/{processId}_analytics.json";

            var presignedUrlResponse = await GetPresignedURL(path, Config.GetConfigObject());
            var presignedUrlResponseData = await presignedUrlResponse.Content.ReadAsStringAsync();
            PreSignedURLResponse presignedURL = JsonConvert.DeserializeObject<PreSignedURLResponse>(presignedUrlResponseData);
            uploadTask = UploadUsingPresignedURL(data, presignedURL);

            await uploadTask;
        }

        public static async Task UploadMaterials(Dictionary<string, string> materials)
        {
            List<string> keys = materials.Keys.ToList();
            Config config = Config.GetConfigObject();
            string userId = config.userId;
            string floorkey = config.floorKey;

            Dictionary<string, string> materialPaths = new Dictionary<string, string>();

            List<Task> uploadTasks = new List<Task>();

            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                string path = $"media/{userId}/revitImport/{floorkey}/materials/{key}";
                materialPaths[key] = path;
            }
            var presignedUrlResponse = await GetPresignedURLs(materialPaths, config);
            var presignedUrlsResponseData = await presignedUrlResponse.Content.ReadAsStringAsync();
            Dictionary<string, PreSignedURLResponse> presignedURLs = JsonConvert.DeserializeObject<Dictionary<string, PreSignedURLResponse>>(presignedUrlsResponseData);
            int uploadTasksDone = 0;

            foreach (KeyValuePair<string, string> entry in materials)
            {
                string key = entry.Key;
                byte[] imageData = File.ReadAllBytes(entry.Value);
                var uploadTask = UploadUsingPresignedURL(imageData, presignedURLs[key])
                .ContinueWith((a) =>
                {
                    uploadTasksDone++;
                    float p = uploadTasksDone / (float)uploadTasks.Count;
                    int progress = (int)Math.Round(90.0 + p * 8.0);
                    UpdateUploadProgressValues(progress, $"Uploading Materials... {uploadTasksDone} / {uploadTasks.Count}");
                });
                uploadTasks.Add(uploadTask);
            }
            UpdateUploadProgressValues(90, $"Uploading Materials... {uploadTasksDone} / {uploadTasks.Count}");
            await Task.WhenAll(uploadTasks);
        }
    }
}
