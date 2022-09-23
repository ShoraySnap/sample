using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;

namespace Snaptrude
{
    public class ST_Layer
    {
        public string BaseTypeName;
        public string Name;
        public double ThicknessInMm;
        public bool IsCore;
        public string Function;

        public ST_Layer(JToken wallLayerJToken, string baseType, string function = null)
        {
            this.BaseTypeName = baseType;
            this.Name = (string)wallLayerJToken["value"];
            this.ThicknessInMm = (string)wallLayerJToken["thickness"] == "Variable" ? 25 : (double)wallLayerJToken["thickness"];
            this.IsCore = wallLayerJToken["core"] is null ? false : (bool)wallLayerJToken["core"];
            this.Function = function;
        }
        public ST_Layer(string baseType, string name, double thickness, bool isCore, string function = null)
        {
            this.BaseTypeName = baseType;
            this.Name = name;
            this.ThicknessInMm = thickness;
            this.IsCore = isCore;
            this.Function = function;
        }
        public bool Equals(ST_Layer other)
        {
            return this.Name == other.Name && this.ThicknessInMm == other.ThicknessInMm;
        }
        public CompoundStructureLayer ToCompoundStructureLayer(ElementId materialId, MaterialFunctionAssignment materialFunctionAssignment)
        {
            if (this.ThicknessInMm == 0) materialFunctionAssignment = MaterialFunctionAssignment.Membrane;

            return new CompoundStructureLayer(UnitsAdapter.MMToFeet(this.ThicknessInMm), materialFunctionAssignment, materialId);
        }
        public override string ToString()
        {
            return $"{this.Name}-{this.ThicknessInMm}";
        }

        private void SetFunction()
        {

        }
    }
}
