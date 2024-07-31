using Newtonsoft.Json;
using NLog.LayoutRenderers.Wrappers;
using System;
using System.Collections.Generic;
using System.Text;
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

        public Identifier(string email, string userId, string floorkey, string units, string env)
        {
            created = DateTime.Now.ToString();
            this.user = email;
            this.userId = userId;
            this.floorkey = floorkey;
            this.units = units;
            this.env = env;

            var idString = created + userId + floorkey;
            _id = Convert.ToBase64String(Encoding.UTF8.GetBytes(idString)).ToString();
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
        public static void SetIdentifer(string email, string userId, string floorkey, string units, string env)
        {
            uploadData.identifier = new Identifier(email, userId, floorkey, units, env);
        }

        public static void SetTeamData(Dictionary <string, string> team)
        {
            uploadData.identifier.SetTeamData(team);
        }
        public static void SetData(string str_data)
        {
           uploadData.data = JsonConvert.DeserializeObject(str_data);
        }

        public static void Save()
        {
            var serializedUploadData = JsonConvert.SerializeObject(uploadData);
            TrudeLocalAppData.StoreData(serializedUploadData, "analytics_data.json");
        }

        public static string GetUploadData()
        {
            return JsonConvert.SerializeObject(uploadData);
        }
    }
}
