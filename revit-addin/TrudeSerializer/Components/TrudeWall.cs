using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using System;
using System.Collections.Generic;
using System.Linq;
using TrudeSerializer.Importer;
using TrudeSerializer.Types;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Components
{
    internal class TrudeWall : TrudeComponent
    {
        public string type;
        public double width;
        public double height;
        public bool function;
        public double[] orientation;
        public List<List<double>> endpoints;
        public List<List<double>> wallBottomProfile;
        public List<List<double>> sideProfile;
        public List<List<List<double>>> sideProfileVoids;
        public List<TrudeWallOpening> openings;
        public List<List<string>> wallInserts;
        public double[] center;
        private bool isCurvedWall;

        private TrudeWall(string elementId, string level, string wallType, double width, double height, string family, bool function, double[] orientation, List<List<double>> endpoints, bool isCurvedWall) : base(elementId, "Walls", family, level)
        {
            this.type = wallType;
            this.width = width;
            this.height = height;
            this.function = function;
            this.orientation = orientation;
            this.endpoints = endpoints;
            this.isCurvedWall = isCurvedWall;
        }

        private TrudeWall(string elementId) : base(elementId, "Walls", "", "")
        {
            this.elementId = elementId;
        }

        public void SetBottomProfile(List<List<double>> wallBottomProfile)
        {
            this.wallBottomProfile = wallBottomProfile;
        }

        public void SetSideProfile(List<List<double>> sideProfile, List<List<List<double>>> voidProfiles)
        {
            this.sideProfile = sideProfile;
            this.sideProfileVoids = voidProfiles;
        }

        public void SetOpenings(List<TrudeWallOpening> openings)
        {
            this.openings = openings;
        }

        public void SetWallInserts(List<List<string>> wallInserts)
        {
            this.wallInserts = wallInserts;
        }

        public void SetCenter(double[] center)
        {
            this.center = center;
        }

        public static TrudeWall GetDefaultTrudeWall()
        {
            TrudeWall wall = new TrudeWall("-1");
            wall.isParametric = false;
            return wall;
        }

        public static bool IsNotValidWall(Element element)
        {
            if (!(element is Wall wall)) return true;
            if (wall.CurtainGrid != null) return true;
            if (wall.IsStackedWall) return true;
            return false;
        }

        public static TrudeWall GetSerializedComponent(SerializedTrudeData importData, Element element)
        {
            if (IsNotValidWall(element)) return GetDefaultTrudeWall();

            Wall wall = element as Wall;

            string elementId = element.Id.ToString();
            Curve baseLine = GetBaseLine(element);
            string levelName = TrudeLevel.GetLevelName(element);
            string wallType = element.Name;
            double width = UnitConversion.ConvertToSnaptrudeUnitsFromFeet(wall.Width);
            string family = wall.WallType.FamilyName;
            bool function = wall.WallType.Function == WallFunction.Exterior;
            double[] orientation = new double[] { wall.Orientation.X, wall.Orientation.Z, wall.Orientation.Y };
            List<List<double>> endpoints = GetEndPoints(baseLine);
            bool isCurvedWall = baseLine is Arc;
            double height = GetWallHeight(element);

            SetWallType(importData, wall);

            TrudeWall serializedWall = new TrudeWall(elementId, levelName, wallType, width, height, family, function, orientation, endpoints, isCurvedWall);
            serializedWall.SetIsParametric(wall);
            if (!serializedWall.IsParametric())
            {
                double[] center = UnitConversion.ConvertToSnaptrudeUnitsFromFeet(GetCenterFromBoundingBox(element));
                serializedWall.SetCenter(center);
                return serializedWall;
            }

            List<List<string>> wallInserts = GetWallInserts(wall);
            if (wallInserts.Count != 0)
            {
                serializedWall.SetWallInserts(wallInserts);
            }

            List<List<double>> wallBottomProfile = GetBottomProfile(element, endpoints, width);
            if (wallBottomProfile.Count != 0)
            {
                serializedWall.SetBottomProfile(wallBottomProfile);
            }

            GetSideProfileOfWall(element, height, out List<List<double>> sideProfile, out List<List<List<double>>> voidProfiles);

            if (sideProfile.Count != 0)
            {
                serializedWall.SetSideProfile(sideProfile, voidProfiles);
                return serializedWall;
            }

            bool isNonParametric = MarkNonParametericBasedOnSideProfile(wall);
            if (isNonParametric)
            {
                double[] center = UnitConversion.ConvertToSnaptrudeUnitsFromFeet(GetCenterFromBoundingBox(element));
                serializedWall.SetCenter(center);
                serializedWall.isParametric = false;
                return serializedWall;
            }

            List<TrudeWallOpening> openings = GetOpenings(element);
            if (openings.Count != 0)
            {
                serializedWall.SetOpenings(openings);
            }

            return serializedWall;
        }

        public static double GetWallHeight(Element element)
        {
            double height = GetHeightFromBoundingBox(element);
            return height;
        }

        private static Curve GetBaseLine(Element element)
        {
            Location location = element.Location;
            LocationCurve locationCurve = location as LocationCurve;
            Curve curve = locationCurve.Curve;
            return curve;
        }

        private static List<List<double>> GetEndPoints(Curve curve)
        {
            XYZ startPoint = curve.GetEndPoint(0);
            XYZ endPoint = curve.GetEndPoint(1);
            List<List<double>> endPoints = new List<List<double>>
            {
                UnitConversion.ConvertToSnaptrudeUnitsFromFeet(startPoint).ToList(),
                UnitConversion.ConvertToSnaptrudeUnitsFromFeet(endPoint).ToList()
            };

            return endPoints;
        }

        private void SetIsParametric(Wall wall)
        {
            WallCrossSection crossSection = wall.CrossSection;
            bool isSupportedCrossSection = crossSection == WallCrossSection.Vertical;

            isParametric = isSupportedCrossSection && !isCurvedWall;
        }

        public static void SetWallType(SerializedTrudeData importData, Wall wall)
        {
            string name = wall.WallType.Name;
            if (importData.FamilyTypes.HasWallType(name)) return;

            TrudeWallType snaptrudeWallType = TrudeWallType.GetLayersData(wall);
            importData.FamilyTypes.AddWallType(name, snaptrudeWallType);
        }

        public static List<List<double>> GetBottomProfile(Element element, List<List<double>> baseLine, double width)
        {
            List<XYZ> bottomFaceVertices = new List<XYZ> { };

            Options opt = new Options { };

            GeometryElement geometry = element.get_Geometry(opt);

            foreach (GeometryObject geom in geometry)
            {
                if (!(geom is Solid)) continue;
                Solid solid = geom as Solid;
                bottomFaceVertices = GetBottomVerticesFromSolid(solid);
            }

            List<List<double>> centerLine = GetCenterLineofWallFromProfile(bottomFaceVertices, true);

            centerLine = CorrectBottomProfile(centerLine, baseLine, width);

            return centerLine;
        }

        private static bool MarkNonParametericBasedOnSideProfile(Wall wall)
        {
            IList<Reference> sideFaceRef = HostObjectUtils.GetSideFaces(wall, ShellLayerType.Exterior);
            GeometryObject sideFaceProfile = wall.GetGeometryObjectFromReference(sideFaceRef[0]);
            List<double> ZValues = new List<double> { };

            Face sideFace = sideFaceProfile as Face;
            if (sideFace == null) return false;
            List<XYZ> sideFaceVertices = GetFaceVerticesFromFace(sideFace);

            sideFaceVertices = RemoveCollinearPoints(sideFaceVertices);

            foreach (XYZ vertex in sideFaceVertices)
            {
                ZValues.Add(vertex.Z);
            }

            double maxZ = ZValues.Max();

            double countMaxZ = ZValues.Count(x => IsAlmostEqual(x, maxZ));

            return countMaxZ == 1 || countMaxZ > 2;
        }

        private static List<XYZ> RemoveCollinearPoints(List<XYZ> vertices)
        {
            int i = 1;
            while (i < vertices.Count)
            {
                XYZ v = vertices[(i - 1) % vertices.Count] - vertices[(i)];
                XYZ w = vertices[(i)] - vertices[(i + 1) % vertices.Count];
                double angle = v.AngleTo(w);
                if (IsAlmostEqual(angle, 0))
                {
                    vertices.RemoveAt((i));
                }
                else if (IsAlmostEqual(angle, Math.PI))
                {
                    vertices.RemoveAt((i - 1) % vertices.Count);
                }
                else
                {
                    i++;
                }
            }

            return vertices;
        }

        private static List<XYZ> GetBottomVerticesFromSolid(Solid solid)
        {
            List<XYZ> bottomVertices = new List<XYZ>();
            foreach (Face face in solid.Faces)
            {
                if (face.ComputeNormal(new UV(0.5, 0.5)).Z < 0)
                {
                    bottomVertices.AddRange(GetFaceVerticesFromFace(face));
                }
            }
            return bottomVertices;
        }

        private static List<XYZ> GetFaceVerticesFromFace(Face face)
        {
            List<XYZ> vertices = new List<XYZ> { };
            IList<CurveLoop> edges = face.GetEdgesAsCurveLoops();

            IList<IList<CurveLoop>> sortedEdges = ExporterIFCUtils.SortCurveLoops(edges);

            foreach (var loop in sortedEdges)
            {
                foreach (var edge in loop)
                {
                    var curveIterator = edge.GetCurveLoopIterator();
                    while (curveIterator.MoveNext())
                    {
                        var curve = curveIterator.Current;
                        if (curve is Line)
                        {
                            vertices.Add(curve.GetEndPoint(0));
                        }
                        else
                        {
                            IList<XYZ> points = curve.Tessellate();
                            foreach (var point in points)
                            {
                                vertices.Add(point);
                            }
                        }
                    }
                }
            }
            return vertices;
        }

        static XYZ MaxPoint(XYZ a, XYZ b, Func<XYZ, double> selector)
        {
            return selector(a) > selector(b) ? a : b;
        }

        static XYZ MinPoint(XYZ a, XYZ b, Func<XYZ, double> selector)
        {
            return selector(a) < selector(b) ? a : b;
        }

        private static List<List<double>> GetCenterLineofWallFromProfile(List<XYZ> faceVertices, bool isBottomProfile)
        {
            List<List<double>> centerLineInSnaptrudeUnits = new List<List<double>> { };

            double maxZ = GetMax(faceVertices, 2);
            double minZ = GetMin(faceVertices, 2);
            double maxX = GetMax(faceVertices, 0);
            double maxY = GetMax(faceVertices, 1);

            XYZ topRight = faceVertices[0];
            XYZ topLeft = faceVertices[0];
            XYZ bottomRight = faceVertices[0];
            XYZ bottomLeft = faceVertices[0];

            foreach (XYZ vertex in faceVertices)
            {
                topRight = MaxPoint(vertex, topRight, (k) => k.X + k.Y);
                bottomLeft = MinPoint(vertex, bottomLeft, (k) => k.X + k.Y);
                topLeft = MinPoint(vertex, topLeft, (k) => k.X + maxY - k.Y);
                bottomRight = MinPoint(vertex, bottomRight, (k) => maxX - k.X + k.Y);
            }

            List<XYZ> endpoints = new List<XYZ>
            {
                topLeft, topRight, bottomRight, bottomLeft, topLeft
            };

            List<XYZ> midPoints = new List<XYZ> { };

            for (int i = 1; i < endpoints.Count; i++)
            {
                double midX = (endpoints[i].X + endpoints[i - 1].X) / 2;
                double midY = (endpoints[i].Y + endpoints[i - 1].Y) / 2;
                double midZ = endpoints[i].Z;
                midPoints.Add(new XYZ(midX, midY, midZ));
            }

            double maxDistance = 0;
            List<XYZ> longestCenterLine = new List<XYZ> { };

            for (int i = 2; i < midPoints.Count; i++)
            {
                double distance = GetDistance(midPoints[i], midPoints[i - 2]);
                if (maxDistance < distance)
                {
                    longestCenterLine.Clear();
                    longestCenterLine.Add(midPoints[i - 2]);
                    longestCenterLine.Add(midPoints[i]);
                    maxDistance = distance;
                }
            }
            double yCoordinate = isBottomProfile ? minZ : maxZ;
            foreach (XYZ point in longestCenterLine)
            {
                double[] pointList = { point.X, yCoordinate, point.Y };
                centerLineInSnaptrudeUnits.Add(UnitConversion.ConvertToSnaptrudeUnitsFromFeet(pointList).ToList());
            }

            return centerLineInSnaptrudeUnits;
        }

        private static double GetDistance(XYZ point1, XYZ point2)
        {
            double distance = Math.Sqrt(Math.Pow(point2.X - point1.X, 2) + Math.Pow(point2.Y - point1.Y, 2) + Math.Pow(point2.Z - point1.Z, 2));
            return distance;
        }

        private static double GetMin(List<XYZ> vertices, int index)
        {
            double minZ = double.MaxValue;
            foreach (var vertex in vertices)
            {
                if (vertex[index] < minZ)
                    minZ = vertex[index];
            }
            return minZ;
        }

        private static double GetMax(List<XYZ> vertices, int index)
        {
            double maxZ = double.MinValue;
            foreach (var vertex in vertices)
            {
                if (vertex[index] > maxZ)
                    maxZ = vertex[index];
            }
            return maxZ;
        }

        private static List<List<double>> CorrectBottomProfile(List<List<double>> bottomProfile, List<List<double>> baseLine, double width)
        {
            if (IsAlmostEqual(bottomProfile[0], baseLine[0], width))
            {
                bottomProfile[0][0] = baseLine[0][0];
                bottomProfile[0][2] = baseLine[0][2];
            }
            else if (IsAlmostEqual(bottomProfile[0], baseLine[1], width))
            {
                bottomProfile[0][0] = baseLine[1][0];
                bottomProfile[0][2] = baseLine[1][2];
            }

            if (IsAlmostEqual(bottomProfile[1], baseLine[0], width))
            {
                bottomProfile[1][0] = baseLine[0][0];
                bottomProfile[1][2] = baseLine[0][2];
            }
            else if (IsAlmostEqual(bottomProfile[1], baseLine[1], width))
            {
                bottomProfile[1][0] = baseLine[1][0];
                bottomProfile[1][2] = baseLine[1][2];
            }

            return bottomProfile;
        }

        private static bool IsAlmostEqual(List<double> a, List<double> b, double tolerance)
        {
            return Math.Abs(a[0] - b[0]) < tolerance && Math.Abs(a[2] - b[2]) < tolerance;
        }

        private static bool IsAlmostEqual(double a, double b, double tolerance = 1e-4)
        {
            return Math.Abs(a - b) < tolerance;
        }

        private static void GetSideProfileOfWall(Element element, double height, out List<List<double>> sideProfile, out List<List<List<double>>> voidProfiles)
        {
            List<XYZ> sideProfilePoints = new List<XYZ> { };

            sideProfile = new List<List<double>> { };
            voidProfiles = new List<List<List<double>>> { };
            if (!(element is Wall wall)) return;
            if (!(ExporterIFCUtils.HasElevationProfile(wall))) return;
            double lowestZ = double.MaxValue;
            double highestZ = double.MinValue;

            IList<CurveLoop> curveLoops = ExporterIFCUtils.GetElevationProfile(wall);

            if (curveLoops.Count == 0) return;
            CurveLoop curveLoop = curveLoops[0];
            bool mainLoopOrientation = curveLoop.IsCounterclockwise(wall.Orientation);

            CurveLoopIterator curveLoopItr = curveLoop.GetCurveLoopIterator();
            while (curveLoopItr.MoveNext())
            {
                Curve curve = curveLoopItr.Current;
                if (curve is Line)
                {
                    XYZ point = curve.GetEndPoint(0);
                    lowestZ = Math.Min(lowestZ, point.Z);
                    highestZ = Math.Max(highestZ, point.Z);
                    sideProfilePoints.Add(point);
                }
                else
                {
                    IList<XYZ> points = curve.Tessellate();
                    foreach (var point in points)
                    {
                        sideProfilePoints.Add(point);
                    }
                }
            }

            double heightOfProfile = UnitConversion.ConvertToSnaptrudeUnitsFromFeet(highestZ - lowestZ);

            if (sideProfilePoints.Count == 4 && curveLoops.Count == 1 && heightOfProfile == height) return;

            foreach (XYZ point in sideProfilePoints)
            {
                sideProfile.Add(UnitConversion.ConvertToSnaptrudeUnitsFromFeet(point).ToList());
            }

            if (curveLoops.Count == 1) return;

            List<List<XYZ>> voids = GetVoidProfileFromSideProfile(wall, curveLoops, mainLoopOrientation);

            foreach (List<XYZ> voidProfile in voids)
            {
                List<List<double>> voidProfileInSnaptrudeUnits = new List<List<double>> { };
                foreach (XYZ point in voidProfile)
                {
                    voidProfileInSnaptrudeUnits.Add(UnitConversion.ConvertToSnaptrudeUnitsFromFeet(point).ToList());
                }
                voidProfiles.Add(voidProfileInSnaptrudeUnits);
            }
        }

        private static List<List<XYZ>> GetVoidProfileFromSideProfile(Wall wall, IList<CurveLoop> curveLoops, bool mainLoopOrientation)
        {
            List<List<XYZ>> voids = new List<List<XYZ>> { };
            for (int i = 1; i < curveLoops.Count; i++)
            {
                CurveLoop loop = curveLoops[i];
                CurveLoopIterator loopItr = loop.GetCurveLoopIterator();
                if (loop.IsCounterclockwise(wall.Orientation) == mainLoopOrientation) continue;
                List<XYZ> voidVertices = new List<XYZ> { };
                while (loopItr.MoveNext())
                {
                    Curve curve = loopItr.Current;

                    if (curve is Line)
                    {
                        XYZ point = curve.GetEndPoint(0);
                        voidVertices.Add(point);
                    }
                    else
                    {
                        IList<XYZ> points = curve.Tessellate();
                        foreach (var point in points)
                        {
                            voidVertices.Add(point);
                        }
                    }
                }
                voids.Add(voidVertices);
            }
            return voids;
        }

        private static List<TrudeWallOpening> GetOpenings(Element element)
        {
            List<TrudeWallOpening> trudeWallOpenings = new List<TrudeWallOpening> { };

            Wall wall = element as Wall;
            if (wall == null) return trudeWallOpenings;

            Document doc = GlobalVariables.Document;

            List<double> positiveDirection = new List<double> { 0, 1, 0 };
            List<double> negativeDirection = new List<double> { 0, -1, 0 };

            Options opt = new Options { };
            GeometryElement geometry = wall.get_Geometry(opt);
            List<String> elements = new List<String> { };
            foreach (GeometryObject geom in geometry)
            {
                if (geom is Solid solid)
                {
                    foreach (Face face in solid.Faces)
                    {
                        if (!(face is PlanarFace planarFace)) continue;

                        ICollection<ElementId> generatingElementIds = wall.GetGeneratingElementIds(face);
                        if (generatingElementIds.Count == 0) continue;
                        ElementId openingId = generatingElementIds.First();
                        if (openingId == element.Id) continue;
                        //if (elements.Contains(openingId.ToString())) continue;
                        Element opening = doc.GetElement(openingId);
                        if (opening == null) continue;
                        GetOpening(opening, face, out List<XYZ> faceVerticesPoints, out double height);
                        if (faceVerticesPoints.Count == 0) continue;

                        bool orientation = face.ComputeNormal(new UV(0.5, 0.5)).Z > 0;
                        List<double> normal = orientation ? positiveDirection : negativeDirection;
                        List<List<double>> faceVertices = new List<List<double>> { };
                        foreach (XYZ point in faceVerticesPoints)
                        {
                            faceVertices.Add(UnitConversion.ConvertToSnaptrudeUnitsFromFeet(point).ToList());
                        }
                        elements.Add(openingId.ToString());

                        trudeWallOpenings.Add(new TrudeWallOpening(faceVertices, UnitConversion.ConvertToSnaptrudeUnitsFromFeet(height), normal));
                    }
                }
            }

            return trudeWallOpenings;
        }

        private static void GetOpening(Element element, Face face, out List<XYZ> faceVertices, out double height)
        {
            faceVertices = new List<XYZ> { };
            height = 0;
            if (!(face.ComputeNormal(new UV(0.5, 0.5)).Z < 0)) return;
            string[] SUPPORTED_OPENING_CATEGORIES = { "Generic Models", "Rectangular Straight Wall Opening" };
            string category = element.Category?.Name;
            if (!SUPPORTED_OPENING_CATEGORIES.Contains(category)) return;

            faceVertices = GetFaceVerticesFromFace(face);
            height = GetHeightFromBoundingBox(element);
        }

        private static List<List<string>> GetWallInserts(Wall wall)
        {
            List<List<string>> inserts = new List<List<string>> { };
            ICollection<ElementId> insertIds = wall.FindInserts(false, false, true, false);

            if (insertIds.Count == 0) return inserts;

            foreach (ElementId insertId in insertIds)
            {
                Element insert = GlobalVariables.Document.GetElement(insertId);
                List<string> curtainWallInserts = new List<string> { };

                if (insert == null) continue;
                if (insert is Wall insertedWall)
                {
                    var grid = insertedWall.CurtainGrid;
                    if (grid == null) continue;

                    var mullions = grid.GetMullionIds()
                            .Select(id => id.ToString())
                            .ToList();
                    var panels = grid.GetPanelIds().Select(id => id.ToString()).ToList();
                    if (mullions.Count == 0 && panels.Count == 0) continue;

                    curtainWallInserts.AddRange(mullions);
                    curtainWallInserts.AddRange(panels);
                }
                if (curtainWallInserts.Count != 0)
                {
                    inserts.Add(curtainWallInserts);
                }
            }

            return inserts;
        }
    }

    class TrudeWallOpening
    {
        public List<List<double>> faceVertices;
        public double height;
        public List<double> normal;

        public TrudeWallOpening(List<List<double>> faceVertices, double height, List<double> normal)
        {
            this.faceVertices = faceVertices;
            this.height = height;
            this.normal = normal;
        }
    }
}