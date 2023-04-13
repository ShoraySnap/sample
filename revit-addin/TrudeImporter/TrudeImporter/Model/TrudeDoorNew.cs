using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;

namespace TrudeImporter
{
    internal class TrudeDoorNew : TrudeModel
    {
        string doorFamilyName = null;
        string fsName = null;
        FamilySymbol existingFamilySymbol = null;
        XYZ CenterPosition = null;
        public TrudeDoorNew(DoorProperties door, ElementId levelId)
        {
            XYZ direction = door.Direction == null
                                ? XYZ.Zero
                                : door.Direction;
            CenterPosition = door.CenterPosition;
            try
            {
                if (door.RevitFamilyName != null)
                {
                    doorFamilyName = door.RevitFamilyName;
                    existingFamilySymbol = GlobalVariables.idToFamilySymbol[door.ExistingElementId.ToString()];
                }
                else
                {
                    doorFamilyName = door.Name.RemoveIns();
                    fsName = doorFamilyName;
                }

                //getting wall to add door to
                Wall wall = null;
                if (GlobalVariables.childUniqueIdToWallElementId.ContainsKey(door.UniqueId))
                {
                    ElementId wallElementId = GlobalVariables.childUniqueIdToWallElementId[door.UniqueId];
                    wall = (Wall)GlobalVariables.Document.GetElement(wallElementId);
                }

                FamilySymbol familySymbol = null;
                FamilySymbol defaultFamilySymbol = null;
                if (door.ExistingElementId != null)
                {
                    defaultFamilySymbol = existingFamilySymbol;
                    if (!defaultFamilySymbol.IsActive)
                    {
                        defaultFamilySymbol.Activate();
                        GlobalVariables.Document.Regenerate();
                    }
                }
                else
                {
                    if (door.RevitFamilyName is null)
                    {
                        var family = FamilyLoader.LoadCustomDoorFamily(doorFamilyName);
                        if (family is null)
                        {
                            System.Diagnostics.Debug.WriteLine("couln't find door family");
                            return;
                        }
                    }
                    defaultFamilySymbol = TrudeModel.GetFamilySymbolByName(GlobalVariables.Document, doorFamilyName, fsName);
                }

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
                    familySymbol = TrudeDoor.TypeStore.GetType(new double[] { door.Height, door.Width}, defaultFamilySymbol);
                }
                else
                {
                    familySymbol = defaultFamilySymbol;
                }

                var instance = CreateDoor(familySymbol, levelId, wall, direction);

                (Parameter widthInstanceParam, Parameter heightInstanceParam) = instance.FindWidthAndHeightParameters();
                if (!setHeightAndWidthParamsInFamilySymbol)
                {
                    heightInstanceParam.Set(door.Height);
                    widthInstanceParam.Set(door.Width);
                }
                if (heightTypeParam.IsReadOnly) heightInstanceParam.Set(door.Height);
                if (widthTypeParam.IsReadOnly) widthInstanceParam.Set(door.Width);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"No door with name {door.RevitFamilyName} {door.Name}\n", e.Message);
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