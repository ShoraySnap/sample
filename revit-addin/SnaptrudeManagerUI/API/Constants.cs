﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SnaptrudeManagerUI.API
{
    public static class Constants
    {


        public static string AUTH_FILE = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "snaptrude-manager",
                "auth.txt"
                );
        public static string PERSONAL_WORKSPACE_ID = "0";
        public static string PERSONAL_WORKSPACE_NAME = "My Workspace";
        public static string ROOT_FOLDER_ID = "root";
        public static string SNAPTRUDE_PROTOCOL = "snaptrude";
        public enum WorkspaceType
        {
            Top,
            Personal,
            Shared,
            Folder
        }
    }
}
