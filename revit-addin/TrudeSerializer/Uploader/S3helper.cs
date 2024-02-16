using Amazon;
using Amazon.S3;
using System;

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
    }
}
