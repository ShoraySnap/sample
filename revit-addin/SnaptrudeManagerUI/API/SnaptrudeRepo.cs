using NLog;
using static SnaptrudeManagerUI.API.Constants;
using SnaptrudeManagerUI.Models;
using SnaptrudeManagerUI.ViewModels;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Net.Http;

namespace SnaptrudeManagerUI.API
{
    public static class SnaptrudeRepo
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        public static async Task<List<Folder>> GetUserWorkspacesAsync()
        {
            List<Dictionary<string, string>> workspacesData = await SnaptrudeService.GetUserWorkspacesAsync();
            List<Folder> workspaces = new List<Folder>();

            foreach (Dictionary<string, string> workspaceData in workspacesData)
            {
                WorkspaceType workspaceType = WorkspaceType.Folder;
                switch (workspaceData["type"])
                {
                    case "top":
                        workspaceType = WorkspaceType.Top;
                        break;
                    case "personal":
                        workspaceType = WorkspaceType.Personal;
                        break;
                    case "personalExceedLimit":
                        workspaceType = WorkspaceType.PersonalExceedLimit;
                        break;
                    case "teamFree":
                        workspaceType = WorkspaceType.TeamFree;
                        break;
                    case "teamFreeExceedLimit":
                        workspaceType = WorkspaceType.TeamFreeExceedLimit;
                        break;
                    case "teamPaid":
                        workspaceType = WorkspaceType.TeamPaid;
                        break;
                    case "teamWithoutPermission":
                        workspaceType = WorkspaceType.TeamWithoutPermission;
                        break;
                }
                string teamId = workspaceData["type"] == "personal" ? "-1" : workspaceData["id"];
                Folder workspace = new Folder(workspaceData["id"], workspaceData["name"], workspaceType, teamId);
                workspaces.Add(workspace);
            }

            return workspaces;
        }

        public static async Task<List<Folder>> GetSubFoldersAsync(FolderViewModel selectedFolder)
        {
            string currentFolderId = selectedFolder.FolderType != WorkspaceType.Folder ? "root" : selectedFolder.Id;
            List<Dictionary<string, string>> foldersData = await SnaptrudeService.GetFoldersAsync(selectedFolder.TeamId, currentFolderId);
            List<Folder> folders = new List<Folder>();

            foreach (Dictionary<string, string> folderData in foldersData)
            {
                Folder folder = new Folder(folderData["id"], folderData["name"], WorkspaceType.Folder, selectedFolder.TeamId);
                folders.Add(folder);
            }

            return folders;
        }

        public static async Task<ValidateUrl> ValidateURLAsync(string floorkey)
        {
            var response = await SnaptrudeService.ValidateURLAsync(floorkey);
            if (!response.ContainsKey("access")) response.Add("access", "false");
            if (!response.ContainsKey("name")) response.Add("name", null);
            if (!response.ContainsKey("image")) response.Add("image", null);
            if (!response.ContainsKey("message")) response.Add("message", "");

            ValidateUrl validateUrl = new ValidateUrl(response["access"] == "true", response["name"], response["image"], response["message"]);
            return validateUrl;
        }

        public static async Task<bool> CheckIfUserLoggedInAsync()
        {
            var response = await SnaptrudeService.CheckIfUserLoggedInAsync();
            return response;
        }

        public static async Task<string> CreateProjectAsync()
        {
            try
            {
                var folder_id = Store.Get("folder_id").ToString();
                var team_id = Store.Get("team_id").ToString();
                var project_name = Store.Get("projectName").ToString();
                if (string.IsNullOrEmpty(folder_id) || string.Equals(folder_id, "-1"))
                    folder_id = "root";

                if (string.IsNullOrEmpty(team_id) || string.Equals(team_id, "-1"))
                    team_id = "";

                if (string.IsNullOrEmpty(project_name))
                    project_name = "Untitled";

                var response = await SnaptrudeService.CreateProjectAsync(folder_id, team_id, project_name);
                return response;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return "";
            }
        }

        public static async Task<bool> DeleteProjectAsync()
        {
            try
            {
                var floorkey = Store.Get("floorkey").ToString();
                var response = await SnaptrudeService.DeleteProjectAsync(floorkey);
                return response;
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
                return false;
            }
        }
    }
}