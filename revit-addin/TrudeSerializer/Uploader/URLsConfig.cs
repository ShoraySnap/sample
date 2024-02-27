using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Uploader
{
    internal class URLsConfig
    {
        public string snaptrudeReactUrl { get; set; }
        public string snaptrudeDjangoUrl { get; set; }

        private static URLsConfig GetURLObject()
        {
            string snaptrudeManagerPath = "snaptrude-manager";
            string configFileName = "urls.json";

            string configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                snaptrudeManagerPath,
                configFileName
            );

            string config = File.ReadAllText(configPath);
            URLsConfig configObject = JsonConvert.DeserializeObject<URLsConfig>(config);
            return configObject;
        }

        public static string GetSnaptrudeDjangoUrl()
        {
            URLsConfig urlsConfig = GetURLObject();
            return urlsConfig.snaptrudeDjangoUrl;
        }

        public static bool IsLocalENV()
        {
            URLsConfig urlsConfig = GetURLObject();
            return urlsConfig.snaptrudeDjangoUrl.Contains("localhost");
        }
    }
}