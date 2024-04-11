using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using NUnit.Framework;
using System.IO;
using Autodesk.Revit.DB.Visual;
using TrudeSerializer.Debug;
using TrudeSerializer;
using TrudeSerializer.Importer;
using Newtonsoft.Json;


namespace UnitTests.Serializer
{
    public class BasicDocumentTests
    {
        UIApplication uiApp;

        string projectName = "1";

        [SetUp]
        public void SetUp(UIApplication uiApplication)
        {
            Assert.NotNull(uiApplication);
            Assert.NotNull(uiApplication.Application);

            uiApp = uiApplication;
            var document = Common.OpenProject(uiApp, projectName).Document;
            Assert.NotNull(document);

            Assert.NotNull(uiApp.ActiveUIDocument);
            Assert.NotNull(uiApp.ActiveUIDocument.Document);
        }

        [TearDown]
        public void TearDown(UIApplication application)
        {
            Assert.NotNull(application);
            Assert.NotNull(application.Application);
            Assert.NotNull(uiApp);
            Assert.NotNull(uiApp.ActiveUIDocument);

            Common.CloseCurrentDocument(uiApp);
        }


        [Test]
        public void DocumentFullCycleSmokeSerializer()
        {
            Assert.NotNull(uiApp);
            Assert.NotNull(uiApp.Application);

            // CHECK DOCUMENT OPENED
            Assert.NotNull(uiApp.ActiveUIDocument);
            Assert.NotNull(uiApp.ActiveUIDocument.Document);

            // SMOKE SERIALIZER
            Command trudeSerializer = new Command();
            var result = trudeSerializer.ExecuteWithUIApplication(uiApp, true);
            Assert.IsTrue(result == Result.Succeeded);
            Assert.True(trudeSerializer.isDone);
        }

        [Test]
        public void DocumentInitializeTest()
        {
            Assert.NotNull(uiApp);
            Assert.NotNull(uiApp.Application);

            // CHECK DOCUMENT OPENED
            Assert.NotNull(uiApp.ActiveUIDocument);
            Assert.NotNull(uiApp.ActiveUIDocument.Document);

            // Init Serializer
            Command trudeSerializer = new Command();
            trudeSerializer.OnInit += (processId, app, doc) =>
            {
                Assert.NotNull(processId);
                Assert.IsTrue(processId.Length > 0);
                Assert.NotNull(TrudeLogger.Instance);
                Assert.NotNull(doc);
                Assert.AreEqual(GlobalVariables.Document, doc);
                Assert.NotNull(GlobalVariables.RvtApp);
            };
            var result = trudeSerializer.ExecuteWithUIApplication(uiApp, true);
            Assert.IsTrue(result == Result.Succeeded);
            Assert.True(trudeSerializer.isDone);
        }

        [Test]
        public void DocumentView3DTest()
        {
            Assert.NotNull(uiApp);
            Assert.NotNull(uiApp.Application);

            // CHECK DOCUMENT OPENED
            Assert.NotNull(uiApp.ActiveUIDocument);
            Assert.NotNull(uiApp.ActiveUIDocument.Document);

            Command trudeSerializer = new Command();
            trudeSerializer.OnView3D += (view, doc) =>
            {
                Assert.NotNull(view);
            };
            var result = trudeSerializer.ExecuteWithUIApplication(uiApp, true);
            Assert.IsTrue(result == Result.Succeeded);
            Assert.True(trudeSerializer.isDone);
        }

        [Test]
        public void DocumentBasicSerializedDataTest()
        {
            Assert.NotNull(uiApp);
            Assert.NotNull(uiApp.Application);

            // CHECK DOCUMENT OPENED
            Assert.NotNull(uiApp.ActiveUIDocument);
            Assert.NotNull(uiApp.ActiveUIDocument.Document);

            Command trudeSerializer = new Command();
            trudeSerializer.OnCleanSerializedTrudeData += (data) =>
            {
                Assert.NotNull(data);
                Assert.IsTrue(Common.IsJsonSame(data, projectName));
            };

            var result = trudeSerializer.ExecuteWithUIApplication(uiApp, true);
            Assert.IsTrue(result == Result.Succeeded);
            Assert.True(trudeSerializer.isDone);
        }

        [Test]
        public void DocumentSerializedWallsCheck()
        {
            Assert.NotNull(uiApp);
            Assert.NotNull(uiApp.Application);

            // CHECK DOCUMENT OPENED
            Assert.NotNull(uiApp.ActiveUIDocument);
            Assert.NotNull(uiApp.ActiveUIDocument.Document);

            Command trudeSerializer = new Command();
            trudeSerializer.OnCleanSerializedTrudeData += (data) =>
            {
                Assert.NotNull(data);
                Assert.IsTrue(Common.IsJsonKeySame(data, projectName, "Walls"));
            };

            var result = trudeSerializer.ExecuteWithUIApplication(uiApp, true);
            Assert.IsTrue(result == Result.Succeeded);
            Assert.True(trudeSerializer.isDone);
        }


    }
}
