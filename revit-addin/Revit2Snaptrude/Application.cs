using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Revit2Snaptrude
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Application : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            Assembly myAssembly = typeof(Application).Assembly;
            string assemblyPath = myAssembly.Location;


            string tabName = "Snaptrude";
            string panelName = "Snaptrude";

            // Create Ribbon Tab
            application.CreateRibbonTab(tabName);

            // Create Ribbon Panel to host the button
            RibbonPanel panel = application.CreateRibbonPanel(tabName, panelName);

            // Create the push button
            PushButton button = panel.AddItem(new PushButtonData("Export", "Snaptrude Manager", assemblyPath, "Revit2Snaptrude.DynamoExport")) as PushButton;
            button.Image = GetEmbeddedImage(myAssembly, "Revit2Snaptrude.Icons.logo16.png");
            button.LargeImage = GetEmbeddedImage(myAssembly, "Revit2Snaptrude.Icons.logo24.png");
            button.ToolTip = "Export the model to Snaptrude";

            //PushButton importButton = panel.AddItem(new PushButtonData("Import", "Trude Importer", assemblyPath, "Snaptrude.TrudeImporter")) as PushButton;
            //importButton.Image = GetEmbeddedImage(myAssembly, "Revit2Snaptrude.Icons.logo16.png");
            //importButton.LargeImage = GetEmbeddedImage(myAssembly, "Revit2Snaptrude.Icons.logo24.png");
            //importButton.ToolTip = "Import model from Snaptrude";

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private ImageSource GetEmbeddedImage(System.Reflection.Assembly assemb, string imageName)
        {
            System.IO.Stream file = assemb.GetManifestResourceStream(imageName);
            PngBitmapDecoder bd = new PngBitmapDecoder(file, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);

            return bd.Frames[0];
        }
    }
}