using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SnaptrudeForgeExport;
using System;
using System.IO;
using System.Linq;
using TrudeImporter;

namespace UnitTests.SnaptrudeForgeExport
{
    public class ExportPDFViewsTests
    {
        UIApplication uiApp;
        readonly string projectName = "input.trude";
        TrudeProperties trudeProperties;
        ViewPlan plan;

        [SetUp]
        public void SetUp(UIApplication uiApplication)
        {
            Assert.NotNull(uiApplication);
            Assert.NotNull(uiApplication.Application);

            uiApp = uiApplication;
            if (trudeProperties == null)
            {
                ImportTrudeFile();
            }
        }

        [Test]
        public void GetViewPosition()
        {
            SheetSizeEnum sheetSize = SheetSizeEnum.ANSI_A;
            XYZ position = ExportPDFViews.GetViewPosition(sheetSize, UV.Zero, UV.Zero, UV.Zero);
            UV size = ExportPDFViews.GetSheetSize(sheetSize);

            Assert.That(position.X, Is.EqualTo(size.V / 2).Within(0.000001));
            Assert.That(position.Y, Is.EqualTo(size.U / 2).Within(0.000001));

            position = ExportPDFViews.GetViewPosition(sheetSize, new UV(1, 3), new UV(3, 1), new UV(-1, 1));

            Assert.That(position.X, Is.EqualTo(size.V / 2).Within(0.000001));
            Assert.That(position.Y, Is.EqualTo(size.U / 2).Within(0.000001));
        }

        [Test]
        public void SetViewColorScheme()
        {
            using (Transaction t = new Transaction(GlobalVariables.Document, "Set view color scheme"))
            {
                t.Start();
                ExportPDFViews.SetViewColorScheme(GlobalVariables.Document, plan, trudeProperties, trudeProperties.Views[0]);
                t.Commit();
            }

            Assert.AreEqual(plan.DisplayStyle, DisplayStyle.FlatColors);
        }

        [Test]
        public void ApplyColorOverridesToElement()
        {
            Element element = Utils.FindElement(GlobalVariables.Document, typeof(DirectShape));
            string color = "#2a2b2c";
            using (Transaction t = new Transaction(GlobalVariables.Document, "Apply color overrides"))
            {
                t.Start();
                ExportPDFViews.ApplyColorOverridesToElement(GlobalVariables.Document, plan, element.Id, color, false);
                t.Commit();
            }

            Assert.AreEqual(Utils.ConvertColorToHex(plan.GetElementOverrides(element.Id).CutForegroundPatternColor), color);
            Assert.AreEqual(plan.GetElementOverrides(element.Id).CutForegroundPatternId, Utils.GetSolidFillPatternElement(GlobalVariables.Document).Id);
            Assert.AreEqual(plan.GetElementOverrides(element.Id).IsCutForegroundPatternVisible, true);

            Assert.AreEqual(Utils.ConvertColorToHex(plan.GetElementOverrides(element.Id).SurfaceForegroundPatternColor), color);
            Assert.AreEqual(plan.GetElementOverrides(element.Id).SurfaceForegroundPatternId, Utils.GetSolidFillPatternElement(GlobalVariables.Document).Id);
            Assert.AreEqual(plan.GetElementOverrides(element.Id).IsSurfaceForegroundPatternVisible, true);
        }

        [Test]
        public void HideHiddenViewElements()
        {
            using (Transaction t = new Transaction(GlobalVariables.Document, "Hide view elements"))
            {
                t.Start();
                ExportPDFViews.HideHiddenViewElements(GlobalVariables.Document, plan, trudeProperties.Views[0]);
                t.Commit();
            }
            trudeProperties.Views[0].Elements.HiddenIds.Select(id => {
                ElementId elemId;
                GlobalVariables.UniqueIdToElementId.TryGetValue(id, out elemId);
                return GlobalVariables.Document.GetElement(elemId).IsHidden(plan);
            }).Select(isHidden => {
                Assert.AreEqual(true, isHidden);
                return true;
            });
        }

        [Test]
        public void GetRoomTagTypeFamilyName()
        {
            string familyName = ExportPDFViews.GetRoomTagTypeFamilyName(trudeProperties.Views[0]);
            Assert.AreEqual("Room Tag+Area_Snaptrude_1-16", familyName);
        }

        private void ImportTrudeFile()
        {
            string value = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\ForgeUnitTests\\projects\\2024\\";
            JObject trudeData = JObject.Parse(File.ReadAllText(value + projectName));
            Document newDoc = uiApp.Application.OpenDocumentFile(value + "host.rvt");

            GlobalVariables.RvtApp = uiApp.Application;
            GlobalVariables.Document = newDoc;
            GlobalVariables.ForForge = true;
            GlobalVariables.ForForgeViewsPDFExport = ((string)trudeData["outputFormat"] == "views_pdf");
            GlobalVariables.materials = trudeData["materials"] as JArray;
            GlobalVariables.multiMaterials = trudeData["multiMaterials"] as JArray;

            Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer()
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore,
            };
            serializer.Converters.Add(new XyzConverter());

            using (Transaction t = new Transaction(newDoc, "Parse Trude"))
            {
                GlobalVariables.Transaction = t;
                t.Start();
                trudeProperties = trudeData.ToObject<TrudeProperties>(serializer);
                ViewPlan template = Utils.FindElement(GlobalVariables.Document, typeof(ViewPlan), "View Template") as ViewPlan;
                TrudeImporterMain.Import(trudeProperties);
                plan = ExportPDFViews.DuplicateViewFromTemplateWithRoomTags(GlobalVariables.Document, trudeProperties.Views[0], template);
                t.Commit();
            }
        }
    }
}
