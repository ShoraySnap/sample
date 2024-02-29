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
        
        private void CreateFurniture(FamilySymbol familySymbol, ElementId levelId, Wall wall, XYZ direction)
        {
        }
    }
}