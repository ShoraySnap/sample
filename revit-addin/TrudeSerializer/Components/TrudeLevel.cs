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
#if REVIT2019 || REVIT2020
            double elevation = UnitConversion.ConvertToMillimeter((element as Level).Elevation, DisplayUnitType.DUT_DECIMAL_FEET);
#else
            double elevation = UnitConversion.ConvertToMillimeter((element as Level).Elevation, UnitTypeId.Feet);
#endif
            return new TrudeLevel(elementId, name, elevation);
        }
    }
}