using System;
using System.IO;

namespace TrudeCommon.Utils
{
    internal static class TrudeLocalAppData
    {
        public static void StoreSerializedData(string serializedObject)
        {
            if (!URLsConfig.IsDevEnv()) return;

            
            string fileName = "serializedTrudeData.json";

            StoreData(serializedObject, fileName);
        }

        public static void StoreData(string data, string fileName)
        {
            string snaptrudeManagerPath = "snaptrude-manager";
            string filePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                snaptrudeManagerPath,
                fileName
            );

            File.WriteAllText(filePath, data);
        }
    }
}
