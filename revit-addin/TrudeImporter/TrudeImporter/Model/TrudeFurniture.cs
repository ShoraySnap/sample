using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace TrudeImporter
{
    internal class TrudeFurniture : TrudeModel
    {
        readonly FurnitureProperties FurnitureProps;

        public TrudeFurniture(FurnitureProperties furnitureProps, List<ElementId> sourcesIdsToDelete, int index)
        {
            double familyRotation = 0;
            bool isFacingFlip = false;
            string familyType = null;
            int sourceElementId = 0;
            int? revitId = null;

            XYZ localOriginOffset = XYZ.Zero;

            string revitFamilyName = furnitureProps.RevitFamilyName;
            Family family = new FilteredElementCollector(GlobalVariables.Document)
                .OfClass(typeof(Family))
                .Cast<Family>()
                .FirstOrDefault(f => f.Name == furnitureProps.RevitFamilyName);
            FamilySymbol familySymbol = null;
            if (family != null)
            {
                familySymbol = new FilteredElementCollector(GlobalVariables.Document)
                    .OfClass(typeof(FamilySymbol))
                    .Cast<FamilySymbol>()
                    .Where(f => f.FamilyName == furnitureProps.RevitFamilyName)
                    .FirstOrDefault(f => f.Name == furnitureProps.RevitFamilyType);
            }

            try
            {
                if (furnitureProps.Offset != null)
                    localOriginOffset = furnitureProps.Offset;

                if (furnitureProps.FamilyRotation != null)
                    familyRotation = (double)furnitureProps.FamilyRotation;

                if (furnitureProps.FacingFlipped != null)
                    isFacingFlip = (bool)furnitureProps.FacingFlipped;

                if (furnitureProps.RevitFamilyType != null)
                    familyType = (string)furnitureProps.RevitFamilyType;

                if (furnitureProps.SourceElementId != null)
                    sourceElementId = (int)furnitureProps.SourceElementId;

                if (furnitureProps.ExistingElementId != null)
                    revitId = (int)furnitureProps.ExistingElementId;
            }
            catch
            {

            }


            try
            {
                bool isExistingFurniture = false;
                FamilyInstance existingFamilyInstance = null;
                AssemblyInstance existingAssemblyInstance = null;
                Group existingGroup = null;
                if (revitId == null)
                {
                    revitId = sourceElementId;
                }
                using (SubTransaction trans = new SubTransaction(GlobalVariables.Document))
                {
                    trans.Start();
                    try
                    {
                        Element e = GlobalVariables.Document.GetElement(new ElementId((int)revitId));

                        if (e != null && e.IsValidObject)
                        {
                            isExistingFurniture = true;
                            if (e.GetType().Name == "AssemblyInstance")
                            {
                                existingAssemblyInstance = (AssemblyInstance)e;
                            }
                            else if (e.GetType().Name == "Group")
                            {
                                existingGroup = (Group)e;
                            }
                            else
                            {
                                existingFamilyInstance = (FamilyInstance)e;
                                isFacingFlip = (existingFamilyInstance).FacingFlipped;
                            }

                            trans.Commit();
                        }
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("Furniture creation ERROR", e.ToString());
                    }
                }
                using (SubTransaction trans = new SubTransaction(GlobalVariables.Document))
                {
                    trans.Start();

                    // Creation ...................
                    TrudeInterior st_interior = new TrudeInterior(furnitureProps);

                    if (familySymbol != null && familySymbol.IsValidObject)
                    {
                        Parameter offsetParam = st_interior.GetOffsetParameter(existingFamilyInstance);
                        if (familySymbol.Category.Name == "Casework" && offsetParam == null)
                        {
                            BoundingBoxXYZ bbox = existingFamilyInstance.get_BoundingBox(null);

                            XYZ existingInstanceCenter = (bbox.Max + bbox.Min).Divide(2);

                            ElementId newId = ElementTransformUtils.CopyElement(GlobalVariables.Document, existingFamilyInstance.Id, existingInstanceCenter.Multiply(-1)).First();

                            st_interior.element = GlobalVariables.Document.GetElement(newId);

                            BoundingBoxXYZ bboxNew = st_interior.element.get_BoundingBox(null);
                            XYZ centerNew = (bboxNew.Max + bboxNew.Min).Divide(2);

                            double originalRotation = ((LocationPoint)existingFamilyInstance.Location).Rotation;

                            LocationPoint pt = (LocationPoint)st_interior.element.Location;
                            ElementTransformUtils.RotateElement(GlobalVariables.Document, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -originalRotation);

                            ElementTransformUtils.RotateElement(GlobalVariables.Document, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -st_interior.Rotation.Z);

                            ElementTransformUtils.MoveElement(GlobalVariables.Document, newId, st_interior.Position);

                            BoundingBoxXYZ bboxAfterMove = st_interior.element.get_BoundingBox(null);
                            XYZ centerAfterMove = (bboxAfterMove.Max + bboxAfterMove.Min).Divide(2);

                            if (isFacingFlip)
                            {
                                XYZ normal = (st_interior.element as FamilyInstance).FacingOrientation;
                                XYZ origin = (st_interior.element.Location as LocationPoint).Point;
                                Plane pl = Plane.CreateByNormalAndOrigin(normal, centerAfterMove);
                                var ids = ElementTransformUtils.MirrorElements(GlobalVariables.Document, new List<ElementId>() { newId }, pl, false);
                            }

                            if (st_interior.Scaling.Z < 0)
                            {
                                st_interior.SnaptrudeFlip(st_interior.element, centerAfterMove);
                            }
                        }
                        else
                        {
                            ElementId levelId = GlobalVariables.LevelIdByNumber[st_interior.levelNumber];
                            Level level = (Level)GlobalVariables.Document.GetElement(levelId);
                            st_interior.CreateWithFamilySymbol(familySymbol, level, familyRotation, isFacingFlip, localOriginOffset);
                        }
                    }
                    else if (family != null)
                    {
                        if (familySymbol?.Category?.Name == "Casework")
                        {
                            XYZ originalPoint = ((LocationPoint)existingFamilyInstance.Location).Point;

                            BoundingBoxXYZ bbox = existingFamilyInstance.get_BoundingBox(null);

                            XYZ center = (bbox.Max + bbox.Min).Divide(2);

                            ElementId newId = ElementTransformUtils.CopyElement(GlobalVariables.Document, existingFamilyInstance.Id, center.Multiply(-1)).First();

                            st_interior.element = GlobalVariables.Document.GetElement(newId);

                            BoundingBoxXYZ bboxNew = st_interior.element.get_BoundingBox(null);
                            XYZ centerNew = (bboxNew.Max + bboxNew.Min).Divide(2);

                            double originalRotation = ((LocationPoint)existingFamilyInstance.Location).Rotation;

                            LocationPoint pt = (LocationPoint)st_interior.element.Location;
                            ElementTransformUtils.RotateElement(GlobalVariables.Document, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -originalRotation);

                            ElementTransformUtils.RotateElement(GlobalVariables.Document, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -st_interior.Rotation.Z);

                            ElementTransformUtils.MoveElement(GlobalVariables.Document, newId, st_interior.Position);

                            BoundingBoxXYZ bboxAfterMove = st_interior.element.get_BoundingBox(null);
                            XYZ centerAfterMove = (bboxAfterMove.Max + bboxAfterMove.Min).Divide(2);

                            if (isFacingFlip)
                            {

                                XYZ normal = (st_interior.element as FamilyInstance).FacingOrientation;
                                XYZ origin = (st_interior.element.Location as LocationPoint).Point;
                                Plane pl = Plane.CreateByNormalAndOrigin(normal, centerAfterMove);
                                var ids = ElementTransformUtils.MirrorElements(GlobalVariables.Document, new List<ElementId>() { newId }, pl, false);
                            }

                            if (st_interior.Scaling.Z < 0)
                            {
                                st_interior.SnaptrudeFlip(st_interior.element, centerAfterMove);
                            }
                        }
                        else
                        {
                            FamilySymbol defaultFamilySymbol = TrudeModel.GetFamilySymbolByName(GlobalVariables.Document, revitFamilyName, familyType);
                            if (defaultFamilySymbol is null)
                            {
                                defaultFamilySymbol = TrudeModel.GetFamilySymbolByName(GlobalVariables.Document, revitFamilyName);
                            }
                            if (!defaultFamilySymbol.IsActive)
                            {
                                defaultFamilySymbol.Activate();
                                GlobalVariables.Document.Regenerate();
                            }
                            ElementId levelId = GlobalVariables.LevelIdByNumber[st_interior.levelNumber];
                            Level level = (Level)GlobalVariables.Document.GetElement(levelId);

                            st_interior.CreateWithFamilySymbol(defaultFamilySymbol, level, familyRotation, isFacingFlip, localOriginOffset);
                        }
                    }
                    else if (existingAssemblyInstance != null)
                    {
                        XYZ originalPoint = ((LocationPoint)existingAssemblyInstance.Location).Point;

                        BoundingBoxXYZ bbox = existingAssemblyInstance.get_BoundingBox(null);

                        //XYZ center = (bbox.Max + bbox.Min).Divide(2);
                        XYZ center = ((LocationPoint)existingAssemblyInstance.Location).Point;

                        ElementId newId = ElementTransformUtils.CopyElement(GlobalVariables.Document, existingAssemblyInstance.Id, center.Multiply(-1)).First();

                        st_interior.element = GlobalVariables.Document.GetElement(newId);

                        BoundingBoxXYZ bboxNew = st_interior.element.get_BoundingBox(null);
                        //XYZ centerNew = (bboxNew.Max + bboxNew.Min).Divide(2);

                        //double originalRotation = ((LocationPoint)existingAssemblyInstance.Location).Rotation;

                        LocationPoint pt = (LocationPoint)st_interior.element.Location;
                        XYZ centerNew = pt.Point;
                        //ElementTransformUtils.RotateElement(GlobalVariables.Document, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -originalRotation);

                        ElementTransformUtils.RotateElement(GlobalVariables.Document, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -st_interior.Rotation.Z);

                        ElementTransformUtils.MoveElement(GlobalVariables.Document, newId, st_interior.Position);

                        BoundingBoxXYZ bboxAfterMove = st_interior.element.get_BoundingBox(null);
                        //XYZ centerAfterMove = (bboxAfterMove.Max + bboxAfterMove.Min).Divide(2);
                        XYZ centerAfterMove = ((LocationPoint)st_interior.element.Location).Point;

                        if (isFacingFlip)
                        {

                            XYZ normal = (st_interior.element as FamilyInstance).FacingOrientation;
                            XYZ origin = (st_interior.element.Location as LocationPoint).Point;
                            Plane pl = Plane.CreateByNormalAndOrigin(normal, centerAfterMove);
                            var ids = ElementTransformUtils.MirrorElements(GlobalVariables.Document, new List<ElementId>() { newId }, pl, false);
                        }

                        if (st_interior.Scaling.Z < 0)
                        {
                            st_interior.SnaptrudeFlip(st_interior.element, centerAfterMove);
                        }
                    }
                    else if (existingGroup != null)
                    {
                        XYZ originalPoint = ((LocationPoint)existingGroup.Location).Point;

                        BoundingBoxXYZ bbox = existingGroup.get_BoundingBox(null);

                        XYZ center = (bbox.Max + bbox.Min).Divide(2);
                        //XYZ center = ((LocationPoint)existingGroup.Location).Point;

                        ElementId newId = ElementTransformUtils.CopyElement(GlobalVariables.Document, existingGroup.Id, center.Multiply(-1)).First();

                        st_interior.element = GlobalVariables.Document.GetElement(newId);

                        //BoundingBoxXYZ bboxNew = st_interior.element.get_BoundingBox(null);
                        //XYZ centerNew = (bboxNew.Max + bboxNew.Min).Divide(2);

                        //double originalRotation = ((LocationPoint)existingAssemblyInstance.Location).Rotation;

                        // LocationPoint pt = (LocationPoint)st_interior.element.Location;
                        //XYZ centerNew = pt.Point;
                        //ElementTransformUtils.RotateElement(GlobalVariables.Document, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -originalRotation);
                        ElementTransformUtils.MoveElement(GlobalVariables.Document, newId, st_interior.Position);
                        GlobalVariables.Document.Regenerate();
                        BoundingBoxXYZ bboxNew = st_interior.element.get_BoundingBox(null);
                        XYZ centerNew = (bboxNew.Max + bboxNew.Min).Divide(2);

                        XYZ centerAfterMove = ((LocationPoint)st_interior.element.Location).Point;

                        ElementTransformUtils.RotateElement(
                                GlobalVariables.Document,
                                newId,
                                Line.CreateBound(centerNew, centerNew + XYZ.BasisZ),
                                -st_interior.Rotation.Z);


                        if (isFacingFlip)
                        {
                            XYZ normal = (st_interior.element as FamilyInstance).FacingOrientation;
                            XYZ origin = (st_interior.element.Location as LocationPoint).Point;
                            Plane pl = Plane.CreateByNormalAndOrigin(normal, centerAfterMove);
                            var ids = ElementTransformUtils.MirrorElements(GlobalVariables.Document, new List<ElementId>() { newId }, pl, false);
                        }

                        if (st_interior.Scaling.Z < 0)
                        {
                            st_interior.SnaptrudeFlip(st_interior.element, st_interior.Position);
                        }
                    }
                    else
                    {
                        //String familyName = st_interior.Name.RemoveIns();
                        String familyName = st_interior.FamilyName;
                        if (familyName is null) familyName = st_interior.FamilyTypeName;

                        FamilySymbol defaultFamilySymbol = TrudeModel.GetFamilySymbolByName(GlobalVariables.Document, familyName);
                        //FamilySymbol defaultFamilySymbol = ST_Abstract.GetFamilySymbolByName(GlobalVariables.Document, "Casework Assembly", "Casework 044");
                        if (defaultFamilySymbol is null)
                        {
                            Family loadedFamily = FamilyLoader.LoadCustomFamily(familyName, FamilyLoader.FamilyFolder.Furniture);
                            if (loadedFamily == null)
                            {
                                GlobalVariables.MissingFurnitureFamiliesCount[familyName] = GlobalVariables.MissingFurnitureFamiliesCount.ContainsKey(familyName) ?
                                    (true, GlobalVariables.MissingFurnitureFamiliesCount[familyName].NumberOfElements + 1, "") : (true, 1, "");
                                GlobalVariables.MissingFurnitureIndexes.Add(index);
                                System.Diagnostics.Debug.WriteLine("couln't find door family: " + familyName);
                                return;
                            }

                            defaultFamilySymbol = TrudeModel.GetFamilySymbolByName(GlobalVariables.Document, familyName);
                            if (defaultFamilySymbol == null)
                            {
                                defaultFamilySymbol = TrudeModel.GetFamilySymbolByName(GlobalVariables.Document, familyName.Replace("_", " "));
                            }
                        }

                        if (!defaultFamilySymbol.IsActive)
                        {
                            defaultFamilySymbol.Activate();
                            GlobalVariables.Document.Regenerate();
                        }
                        ElementId levelId;
                        if (GlobalVariables.LevelIdByNumber.ContainsKey(st_interior.levelNumber))
                            levelId = GlobalVariables.LevelIdByNumber[st_interior.levelNumber];
                        else
                            levelId = GlobalVariables.LevelIdByNumber.First().Value;
                        Level level = (Level)GlobalVariables.Document.GetElement(levelId);

                        st_interior.CreateWithFamilySymbol(defaultFamilySymbol, level, familyRotation, isFacingFlip, localOriginOffset);
                    }
                    if (st_interior.element is null)
                    {

                        //st_interior.CreateWithDirectShape(GlobalVariables.Document);
                    }

                    try
                    {
                        if (isExistingFurniture)
                        {
                            // delete original furniture
                            //if (existingFamilyInstance.IsValidObject) GlobalVariables.Document.Delete(existingFamilyInstance.Id);
                            if (existingFamilyInstance != null) sourcesIdsToDelete.Add(existingFamilyInstance.Id);
                            if (existingAssemblyInstance != null) sourcesIdsToDelete.Add(existingAssemblyInstance.Id);
                            if (existingGroup != null) sourcesIdsToDelete.Add(existingGroup.Id);
                        }
                    }
                    catch
                    {

                    }

                    TransactionStatus tstatus = trans.Commit();
                    System.Diagnostics.Debug.WriteLine(tstatus.ToString());
                }
                System.Diagnostics.Debug.WriteLine("furniture created");
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Furniture creation ERROR", e.ToString());
            }
        }
    }
}