using System;
using System.Collections.Generic;
using System.Text;

namespace TrudeImporter.TrudeImporter.Model
{
    internal class TrudeMissing
    {
        public static void ImportMissingDoors(List<DoorProperties> doorProps)
        {
                if (GlobalVariables.MissingDoorFamiliesCount.Count == 0) return;
                //print missing door families
                foreach (var missingFamily in GlobalVariables.MissingDoorFamiliesCount)
                {
                    System.Diagnostics.Debug.WriteLine("Missing Family: " + missingFamily.Key+ " Count: "+ missingFamily.Value);
                
                }
        }
        //public static void ImportMissingWindows(WindowProperties window)
        //{

        //    foreach (var missingFamily in GlobalVariables.MissingDoorFamilies)
        //    {
        //        System.Diagnostics.Debug.WriteLine("Missing Family: " + missingFamily.Value);
        //    }

        //}
    }
}
