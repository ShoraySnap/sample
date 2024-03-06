using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Components
{
    internal class TrudePlanViewIndicator

    {
        public string type;
        public double[] startPoint;
        public double[] endPoint;
        public List<double[]> points;

        public TrudePlanViewIndicator(string type)
        {
            this.type = type;
        }

        public TrudePlanViewIndicator(string type, double[] startPoint, double[] endPoint)
        {
            this.type = type;
            this.startPoint = startPoint;
            this.endPoint = endPoint;
        }

        public void SetStartPoint(double[] startPoint)
        {
            this.startPoint = startPoint;
        }

        public void SetEndPoint(double[] endPoint)
        {
            this.endPoint = endPoint;
        }

        public void SetArcPoints(List<double[]> points)
        {
            this.points = points;
        }
        public static TrudePlanViewIndicator GetDefaultPlanViewIndicator()
        {
            return new TrudePlanViewIndicator("Default", new double[] { 0, 0, 0 }, new double[] { 0, 0, 0 });
        }

        public static TrudePlanViewIndicator GetLinePlanViewGeometry(Curve line)
        {
            TrudePlanViewIndicator planViewGeometry = new TrudePlanViewIndicator("Line");
            XYZ startPoint = line.GetEndPoint(0);
            double[] startPointArray = new double[] { startPoint.X, startPoint.Z, startPoint.Y };
            startPointArray = UnitConversion.ConvertToSnaptrudeUnitsFromFeet(startPointArray);
            planViewGeometry.SetStartPoint(startPointArray);
            XYZ endPoint = line.GetEndPoint(1);
            double[] endPointArray = new double[] { endPoint.X, endPoint.Z, endPoint.Y };
            endPointArray = UnitConversion.ConvertToSnaptrudeUnitsFromFeet(endPointArray);
            planViewGeometry.SetEndPoint(endPointArray);
            return planViewGeometry;
        }

        public static TrudePlanViewIndicator GetArcPlanViewGeometry(Curve arc)
        {
            TrudePlanViewIndicator planViewGeometry = new TrudePlanViewIndicator("Arc");
            IList<XYZ> points = arc.Tessellate();

            List<double[]> pointsArray = new List<double[]> { };
            foreach (XYZ point in points)
            {
                double[] pointArray = new double[] { point.X, point.Z, point.Y };
                pointArray = UnitConversion.ConvertToSnaptrudeUnitsFromFeet(pointArray);
                pointsArray.Add(pointArray);
            }
            planViewGeometry.SetArcPoints(pointsArray);

            return planViewGeometry;
        }

        public static List<TrudePlanViewIndicator> GetPlanViewIndicator(Element element)
        {
            List<TrudePlanViewIndicator> planViewIndicator = new List<TrudePlanViewIndicator> { };
            if (!IsElementEligibleForPlanViewIndicator(element))
            {
                return planViewIndicator;
            }

            Document doc = GlobalVariables.Document;

            ElementId viewId;
            View view;

            try
            {
                viewId = (doc.GetElement(element.LevelId) as Level).FindAssociatedPlanViewId();
                view = doc.GetElement(viewId) as View;
            }
            catch
            {
                view = doc.ActiveView;
            }

            Options options = new Options
            {
                View = view
            };

            GeometryElement geomElem = element.get_Geometry(options);
            List<Line> allLinesInSolid = new List<Line>();
            foreach (GeometryObject geomObj in geomElem)
            {
                if (!(geomObj is GeometryInstance)) continue;

                GeometryInstance geomInst = geomObj as GeometryInstance;
                foreach (GeometryObject instObj in geomInst.GetSymbolGeometry())
                {
                    if (instObj is Line)
                    {
                        planViewIndicator.Add(GetLinePlanViewGeometry(instObj as Curve));
                    }
                    else if (instObj is Arc)
                    {
                        planViewIndicator.Add(GetArcPlanViewGeometry(instObj as Curve));
                    }
                    else if (instObj is Solid)
                    {
                        Solid solid = instObj as Solid;
                        EdgeArray edges = solid.Edges;

                        foreach (Edge edge in edges)
                        {
                            Line curve = edge.AsCurve() as Line;
                            if (curve == null) continue;

                            if (!IsEligibleLine(curve, allLinesInSolid))
                            {
                                allLinesInSolid.Add(curve);

                                planViewIndicator.Add(GetLinePlanViewGeometry(curve));
                            }
                        }
                    }
                }
            }

            return planViewIndicator;
        }

        public static bool IsEligibleLine(Line line, List<Line> allLines)
        {
            return !DoesPointExistInLinesIgnoringUpDirection(line, allLines) && IsLineHorizontal(line);
        }

        public static bool IsLineHorizontal(Line line)
        {
            return Math.Abs(line.GetEndPoint(0).Z - line.GetEndPoint(1).Z) < 0.001;
        }
        public static bool DoesPointExistInLinesIgnoringUpDirection(Line currenLine, List<Line> allLines)
        {
            XYZ currentStartPoint = currenLine.GetEndPoint(0);
            XYZ currentEndPoint = currenLine.GetEndPoint(1);

            foreach (Line line in allLines)
            {
                XYZ startPoint = line.GetEndPoint(0);
                XYZ endPoint = line.GetEndPoint(1);

                if ((PointsMatch(startPoint, currentStartPoint) && PointsMatch(endPoint, currentEndPoint)) ||
                (PointsMatch(endPoint, currentStartPoint) && PointsMatch(startPoint, currentEndPoint)))
                {
                    return true;
                }
            }
            return false;
        }

        static bool PointsMatch(XYZ point1, XYZ point2)
        {
            double tolerance = 0.001;

            // Check if the difference between x and y coordinates is within the tolerance
            return Math.Abs(point1.X - point2.X) < tolerance && Math.Abs(point1.Y - point2.Y) < tolerance;
        }

        public static bool IsElementEligibleForPlanViewIndicator(Element element)
        {
            return TrudeDoor.IsDoor(element) || TrudeWindow.IsWindow(element);
        }
    }
}