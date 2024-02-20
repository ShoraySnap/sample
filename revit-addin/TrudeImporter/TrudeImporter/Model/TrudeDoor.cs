using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;


namespace TrudeImporter
{
    internal class TrudeDoor : TrudeModel
    {
        string doorFamilyName = null;
        string fsName = null;
        XYZ CenterPosition = null;
        public static DoorTypeStore TypeStore = new DoorTypeStore();

        public TrudeDoor(DoorProperties doorProps, ElementId levelId, int index)
        {
            XYZ direction = doorProps.Direction == null
                                ? XYZ.Zero
                                : doorProps.Direction;
            CenterPosition = doorProps.CenterPosition;
            try
            {
                if (doorProps.RevitFamilyName != null)
                {
                    doorFamilyName = doorProps.RevitFamilyName;
                }
                else
                {
                    doorFamilyName = doorProps.Name.RemoveIns();
                    fsName = doorFamilyName;
                }

                //getting wall to add door to
                Wall wall = null;
                if (GlobalVariables.childUniqueIdToWallElementId.ContainsKey(doorProps.UniqueId))
                {
                    ElementId wallElementId = GlobalVariables.childUniqueIdToWallElementId[doorProps.UniqueId];
                    wall = (Wall)GlobalVariables.Document.GetElement(wallElementId);
                }

                FamilySymbol familySymbol = null;
                FamilySymbol defaultFamilySymbol = null;
                if (doorProps.ExistingElementId == null)
                {
                    if (doorProps.RevitFamilyName == null)
                    {
                        var family = FamilyLoader.LoadCustomDoorFamily(doorFamilyName);
                        if (family is null)
                        {
                            GlobalVariables.MissingDoorFamiliesCount[doorFamilyName] = GlobalVariables.MissingDoorFamiliesCount.ContainsKey(doorFamilyName) ? 
                                (true, GlobalVariables.MissingDoorFamiliesCount[doorFamilyName].NumberOfElements + 1) : (true, 1);
                            System.Diagnostics.Debug.WriteLine("couln't find door family: " + doorFamilyName);
                            return;
                        }
                    }
                }

                defaultFamilySymbol = GetFamilySymbolByName(GlobalVariables.Document, doorFamilyName, fsName);
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
                    familySymbol = TypeStore.GetType(new double[] { doorProps.Height, doorProps.Width }, defaultFamilySymbol);
                }
                else
                {
                    familySymbol = defaultFamilySymbol;
                }

                var instance = CreateDoor(familySymbol, levelId, wall, direction);

                (Parameter widthInstanceParam, Parameter heightInstanceParam) = instance.FindWidthAndHeightParameters();
                if (!setHeightAndWidthParamsInFamilySymbol)
                {
                    heightInstanceParam.Set(doorProps.Height);
                    widthInstanceParam.Set(doorProps.Width);
                }
                if (heightTypeParam.IsReadOnly) heightInstanceParam.Set(doorProps.Height);
                if (widthTypeParam.IsReadOnly) widthInstanceParam.Set(doorProps.Width);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"No door with name {doorProps.RevitFamilyName}, UniqueId: {doorProps.UniqueId}\n", e.Message);
            }
        }

        private FamilyInstance CreateDoor(FamilySymbol familySymbol, ElementId levelId, Wall wall, XYZ direction)
        {
            FamilyInstance instance;
            var doc = GlobalVariables.Document;
            Level level = doc.GetElement(levelId) as Level;

            XYZ xyz = new XYZ(CenterPosition.X, CenterPosition.Y, 0.0);

            if (wall is null)
            {
                wall = GetProximateWall(xyz, doc, level.Id);
            }

            BoundingBoxXYZ bbox = wall.get_BoundingBox(null);
            XYZ loc = new XYZ(CenterPosition.X, CenterPosition.Y, bbox.Min.Z);

            instance = doc.Create.NewFamilyInstance(loc, familySymbol, wall, (Level)doc.GetElement(wall.LevelId), StructuralType.NonStructural);

            // Done to make sure door is cutting the wall
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