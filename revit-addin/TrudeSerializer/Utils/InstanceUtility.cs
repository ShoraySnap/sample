using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Components
{
    internal class InstanceUtility
    {
        public static string GetRevitName(string subType, string family, double length, double width, double height, bool isFaceFlipped)
        {
            string name = subType;

            if (!string.IsNullOrEmpty(family))
            {
                name += $"_{family}";
            }

            if (length > 0)
            {
                name += $"_L{(int)length}";
            }

            if (width > 0)
            {
                name += $"_W{(int)width}";
            }

            if (height > 0)
            {
                name += $"_H{(int)height}";
            }

            if (isFaceFlipped)
            {
                name += "_F";
            }

            return name;
        }
        static double GetParameterValueOrDefault(Element element, BuiltInParameter parameter)
        {
            Parameter parameterValue = element.get_Parameter(parameter);
            return parameterValue?.AsDouble() ?? 0;
        }

        static double GetParameterValueOrDefault(Element element, string parameterName)
        {
            Parameter parameterValue = element.LookupParameter(parameterName);
            return parameterValue?.AsDouble() ?? 0;
        }

        static public double GetWidth(Element element)
        {
            double width = GetDimensionValue(element, BuiltInParameter.FURNITURE_WIDTH, "WW-Width");
            return width;
        }

        static public double GetHeight(Element element)
        {
            double height = GetDimensionValue(element, BuiltInParameter.DOOR_HEIGHT, "WW-Height");
            return height;
        }

        static public double GetLength(Element element)
        {
            double length = GetDimensionValue(element, "height", "WW-Height");
            return length;
        }

        static private double GetDimensionValue(Element element, BuiltInParameter parameter1, string parameter2)
        {
            double dimension = GetParameterValueOrDefault(element, parameter1);
            if (dimension == 0)
            {
                dimension = GetParameterValueOrDefault(element, parameter2);
            }

            if (dimension == 0 && element is FamilyInstance familyInstance && familyInstance.Symbol is FamilySymbol familySymbol)
            {
                dimension = GetParameterValueOrDefault(familySymbol, parameter1);
                if (dimension == 0)
                {
                    dimension = GetParameterValueOrDefault(familySymbol, parameter2);
                }
            }

            return dimension;
        }

        static private double GetDimensionValue(Element element, string parameter1, string parameter2)
        {
            double dimension = GetParameterValueOrDefault(element, parameter1);
            if (dimension == 0)
            {
                dimension = GetParameterValueOrDefault(element, parameter2);
            }

            if (dimension == 0 && element is FamilyInstance familyInstance && familyInstance.Symbol is FamilySymbol familySymbol)
            {
                dimension = GetParameterValueOrDefault(familySymbol, parameter1);
                if (dimension == 0)
                {
                    dimension = GetParameterValueOrDefault(familySymbol, parameter2);
                }
            }

            return dimension;
        }

        static public bool IsFaceFlipped(Element element)
        {
            bool isFaceFlipped = false;
            try
            {
                if (element is FamilyInstance familyInstance)
                {
                    isFaceFlipped = familyInstance.FacingFlipped;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return isFaceFlipped;
        }

        static public bool IsHandFlipped(Element element)
        {
            bool isHandFlipped = false;
            try
            {
                if (element is FamilyInstance familyInstance)
                {
                    isHandFlipped = familyInstance.HandFlipped;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return isHandFlipped;
        }

        static public string GetFamily(Element element)
        {
            string family = "";
            try
            {
                if (element is FamilyInstance familyInstance)
                {
                    family = familyInstance.Symbol.FamilyName;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return family;
        }

        static public bool IsMirrored(Element element)
        {
            bool isMirrored = false;
            try
            {
                if (element is FamilyInstance familyInstance)
                {
                    isMirrored = familyInstance.Mirrored;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return isMirrored;
        }

        static public List<double> GetPosition(Element element)
        {
            if (element.Category.Name == "Doors")
            {
                XYZ position = (element as FamilyInstance).GetTotalTransform().Origin;
                List<double> positionPoint = new List<double> { position.X, position.Z, position.Y };
                for (int i = 0; i < 3; i++)
                {
                    positionPoint[i] = UnitConversion.ConvertToSnaptrudeUnitsFromFeet(positionPoint[i]);
                }
                return positionPoint;
            }

            if (element.Location is LocationPoint location)
            {
                XYZ position = location.Point;

                List<double> positionPoint = new List<double> { position.X, position.Z, position.Y };
                for (int i = 0; i < 3; i++)
                {
                    positionPoint[i] = UnitConversion.ConvertToSnaptrudeUnitsFromFeet(positionPoint[i]);
                }
                return positionPoint;
            }

            return new List<double> { 0, 0, 0 };
        }

        static public double GetRotation(Element element)
        {
            double rotation = 0;
            try
            {
                if (element is FamilyInstance familyInstance)
                {
                    Transform transform = familyInstance.GetTransform();
                    //Reference: https://stackoverflow.com/questions/1996957/conversion-euler-to-matrix-and-matrix-to-euler
                    XYZ vec_x = transform.BasisX;
                    XYZ vec_y = transform.BasisY;
                    XYZ vec_z = transform.BasisZ;

                    double angle_x = (2 * Math.PI) - Math.Asin(vec_y.Z);

                    if (Math.Cos(angle_x) > 0.0001)
                    {
                        rotation = (2 * Math.PI) - Math.Atan2(vec_y.X, vec_y.Y);
                    }
                    else
                    {
                        rotation = (2 * Math.PI) - Math.Atan2(vec_x.Y, vec_x.X);
                    }

                    if (element.Category.Name == "Generic Models")
                    {
                        Transform identity = Transform.Identity;
                        rotation += identity.BasisZ.AngleTo(transform.BasisZ);
                    }

                    if (element.Category.Name == "Doors" || element.Category.Name == "Windows")
                    {
                        if (element.Location is LocationPoint location)
                        {
                            rotation = location.Rotation;
                        }
                    }
                }
            }
            catch
            {
                rotation = 0;
            }

            return rotation;
        }

        static public List<double> GetCustomCenterPoint(Element element)
        {
            string category = element.Category?.Name;
            List<double> center;
            // todo: check and implement for other categories

            if (element is Group)
            {
                center = GetCenterForModelGroups(element);
            }
            else if (element is FamilyInstance)
            {
                FamilyInstance familyInstance = element as FamilyInstance;

                XYZ transformOrigin = familyInstance.GetTotalTransform().Origin;
                center = new List<double> { transformOrigin.X, transformOrigin.Z, transformOrigin.Y };
            }
            else
            {
                Location location = element.Location;
                if (location is LocationPoint locationPoint)
                {
                    center = new List<double> { locationPoint.Point.X, locationPoint.Point.Z, locationPoint.Point.Y };
                }
                else
                {
                    center = GetCenterFromBoundingBox(element);
                }
            }

            for (int i = 0; i < 3; i++)
            {
                center[i] = UnitConversion.ConvertToSnaptrudeUnits(center[i], UnitTypeId.Feet);
            }
            return center;
        }

        static public List<double> GetCenterFromBoundingBox(Element element)
        {
            Document doc = GlobalVariables.Document;
            View view = doc.ActiveView;
            BoundingBoxXYZ bbox = element.get_BoundingBox(view);
            if (bbox == null)
            {
                return new List<double>() { 0, 0, 0 };
            }

            XYZ center = (bbox.Max + bbox.Min) / 2;

            return new List<double> { center.X, center.Z, center.Y };
        }

        static public List<double> GetCenterForModelGroups(Element element)
        {
            Group group = element as Group;
            BoundingBoxXYZ bb = new BoundingBoxXYZ();
            bool isFirstElement = true;
            bool isContainsWallsOrFloors = false;
            IList<ElementId> subComponents = group.GetMemberIds();
            Document doc = GlobalVariables.Document;
            View view = doc.ActiveView;

            foreach (ElementId subComponentId in subComponents)
            {
                Element currentElement = doc.GetElement(subComponentId) as Element;
                Category cat = currentElement.Category;
                string catName = cat != null ? cat.Name : "";

                if (catName == "Walls" || catName == "Floors")
                {
                    isContainsWallsOrFloors = true;
                    break;
                }
            }

            if (isContainsWallsOrFloors)
            {
                foreach (ElementId subComponentId in subComponents)
                {
                    Element currentElement = doc.GetElement(subComponentId) as Element;
                    Category cat = currentElement.Category;
                    string catName = cat != null ? cat.Name : "";

                    if (catName != null && CheckIfElementIsVisible(currentElement) != null)
                    {
                        BoundingBoxXYZ currentbb = currentElement.get_BoundingBox(view);
                        if (isFirstElement)
                        {
                            bb = currentbb;
                            isFirstElement = false;
                        }
                        else
                        {
                            bb = ExpandToContain(bb, currentbb.Max);
                            bb = ExpandToContain(bb, currentbb.Min);
                        }
                    }
                }

                List<double> center = new List<double> {
                    (bb.Max.X + bb.Min.X) / 2,
                    (bb.Max.Z + bb.Min.Z) / 2,
                    (bb.Max.Y + bb.Min.Y) / 2
                };

                return center;
            }

            List<double> groupCenter = GetCenterFromBoundingBox(element);
            return groupCenter;
        }

        static public Element CheckIfElementIsVisible(Element element)
        {
            Document doc = GlobalVariables.Document;
            View view = doc.ActiveView;
            FilterRule idRule = ParameterFilterRuleFactory.CreateEqualsRule(new ElementId(BuiltInParameter.ID_PARAM), element.Id);
            ElementParameterFilter idFilter = new ElementParameterFilter(idRule);
            Category cat = element.Category;
            if (cat == null)
            {
                return null;
            }
            ElementCategoryFilter catFilter = new ElementCategoryFilter(cat.Id);
            FilteredElementCollector collector = new FilteredElementCollector(doc, view.Id)
                .WhereElementIsNotElementType()
                .WherePasses(catFilter)
                .WherePasses(idFilter);
            return collector.FirstElement();
        }

        static public BoundingBoxXYZ ExpandToContain(BoundingBoxXYZ bb, XYZ p)
        {
            bb.Min = new XYZ(Math.Min(bb.Min.X, p.X), Math.Min(bb.Min.Y, p.Y), Math.Min(bb.Min.Z, p.Z));
            bb.Max = new XYZ(Math.Max(bb.Max.X, p.X), Math.Max(bb.Max.Y, p.Y), Math.Max(bb.Max.Z, p.Z));
            return bb;
        }

        static public string GetHostId(Element element)
        {
            string hostId = "";
            try
            {
                if (element is FamilyInstance familyInstance)
                {
                    hostId = familyInstance.Host != null ? familyInstance.Host.Id.ToString() : "";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // add logging
            }
            return hostId;
        }
    }
}