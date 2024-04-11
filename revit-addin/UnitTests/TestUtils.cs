using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    internal class TestUtils
    {
        public static string GetRevitVersion()
        {
            string version = "2024";
#if REVIT2019
            version = "2019";
#elif REVIT2020
            version = "2020";
#elif REVIT2021
            version = "2021";
#elif REVIT2022
            version = "2022";
#elif REVIT2023
            version = "2023";
#elif REVIT2024
            version = "2024";
#endif
            return version;
        }

        public static string GetTestProjectFolder()
        {
            string value = "./projects/";
            var dir =  Directory.GetCurrentDirectory();

            value = Directory.GetCurrentDirectory() + "\\..\\..\\..\\..\\projects\\";
            return value;
        }

    }
}
