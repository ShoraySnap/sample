using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using TrudeImporter;

namespace SnaptrudeForgeExport
{
    public static class ExportPDFViews
    {
        private const double ProgressBeforeHand = 50;
        private const double ProgressAfterSheetsSetup = 95;
        public static void Export(Document newDoc, TrudeProperties trudeProperties)
        {
#if REVIT2019 || REVIT2020 || REVIT2021
            return;
#else
            using (Transaction t = new Transaction(newDoc, "Set up project information"))
            {
                t.Start();

                ProjectInfo projectInfo = newDoc.ProjectInformation;
                projectInfo.ClientName = trudeProperties.PDFExport.CompanyName;
                projectInfo.LookupParameter("Project Name").Set(trudeProperties.PDFExport.ProjectName);

                Units units = new Units(trudeProperties.Project.Unit == UnitEnum.Metric ? UnitSystem.Metric : UnitSystem.Imperial);
                
                // modify the accuracy of the spec used to display areas
                FormatOptions formatOptions = units.GetFormatOptions(SpecTypeId.Area);
                formatOptions.Accuracy = trudeProperties.Project.Tolerance;
                units.SetFormatOptions(SpecTypeId.Area, formatOptions);

                newDoc.SetUnits(units);

                t.Commit();
            }

            // This must be created in host.rvt
            ViewPlan template = Utils.FindElement(newDoc, typeof(ViewPlan), "View Template") as ViewPlan;
            double progress = ProgressBeforeHand;
            double delta = (ProgressAfterSheetsSetup - ProgressBeforeHand) / trudeProperties.Views.Count;
            List<ViewSheet> sheets = trudeProperties.Views.Select(viewProperties =>
            {
                using (Transaction t = new Transaction(newDoc, "Set up view detail levels, color scheme and sheet"))
                {
                    Utils.LogProgress(progress, "Importing view " + viewProperties.Name);
                    progress += delta;
                    t.Start();
                    ViewPlan viewPlan = DuplicateViewFromTemplateWithRoomTags(newDoc, viewProperties, template);
                    ViewSheet sheet = null;
                    if (viewPlan != null)
                    {
                        viewPlan.ApplyViewTemplateParameters(template);
                        viewPlan.Scale = viewProperties.Sheet.Scale;
                        SetCropBoxToFitPaperSize(newDoc, viewPlan, viewProperties.Camera.BottomLeft, viewProperties.Camera.TopRight, viewProperties, trudeProperties);
                        FamilySymbol titleBlockType = Utils.GetElements(newDoc, typeof(FamilySymbol))
                                .Cast<FamilySymbol>()
                                .Where(f => f.FamilyName.Replace(" ", "_").ToLower().Contains(Enum.GetName(typeof(SheetSizeEnum), viewProperties.Sheet.SheetSize).ToLower()) && f.Name.Contains("Snaptrude"))
                                .FirstOrDefault();
                        sheet = ViewSheet.Create(newDoc, titleBlockType.Id);
                        sheet.Name = viewProperties.Name;
                        Viewport vp = Viewport.Create(newDoc, sheet.Id, viewPlan.Id,
                            GetViewPosition(viewProperties.Sheet.SheetSize, GlobalVariables.PDFPaddingX, GlobalVariables.PDFPaddingY, new UV(0.008, -0.0035)));
                        SetViewColorScheme(newDoc, viewPlan, trudeProperties, viewProperties);
                        HideHiddenViewElements(newDoc, viewPlan, viewProperties);
                        newDoc.GetElement(vp.GetTypeId()).get_Parameter(BuiltInParameter.VIEWPORT_ATTR_SHOW_LABEL).Set(0);
                        t.Commit();
                    }
                    return sheet;
                }
            }).Where(sheet => sheet != null).ToList();

            Directory.CreateDirectory(Configs.PDF_EXPORT_DIRECTORY);

            PDFExportProperties pdfExport = trudeProperties.PDFExport;
            Utils.LogProgress(ProgressAfterSheetsSetup, "Almost done...");

            using (Transaction t = new Transaction(newDoc, "Export to PDF"))
            {
                t.Start();

                PDFExportOptions options = new PDFExportOptions
                {
                    ColorDepth = ColorDepthType.Color,
                    Combine = pdfExport.MergePDFs,
                    ExportQuality = PDFExportQualityType.DPI4000,
                    HideCropBoundaries = true,
                    PaperFormat = ExportPaperFormat.Default,
                    FileName = pdfExport.ProjectName + "_Sheets",
                    //HideReferencePlane = true,
                    //HideScopeBoxes = true,
                    //HideUnreferencedViewTags = true,
                    //MaskCoincidentLines = true,
                    //StopOnError = true,
                    //ViewLinksInBlue = false,
                    ZoomType = ZoomType.Zoom,
                    ZoomPercentage = 100
                };
                newDoc.Export(Configs.PDF_EXPORT_DIRECTORY, sheets.Select(sheet => sheet.Id).ToList(), options);
                t.Commit();
            }

            if (File.Exists(Configs.OUTPUT_FILE)) File.Delete(Configs.OUTPUT_FILE);
            ZipFile.CreateFromDirectory(Configs.PDF_EXPORT_DIRECTORY, Configs.OUTPUT_FILE);

            Directory.Delete(Configs.PDF_EXPORT_DIRECTORY, true);
#endif
        }

        public static XYZ GetViewPosition(SheetSizeEnum sheetSize, UV PDFPaddingX, UV PDFPaddingY, UV corrections)
        {
            UV size = GetSheetSize(sheetSize);
            double x = (size.V / 2) + corrections.U + (PDFPaddingX.V - PDFPaddingX.U) / 2;
            double y = (size.U / 2) + corrections.V + (PDFPaddingY.V - PDFPaddingY.U) / 2;
            return new XYZ(x, y, 0);
        }

        public static ViewPlan DuplicateViewFromTemplateWithRoomTags(Document doc, ViewProperties viewProperties, View template)
        {
            // Duplicate the view
            ViewFamilyType floorPlanType = new FilteredElementCollector(doc)
                                .OfClass(typeof(ViewFamilyType))
                                .Cast<ViewFamilyType>()
                                .FirstOrDefault(x => ViewFamily.FloorPlan == x.ViewFamily);
            bool doesLevelExist = GlobalVariables.LevelIdByNumber.TryGetValue(viewProperties.Storey, out ElementId lvlId);
            if (doesLevelExist)
            {
                Level lvl = doc.GetElement(lvlId) as Level;
                ViewPlan ogView = Utils.FindElement(doc, typeof(ViewPlan), lvl.Name) as ViewPlan;
                ViewPlan newView = ViewPlan.Create(doc, floorPlanType.Id, lvl.Id);
                newView.ApplyViewTemplateParameters(template);
                newView.Scale = viewProperties.Sheet.Scale;

                // Collect room tags in the original view
                FilteredElementCollector collector = new FilteredElementCollector(doc, ogView.Id);

                ElementId roomTagTypeId = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .Cast<FamilySymbol>()
                    .Where(rtt => rtt.FamilyName == GetRoomTagTypeFamilyName(viewProperties))
                    .Select(rtt => rtt.Id)
                    .FirstOrDefault();
                RoomTagType roomTagType = doc.GetElement(roomTagTypeId) as RoomTagType;

                if (viewProperties.Label.Selected.Count > 0)
                {
                    IEnumerable<RoomTag> tags = collector.OfClass(typeof(SpatialElementTag))
                        .Where(s => s.GetType() == typeof(RoomTag)).Cast<RoomTag>();
                    foreach (RoomTag roomTag in tags)
                    {
                        // Get room tag location
                        LocationPoint locPoint = roomTag.Location as LocationPoint;
                        XYZ tagLocation = locPoint.Point;

                        // Get the referenced room
                        Room room = doc.GetElement(roomTag.Room.Id) as Room;

                        // Create a new room tag in the new view at the same location
                        RoomTag newRoomTag = doc.Create.NewRoomTag(new LinkElementId(room.Id), new UV(tagLocation.X, tagLocation.Y), newView.Id);
                        newRoomTag.RoomTagType = roomTagType; // Copy the tag type
                    }
                }

                return newView;
            }
            else
            {
                Utils.LogTrace("Failed to get level for storey " + viewProperties.Storey.ToString());
                return null;
            }
        }

        public static void SetViewColorScheme(Document doc, ViewPlan viewPlan, TrudeProperties trudeProperties, ViewProperties viewProperties)
        {
            if (viewProperties.Color.Scheme == ColorSchemeEnum.texture)
            {
                viewPlan.DisplayStyle = DisplayStyle.FlatColors;
                trudeProperties.Masses.ForEach(mass =>
                {
                    bool doesMassExist = GlobalVariables.UniqueIdToElementId.TryGetValue(mass.UniqueId, out ElementId elemId);
                    if (doesMassExist)
                    {
                        ApplyColorOverridesToElement(doc, viewPlan, elemId, mass.MaterialHex, mass.Storey < viewProperties.Storey);
                    }
                });

                trudeProperties.Slabs.ForEach(slab =>
                {
                    bool doesSlabExist = GlobalVariables.UniqueIdToElementId.TryGetValue(slab.UniqueId, out ElementId elemId);
                    if (doesSlabExist)
                    {
                        ApplyColorOverridesToElement(doc, viewPlan, elemId, slab.MaterialHex, slab.Storey < viewProperties.Storey);
                    }
                });
            }
            else
            {
                viewPlan.DisplayStyle = DisplayStyle.HLR;
            }
        }

        public static void ApplyColorOverridesToElement(Document doc, View view, ElementId elemId, string materialHex, bool halfTone)
        {
            try
            {
                string hex = materialHex != null ? materialHex.Replace("#", "") : "ffffff";
                byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

                Color color = new Color(r, g, b);

                OverrideGraphicSettings overrideGraphicSettings = new OverrideGraphicSettings();
                overrideGraphicSettings.SetCutForegroundPatternColor(color);
                overrideGraphicSettings.SetCutForegroundPatternId(Utils.GetSolidFillPatternElement(doc).Id);
                overrideGraphicSettings.SetCutForegroundPatternVisible(true);

                overrideGraphicSettings.SetSurfaceForegroundPatternColor(color);
                overrideGraphicSettings.SetSurfaceForegroundPatternId(Utils.GetSolidFillPatternElement(doc).Id);
                overrideGraphicSettings.SetSurfaceForegroundPatternVisible(true);

                overrideGraphicSettings.SetHalftone(halfTone);

                view.SetElementOverrides(elemId, overrideGraphicSettings);
            } catch
            {
                Utils.LogTrace("Failed to apply color overrides for color " + materialHex);
            }
        }

        public static void HideHiddenViewElements(Document doc, ViewPlan viewPlan, ViewProperties viewProperties)
        {
            List<ElementId> elementsToHide = viewProperties.Elements.HiddenIds.Select(id =>
            {
                bool elementExists = GlobalVariables.UniqueIdToElementId.TryGetValue(id, out ElementId elementId);
                if (elementExists)
                {
                    return elementId;
                }
                else
                {
                    return null;
                }
            }).Where(elementId => elementId != null).Where(elementId => !doc.GetElement(elementId).IsHidden(viewPlan)).ToList();
            if (elementsToHide.Count > 0)
            {
                viewPlan.HideElements(elementsToHide);
            }

            List<ElementId> roomsToHide = viewProperties.Elements.HiddenIds.Select(id =>
            {
                bool elementExists = GlobalVariables.UniqueIdToRoomId.TryGetValue(id, out ElementId elementId);
                if (elementExists)
                {
                    return elementId;
                }
                else
                {
                    return null;
                }
            }).Where(elementId => elementId != null).Where(elementId => !doc.GetElement(elementId).IsHidden(viewPlan)).ToList();
            if (roomsToHide.Count > 0)
            {
                viewPlan.HideElements(roomsToHide);
            }
        }

        public static string GetRoomTagTypeFamilyName(ViewProperties viewProperties)
        {
            string area = viewProperties.Label.Selected.Any(value => value == LabelsEnum.areas) ? "+Area" : "";
            switch (viewProperties.Sheet.Scale)
            {
                case 50:
                case 64:
                    return "Room Tag" + area + "_Snaptrude_3-16";

                case 100:
                case 96:
                    return "Room Tag" + area + "_Snaptrude_1-8";

                case 150:
                case 128:
                    return "Room Tag" + area + "_Snaptrude_3-32";

                case 200:
                case 192:
                    return "Room Tag" + area + "_Snaptrude_1-16";
            }

            return "Room Tag" + area + "_Snaptrude_1-8";
        }

        public static void SetCropBoxToFitPaperSize(Document doc, ViewPlan view, XYZ min, XYZ max, ViewProperties viewProperties, TrudeProperties trudeProperties)
        {
            SheetSettings sheetSettings = viewProperties.Sheet;
            BoundingBoxXYZ cropBox = new BoundingBoxXYZ();
            UV PDFPaddingX = GlobalVariables.PDFPaddingX;
            UV PDFPaddingY = GlobalVariables.PDFPaddingY;

            double height_delta = ((max.Y - min.Y) - GetSheetSize(sheetSettings.SheetSize).U * view.Scale) / 2;

            cropBox.Min = new XYZ(min.X + PDFPaddingX.U * view.Scale, min.Y + height_delta + PDFPaddingY.U * view.Scale, cropBox.Min.Z);
            cropBox.Max = new XYZ(max.X - PDFPaddingX.V * view.Scale, max.Y - height_delta - PDFPaddingY.V * view.Scale, cropBox.Max.Z);

            view.CropBox = cropBox;
            view.CropBoxActive = true;
            view.CropBoxVisible = false;
            List<ElementId> roomsToHide = trudeProperties.Masses
                .Select(mass =>
                {
                    bool doesRoomExist = GlobalVariables.UniqueIdToRoomId.TryGetValue(mass.UniqueId, out ElementId elementId);
                    if (doesRoomExist &&
                        !Utils.IsElementInsideBoundingBox(doc.GetElement(elementId), cropBox) &&
                        !doc.GetElement(elementId).IsHidden(view))
                    {
                        return elementId;
                    }
                    return null;
                })
                .Where(elementId => elementId != null)
                .ToList();
            if (roomsToHide.Count > 0)
            {
                view.HideElements(roomsToHide);
            }
        }

        public static UV GetSheetSize(SheetSizeEnum size)
        {
            switch (size)
            {
                case SheetSizeEnum.ANSI_A: return new UV(0.708, 0.917); // 8.5 x 11 inches
                case SheetSizeEnum.ANSI_B: return new UV(0.917, 1.417); // 11 x 17 inches
                case SheetSizeEnum.ANSI_C: return new UV(1.417, 1.833); // 17 x 22 inches
                case SheetSizeEnum.ANSI_D: return new UV(1.833, 2.833); // 22 x 34 inches
                case SheetSizeEnum.ANSI_E: return new UV(2.833, 3.666); // 34 x 44 inches
                case SheetSizeEnum.ISO_A0: return new UV(2.759, 3.901); // 841 x 1189 mm
                case SheetSizeEnum.ISO_A1: return new UV(1.949, 2.759); // 594 x 841 mm
                case SheetSizeEnum.ISO_A2: return new UV(1.378, 1.949); // 420 x 594 mm
                case SheetSizeEnum.ISO_A3: return new UV(0.974, 1.378); // 297 x 420 mm
                case SheetSizeEnum.ISO_A4: return new UV(0.689, 0.974); // 210 x 297 mm
                case SheetSizeEnum.ARCH_A: return new UV(0.75, 1.0); // 9 x 12 inches
                case SheetSizeEnum.ARCH_B: return new UV(1.0, 1.5); // 12 x 18 inches
                case SheetSizeEnum.ARCH_C: return new UV(1.5, 2.0); // 18 x 24 inches
                case SheetSizeEnum.ARCH_D: return new UV(2.0, 3.0); // 24 x 36 inches
                case SheetSizeEnum.ARCH_E: return new UV(3.0, 4.0); // 36 x 48 inches
                case SheetSizeEnum.ARCH_E1: return new UV(2.5, 3.5); // 30 x 42 inches
                default: throw new ArgumentOutOfRangeException(nameof(size), size, null);
            }
        }

    }
}
