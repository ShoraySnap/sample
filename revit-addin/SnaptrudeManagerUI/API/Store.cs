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
    public class Store
    {
        private static string filePath;
        private static Dictionary<string, string> data = new Dictionary<string, string>();
        private static readonly string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SnaptrudeManager");
        static Logger logger = LogManager.GetCurrentClassLogger();

        static Store()
        {
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
            filePath = Path.Combine(appDataPath, "config.json");

            if (File.Exists(filePath))
            {
                data = ParseDataFile(filePath);
            }
            else
            {
                CreateEmptyConfig();
            }
        }

        private static Dictionary<string, string> ParseDataFile(string filePath)
        {
            try
            {
                var jsonData = File.ReadAllText(filePath);
                var parsedData = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData);
                if (parsedData == null) throw new Exception("parsed config.json data is null.");
                return parsedData;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                CreateEmptyConfig();
                return data;
            }
        }

        private static void CreateEmptyConfig()
        {
            data = new Dictionary<string, string>();
            Save();
        }

        private static void ClearLoginData()
        {
            data["fullname"] = "";
            data["accessToken"] = "";
            data["refreshToken"] = "";
            data["userId"] = "";
            Save();
        }

        public static object Get(string key)
        {
            if (data == null) CreateEmptyConfig();

            data.TryGetValue(key, out var value);
            return value;
        }

        public static void Set(string key, string value)
        {
            data[key] = value;
        }

        public static void Unset(string key)
        {
            data.Remove(key);
        }

        public static void SetAllAndSave(Dictionary<string, string> dataObject)
        {
            foreach (var prop in dataObject.Keys)
                data[prop] = dataObject[prop];
            Save();
        }

        public static void Save()
        {
            try
            {
                var jsonData = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(filePath, jsonData);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
        }

        public static Dictionary<string, string> GetData()
        {
            if (data == null) CreateEmptyConfig();
            return new Dictionary<string, string>(data);
        }

        public static void Flush()
        {
            CreateEmptyConfig();
        }

        private static bool isMissingKey()
        {
            return
            !data.ContainsKey("fullname") ||
            !data.ContainsKey("accessToken") ||
            !data.ContainsKey("refreshToken") ||
            !data.ContainsKey("userId");
        }

        private static bool isNullOrEmpty()
        {
            if(
                data["fullname"] == null || data["fullname"] == "" ||
                data["accessToken"] == null || data["accessToken"] == "" ||
                data["refreshToken"] == null || data["refreshToken"] == "" ||
                data["userId"] == null || data["userId"] == ""
                )
            {
                return true;
            }
            return false;
        }

        public static bool isDataValid()
        {
            if (data == null) CreateEmptyConfig();
            if (!isMissingKey())
                if (!isNullOrEmpty())
                    return true;

            ClearLoginData();
            return false;
        }

        public static void Reset()
        {
            ClearLoginData();
            Save();
        }
    }

}
