using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace TrudeImporter
{
    public class DirectShapeProperties
    {
        public string MaterialName { get; set; }
        public List<int> FaceMaterialIds { get; set; }
        public List<List<XYZ>> AllFaceVertices { get; set; }

        public DirectShapeProperties(string materialName, List<int> faceMaterialIds, List<List<XYZ>> allFaceVertices)
        {
            MaterialName = materialName;
            FaceMaterialIds = faceMaterialIds;
            AllFaceVertices = allFaceVertices;
        }
    }
}