using Autodesk.Revit.DB;

namespace TrudeSerializer.Utils
{
    internal class UnitConversion
    {
        public static double ConvertToMillimeterForRevit2021AndAbove(double value, ForgeTypeId unit)
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
    }
}