using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Documents;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        public static async Task<bool> IsInternetAvailableAsync()
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync("8.8.8.8", 2000);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> IsSnaptrudeAvailableAsync()
        {
            try
            {
                string djangoUrl = Urls.Get("snaptrudeDjangoUrl");
                var response = await httpClient.GetAsync(djangoUrl);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<HttpResponseMessage> CallApiAsync(string endPoint, HttpMethod httpMethod, Dictionary<string, string> data = null)
        {
            if (!await IsInternetAvailableAsync())
            {
                throw new NoInternetException();
            }

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

            var responseData = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("\n" + responseData);
            }

            try
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(responseData);
                if (result.ContainsKey("error") && result["message"] == "Invalid Access Token.")
                {
                    throw new InvalidTokenException("Invalid Access Token");
                }
            }
            catch (Exception)
            {
            }

            return response;
        }

        public static async Task<string> CreateProjectAsync(string folder_id, string team_id, string project_name)
        {
            logger.Info("Creating Snaptrude project");
            string endPoint = "/import/project/";

            var data = new Dictionary<string, string>
            {
                { "project_name", project_name },
                { "team_id", team_id },
                { "folder_id", folder_id }
            };

            var response = await CallApiAsync(endPoint, HttpMethod.Post, data);

            if (response != null && response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<JToken>(responseData);
                if (result != null)
                {
                    string floorKey = result["floorkey"].ToString();
                    logger.Info("Created Snaptrude project", floorKey);
                    return floorKey;
                }
            }

            return null;
        }

        public static async Task<bool> DeleteProjectAsync(string floorkey)
        {
            logger.Info("Deleting Snaptrude project");
            string endPoint = "/deleteProject/";

            var data = new Dictionary<string, string>
            {
                { "floorkey", floorkey }
            };

            var response = await CallApiAsync(endPoint, HttpMethod.Post, data);

            if (response != null && response.IsSuccessStatusCode)
                return true;
            else
                return false;
        }

        public static async Task<List<Dictionary<string, string>>> GetUserWorkspacesAsync()
        {
            string endPoint = "/user/teams/active";
            var response = await CallApiAsync(endPoint, HttpMethod.Get);

            if (response != null)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Dictionary<string, List<Team>>>(responseData);

                var workspaces = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> {
                        { "id", Constants.PERSONAL_WORKSPACE_ID },
                        { "name", Constants.PERSONAL_WORKSPACE_NAME },
                        { "type", "personal"}
                    }
                };

                var teamsTask = GetTeamsData(result["teams"]);
                var teamsWorkspaces = await teamsTask;

                workspaces.AddRange(teamsWorkspaces);

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
        private static async Task<Dictionary<string, string>> GetTeamData(Team team)
        {
            var checkPermissionTask = CheckRoleForPermissionToCreateProjectAsync(team);

            string endPoint = $"/team/{team.id}/project?";
            var data = new Dictionary<string, string>
            {
                { "limit", "10" },
                { "offset", "0" }
            };
            var queryParams = string.Join("&", data.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            var callApiTask = CallApiAsync($"{endPoint}{queryParams}", HttpMethod.Get, data);

            await Task.WhenAll(new Task[] { checkPermissionTask, callApiTask });

            bool isPermissionToCreateProject = await checkPermissionTask;
            HttpResponseMessage response = await callApiTask;

            if (!isPermissionToCreateProject)
            {
                return new Dictionary<string, string>
                {
                    { "id", team.id.ToString() },
                    { "name", team.name.ToString() },
                    { "type", "teamWithoutPermission" },
                };
            }
            if (team.isManuallyPaid)
            {
                return new Dictionary<string, string>
                {
                    { "id", team.id.ToString() },
                    { "name", team.name.ToString() },
                    { "type", "teamPaid" },
                };
            }

            if (response != null)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ProjectsResponse>(responseData);

                if (result.Projects.Count < 1)
                {
                    return new Dictionary<string, string>
                    {
                        { "id", team.id.ToString() },
                        { "name", team.name.ToString() },
                        { "type", "teamFree" },
                    };
                }
                else
                {
                    return new Dictionary<string, string>
                    {
                        { "id", team.id.ToString() },
                        { "name", team.name.ToString() },
                        { "type", "teamFreeExceedLimit" },
                    };
                }
            }
            return null;
        }

        private static async Task<List<Dictionary<string, string>>> GetTeamsData(List<Team> teams)
        {
            var teamTasks = new List<Task<Dictionary<string, string>>>();
            //teamTasks.Add(GetTeamData(teams[3]));
            foreach (var team in teams)
            {
                teamTasks.Add(GetTeamData(team));
            }
            await Task.WhenAll(teamTasks);

            var teamsData = new List<Dictionary<string, string>>();
            foreach (var task in teamTasks)
            {
                teamsData.Add(await task);
            }
            return teamsData;
        }

        private static async Task<bool> CheckRoleForPermissionToCreateProjectAsync(Team team)
        {
            if (team.role == "viewer" || team.role == "editor")
            {
                return false;
            }

            if (team.role != "admin" && team.role != "creator")
            {
                string endPoint = $"/team/{team.id}/getrole/";
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

                    if (!bool.Parse(roleBasedPermissions[team.role]["create_project"].ToString()))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static async Task<bool> CheckIfUserLoggedInAsync()
        {
            string accessToken = Store.Get("accessToken")?.ToString();
            if (string.IsNullOrEmpty(accessToken)) { return false; }
            string refreshToken = Store.Get("refreshToken")?.ToString();

            string djangoUrl = Urls.Get("snaptrudeDjangoUrl");
            var data = new Dictionary<string, string>
            {
                { "accessToken", accessToken },
                { "refreshToken", refreshToken }
            };

            var serializedData = JsonConvert.SerializeObject(data);
            var stringContentData = new StringContent(serializedData, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{djangoUrl}/refreshAccessToken/", stringContentData);

            if (response != null && response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseData);

                if (result.ContainsKey("accessToken"))
                {
                    Store.Set("accessToken", result["accessToken"]);
                    Store.Save();
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
                var result = JsonConvert.DeserializeObject<List<dynamic>>(responseData);
                return result.Count;
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

        public static async Task<bool> SetRevitImportState(string floorkey, string state)
        {
            string endPoint = $"/import/state";
            var data = new Dictionary<string, string>
            {
                { "floorkey", floorkey },
                {"revitImportState", state },
            };

            var response = await CallApiAsync(endPoint, HttpMethod.Post, data);

            if (response != null && response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                return true;
            }

            return false;

        }

    }

}
