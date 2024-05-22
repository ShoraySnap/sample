using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnaptrudeManagerUI.API
{
    public class SessionData
    {
        public static string? RevitModelName { get; set; }
        public static string? FileType { get; set; }
        public static UserData? UserData { get; set; }

        public static void FlushUserData()
        {
            UserData = new UserData();
        }
    }

    public class UserData
    {
        public string? RevitProjectName { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }
}
