using Newtonsoft.Json;
using System;
using System.IO;

namespace TrudeSerializer.Utils
{
    internal class Config
    {
        public string floorKey { get; set; }
        public string accessToken { get; set; }
        public string refreshToken { get; set; }
        public string userId { get; set; }

        public static Config GetConfigObject()
        {
            string snaptrudeManagerPath = "snaptrude-manager";
            string configFileName = "config.json";

            string configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                snaptrudeManagerPath,
                configFileName
            );

            string config = File.ReadAllText(configPath);
            Config configObject = JsonConvert.DeserializeObject<Config>(config);
            return configObject;
        }
    }
}