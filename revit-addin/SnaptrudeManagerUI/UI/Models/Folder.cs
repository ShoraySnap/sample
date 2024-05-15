using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnaptrudeManagerUI.API;

namespace SnaptrudeManagerUI.Models
{
    public class Folder
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Constants.WorkspaceType FolderType { get; set; }
        public string ParentId { get; set; }
        public List<Folder> SubFolders { get; set; }
        public string TeamId { get; set; }

        public Folder(string id, string name, Constants.WorkspaceType type, string parentId = null, string teamId = "-1")
        {
            Id = id;
            Name = name;
            FolderType = type;
            SubFolders = new List<Folder>();
            ParentId = parentId;
            TeamId = teamId;
        }
    }
}
