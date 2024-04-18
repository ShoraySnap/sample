using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TrudeSerializer.Importer;

namespace TrudeSerializer.Utils
{
    internal class UnitConversion
    {
#if REVIT2019 || REVIT2020
        public static void GetUnits(Document doc, SerializedTrudeData serializedSnaptrudeData)
        {
            string unitsId = doc.GetUnits().GetFormatOptions(UnitType.UT_Length).DisplayUnits.ToString();
            const string pattern = @"DUT_(.*)";
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

        public static double ConvertToMillimeter(double value, DisplayUnitType unit)
        {
            switch(unit)
            {
                case DisplayUnitType.DUT_DECIMAL_INCHES:
                    return value * 25.4;
                case DisplayUnitType.DUT_DECIMAL_FEET:
                    return value * 304.8;
                case DisplayUnitType.DUT_METERS:
                    return value * 1000.0;
                case DisplayUnitType.DUT_CENTIMETERS:
                    return value * 10.0;
            }
            return value;
        }

        public static double ConvertToSnaptrudeUnits(double value, DisplayUnitType unit)
        {
            if (unit.Equals(DisplayUnitType.DUT_DECIMAL_INCHES))
            {
                value /= 10;
            }
            else if (unit.Equals(DisplayUnitType.DUT_DECIMAL_FEET))
            {
                value *= (1.2);
            }
            else if (unit.Equals(DisplayUnitType.DUT_METERS))
            {
                value *= (39.37 / 10);
            }
            else if (unit.Equals(DisplayUnitType.DUT_CENTIMETERS))
            {
                value /= 25.4;
            }
            else if (unit.Equals(DisplayUnitType.DUT_MILLIMETERS))
            {
                value /= 254;
            }
            return value;
        }
        public static double ConvertToSnaptrudeUnitsFromFeet(double value)
        {
            return ConvertToSnaptrudeUnits(value, DisplayUnitType.DUT_DECIMAL_FEET);
        }

        public static double[] ConvertToSnaptrudeUnitsFromFeet(double[] values)
        {
            return new double[] { ConvertToSnaptrudeUnits(values[0], DisplayUnitType.DUT_DECIMAL_FEET), ConvertToSnaptrudeUnits(values[1], DisplayUnitType.DUT_DECIMAL_FEET), ConvertToSnaptrudeUnits(values[2], DisplayUnitType.DUT_DECIMAL_FEET) };
        }

        public static double ConvertToSnaptrudeAreaUnits(double areaValue)
        {
            return areaValue * 1.44;
        }

        public static double[] ConvertToSnaptrudeUnitsFromFeet(XYZ value)
        {
            return new double[] { ConvertToSnaptrudeUnits(value.X, DisplayUnitType.DUT_DECIMAL_FEET), ConvertToSnaptrudeUnits(value.Z, DisplayUnitType.DUT_DECIMAL_FEET), ConvertToSnaptrudeUnits(value.Y, DisplayUnitType.DUT_DECIMAL_FEET) };
        }

        public static List<double> ConvertSnaptrudeUnitsFromXYZFromFeet(XYZ value)
        {
            return new List<double> { ConvertToSnaptrudeUnits(value.X, DisplayUnitType.DUT_DECIMAL_FEET), ConvertToSnaptrudeUnits(value.Z, DisplayUnitType.DUT_DECIMAL_FEET), ConvertToSnaptrudeUnits(value.Y, DisplayUnitType.DUT_DECIMAL_FEET) };
        }

#endif
#if REVIT2021 || REVIT2022 || REVIT2023 || REVIT2024

        public static void GetUnits(Document doc, SerializedTrudeData serializedSnaptrudeData)
        {
            string unitsId = doc.GetUnits().GetFormatOptions(SpecTypeId.Length).GetUnitTypeId().TypeId;
            const string pattern = @":(.*?)-";
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
        public static double ConvertToMillimeter(double value, ForgeTypeId unit)
        {
            if (unit.Equals(UnitTypeId.Inches))
            {
                value *= 25.4;
            }
            else if (unit.Equals(UnitTypeId.Feet))
            {
                value *= 304.8;
            }
            else if (unit.Equals(UnitTypeId.Meters))
            {
                value *= 1000;
            }
            else if (unit.Equals(UnitTypeId.Centimeters))
            {
                value *= 10;
            }
            return value;
        }

        public static double ConvertToSnaptrudeUnits(double value, ForgeTypeId unit)
        {
            if (unit.Equals(UnitTypeId.Inches))
            {
                value /= 10;
            }
            else if (unit.Equals(UnitTypeId.Feet))
            {
                value *= (1.2);
            }
            else if (unit.Equals(UnitTypeId.Meters))
            {
                value *= (39.37 / 10);
            }
            else if (unit.Equals(UnitTypeId.Centimeters))
            {
                value /= 25.4;
            }
            else if (unit.Equals(UnitTypeId.Millimeters))
            {
                value /= 254;
            }
            return value;
        }

        public static double ConvertToSnaptrudeUnitsFromFeet(double value)
        {
            return ConvertToSnaptrudeUnits(value, UnitTypeId.Feet);
        }

        public static double[] ConvertToSnaptrudeUnitsFromFeet(double[] values)
        {
            return new double[] { ConvertToSnaptrudeUnits(values[0], UnitTypeId.Feet), ConvertToSnaptrudeUnits(values[1], UnitTypeId.Feet), ConvertToSnaptrudeUnits(values[2], UnitTypeId.Feet) };
        }

        public static double ConvertToSnaptrudeAreaUnits(double areaValue)
        {
            return areaValue * 1.44;
        }

        public static double[] ConvertToSnaptrudeUnitsFromFeet(XYZ value)
        {
            return new double[] { ConvertToSnaptrudeUnits(value.X, UnitTypeId.Feet), ConvertToSnaptrudeUnits(value.Z, UnitTypeId.Feet), ConvertToSnaptrudeUnits(value.Y, UnitTypeId.Feet) };
        }
        public static List<double> ConvertSnaptrudeUnitsFromXYZFromFeet(XYZ value)
        {
            return new List<double> { ConvertToSnaptrudeUnits(value.X, UnitTypeId.Feet), ConvertToSnaptrudeUnits(value.Z, UnitTypeId.Feet), ConvertToSnaptrudeUnits(value.Y, UnitTypeId.Feet) };
        }
#endif
    }
}