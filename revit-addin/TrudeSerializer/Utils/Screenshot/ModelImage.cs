using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace TrudeSerializer.Utils.Screenshot
{
    internal class ModelImage
    {
        static private string snapshotPath;

        public static string GetSnapshotPath()
        {
            return snapshotPath;
        }
        public static void Capture()
        {
            Document document = GlobalVariables.Document;
            using (Transaction tx = new Transaction(document))
            {
                tx.Start("Export Image");

                string snaptrudeManagerPath = "snaptrude-manager";
                string configFileName = "snapshot";

                string filepath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    snaptrudeManagerPath,
                    configFileName
                );

                View view = document.ActiveView;
                DisplayStyle prevDisplayStyle = view.DisplayStyle;
                view.DisplayStyle = DisplayStyle.FlatColors;

                ImageExportOptions img = new ImageExportOptions();
                img.ZoomType = ZoomFitType.FitToPage;
                img.PixelSize = 512;
                img.ImageResolution = ImageResolution.DPI_600;
                img.FitDirection = FitDirectionType.Horizontal;
                img.ExportRange = ExportRange.CurrentView;
                img.HLRandWFViewsFileType = ImageFileType.PNG;
                img.FilePath = filepath;
                img.ShadowViewsFileType = ImageFileType.PNG;
                document.ExportImage(img);
                view.DisplayStyle = prevDisplayStyle;
                filepath = Path.ChangeExtension(filepath, "png");
                snapshotPath = filepath;
                tx.Commit();
            }
        }
    }
}