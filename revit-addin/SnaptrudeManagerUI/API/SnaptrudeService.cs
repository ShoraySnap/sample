﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using SnaptrudeManagerUI.API;
using static SnaptrudeManagerUI.API.Constants;

namespace SnaptrudeManagerUI.API
{
    public static class SnaptrudeService
    {
        private static readonly HttpClient httpClient;
        static Logger logger = LogManager.GetCurrentClassLogger();

        static SnaptrudeService()
        {
            var handler = new CustomHttpHandler
            {
                InnerHandler = new HttpClientHandler()
            };
            httpClient = new HttpClient(handler);

            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Add("User-Agent", "SnaptrudeService");
        }

        private static async Task<HttpResponseMessage> CallApiAsync(string endPoint, HttpMethod httpMethod, Dictionary<string, string> data = null)
        {
            string djangoUrl = Urls.Get("snaptrudeDjangoUrl");
            HttpResponseMessage response = null;

            if (httpMethod == HttpMethod.Post)
            {
                var formData = new FormUrlEncodedContent(data);
                response = await httpClient.PostAsync($"{djangoUrl}{endPoint}", formData);
            }
            else if (httpMethod == HttpMethod.Get)
            {
                response = await httpClient.GetAsync($"{djangoUrl}{endPoint}");
            }

            return response;
        }

        public static async Task<string> CreateProjectAsync()
        {
            logger.Info("Creating Snaptrude project");
            string endPoint = "/newBlankProject";
            var data = new Dictionary<string, string>
            {
                { "project_name", SessionData.UserData.RevitProjectName }
            };

            var response = await CallApiAsync(endPoint, HttpMethod.Post, data);

            if (response != null && response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseData);
                string floorKey = result["floorkey"];
                logger.Info("Created Snaptrude project", floorKey);
                return floorKey;
            }

            return null;
        }

        public static async Task<List<Dictionary<string, string>>> GetUserWorkspacesAsync()
        {
            string endPoint = "/user/workspaces/valid-teams";
            var response = await CallApiAsync(endPoint, HttpMethod.Get);

            if (response != null)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(responseData);

                var workspaces = new List<Dictionary<string, string>>();

                foreach (var workspace in result["myWorkspace"])
                {
                    workspaces.Add(new Dictionary<string, string>
                    {
                        { "id", Constants.PERSONAL_WORKSPACE_ID },
                        { "name", Constants.PERSONAL_WORKSPACE_NAME },
                        { "type", "workspace"}
                    });
                }

                foreach (var team in result["teams"])
                {
                    workspaces.Add(new Dictionary<string, string>
                    {
                        { "id", team["id"].ToString() },
                        { "name", team["name"].ToString() },
                        { "type", "team" }
                    });
                }

                return workspaces;
            }

            return null;
        }

        public static async Task<List<Dictionary<string, string>>> GetFoldersAsync(string teamId, string currentFolderId)
        {
            bool fetchFromPersonalWorkspace = teamId == "-1";

            string endPoint = fetchFromPersonalWorkspace
                ? "/folderWithoutProject/"
                : $"/team/{teamId}/folderWithoutProject/";

            var data = new Dictionary<string, string>
            {
                { "limit", "1000" },
                { "offset", "0" },
                { "folder", currentFolderId }
            };

            var response = await CallApiAsync(endPoint, HttpMethod.Post, data);

            if (response != null)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(responseData);
                var folders = new List<Dictionary<string, string>>();

                foreach (var folder in result["folders"])
                {
                    folders.Add(new Dictionary<string, string>
                    {
                        { "id", folder["id"].ToString() },
                        { "name", folder["name"].ToString() }
                    });
                }

                return folders;
            }

            return null;
        }


        public static async Task<Dictionary<string, string>> ValidateURLAsync(string floorkey)
        {
            string endPoint = "/import/permission/?floorkey=" + floorkey;
            var data = new Dictionary<string, string>
            {
                { "floorkey", floorkey }
            };
            var response = await CallApiAsync(endPoint, HttpMethod.Get, data);

            if (response != null)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseData);
                return result;
            }

            return null;
        }



        private static async Task<bool> CheckRoleForPermissionToCreateProjectAsync(Dictionary<string, string> team)
        {
            if (team["role"] == "viewer" || team["role"] == "editor")
            {
                return false;
            }

            if (team["role"] != "admin" && team["role"] != "creator")
            {
                string endPoint = $"/team/{team["id"]}/getrole/";
                var response = await CallApiAsync(endPoint, HttpMethod.Post, new Dictionary<string, string>());

                if (response != null && response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(responseData);
                    var permissionObject = result["team"]["permissions"];
                    var roleBasedPermissions = new Dictionary<string, dynamic>();

                    foreach (var permission in permissionObject)
                    {
                        roleBasedPermissions[permission["name"].ToString()] = permission;
                    }

                    if (!bool.Parse(roleBasedPermissions[team["role"]]["create_project"].ToString()))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static async Task<bool> CheckIfUserLoggedInAsync()
        {
            string accessToken = Store.GetData()["accessToken"];
            string refreshToken = Store.GetData()["refreshToken"];

            string djangoUrl = Urls.Get("snaptrudeDjangoUrl");
            var data = new Dictionary<string, string>
            {
                { "accessToken", accessToken },
                { "refreshToken", refreshToken }
            };

            var response = await httpClient.PostAsync($"{djangoUrl}/refreshAccessToken/", new FormUrlEncodedContent(data));

            if (response != null && response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseData);

                if (result.ContainsKey("accessToken"))
                {
                    Store.GetData()["ccessToken"] = result["accessToken"];
                    // Update user data in your application state here
                    return true;
                }
            }

            return false;
        }

        public static async Task<bool> IsPaidUserAccountAsync()
        {
            string endPoint = "/getuserprofile/";
            var response = await CallApiAsync(endPoint, HttpMethod.Get);

            if (response != null && response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(responseData);

                bool isPro = bool.Parse(result["isPro"].ToString());
                string customerLifeCycle = result["customer_lifeCycle"].ToString();

                return isPro || customerLifeCycle == "Paid_User" || customerLifeCycle == "Trial_Started";
            }

            return false;
        }



        public static async Task<bool> CheckPersonalWorkspacesAsync()
        {
            try
            {
                bool isUserPro = await IsPaidUserAccountAsync();
                if (isUserPro) return true;

                int projectCount = await GetProjectInPersonalWorkSpaceAsync();
                return projectCount < 5;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static async Task<int> GetProjectInPersonalWorkSpaceAsync()
        {
            string endPoint = "/getprojects/";
            var data = new Dictionary<string, string>
            {
                { "limit", "10" },
                { "offset", "0" }
            };

            var response = await CallApiAsync(endPoint, HttpMethod.Post, data);

            if (response != null && response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(responseData);
                return result["projects"].Count;
            }

            return 0;
        }

        public static async Task<Dictionary<string, dynamic>> CheckModelUrlAsync(string floorkey)
        {
            string endPoint = $"/import/permission/?floorkey={floorkey}";
            var data = new Dictionary<string, string>
            {
                { "floorkey", floorkey }
            };

            var response = await CallApiAsync(endPoint, HttpMethod.Get, data);

            if (response != null && response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(responseData);
            }

            return null;
        }
    }

}
