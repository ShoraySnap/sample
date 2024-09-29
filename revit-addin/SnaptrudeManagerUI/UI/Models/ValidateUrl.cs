using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SnaptrudeManagerUI.API;

namespace SnaptrudeManagerUI.Models
{
    public class ValidateUrl
    {
        public bool Access { get; set; }
        public string ProjectName { get; set; }
        public string ImagePath { get; set; }
        public string Message { get; set; }

        public ValidateUrl(bool access, string name, string image, string message)
        {
            Access = access;
            ProjectName = name;
            ImagePath = image;
            Message = message;
        }
    }
}
