// Add a user discretion for when a door or window is floating in the air, that will not get reconciled.

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace TrudeImporter
{
    internal class TrudeFurnitureNew : TrudeModel
    {
        readonly FurnitureProperties FurnitureProps;

        public TrudeFurnitureNew(FurnitureProperties furnitureProps)
        {
            FurnitureProps = furnitureProps;

            try
            {
                Element existingElement = null;

                if (furnitureProps.ExistingElementId != null)
                {
                    existingElement = GlobalVariables.Document.GetElement(new ElementId((int)furnitureProps.ExistingElementId));

                    if (existingElement != null && existingElement.IsValidObject)
                    {
                        // Check if its a family and if exists in model
                        if (furnitureProps.RevitFamilyName != null)
                        {
                            Family family = new FilteredElementCollector(GlobalVariables.Document)
                                .OfClass(typeof(Family))
                                .Cast<Family>()
                                .FirstOrDefault(fs => fs.Name == furnitureProps.RevitFamilyName);

                            if (family == null)
                            {
                                // USE UI
                                return;
                            }
                        }

                        XYZ originalPoint = ((LocationPoint)existingElement.Location).Point;
                        BoundingBoxXYZ bbox = existingElement.get_BoundingBox(null);
                        XYZ center = (bbox.Max + bbox.Min).Divide(2);
                        double originalRotation = ((LocationPoint)existingElement.Location).Rotation;

                        // Check type

                        if (existingElement is FamilyInstance existingFamilyInstance)
                        {
                            bool isExistingFacingFlipped = existingFamilyInstance.FacingFlipped;
                            ElementType elementType = new FilteredElementCollector(GlobalVariables.Document)
                                .OfClass(typeof(FamilySymbol))
                                .Cast<FamilySymbol>()
                                .Where(fs => fs.FamilyName == furnitureProps.RevitFamilyName)
                                .FirstOrDefault(fs => fs.Name == furnitureProps.RevitFamilyType);

                            if (elementType == null)
                            {
                                // Pop UI or use the one from the instance (or both)?
                                return;
                            }

                            // Check family and type
                            if (existingFamilyInstance.Symbol.FamilyName != furnitureProps.RevitFamilyName ||
                                existingFamilyInstance.Symbol.Name != furnitureProps.RevitFamilyType)
                            {
                                existingFamilyInstance.ChangeTypeId(elementType.Id);
                            }
                        }
                        else if (existingElement is AssemblyInstance || existingElement is Group existingGroup)
                        {
                            ElementType elementType = new FilteredElementCollector(GlobalVariables.Document)
                                .OfClass(typeof(AssemblyType))
                                .Cast<AssemblyType>()
                                .FirstOrDefault(fs => fs.Name == furnitureProps.RevitFamilyType);

                            if (elementType == null)
                            {
                                // Pop UI or use the one from the instance (or both)?
                                return;
                            }
                            if (existingElement.Name != furnitureProps.RevitFamilyType)
                            {
                                existingElement.ChangeTypeId(elementType.Id);
                            }
                        }

                        // Check position



                        // Check rotation



                        // Check offset



                    }
                }
                else if (furnitureProps.SourceElementId != null)
                {
                    // Create new instance

                }


            }
            catch (OutOfMemoryException e)
            {
                System.Diagnostics.Debug.WriteLine("Furniture creation ERROR - out of memeroy -", e.ToString());
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Furniture creation ERROR", e.ToString());
            }
        }
        private void FlipFacing(FamilyInstance instance)
        {
            bool facingFlipResult = instance.flipFacing();

            if (!facingFlipResult)
            {
                XYZ normal = instance.FacingOrientation;
                XYZ origin = (instance.Location as LocationPoint).Point;
                Plane pl = Plane.CreateByNormalAndOrigin(normal, origin);
                var ids = ElementTransformUtils.MirrorElements(GlobalVariables.Document, new List<ElementId>() { instance.Id }, pl, false);
            }
        }

        public void SnaptrudeFlip(Element element, XYZ origin = null)
        {
            Parameter offset = GetOffsetParameter(element as FamilyInstance);

            if (origin == null) origin = (element.Location as LocationPoint).Point;

            //XYZ normal = new XYZ(0, 0, 1);
            XYZ normal = new XYZ(0, 1, 0);

            Plane pl = Plane.CreateByNormalAndOrigin(normal, origin);

            double originalOffset = 0;
            if (offset != null) originalOffset = offset.AsDouble();

            var ids = ElementTransformUtils.MirrorElements(GlobalVariables.Document, new List<ElementId>() { element.Id }, pl, false);

            if (offset != null) offset.Set(originalOffset);
        }

        public Parameter GetOffsetParameter(FamilyInstance instance)
        {
            if (instance == null) return null;

            Parameter offset = GlobalVariables.RvtApp.VersionNumber == "2019"
                ? instance.LookupParameter("Offset")
                : instance.LookupParameter("Offset from Host");

            return offset;
        }

        public void CreateFamilyWithSymbol(FamilySymbol familySymbol, Level level, double familyRotation, bool isFacingFlip, XYZ originOffset = null)
        {
            bool isSnaptrudeFlipped = Scaling.Z < 0;

            FamilyInstance instance = GlobalVariables.Document.Create.NewFamilyInstance(XYZ.Zero, familySymbol, level, level, Autodesk.Revit.DB.Structure.StructuralType.UnknownFraming);

            if (isFacingFlip && instance.FacingFlipped == false) FlipFacing(instance);

            if (isSnaptrudeFlipped) SnaptrudeFlip(instance);

            Transform offsetRotationTransform = Transform.CreateRotation(XYZ.BasisZ, familyRotation);

            if (isSnaptrudeFlipped)
                originOffset = offsetRotationTransform.OfPoint(originOffset);
            else
                originOffset = offsetRotationTransform.OfPoint(-originOffset);

            Line rotationAxis = Line.CreateBound(originOffset, originOffset + XYZ.BasisZ);

            instance.Location.Rotate(rotationAxis, -Rotation.Z);

            if (isSnaptrudeFlipped)
                instance.Location.Rotate(rotationAxis, -familyRotation);
            else
                instance.Location.Rotate(rotationAxis, familyRotation);

            XYZ positionRelativeToLevel = new XYZ(
                Position.X - originOffset.X,
                Position.Y - originOffset.Y,
                Position.Z - level.ProjectElevation + localBaseZ);

            instance.Location.Move(positionRelativeToLevel);

            element = instance;
        }
    }
}