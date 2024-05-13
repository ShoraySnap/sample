﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using TrudeSerializer.Debug;
using TrudeSerializer.Importer;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Uploader
{
    internal class S3helper
    {
        private static readonly string GET_PRESIGNED_URL = "/s3/presigned-url/upload/";
        private static readonly string GET_PRESIGNED_URLS = "/s3/presigned-urls/upload/";

        public static async void UploadAndRedirectToSnaptrude(SerializedTrudeData serializedData)
        {
            Dictionary<string, string> jsonData = serializedData.GetSerializedObject();
            Dictionary<string, byte[]> compressedJsonData = new Dictionary<string, byte[]>();

            Config config = Config.GetConfigObject();

            string userId = config.userId;
            string projectFloorKey = config.floorKey;

            List<Task<HttpResponseMessage>> uploadTasks = new List<Task<HttpResponseMessage>>();
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

            foreach (KeyValuePair<string, PreSignedURLResponse> presignedURL in presignedURLs)
            {
                uploadTasks.Add(UploadUsingPresignedURL(compressedJsonData[presignedURL.Key], presignedURL.Value));
            }
            await Task.WhenAll(uploadTasks);

            string requestURL = "snaptrude://finish?name=" + projectFloorKey;
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(requestURL) { UseShellExecute = true });
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