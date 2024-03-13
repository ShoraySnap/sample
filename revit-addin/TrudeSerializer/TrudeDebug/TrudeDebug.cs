using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Reflection;
using TrudeSerializer.Components;
using TrudeSerializer.Debug;
using TrudeSerializer.Importer;
using TrudeSerializer.Uploader;

namespace TrudeSerializer.Utils
{
    internal class TrudeDebug
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