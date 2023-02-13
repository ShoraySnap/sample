using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrudeImporter
{
    public class Utils
    {
        public static Element FindElement(Document doc, Type targetType, string targetName = null)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);

            if (targetName is null)
            {
                return collector.OfClass(targetType).FirstOrDefault<Element>();
            }
            else
            {
                return collector.OfClass(targetType).FirstOrDefault<Element>(e => e.Name.Equals(targetName));
            }
        }
        public static FillPatternElement GetSolidFillPatternElement(Document Document)
        {
            FilteredElementCollector FEC = new FilteredElementCollector(Document);
            ElementClassFilter ECF = new ElementClassFilter(typeof(FillPatternElement));
            List<FillPatternElement> Els = FEC.WherePasses(ECF).ToElements().Cast<FillPatternElement>().ToList();
            FillPatternElement SFFP = Els.Find(x => x.GetFillPattern().IsSolidFill);
            if (SFFP == null)
            {
                throw new Exception("Solid fill pattern not found, should exist.");
            }
            return SFFP;
        }

        public static List<Element> GetElements(Document doc, Type targetType)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);

            return collector.OfClass(targetType).ToList<Element>();
        }
    }
}
