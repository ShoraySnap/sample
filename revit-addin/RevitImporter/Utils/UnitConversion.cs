﻿using Autodesk.Revit.DB;

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
                value *= (12/10);
            }
            else if (unit.Equals(UnitTypeId.Meters))
            {
                value *= (39.37/10);
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
    }
}
