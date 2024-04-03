using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using TrudeSerializer.Importer;

namespace UnitTests
{
    internal class Common
    {
        public static UIDocument OpenProject(UIApplication uiApplication, string projectName)
        {
            string folder = TestUtils.GetTestProjectFolder();
            string revitVersion = TestUtils.GetRevitVersion();
            var doc = uiApplication.OpenAndActivateDocument(folder + revitVersion + "\\" + projectName + ".rvt");
            Assert.NotNull( doc );

            return doc;
        }

        public static JToken GetExpectedData(string projectName)
        {
            string folder = TestUtils.GetTestProjectFolder();
            string revitVersion = TestUtils.GetRevitVersion();
            string fileData = File.ReadAllText(folder + revitVersion + "\\" + projectName + ".json");
            return JsonConvert.DeserializeObject<JToken>(fileData);
        }

        public static void WriteData(SerializedTrudeData data)
        {
            string folder = TestUtils.GetTestProjectFolder();
            string revitVersion = TestUtils.GetRevitVersion();
            File.WriteAllText(folder + revitVersion + "\\" + "newData.json",JsonConvert.SerializeObject(data));
        }

        public static bool IsJsonSame(SerializedTrudeData data, string projectName)
        {
            string serializedObject = JsonConvert.SerializeObject(data);
            JToken newData = JsonConvert.DeserializeObject<JToken> (serializedObject);
            JToken expectedDict = GetExpectedData(projectName);

            //CONSIDER PROCESS ID
            string newProcessId = (string)newData["ProjectProperties"]["processId"];
            string oldProcessId = (string)expectedDict["ProjectProperties"]["processId"];
            newData["ProjectProperties"]["processId"] = expectedDict["ProjectProperties"]["processId"];

            bool flag = JToken.DeepEquals(newData, expectedDict);
            if(!flag)
            {
                WriteData(data);
            }
            return flag;
        }


        public static void CloseCurrentDocument(UIApplication uiApp)
        {
            if (uiApp.ActiveUIDocument != null) return;
            RevitCommandId closeDoc = RevitCommandId.LookupPostableCommandId(PostableCommand.Close);
            uiApp.PostCommand(closeDoc);
        }

    }
}
