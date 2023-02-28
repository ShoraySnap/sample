using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrudeImporter
{
    /// <summary>
    /// Units Adapter Class to convert local internal units to Revit internal units.
    /// </summary>
    public static class UnitsAdapter
    {
        /// <summary>
        /// Integer representing the units used in Snaptrude project.
        /// </summary>
        public static int metricSystem { get; set; }
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

        public static double FeetToMM(double feet)
        {
            return Math.Round((feet / 3.2808399) * 1000, 1); // remove decimals for mm
        }

        public static double MMToFeet(double mm)
        {
            return mm / 304.8;
        }
    }
}
