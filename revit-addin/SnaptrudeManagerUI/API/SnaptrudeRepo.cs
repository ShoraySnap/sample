using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using SnaptrudeManagerUI.API;
using static SnaptrudeManagerUI.API.Constants;
using SnaptrudeManagerUI.Models;
using SnaptrudeManagerUI.ViewModels;

namespace SnaptrudeManagerUI.API
{
  public static class SnaptrudeRepo
  {
    public static async Task<List<Folder>> GetUserWorkspacesAsync()
    {
      List<Dictionary<string, string>> workspacesData = await SnaptrudeService.GetUserWorkspacesAsync();
      List<Folder> workspaces = new List<Folder>();

      foreach (Dictionary<string, string> workspaceData in workspacesData)
      {
        WorkspaceType workspaceType = workspaceData["type"] == "workspace" ? WorkspaceType.Personal : WorkspaceType.Shared;
        string teamId = workspaceData["type"] == "workspace" ? "-1" : workspaceData["id"];
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

    public static async Task<string> CreateProjectAsync()
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
    }
}