using Newtonsoft.Json;
using NLog;
using NLog.LayoutRenderers.Wrappers;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TrudeCommon.Utils;

namespace TrudeCommon.Analytics
{
    internal class Identifier
    {
        public string _id;
        public string created;
        public string user;
        public string userId;
        public Dictionary<string, string> team;
        public string floorkey;
        public string units;
        public string env;
        public string revit_version;
        public string manager_version;

        public Identifier(string email, string userId, string floorkey, string units, string env, string id = "-1", string revit_version = "UNKNOWN")
        {
            created = DateTime.Now.ToString();
            this._id = id;
            this.user = email;
            this.userId = userId;
            this.floorkey = floorkey;
            this.units = units;
            this.env = env;
            this.revit_version = revit_version;
            this.manager_version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        public void SetTeamData(Dictionary<string, string> team)
        {
            this.team = team;
        }
    }
    internal class UploadData
    {
        public Identifier identifier;
        public object data;
    }
    internal static class AnalyticsManager
    {
        public static UploadData uploadData = new UploadData();

        private static Logger logger = LogManager.GetCurrentClassLogger();
        public static void SetIdentifer(string email, string userId, string floorkey, string units, string env, string processId, string rvt)
        {
            uploadData.identifier = new Identifier(email, userId, floorkey, units, env, processId, rvt);
        }

        public static void SetTeamData(Dictionary <string, string> team)
        {
            uploadData.identifier.SetTeamData(team);
        }
        public static void SetData(string str_data)
        {
           uploadData.data = JsonConvert.DeserializeObject(str_data);
        }

        public static void Save(string filename)
        {
            var serializedUploadData = JsonConvert.SerializeObject(uploadData, Formatting.Indented);
            TrudeLocalAppData.StoreData(serializedUploadData, filename);
        }

        public static Identifier GetIdentifier()
        {
            return uploadData.identifier;
        }

        public static string GetUploadData()
        {
            return JsonConvert.SerializeObject(uploadData);
        }

        public static async Task CommitExportDataToAPI()
        {
            string url = "http://localhost:6066/metrics/revitExport";
            var config = Config.GetConfigObject();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("auth", "Bearer " + config.accessToken);
                string jsonData = GetUploadData();
                HttpContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                try
                {
                    HttpResponseMessage response = await client.PostAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        string responseData = await response.Content.ReadAsStringAsync();
                        logger.Info("Response: " + responseData);
                    }
                    else
                    {
                        logger.Error("Error: " + response.StatusCode);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("Exception occurred: " + ex.Message);
                }
            }
        }
    }
}
