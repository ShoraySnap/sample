using System.Collections.Generic;
using System.Windows.Documents;

namespace SnaptrudeManagerUI.API
{
    internal class ProjectsResponse
    {
        public string Message { get; set; }
        public List<object> Projects { get; set; }
    }
}