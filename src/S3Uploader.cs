using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Amazon.S3;

namespace Cythral.CloudFormation.BuildTasks
{
    public class S3Uploader
    {
        private readonly IAmazonS3 s3Client;
        private readonly string bucketName;

        public S3Uploader(IAmazonS3 s3Client, string bucketName)
        {
            this.s3Client = s3Client;
            this.bucketName = bucketName;
        }

        public async Task Upload(string path, string key)
        {
            if (await Exists(key))
            {
                return;
            }

            Console.WriteLine("Uploading: " + key);
            await s3Client.UploadObjectFromFilePathAsync(bucketName, key, path, new Dictionary<string, object>());
        }

        private async Task<bool> Exists(string filename)
        {
#pragma warning disable CA1031
            try
            {
                await s3Client.GetObjectMetadataAsync(bucketName, filename);
                return true;
            }
            catch (Exception) { return false; }
#pragma warning restore CA1031
        }
    }
}