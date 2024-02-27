using System;
using System.IO;
using TrudeSerializer.Uploader;

namespace TrudeSerializer.Utils
{
    internal class TrudeDebug
    {
        public static void StoreSerializedData(string serializedObject)
        {
            if (!URLsConfig.IsLocalENV()) return;

            string snaptrudeManagerPath = "snaptrude-manager";
            string fileName = "serializedTrudeData.json";

            string filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                snaptrudeManagerPath,
                fileName
            );

            File.WriteAllText(filePath, serializedObject);
        }
    }
}