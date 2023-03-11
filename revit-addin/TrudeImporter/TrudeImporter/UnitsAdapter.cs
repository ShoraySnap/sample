using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrudeImporter
{
    public static class UnitsAdapter
    {
        /// <summary>
        /// Method to convert local units to Revit units.
        /// </summary>
        /// <param name="SnaptrudeUnit">Value in Snaptrude units.</param>
        /// <returns>Value in Revit's internal units.</returns>
        public static double convertToRevit(double SnaptrudeUnit)
        {
            return Math.Round((double)(SnaptrudeUnit * 10 / 12), 6);
        }
        public static double convertToRevit(JToken SnaptrudeUnitJToken)
        {
            double SnaptrudeUnits = Convert.ToDouble(SnaptrudeUnitJToken.ToString());
            return convertToRevit(SnaptrudeUnits);
        }

        public static double FeetToMM(double feet, int precision = 0) 
        {
            return Math.Round((feet / 3.2808399) * 1000, precision);
        }

        public static double MMToFeet(double mm)
        {
            return mm / 304.8;
        }
    }
}
