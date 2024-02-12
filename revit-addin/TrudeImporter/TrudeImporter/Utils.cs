﻿using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrudeImporter
{
    public class Utils
    {
        public static bool DocHasFamily(Document doc, string familyName)
        {
            return new FilteredElementCollector(doc)
                        .OfClass(typeof(Family))
                        .Where(f => f.Name == familyName)
                        .Any();
        }

        public static String getMaterialNameFromMaterialId (String materialnameWithId, JArray materials, JArray multiMaterials, int materialIndex)
        {
            if(materialnameWithId == null)
            {
                return null;
            }

            if (materials is null)
            {
                throw new ArgumentNullException(nameof(materials));
            }

            if (multiMaterials is null)
            {
                throw new ArgumentNullException(nameof(multiMaterials));
            }

            String materialName = null;
            
            foreach ( JToken eachMaterial in materials ){

                if ( materialnameWithId == (String)eachMaterial["id"] )
                {
                    materialName = materialnameWithId;
                }

            }

            if (materialName == null)
            {
                foreach (JToken eachMultiMaterial in multiMaterials )
                {
                    if ( materialnameWithId == (String)eachMultiMaterial["id"])
                    {
                        if( !eachMultiMaterial["materials"].IsNullOrEmpty() )
                        {
                            materialName = (String)eachMultiMaterial["materials"][materialIndex];
                        }
                    }
                }

            }

            return materialName;
        }
        
        
        
        private static Random random = new Random();
        public static string RandomString(int length=5)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static void LogTrace(string format, params object[] args) { System.Console.WriteLine(format, args); }

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
        public static CurveLoop GetProfileLoop(List<XYZ> vertices)
        {
            CurveLoop curves = new CurveLoop();

            for (int i = 0; i < vertices.Count(); i++)
            {
                int currentIndex = i.Mod(vertices.Count());
                int nextIndex = (i + 1).Mod(vertices.Count());

                XYZ pt1 = vertices[currentIndex];
                XYZ pt2 = vertices[nextIndex];

                while (pt1.DistanceTo(pt2) <= GlobalVariables.RvtApp.ShortCurveTolerance)
                {
                    i++;
                    if (i > vertices.Count() + 3) break;
                    nextIndex = (i + 1).Mod(vertices.Count());
                    pt2 = vertices[nextIndex];
                }
                curves.Append(Line.CreateBound(pt1, pt2));
            }
            return curves;
        }
    }
}
