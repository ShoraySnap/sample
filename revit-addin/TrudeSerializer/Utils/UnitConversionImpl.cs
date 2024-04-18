using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TrudeSerializer.Importer;

namespace TrudeSerializer.Utils
{
#if REVIT2021 || REVIT2022 || REVIT2023 || REVIT2024
    internal class UnitProvider_21_22_23_24 : IUnitProvider
    {
        public List<double> ConvertSnaptrudeUnitsFromXYZFromFeet(XYZ value)
        {
            return new List<double> { 
                ConvertToSnaptrudeUnits(value.X, UnitTypeId.Feet),
                ConvertToSnaptrudeUnits(value.Z, UnitTypeId.Feet),
                ConvertToSnaptrudeUnits(value.Y, UnitTypeId.Feet) };
        }

        public double ConvertToMillimeter(double value, object unit)
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

        public double ConvertToSnaptrudeAreaUnits(double areaValue)
        {
            return areaValue * 1.44;
        }

        public double ConvertToSnaptrudeUnits(double value, object unit)
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

        public double ConvertToSnaptrudeUnitsFromFeet(double value)
        {
            return ConvertToSnaptrudeUnits(value, UnitTypeId.Feet);
        }

        public double[] ConvertToSnaptrudeUnitsFromFeet(double[] values)
        {
            return new double[] { 
                ConvertToSnaptrudeUnits(values[0], UnitTypeId.Feet),
                ConvertToSnaptrudeUnits(values[1], UnitTypeId.Feet),
                ConvertToSnaptrudeUnits(values[2], UnitTypeId.Feet) };
        }

        public double[] ConvertToSnaptrudeUnitsFromFeet(XYZ value)
        {
            return new double[] { ConvertToSnaptrudeUnits(value.X, UnitTypeId.Feet), ConvertToSnaptrudeUnits(value.Z, UnitTypeId.Feet), ConvertToSnaptrudeUnits(value.Y, UnitTypeId.Feet) };
        }

        public object GetRevitUnit(TRUDE_UNIT_TYPE type)
        {
            switch(type)
            {
                case TRUDE_UNIT_TYPE.INCH:
                    return UnitTypeId.Inches;
                case TRUDE_UNIT_TYPE.FEET:
                    return UnitTypeId.Feet;
                case TRUDE_UNIT_TYPE.METER:
                    return UnitTypeId.Meters;
                case TRUDE_UNIT_TYPE.CENTIMETER:
                    return UnitTypeId.Centimeters;
                case TRUDE_UNIT_TYPE.MILLIMETER:
                    return UnitTypeId.Millimeters;
            }

            return null;
        }

        public object GetRevitUnitFromAssetPropertyDistance(AssetPropertyDistance p)
        {
            return p.GetUnitTypeId();
        }

        public object GetRevitUnitFromParameter(Parameter param)
        {
            return param.GetUnitTypeId();
        }

        public TRUDE_UNIT_TYPE GetTrudeUnit(object unit)
        {
            if (unit.Equals(UnitTypeId.Inches))
            {
                return TRUDE_UNIT_TYPE.INCH;
            }
            else if (unit.Equals(UnitTypeId.Feet))
            {
                return TRUDE_UNIT_TYPE.FEET;
            }
            else if (unit.Equals(UnitTypeId.Meters))
            {
                return TRUDE_UNIT_TYPE.METER;
            }
            else if (unit.Equals(UnitTypeId.Centimeters))
            {
                return TRUDE_UNIT_TYPE.CENTIMETER;
            }

            return TRUDE_UNIT_TYPE._INVALID;
        }

        public TRUDE_UNIT_TYPE GetTrudeUnitFromAssetPropertyDistance(AssetPropertyDistance p)
        {
            return GetTrudeUnit(GetRevitUnitFromAssetPropertyDistance(p));
        }

        public TRUDE_UNIT_TYPE GetTrudeUnitFromParameter(Parameter param)
        {
            return GetTrudeUnit(GetRevitUnitFromParameter(param));
        }

        public string GetUnitId(Document doc)
        {
            string unitsId = doc.GetUnits().GetFormatOptions(SpecTypeId.Length).GetUnitTypeId().TypeId;
            return unitsId;
        }

        public string GetUnitPattern()
        {
            const string pattern = @":(.*?)-";
            return pattern;
        }
    }
#elif REVIT2019 || REVIT2020
    internal class UnitProvider_19_20 : IUnitProvider
    {
        public List<double> ConvertSnaptrudeUnitsFromXYZFromFeet(XYZ value)
        {
            return new List<double> { 
                ConvertToSnaptrudeUnits(value.X, DisplayUnitType.DUT_DECIMAL_FEET),
                ConvertToSnaptrudeUnits(value.Z, DisplayUnitType.DUT_DECIMAL_FEET),
                ConvertToSnaptrudeUnits(value.Y, DisplayUnitType.DUT_DECIMAL_FEET) };
        }

        public double ConvertToMillimeter(double value, object unit)
        {
            if (unit.Equals(DisplayUnitType.DUT_DECIMAL_INCHES))
            {
                value *= 25.4;
            }
            else if (unit.Equals(DisplayUnitType.DUT_DECIMAL_FEET))
            {
                value *= 304.8;
            }
            else if (unit.Equals(DisplayUnitType.DUT_METERS))
            {
                value *= 1000;
            }
            else if (unit.Equals(DisplayUnitType.DUT_CENTIMETERS))
            {
                value *= 10;
            }
            return value;
        }

        public double ConvertToSnaptrudeAreaUnits(double areaValue)
        {
            return areaValue * 1.44;
        }

        public double ConvertToSnaptrudeUnits(double value, object unit)
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

        public double ConvertToSnaptrudeUnitsFromFeet(double value)
        {
            return ConvertToSnaptrudeUnits(value, DisplayUnitType.DUT_DECIMAL_FEET);
        }

        public double[] ConvertToSnaptrudeUnitsFromFeet(double[] values)
        {
            return new double[] { 
                ConvertToSnaptrudeUnits(values[0], DisplayUnitType.DUT_DECIMAL_FEET),
                ConvertToSnaptrudeUnits(values[1], DisplayUnitType.DUT_DECIMAL_FEET),
                ConvertToSnaptrudeUnits(values[2], DisplayUnitType.DUT_DECIMAL_FEET) };
        }

        public double[] ConvertToSnaptrudeUnitsFromFeet(XYZ value)
        {
            return new double[] { ConvertToSnaptrudeUnits(value.X, DisplayUnitType.DUT_DECIMAL_FEET), ConvertToSnaptrudeUnits(value.Z, DisplayUnitType.DUT_DECIMAL_FEET), ConvertToSnaptrudeUnits(value.Y, DisplayUnitType.DUT_DECIMAL_FEET) };
        }

        public object GetRevitUnit(TRUDE_UNIT_TYPE type)
        {
            switch(type)
            {
                case TRUDE_UNIT_TYPE.INCH:
                    return DisplayUnitType.DUT_DECIMAL_INCHES;
                case TRUDE_UNIT_TYPE.FEET:
                    return DisplayUnitType.DUT_DECIMAL_FEET;
                case TRUDE_UNIT_TYPE.METER:
                    return DisplayUnitType.DUT_METERS;
                case TRUDE_UNIT_TYPE.CENTIMETER:
                    return DisplayUnitType.DUT_CENTIMETERS;
                case TRUDE_UNIT_TYPE.MILLIMETER:
                    return DisplayUnitType.DUT_MILLIMETERS;
            }

            return null;
        }

        public object GetRevitUnitFromAssetPropertyDistance(AssetPropertyDistance p)
        {
            return p.DisplayUnitType;
        }

        public object GetRevitUnitFromParameter(Parameter param)
        {
            return param.DisplayUnitType;
        }

        public TRUDE_UNIT_TYPE GetTrudeUnit(object unit)
        {
            if (unit.Equals(DisplayUnitType.DUT_DECIMAL_INCHES))
            {
                return TRUDE_UNIT_TYPE.INCH;
            }
            else if (unit.Equals(DisplayUnitType.DUT_DECIMAL_FEET))
            {
                return TRUDE_UNIT_TYPE.FEET;
            }
            else if (unit.Equals(DisplayUnitType.DUT_METERS))
            {
                return TRUDE_UNIT_TYPE.METER;
            }
            else if (unit.Equals(DisplayUnitType.DUT_CENTIMETERS))
            {
                return TRUDE_UNIT_TYPE.CENTIMETER;
            }

            return TRUDE_UNIT_TYPE._INVALID;
        }

        public TRUDE_UNIT_TYPE GetTrudeUnitFromAssetPropertyDistance(AssetPropertyDistance p)
        {
            return GetTrudeUnit(GetRevitUnitFromAssetPropertyDistance(p));
        }

        public TRUDE_UNIT_TYPE GetTrudeUnitFromParameter(Parameter param)
        {
            return GetTrudeUnit(GetRevitUnitFromParameter(param));
        }

        public string GetUnitId(Document doc)
        {
            string unitsId = doc.GetUnits().GetFormatOptions(UnitType.UT_Length).DisplayUnits.ToString();
            return unitsId;
        }

        public string GetUnitPattern()
        {
            const string pattern = @"DUT_(.*)";
            return pattern;
        }
    }
#endif
}
