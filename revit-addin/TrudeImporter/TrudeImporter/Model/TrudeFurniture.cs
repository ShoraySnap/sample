// Add a user discretion for when a door or window is floating in the air, that will not get reconciled.

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

        public TrudeFurniture(JToken furnitureData)
        {
            double familyRotation = 0;
            bool isFacingFlip = false;
            string familyType = null;
            string sourceElementId = null;

            XYZ localOriginOffset = XYZ.Zero;

            string revitFamilyName = (string)furnitureData["dsProps"]["revitMetaData"]["family"];

            try
            {
                if (!furnitureData["dsProps"]["revitMetaData"]["offset"].IsNullOrEmpty())
                    if (!furnitureData["dsProps"]["revitMetaData"]["offset"].First.IsNullOrEmpty())
                        localOriginOffset = TrudeRepository.ArrayToXYZ(furnitureData["dsProps"]["revitMetaData"]["offset"]);

                if (!furnitureData["dsProps"]["revitMetaData"]["familyRotation"].IsNullOrEmpty())
                    familyRotation = (double)furnitureData["dsProps"]["revitMetaData"]["familyRotation"];

                if (!furnitureData["dsProps"]["revitMetaData"]["facingFlipped"].IsNullOrEmpty())
                    isFacingFlip = (bool)furnitureData["dsProps"]["revitMetaData"]["facingFlipped"];

                if (!furnitureData["dsProps"]["revitMetaData"]["type"].IsNullOrEmpty())
                    familyType = (string)furnitureData["dsProps"]["revitMetaData"]["type"];

                if (!furnitureData["dsProps"]["revitMetaData"]["sourceElementId"].IsNullOrEmpty())
                    sourceElementId = (string)furnitureData["dsProps"]["revitMetaData"]["sourceElementId"];
            }
            catch
            {

            }


            try
            {
                if (IsThrowAway(furnitureData)) continue;


                string revitId = (string)furnitureData["dsProps"]["revitMetaData"]["elementId"];
                bool isExistingFurniture = false;
                FamilyInstance existingFamilyInstance = null;
                AssemblyInstance existingAssemblyInstance = null;
                Group existingGroup = null;
                FamilySymbol existingFamilySymbol = null;
                string existingFamilyType = "";

                if (revitId == null)
                {
                    revitId = sourceElementId;
                }


                if (revitId != null)
                {
                    using (SubTransaction trans = new SubTransaction(newDoc))
                    {
                        trans.Start();
                        try
                        {
                            Element e = newDoc.GetElement(new ElementId(int.Parse(revitId)));
                            isExistingFurniture = idToElement.TryGetValue(revitId, out Element _e);

                            if (isExistingFurniture || e.IsValidObject)
                            {
                                isExistingFurniture = true;
                                if (e.GetType().Name == "AssemblyInstance")
                                {
                                    existingAssemblyInstance = (AssemblyInstance)e;
                                    existingFamilyType = existingAssemblyInstance.Name;
                                }
                                else if (e.GetType().Name == "Group")
                                {
                                    existingGroup = (Group)e;
                                    existingFamilyType = existingGroup.Name;
                                }
                                else
                                {
                                    existingFamilyInstance = (FamilyInstance)e;
                                    existingFamilySymbol = idToFamilySymbol[revitId];
                                    existingFamilyType = existingFamilySymbol.Name;

                                    isFacingFlip = (existingFamilyInstance).FacingFlipped;
                                }


                                trans.Commit();
                            }
                        }
                        catch (Exception e)
                        {
                            LogTrace(e.Message);
                        }
                    }
                }
                using (SubTransaction trans = new SubTransaction(newDoc))
                {
                    trans.Start();

                    // Creation ...................
                    TrudeInterior st_interior = new TrudeInterior(furnitureData);

                    FamilySymbol familySymbol = null;
                    if (existingFamilySymbol != null && existingFamilySymbol.IsValidObject)
                    {
                        Parameter offsetParam = st_interior.GetOffsetParameter(existingFamilyInstance);
                        if (existingFamilySymbol.Category.Name == "Casework" && offsetParam == null)
                        {
                            BoundingBoxXYZ bbox = existingFamilyInstance.get_BoundingBox(null);

                            XYZ existingInstanceCenter = (bbox.Max + bbox.Min).Divide(2);

                            ElementId newId = ElementTransformUtils.CopyElement(newDoc, existingFamilyInstance.Id, existingInstanceCenter.Multiply(-1)).First();

                            st_interior.element = newDoc.GetElement(newId);

                            BoundingBoxXYZ bboxNew = st_interior.element.get_BoundingBox(null);
                            XYZ centerNew = (bboxNew.Max + bboxNew.Min).Divide(2);

                            double originalRotation = ((LocationPoint)existingFamilyInstance.Location).Rotation;

                            LocationPoint pt = (LocationPoint)st_interior.element.Location;
                            ElementTransformUtils.RotateElement(newDoc, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -originalRotation);

                            ElementTransformUtils.RotateElement(newDoc, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -st_interior.eulerAngles.heading);

                            ElementTransformUtils.MoveElement(newDoc, newId, st_interior.Position);

                            BoundingBoxXYZ bboxAfterMove = st_interior.element.get_BoundingBox(null);
                            XYZ centerAfterMove = (bboxAfterMove.Max + bboxAfterMove.Min).Divide(2);

                            if (isFacingFlip)
                            {

                                XYZ normal = (st_interior.element as FamilyInstance).FacingOrientation;
                                XYZ origin = (st_interior.element.Location as LocationPoint).Point;
                                Plane pl = Plane.CreateByNormalAndOrigin(normal, centerAfterMove);
                                var ids = ElementTransformUtils.MirrorElements(newDoc, new List<ElementId>() { newId }, pl, false);
                            }

                            if (st_interior.Scaling.Z < 0)
                            {
                                st_interior.SnaptrudeFlip(st_interior.element, centerAfterMove);
                            }
                        }
                        else
                        {
                            ElementId levelId = LevelIdByNumber[st_interior.levelNumber];
                            Level level = (Level)newDoc.GetElement(levelId);
                            st_interior.CreateWithFamilySymbol(existingFamilySymbol, level, familyRotation, isFacingFlip, localOriginOffset);
                        }
                    }
                    else if (revitFamilyName != null)
                    {
                        if (existingFamilySymbol?.Category?.Name == "Casework")
                        {
                            XYZ originalPoint = ((LocationPoint)existingFamilyInstance.Location).Point;

                            BoundingBoxXYZ bbox = existingFamilyInstance.get_BoundingBox(null);

                            XYZ center = (bbox.Max + bbox.Min).Divide(2);

                            ElementId newId = ElementTransformUtils.CopyElement(newDoc, existingFamilyInstance.Id, center.Multiply(-1)).First();

                            st_interior.element = newDoc.GetElement(newId);

                            BoundingBoxXYZ bboxNew = st_interior.element.get_BoundingBox(null);
                            XYZ centerNew = (bboxNew.Max + bboxNew.Min).Divide(2);

                            double originalRotation = ((LocationPoint)existingFamilyInstance.Location).Rotation;

                            LocationPoint pt = (LocationPoint)st_interior.element.Location;
                            ElementTransformUtils.RotateElement(newDoc, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -originalRotation);

                            ElementTransformUtils.RotateElement(newDoc, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -st_interior.eulerAngles.heading);

                            ElementTransformUtils.MoveElement(newDoc, newId, st_interior.Position);

                            BoundingBoxXYZ bboxAfterMove = st_interior.element.get_BoundingBox(null);
                            XYZ centerAfterMove = (bboxAfterMove.Max + bboxAfterMove.Min).Divide(2);

                            if (isFacingFlip)
                            {

                                XYZ normal = (st_interior.element as FamilyInstance).FacingOrientation;
                                XYZ origin = (st_interior.element.Location as LocationPoint).Point;
                                Plane pl = Plane.CreateByNormalAndOrigin(normal, centerAfterMove);
                                var ids = ElementTransformUtils.MirrorElements(newDoc, new List<ElementId>() { newId }, pl, false);
                            }

                            if (st_interior.Scaling.Z < 0)
                            {
                                st_interior.SnaptrudeFlip(st_interior.element, centerAfterMove);
                            }
                        }
                        else
                        {
                            FamilySymbol defaultFamilySymbol = TrudeModel.GetFamilySymbolByName(newDoc, revitFamilyName, familyType);
                            if (defaultFamilySymbol is null)
                            {
                                defaultFamilySymbol = TrudeModel.GetFamilySymbolByName(newDoc, revitFamilyName);
                            }
                            if (!defaultFamilySymbol.IsActive)
                            {
                                defaultFamilySymbol.Activate();
                                newDoc.Regenerate();
                            }
                            ElementId levelId = LevelIdByNumber[st_interior.levelNumber];
                            Level level = (Level)newDoc.GetElement(levelId);

                            st_interior.CreateWithFamilySymbol(defaultFamilySymbol, level, familyRotation, isFacingFlip, localOriginOffset);
                        }
                    }
                    else if (existingAssemblyInstance != null)
                    {
                        XYZ originalPoint = ((LocationPoint)existingAssemblyInstance.Location).Point;

                        BoundingBoxXYZ bbox = existingAssemblyInstance.get_BoundingBox(null);

                        //XYZ center = (bbox.Max + bbox.Min).Divide(2);
                        XYZ center = ((LocationPoint)existingAssemblyInstance.Location).Point;

                        ElementId newId = ElementTransformUtils.CopyElement(newDoc, existingAssemblyInstance.Id, center.Multiply(-1)).First();

                        st_interior.element = newDoc.GetElement(newId);

                        BoundingBoxXYZ bboxNew = st_interior.element.get_BoundingBox(null);
                        //XYZ centerNew = (bboxNew.Max + bboxNew.Min).Divide(2);

                        //double originalRotation = ((LocationPoint)existingAssemblyInstance.Location).Rotation;

                        LocationPoint pt = (LocationPoint)st_interior.element.Location;
                        XYZ centerNew = pt.Point;
                        //ElementTransformUtils.RotateElement(newDoc, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -originalRotation);

                        ElementTransformUtils.RotateElement(newDoc, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -st_interior.eulerAngles.heading);

                        ElementTransformUtils.MoveElement(newDoc, newId, st_interior.Position);

                        BoundingBoxXYZ bboxAfterMove = st_interior.element.get_BoundingBox(null);
                        //XYZ centerAfterMove = (bboxAfterMove.Max + bboxAfterMove.Min).Divide(2);
                        XYZ centerAfterMove = ((LocationPoint)st_interior.element.Location).Point;

                        if (isFacingFlip)
                        {

                            XYZ normal = (st_interior.element as FamilyInstance).FacingOrientation;
                            XYZ origin = (st_interior.element.Location as LocationPoint).Point;
                            Plane pl = Plane.CreateByNormalAndOrigin(normal, centerAfterMove);
                            var ids = ElementTransformUtils.MirrorElements(newDoc, new List<ElementId>() { newId }, pl, false);
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

                        //XYZ center = (bbox.Max + bbox.Min).Divide(2);
                        XYZ center = ((LocationPoint)existingGroup.Location).Point;

                        ElementId newId = ElementTransformUtils.CopyElement(newDoc, existingGroup.Id, center.Multiply(-1)).First();

                        st_interior.element = newDoc.GetElement(newId);

                        BoundingBoxXYZ bboxNew = st_interior.element.get_BoundingBox(null);
                        //XYZ centerNew = (bboxNew.Max + bboxNew.Min).Divide(2);

                        //double originalRotation = ((LocationPoint)existingAssemblyInstance.Location).Rotation;

                        LocationPoint pt = (LocationPoint)st_interior.element.Location;
                        XYZ centerNew = pt.Point;
                        //ElementTransformUtils.RotateElement(newDoc, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -originalRotation);
                        ElementTransformUtils.MoveElement(newDoc, newId, st_interior.Position - localOriginOffset);

                        XYZ centerAfterMove = ((LocationPoint)st_interior.element.Location).Point;

                        if (st_interior.Scaling.Z < 0)
                        {
                            ElementTransformUtils.RotateElement(
                                newDoc,
                                newId,
                                Line.CreateBound(st_interior.Position, st_interior.Position + XYZ.BasisZ),
                                st_interior.eulerAngles.heading);
                        }
                        else
                        {
                            ElementTransformUtils.RotateElement(
                                newDoc,
                                newId,
                                Line.CreateBound(st_interior.Position, st_interior.Position + XYZ.BasisZ),
                                -st_interior.eulerAngles.heading);
                        }


                        if (isFacingFlip)
                        {
                            XYZ normal = (st_interior.element as FamilyInstance).FacingOrientation;
                            XYZ origin = (st_interior.element.Location as LocationPoint).Point;
                            Plane pl = Plane.CreateByNormalAndOrigin(normal, centerAfterMove);
                            var ids = ElementTransformUtils.MirrorElements(newDoc, new List<ElementId>() { newId }, pl, false);
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

                        FamilySymbol defaultFamilySymbol = TrudeModel.GetFamilySymbolByName(newDoc, familyName);
                        //FamilySymbol defaultFamilySymbol = ST_Abstract.GetFamilySymbolByName(newDoc, "Casework Assembly", "Casework 044");
                        if (defaultFamilySymbol is null)
                        {
                            Family family = FamilyLoader.LoadCustomFamily(familyName);
                            defaultFamilySymbol = TrudeModel.GetFamilySymbolByName(newDoc, familyName);
                            if (defaultFamilySymbol == null)
                            {
                                defaultFamilySymbol = TrudeModel.GetFamilySymbolByName(newDoc, familyName.Replace("_", " "));
                            }
                        }

                        if (!defaultFamilySymbol.IsActive)
                        {
                            defaultFamilySymbol.Activate();
                            newDoc.Regenerate();
                        }
                        ElementId levelId;
                        if (LevelIdByNumber.ContainsKey(st_interior.levelNumber))
                            levelId = LevelIdByNumber[st_interior.levelNumber];
                        else
                            levelId = LevelIdByNumber.First().Value;
                        Level level = (Level)newDoc.GetElement(levelId);

                        st_interior.CreateWithFamilySymbol(defaultFamilySymbol, level, familyRotation, isFacingFlip, localOriginOffset);
                    }
                    if (st_interior.element is null)
                    {
                        st_interior.CreateWithDirectShape(newDoc);
                    }

                    try
                    {
                        if (isExistingFurniture)
                        {
                            // delete original furniture
                            //if (existingFamilyInstance.IsValidObject) newDoc.Delete(existingFamilyInstance.Id);
                            if (existingFamilyInstance != null) sourcesIdsToDelete.Add(existingFamilyInstance.Id);
                            if (existingAssemblyInstance != null) sourcesIdsToDelete.Add(existingAssemblyInstance.Id);
                            if (existingGroup != null) sourcesIdsToDelete.Add(existingGroup.Id);
                        }
                    }
                    catch
                    {

                    }

                    TransactionStatus tstatus = trans.Commit();
                    LogTrace(tstatus.ToString());
                }
                LogTrace("furniture created");
            }
            catch (OutOfMemoryException e)
            {
                LogTrace("furniture creation ERROR - out of memeroy -", e.ToString());
                break;
            }
            catch (Exception e)
            {
                LogTrace("furniture creation ERROR", e.ToString());
            }
        }
    }
}