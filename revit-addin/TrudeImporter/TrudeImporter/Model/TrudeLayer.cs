using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;

namespace TrudeImporter
{
    public class TrudeLayer
    {
        public string BaseTypeName;
        public string Name;
        public double ThicknessInMm;
        public bool IsCore;
        public string Function;

        public TrudeLayer(JToken wallLayerJToken, string baseType, string function = null, double defaultThickness = 25)
        {
            this.BaseTypeName = baseType;
            this.Name = (string)wallLayerJToken["value"];
            this.ThicknessInMm = (string)wallLayerJToken["thickness"] == "Variable" ? defaultThickness : (double)wallLayerJToken["thickness"];
            this.IsCore = wallLayerJToken["core"] is null ? false : (bool)wallLayerJToken["core"];
            this.Function = function;
        }
        public TrudeLayer(string baseType, string name, double thickness, bool isCore, string function = null)
        {
            this.BaseTypeName = baseType;
            this.Name = name;
            this.ThicknessInMm = thickness;
            this.IsCore = isCore;
            this.Function = function;
        }
        public bool Equals(TrudeLayer other)
        {
            return this.Name == other.Name && this.ThicknessInMm == other.ThicknessInMm;
        }
        public CompoundStructureLayer ToCompoundStructureLayer(ElementId materialId, MaterialFunctionAssignment materialFunctionAssignment)
        {
            if (this.ThicknessInMm == 0) materialFunctionAssignment = MaterialFunctionAssignment.Membrane;

            double thickness = UnitsAdapter.MMToFeet(this.ThicknessInMm);
            if (thickness < CompoundStructure.GetMinimumLayerThickness())
            {
                thickness = CompoundStructure.GetMinimumLayerThickness() + 0.0001;
            }

            return new CompoundStructureLayer(thickness, materialFunctionAssignment, materialId);
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
