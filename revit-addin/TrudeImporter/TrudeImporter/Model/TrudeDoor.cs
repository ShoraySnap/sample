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
        double Height;
        XYZ CenterPosition = null;
        XYZ Direction = null;
        bool HandFlipped = false;
        public static DoorTypeStore TypeStore = new DoorTypeStore();

        public TrudeDoor(DoorProperties doorProps, ElementId levelId, int index)
        {
            System.Diagnostics.Debug.WriteLine("Creating door: " + doorProps.Name);
            CenterPosition = doorProps.CenterPosition;
            Direction = doorProps.Direction;
            HandFlipped = doorProps.HandFlipped;
            Height = doorProps.Height;
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
                        var family = FamilyLoader.LoadCustomFamily(doorFamilyName, FamilyLoader.FamilyFolder.Doors);
                        if (family is null)
                        {
                            GlobalVariables.MissingDoorFamiliesCount[doorFamilyName] = GlobalVariables.MissingDoorFamiliesCount.ContainsKey(doorFamilyName) ? 
                                (true, GlobalVariables.MissingDoorFamiliesCount[doorFamilyName].NumberOfElements + 1,"") : (true, 1,"");
                            GlobalVariables.MissingDoorIndexes.Add(index);
                            System.Diagnostics.Debug.WriteLine("couln't find door family: " + doorFamilyName);
                            return;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Door family loaded: " + doorFamilyName);
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

                var instance = CreateDoor(familySymbol, levelId, wall);

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

        private FamilyInstance CreateDoor(FamilySymbol familySymbol, ElementId levelId, Wall wall)
        {
            FamilyInstance instance;
            var doc = GlobalVariables.Document;
            Level level = doc.GetElement(levelId) as Level;

            XYZ xyz = new XYZ(CenterPosition.X, CenterPosition.Y, CenterPosition.Z - Height / 2);

            if (wall is null)
            {
                wall = GetProximateWall(xyz, doc);
            }

            instance = doc.Create.NewFamilyInstance(xyz, familySymbol, wall, level, StructuralType.NonStructural);
            // Done to make sure door is cutting the wall
            // See https://forums.autodesk.com/t5/revit-api-forum/create-doors-but-not-cutting-through-wall/td-p/5564330
            instance.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set("a");
            doc.Regenerate();
            instance.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set("");

            if (!instance.FacingOrientation.IsAlmostEqualTo(Direction,0.01))
                    instance.flipFacing();

            bool isFlipped = (instance.FacingFlipped && !instance.HandFlipped) ||
                (!instance.FacingFlipped && instance.HandFlipped);
            if (HandFlipped != isFlipped)
                instance.flipHand();

            System.Diagnostics.Debug.WriteLine("Door created: " + instance.Id);
            return instance;
        }
    }
}