using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace TrudeSerializer.Uploader
{
    class Compressor
    {
        public static void CompressFile(string inputFilePath, string outputFilePath)
        {
            using (var inputFileStream = File.OpenRead(inputFilePath))
            {
                using (var compressedFileStream = File.Create(outputFilePath))
                {
                    using (var gzipStream = new GZipStream(compressedFileStream, CompressionMode.Compress))
                    {
                        inputFileStream.CopyTo(gzipStream);
                    }
                }
            }
        }

        public static byte[] CompressString(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
                {
                    gzipStream.Write(buffer, 0, buffer.Length);
                }
                return memoryStream.ToArray();
            }
        }
    }
}