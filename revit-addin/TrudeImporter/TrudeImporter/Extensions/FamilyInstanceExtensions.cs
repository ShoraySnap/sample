using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media.Media3D;

namespace TrudeImporter
{
    public static class FamilyInstanceExtensions
    {
        public static (double, double) GetWidthAndHeight(this FamilyInstance instance)
        {
            BoundingBoxXYZ bbox = instance.get_BoundingBox(null);

            double width0 = Math.Abs(bbox.Max.X - bbox.Min.X);
            double width1 = Math.Abs(bbox.Max.Y - bbox.Min.Y);
            double width = width0 > width1 ? width0 : width1;

            double height = Math.Abs(bbox.Max.Z - bbox.Min.Z);

            return (width, height);
        }
        public static (Parameter, Parameter) FindWidthAndHeightParameters(this FamilyInstance instance)
        {
            Parameter widthParam = instance.get_Parameter(BuiltInParameter.WINDOW_WIDTH);
            Parameter heightParam = instance.get_Parameter(BuiltInParameter.WINDOW_HEIGHT);

            if ((widthParam.HasValue && heightParam.HasValue) && (!widthParam.IsReadOnly && !heightParam.IsReadOnly))
            {
                return (widthParam, heightParam);
            }

            (double width, double height) = instance.GetWidthAndHeight();

            foreach (Parameter parameter in instance.GetOrderedParameters())
            {
                if (parameter.Definition.ParameterType != ParameterType.Length) continue;
                if (parameter.IsReadOnly) continue;

                if (parameter.AsDouble().AlmostEquals(width, 0.5))
                {
                    if (width.AlmostEquals(height))
                    {
                        if (!parameter.Definition.Name.Contains("w")) continue;
                    }
                    widthParam = parameter;
                }
                else if (parameter.AsDouble().AlmostEquals(height, 0.5))
                {
                    heightParam = parameter;
                }
            }

            return (widthParam, heightParam);
        }
    }
}
