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
        Folder workspace = new Folder(workspaceData["id"], workspaceData["name"], workspaceType, "-1");
        workspaces.Add(workspace);
      }

      return workspaces;
    }
  }
}