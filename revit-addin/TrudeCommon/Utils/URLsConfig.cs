using Newtonsoft.Json;
using System;
using System.IO;

namespace TrudeCommon.Utils
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
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
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
            return urlsConfig?.snaptrudeDjangoUrl;
        }

        public static string GetSnaptrudeReactUrl()
        {
            URLsConfig urlsConfig = GetURLObject();
            return urlsConfig?.snaptrudeReactUrl;
        }

        public static bool IsLocalEnv()
        {
            URLsConfig urlsConfig = GetURLObject();
            return urlsConfig.snaptrudeReactUrl.Contains("localhost");
        }

        public static bool IsPREnv()
        {
            URLsConfig urlsConfig = GetURLObject();
            return urlsConfig.snaptrudeReactUrl.Contains("amplifyapp");
        }

        public static bool IsDevEnv()
        {
            return IsPREnv() || IsLocalEnv();
        }
    }
}