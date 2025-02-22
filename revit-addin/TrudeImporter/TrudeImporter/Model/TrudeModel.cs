﻿using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

namespace TrudeImporter
{
    public class TrudeModel
    {
        public string Name { get; set; }
        public char Type { get; set; }
        public string Geom_ID { get; set; }
        public string family { get; set; }
        public XYZ Position { get; set; }
        public XYZ Scaling { get; set; }
        public XYZ Rotation { get; set; }
        public int levelNumber { get; set; }

        public static FamilySymbol GetFamilySymbolByName(Document doc, string fsFamilyName, string fsName = null)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            var familySymbols = collector.OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>();

            /* FOR HELP IN DEBUGGING FAMILY SYBMOBLS */

            //foreach (FamilySymbol symbq in familySymbols)
            //{
            //    System.Diagnostics.Debug.WriteLine(symbq.Name, );
            //    System.Diagnostics.Debug.WriteLine(symbq.FamilyName);
            //}
            
            FamilySymbol familySymbol = null;
            try
            {
                if (fsName is null)
                {
                    familySymbol = (from fs in familySymbols
                                    where (fs.Family.Name == fsFamilyName)
                                    select fs)
                           .FirstOrDefault();
                }
                else
                {
                    familySymbol = (from fs in familySymbols
                                    where (fs.Family.Name == fsFamilyName && fs.Name == fsName)
                                    select fs)
                           .FirstOrDefault();
                }

                if (familySymbol == null)
                {
                    familySymbol = (from fs in familySymbols
                                    where (fs.Name == fsFamilyName)
                                    select fs)
                           .FirstOrDefault();
                }

                if (familySymbol == null)
                {
                    familySymbol = (from fs in familySymbols
                                    where (fs.Family.Name == fsFamilyName)
                                    select fs)
                           .FirstOrDefault();
                }

                if (familySymbol == null)
                {
                    familySymbol = (from fs in familySymbols
                                    where (fs.FamilyName == fsFamilyName)
                                    select fs)
                           .FirstOrDefault();
                }

                if (familySymbol == null) return null;

                if (!familySymbol.IsActive)
                {
                    familySymbol.Activate();
                    doc.Regenerate();
                }
            } catch { }

            return familySymbol;
        }
        public static FamilySymbol GetFamilyByName(Document doc, string fsFamilyName, string fsName = null)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            var familySymbols = collector.OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>();

            /* FOR HELP IN DEBUGGING FAMILY SYBMOBLS */

            //foreach (FamilySymbol symbq in familySymbols)
            //{
            //    System.Diagnostics.Debug.WriteLine(symbq.Name, );
            //    System.Diagnostics.Debug.WriteLine(symbq.FamilyName);
            //}
            
            FamilySymbol familySymbol = null;
            try
            {
                if (fsName is null)
                {
                    familySymbol = (from fs in familySymbols
                                    where (fs.Family.Name == fsFamilyName)
                                    select fs)
                           .FirstOrDefault();
                }
                else
                {
                    familySymbol = (from fs in familySymbols
                                    where (fs.Family.Name == fsFamilyName && fs.Name == fsName)
                                    select fs)
                           .FirstOrDefault();
                }

                if (familySymbol == null)
                {
                    familySymbol = (from fs in familySymbols
                                    where (fs.Name == fsFamilyName)
                                    select fs)
                           .FirstOrDefault();
                }

                if (familySymbol == null)
                {
                    familySymbol = (from fs in familySymbols
                                    where (fs.Family.Name == fsFamilyName)
                                    select fs)
                           .FirstOrDefault();
                }

                if (familySymbol == null) return null;

                if (!familySymbol.IsActive)
                {
                    familySymbol.Activate();
                    doc.Regenerate();
                }
            } catch { }

            return familySymbol;
        }

        public static List<Element> GetAllElements(Document doc)
        {
            List<Element> elements = new List<Element>();

            FilteredElementCollector collector = new FilteredElementCollector(doc);

            collector.OfClass(typeof(Wall));

            foreach (Element e in collector)
            {
                elements.Add(e);
            }

            FilteredElementCollector familyInstanceCollector = new FilteredElementCollector(doc);

            familyInstanceCollector.OfClass(typeof(FamilyInstance));

            foreach (Element e in familyInstanceCollector)
            {
                elements.Add(e);
            }

            return elements;

            //FilteredElementCollector collector = new FilteredElementCollector(doc);
            //collector.WhereElementIsNotElementType().WhereElementIsViewIndependent();

            //return collector.ToList();
        }
        public static List<Floor> GetAllFloors(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(Floor));
            List<Floor> walls = new List<Floor>();
            foreach (Floor wl in collector)
            {
                walls.Add(wl);
            }

            return walls;
        }

        public static Wall GetProximateWall(XYZ xyz, Document doc)
        {
            Wall wall = null;

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(Wall));
            var filteredWalls2 = collector.Where(x => x.get_BoundingBox(null).Min.Z < xyz.Z && x.get_BoundingBox(null).Max.Z > xyz.Z);
            
            double distance = double.MaxValue;
            foreach (Wall w in collector)
            {
                //if (w.LevelId != levelId) continue;

                double proximity = (w.Location as LocationCurve).Curve.Distance(xyz);
                if (proximity < distance)
                {
                    distance = proximity;
                    wall = w;
                }
            }

            return wall;
        }
    }
}
