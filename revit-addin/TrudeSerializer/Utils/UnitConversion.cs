using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TrudeSerializer.Importer;

namespace TrudeSerializer.Utils
{
    internal interface IUnitProvider
    {
        List<double> ConvertSnaptrudeUnitsFromXYZFromFeet(XYZ value);
        double ConvertToMillimeter(double value, object unit);
        double ConvertToSnaptrudeAreaUnits(double areaValue);
        double ConvertToSnaptrudeUnits(double value, object unit);
        double ConvertToSnaptrudeUnitsFromFeet(double value);
        double[] ConvertToSnaptrudeUnitsFromFeet(double[] values);
        double[] ConvertToSnaptrudeUnitsFromFeet(XYZ value);
        object GetRevitUnit(TRUDE_UNIT_TYPE type);
        object GetRevitUnitFromParameter(Parameter param);
        object GetRevitUnitFromAssetPropertyDistance(AssetPropertyDistance p);
        TRUDE_UNIT_TYPE GetTrudeUnit(object unit);
        TRUDE_UNIT_TYPE GetTrudeUnitFromParameter(Parameter param);
        TRUDE_UNIT_TYPE GetTrudeUnitFromAssetPropertyDistance(AssetPropertyDistance p);
        string GetUnitId(Document doc);
        string GetUnitPattern();
    }


    internal enum TRUDE_UNIT_TYPE
    {
        INCH, FEET, METER, CENTIMETER, MILLIMETER, _INVALID
    }

    internal class UnitConversion
    {
#if REVIT2019 || REVIT2020
        static IUnitProvider provider = new UnitProvider_19_20();
#elif REVIT2021 || REVIT2022 || REVIT2023 || REVIT2024 || REVIT2025
        static IUnitProvider provider = new UnitProvider_21_22_23_24_25();
#endif

        //TODO: Test this function properly
        public static void GetUnits(Document doc, SerializedTrudeData serializedSnaptrudeData)
        {
            string unitsId = provider.GetUnitId(doc);
            string pattern = provider.GetUnitPattern();
            Match match = Regex.Match(unitsId, pattern);
            string revitUnit = match.Success ? match.Groups[1].Value : null;
            if (revitUnit != null)
            {
                ComponentHandler.Instance.SetProjectUnit(serializedSnaptrudeData, revitUnit);
            }
            else
            {
                switch (doc.DisplayUnitSystem.ToString())
                {
                    case "IMPERIAL":
                        ComponentHandler.Instance.SetProjectUnit(serializedSnaptrudeData, "feetFractionalInches");
                        break;
                    case "METRIC":
                    default:
                        ComponentHandler.Instance.SetProjectUnit(serializedSnaptrudeData, "millimeters");
                        break;
                }
            }
        }
        public static double ConvertToMillimeter(double value, TRUDE_UNIT_TYPE unit)
        {
            var revitUnit = provider.GetRevitUnit(unit);
            return provider.ConvertToMillimeter(value, revitUnit);
        }

        public static double ConvertToSnaptrudeUnits(double value, TRUDE_UNIT_TYPE unit)
        {
            var revitUnit = provider.GetRevitUnit(unit);
            return provider.ConvertToSnaptrudeUnits(value, revitUnit);
        }

        public static double ConvertToSnaptrudeUnitsFromFeet(double value)
        {
            return provider.ConvertToSnaptrudeUnitsFromFeet(value);
        }

        public static double[] ConvertToSnaptrudeUnitsFromFeet(double[] values)
        {
            return provider.ConvertToSnaptrudeUnitsFromFeet(values);
        }

        public static double ConvertToSnaptrudeAreaUnits(double areaValue)
        {
            return provider.ConvertToSnaptrudeAreaUnits(areaValue);
        }

        public static double[] ConvertToSnaptrudeUnitsFromFeet(XYZ value)
        {
            return provider.ConvertToSnaptrudeUnitsFromFeet(value);
        }
        public static List<double> ConvertSnaptrudeUnitsFromXYZFromFeet(XYZ value)
        {
            return provider.ConvertSnaptrudeUnitsFromXYZFromFeet(value);
        }

        public static TRUDE_UNIT_TYPE GetTrudeUnitFromParameter(Parameter parameter)
        {
            return provider.GetTrudeUnitFromParameter(parameter);
        }

        public static TRUDE_UNIT_TYPE GetTrudeUnitFromAssetPropertyDistance(AssetPropertyDistance p)
        {
            return provider.GetTrudeUnitFromAssetPropertyDistance(p);
        }
    }
}