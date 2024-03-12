// Add a user discretion for when a door or window is floating in the air, that will not get reconciled.

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;

namespace TrudeImporter
{
    internal class TrudeWindow : TrudeModel
    {
        string doorFamilyName = null;
        string fsName = null;
        FamilySymbol existingFamilySymbol = null;
        XYZ CenterPosition = null;
        float height = 0;
        public static WindowTypeStore TypeStore = new WindowTypeStore();

        public TrudeWindow(WindowProperties windowProps, ElementId levelId, int index)
        {
            XYZ direction = windowProps.Direction == null
                                ? XYZ.Zero
                                : windowProps.Direction;
            CenterPosition = windowProps.CenterPosition;
            height = windowProps.Height;
            try
            {
                if (windowProps.RevitFamilyName != null)
                {
                    doorFamilyName = windowProps.RevitFamilyName;
                }
                else
                {
                    doorFamilyName = windowProps.Name.RemoveIns();
                    fsName = doorFamilyName;
                }

                //getting wall to add window to
                Wall wall = null;
                if (GlobalVariables.childUniqueIdToWallElementId.ContainsKey(windowProps.UniqueId))
                {
                    ElementId wallElementId = GlobalVariables.childUniqueIdToWallElementId[windowProps.UniqueId];
                    wall = (Wall)GlobalVariables.Document.GetElement(wallElementId);
                }

                FamilySymbol familySymbol = null;
                FamilySymbol defaultFamilySymbol = null;
                if (windowProps.ExistingElementId == null)
                {
                    if (windowProps.RevitFamilyName is null)
                    {
                        var family = FamilyLoader.LoadCustomFamily(doorFamilyName, FamilyLoader.FamilyFolder.Windows);
                        if (family is null)
                        {
                            GlobalVariables.MissingWindowFamiliesCount[doorFamilyName] = GlobalVariables.MissingWindowFamiliesCount.ContainsKey(doorFamilyName) ?
                                (true, GlobalVariables.MissingWindowFamiliesCount[doorFamilyName].NumberOfElements + 1, "") : (true, 1, "");
                            GlobalVariables.MissingWindowIndexes.Add(index);
                            System.Diagnostics.Debug.WriteLine("couln't find window family");
                            return;
                        }
                    }
                }

                defaultFamilySymbol = TrudeModel.GetFamilySymbolByName(GlobalVariables.Document, doorFamilyName, fsName);
                if (!defaultFamilySymbol.IsActive)
                {
                    defaultFamilySymbol.Activate();
                    GlobalVariables.Document.Regenerate();
                }
                // Check if familySymbol BuiltInParameter.DOOR_HEIGHT and  BuiltInParameter.DOOR_WIDTH
                // if so, then set the height and with in the familySymbol itself, otherwise find the correct
                // parameter in the instance.

                Parameter heightTypeParam = defaultFamilySymbol.get_Parameter(BuiltInParameter.DOOR_HEIGHT);
                Parameter widthTypeParam = defaultFamilySymbol.get_Parameter(BuiltInParameter.DOOR_WIDTH);

                bool setHeightAndWidthParamsInFamilySymbol = (heightTypeParam.HasValue && widthTypeParam.HasValue) && (!heightTypeParam.IsReadOnly || !widthTypeParam.IsReadOnly);
                if (setHeightAndWidthParamsInFamilySymbol)
                {
                    familySymbol = TypeStore.GetType(new double[] { windowProps.Height, windowProps.Width }, defaultFamilySymbol);
                }
                else
                {
                    familySymbol = defaultFamilySymbol;
                }

                var instance = CreateWindow(familySymbol, levelId, wall, direction);

                (Parameter widthInstanceParam, Parameter heightInstanceParam) = instance.FindWidthAndHeightParameters();
                if (!setHeightAndWidthParamsInFamilySymbol)
                {
                    heightInstanceParam.Set(windowProps.Height);
                    widthInstanceParam.Set(windowProps.Width);
                }
                if (heightTypeParam.IsReadOnly) heightInstanceParam.Set(windowProps.Height);
                if (widthTypeParam.IsReadOnly) widthInstanceParam.Set(windowProps.Width);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"No window with name {windowProps.RevitFamilyName} , UniqueId: {windowProps.UniqueId}\n", e.Message);
            }
        }

        private FamilyInstance CreateWindow(FamilySymbol familySymbol, ElementId levelId, Wall wall, XYZ direction)
        {
            FamilyInstance instance;
            var doc = GlobalVariables.Document;
            Level level = doc.GetElement(levelId) as Level;

            XYZ xyz = new XYZ(CenterPosition.X, CenterPosition.Y, CenterPosition.Z - height / 2);

            if (wall is null)
            {
                wall = GetProximateWall(xyz, doc, level.Id);
            }

            BoundingBoxXYZ bbox = wall.get_BoundingBox(null);
            XYZ loc = new XYZ(CenterPosition.X, CenterPosition.Y, CenterPosition.Z - height / 2);

            instance = doc.Create.NewFamilyInstance(loc, familySymbol, wall, (Level)doc.GetElement(wall.LevelId), StructuralType.NonStructural);

            // Done to make sure window is cutting the wall
            // See https://forums.autodesk.com/t5/revit-api-forum/create-doors-but-not-cutting-through-wall/td-p/5564330
            instance.flipFacing();
            doc.Regenerate();

            instance.flipFacing();
            doc.Regenerate();

            if (!instance.FacingOrientation.IsAlmostEqualTo(direction))
            {
                instance.flipFacing();
                instance.flipHand();
            }

            return instance;
        }
    }
}