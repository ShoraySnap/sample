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

        public static void WriteData(SerializedTrudeData data, string k = "")
        {
            string folder = TestUtils.GetTestProjectFolder();
            string revitVersion = TestUtils.GetRevitVersion();

            string serializedData = JsonConvert.SerializeObject(data);
            if(k.Length == 0)
            {
                File.WriteAllText(folder + revitVersion + "\\" + "newData.json", serializedData);
            }
            else
            {
                JToken newData = JsonConvert.DeserializeObject<JToken> (serializedData);
                string towrite = JsonConvert.SerializeObject(newData[k]);
                File.WriteAllText(folder + revitVersion + "\\" + "newData_" + k + ".json", towrite);
            }
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

        public static bool IsJsonKeySame(SerializedTrudeData data, string projectName, string key)
        {
            string serializedObject = JsonConvert.SerializeObject(data);
            JToken newData = JsonConvert.DeserializeObject<JToken> (serializedObject);
            JToken expectedDict = GetExpectedData(projectName);

            bool flag = JToken.DeepEquals(newData[key], expectedDict[key]);
            if(!flag)
            {
                WriteData(data, key);
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
