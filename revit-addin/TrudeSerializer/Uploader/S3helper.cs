using Amazon;
using Amazon.S3;
using System;
using System.Collections.Generic;
using System.IO;
using TrudeSerializer.Importer;

namespace TrudeSerializer.Uploader
{
    internal class S3helper
    {
        public static void UploadJSON(String fileName, String filePath)
        {
            var basicCredentials = new Amazon.Runtime.BasicAWSCredentials("<access token>", "<secret token>");
            var client = new AmazonS3Client(basicCredentials, RegionEndpoint.USEast1);
            var uploadRequest = new Amazon.S3.Model.PutObjectRequest
            {
                BucketName = "poojatestbucket",
                Key = fileName,
                FilePath = filePath
            };
            var response = client.PutObject(uploadRequest);
        }

        public static void UploadJSON(SerializedTrudeData serializedData, string projectFloorKey)
        {
            Dictionary<string, string> jsonData = serializedData.GetSerializedObject();

            var basicCredentials = new Amazon.Runtime.BasicAWSCredentials("<access token>", "<secret token>");
            var client = new AmazonS3Client(basicCredentials, RegionEndpoint.USEast1);

            foreach (KeyValuePair<string, string> entry in jsonData)
            {
                var compressedString = Compressor.CompressString(entry.Value);
                var uploadRequest = new Amazon.S3.Model.PutObjectRequest
                {
                    BucketName = "poojatestbucket",
                    Key = projectFloorKey + "/" + entry.Key + ".json",
                    InputStream = new MemoryStream(compressedString),
                };
                var response = client.PutObject(uploadRequest);
            }
        }
    }
}