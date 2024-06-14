using Autodesk.Revit.DB;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Components
{
    internal class TrudeLevel : TrudeComponent
    {
        public string name;
        public double elevation;

        private TrudeLevel(string elementId, string name, double elevation) : base(elementId, "Level", "", "")
        {
            this.name = name;
            this.elevation = elevation;
            this.isParametric = true;
        }
        public static string GetLevelName(Element element)
        {
            Element level = GlobalVariables.Document.GetElement(element.LevelId);
            if (level == null) return "";
            return level.Name;
        }

        public static TrudeLevel GetSerializedComponent(Element element)
        {
            string elementId = element.Id.ToString();
            string name = element.Name;
            double elevation = UnitConversion.ConvertToSnaptrudeUnitsFromFeet((element as Level).ProjectElevation);

            return new TrudeLevel(elementId, name, elevation);
        }
    }
}