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
    }
}