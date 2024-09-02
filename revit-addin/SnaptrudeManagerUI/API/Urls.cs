using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnaptrudeManagerUI.API
{
    public static class Urls
    {
        private static readonly string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SnaptrudeManager");
        private static readonly string fileName = "urls.json";
        private static readonly string filePath = Path.Combine(appDataPath, fileName);
        static Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly Dictionary<string, string> _urls = new Dictionary<string, string>();

        static Urls()
        {
            if (File.Exists(filePath))
            {
                _urls = ParseDataFile(filePath);
            }
        }

        private static Dictionary<string, string> ParseDataFile(string filePath)
        {
            try
            {
                var jsonData = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return new Dictionary<string, string>();
            }
        }

        public static string Get(string key)
        {
            return _urls.ContainsKey(key) ? _urls[key] : null;
        }

        public static Dictionary<string, string> GetAll()
        {
            return new Dictionary<string, string>(_urls);
        }
    }
}
