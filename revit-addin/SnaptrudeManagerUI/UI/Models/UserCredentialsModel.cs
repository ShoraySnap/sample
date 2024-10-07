using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnaptrudeManagerUI.Models
{
    public class UserCredentialsModel
    {
        public string accessToken { get; set; }
        public string refreshToken { get; set; }
        public string fullname { get; set; }
        public int userId { get; set; }

        public UserCredentialsModel(string _accessToken, string _refreshToken, string _fullname, int _userId)
        {
            accessToken = _accessToken;
            refreshToken = _refreshToken;
            fullname = _fullname;
            userId = _userId;
        }
    }
}
