using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Text;
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

        static public double[] GetPosition(Element element)
        {
            LocationPoint location = element.Location as LocationPoint;

            if (location != null)
            {
                XYZ position = location.Point;

                double[] positionPoint = new double[] { position.X, position.Z, position.Y };
                for (int i = 0; i < 3; i++)
                {
                    positionPoint[i] = UnitConversion.ConvertToSnaptrudeUnits(positionPoint[i], UnitTypeId.Feet);
                }
                return positionPoint;
            }

            if (element.Category.Name == "Doors")
            {
                XYZ position = (element as FamilyInstance).GetTotalTransform().Origin;
                double[] positionPoint = new double[] { position.X, position.Z, position.Y };
                for (int i = 0; i < 3; i++)
                {
                    positionPoint[i] = UnitConversion.ConvertToSnaptrudeUnits(positionPoint[i], UnitTypeId.Feet);
                }
                return positionPoint;
            }

            return new double[] { 0, 0, 0 };
        }

        static public double GetRotation(Element element)
        {
            double rotation;
            try
            {
                Transform transform = (element as FamilyInstance).GetTransform();
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

                if (element.Category.Name == "Doors")
                {
                    if (element.Location is LocationPoint location)
                    {
                        rotation = location.Rotation;
                    }
                }
            }
            catch
            {
                rotation = 0;
            }

            return rotation;
        }

        static public double[] GetCustomCenterPoint(Element element)
        {
            string category = element.Category?.Name;
            // todo: check and implement for other categories
            
            XYZ transformOrigin = (element as FamilyInstance).GetTotalTransform().Origin;
            double[] positionPoint = new double[] { transformOrigin.X, transformOrigin.Z, transformOrigin.Y };
            for (int i = 0; i < 3; i++)
            {
                positionPoint[i] = UnitConversion.ConvertToSnaptrudeUnits(positionPoint[i], UnitTypeId.Feet);
            }
            return positionPoint; 
        }
    }
}