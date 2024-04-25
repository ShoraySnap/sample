using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManagerUI.Models
{
    public class Folder
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string FolderType { get; set; }
        public string ParentId { get; set; }
        public List<Folder> SubFolders { get; set; }

        public Folder(string id, string name, string type, string parentId = null)
        {
            Id = id;
            Name = name;
            FolderType = type;
            SubFolders = new List<Folder>();
            ParentId = parentId;
        }
    }
}
