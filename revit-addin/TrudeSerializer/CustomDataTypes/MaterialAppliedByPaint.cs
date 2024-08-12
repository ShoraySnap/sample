using System.Collections.Generic;
using TrudeSerializer.Components;

namespace TrudeSerializer.CustomDataTypes
{
    public class MaterialAppliedByPaint
    {
        public Dictionary<string, TrudeMaterial> materialsMap;
        public Dictionary<string, FaceToMaterialMap> faceToMaterialMap;

        public MaterialAppliedByPaint(Dictionary<string, TrudeMaterial> materialsMap, Dictionary<string, FaceToMaterialMap> faceToMaterialMap)
        {
            this.materialsMap = materialsMap;
            this.faceToMaterialMap = faceToMaterialMap;
        }
    }
    public class FaceToMaterialMap
    {
        public string materialId;
        public List<double> normal;

        public FaceToMaterialMap(List<double> normal, string materialId)
        {
            this.normal = normal;
            this.materialId = materialId;
        }
    }
}