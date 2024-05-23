using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TrudeSerializer.Debug;
using TrudeSerializer.Importer;
using TrudeSerializer.Utils;
using TrudeCommon.Events;
using SnaptrudeManagerAddin;

namespace TrudeSerializer.Uploader
{
    internal class S3helper
    {
        static readonly string GET_PRESIGNED_URL = "/s3/presigned-url/upload/";
        public static async void UploadAndRedirectToSnaptrude(SerializedTrudeData serializedData)
        {
            Dictionary<string, string> jsonData = serializedData.GetSerializedObject();

            Config config = Config.GetConfigObject();

            string userId = config.userId;
            string projectFloorKey = config.floorKey;

            List<Task<HttpResponseMessage>> uploadTasks = new List<Task<HttpResponseMessage>>();

            foreach (KeyValuePair<string, string> entry in jsonData)
            {
                byte[] compressedString = Compressor.CompressString(entry.Value);
                string path = $"media/{userId}/revitImport/{projectFloorKey}/{entry.Key}.json";

                var presignedUrlResponse = await GetPresignedURL(path, config);
                var presignedUrlResponseData = await presignedUrlResponse.Content.ReadAsStringAsync();
                PreSignedURLResponse presignedURL = JsonConvert.DeserializeObject<PreSignedURLResponse>(presignedUrlResponseData);
                uploadTasks.Add(UploadUsingPresignedURL(compressedString, presignedURL));
            }

            await Task.WhenAll(uploadTasks);

            TrudeEventEmitter.EmitEventWithStringData(TRUDE_EVENT.REVIT_PLUGIN_EXPORT_TO_SNAPTRUDE_SUCCESS, projectFloorKey, Application.TransferManager);
        }

        public static async Task<HttpResponseMessage> UploadUsingPresignedURL(byte[] compressedString, PreSignedURLResponse preSignedURLResponse)
        {
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

        public static async void UploadLog(TrudeLogger logger, string processId)
        {
            var jsonData = logger.GetSerializedObject();

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

    }
}