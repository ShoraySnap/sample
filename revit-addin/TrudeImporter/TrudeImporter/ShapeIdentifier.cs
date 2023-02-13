using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrudeImporter
{
    public abstract class ShapeProperties
    {
        public double rotation = 0;

        public abstract string ToFamilyName();
    }
    public class RectangularProperties : ShapeProperties
    {
        public double depth;
        public double width;

        public override string ToFamilyName()
        {
            return $"rectangular_{UnitsAdapter.FeetToMM(depth)}x{UnitsAdapter.FeetToMM(width)}";
        }
    }
    public class LShapeProperties : ShapeProperties
    {
        public double depth;
        public double breadth;
        public double thickness;

        public override string ToFamilyName()
        {
            return $"L_{UnitsAdapter.FeetToMM(depth)}x{UnitsAdapter.FeetToMM(breadth)}x{UnitsAdapter.FeetToMM(thickness)}";
        }
    }
    public class CShapeProperties : ShapeProperties
    {
        public double depth;
        public double flangeBreadth;
        public double flangeThickness;
        public double webThickness;

        public override string ToFamilyName()
        {
            return $"C_{UnitsAdapter.FeetToMM(depth)}x{UnitsAdapter.FeetToMM(webThickness)}x{UnitsAdapter.FeetToMM(flangeBreadth)}x{UnitsAdapter.FeetToMM(flangeThickness)}";
        }
    }

    public class HShapeProperties : ShapeProperties
    {
        public double depth;
        public double flangeBreadth;
        public double flangeThickness;
        public double webThickness;
        public override string ToFamilyName()
        {
            return $"H_{UnitsAdapter.FeetToMM(depth)}x{UnitsAdapter.FeetToMM(webThickness)}x{UnitsAdapter.FeetToMM(flangeBreadth)}x{UnitsAdapter.FeetToMM(flangeThickness)}";
        }
    }

    public class ShapeIdentifier
    {
        public const int XY = 0;
        public const int YZ = 1;
        int planeId;

        public ShapeIdentifier()
        {
            planeId = XY;
        }
        public ShapeIdentifier(int planeId)
        {
            this.planeId = planeId;
        }

        private XYZ rotationVector;
        public ShapeProperties GetShapeProperties(List<XYZ> vertices, bool inverseDirection = false)
        {
            rotationVector = planeId == XY
                           ? new XYZ(-1, 0, 0)
                           : new XYZ(0, inverseDirection ? -1 : 1, 0);
            ShapeProperties props;
            props = IsRectangular(vertices);
            if (props != null) return props;

            props = IsLShaped(vertices);
            if (props != null) return props;

            props = IsHShaped(vertices);
            if (props != null) return props;

            props = IsCShaped(vertices);
            if (props != null) return props;

            return null;
        }

        private  RectangularProperties IsRectangular(List<XYZ> vertices)
        {
            RectangularProperties props = new RectangularProperties();

            List<Line> edges = GetEdges(vertices, 4);

            if (edges == null) return null;
            if (edges.Count != 4) return null;

            if (!edges[0].Length.RoundedEquals(edges[2].Length)) return null;
            if (!edges[1].Length.RoundedEquals(edges[3].Length)) return null;

            if (!ComputeNormal(edges[0]).RoundedEquals(-ComputeNormal(edges[2]))) return null;
            if (!ComputeNormal(edges[1]).RoundedEquals(-ComputeNormal(edges[3]))) return null;

            props.depth = edges[0].Length;
            props.width = edges[1].Length;

            // Get direction
            XYZ localOrigin;
            int baseEdge = -1;

            XYZ p0;
            XYZ p1;

            if (edges[0].GetEndPoint(0).IsAlmostEqualTo(edges[1].GetEndPoint(0)))
            {
                localOrigin = Round(edges[0].GetEndPoint(0));
                p0 = Round(edges[0].GetEndPoint(1)) - localOrigin;
                p1 = Round(edges[1].GetEndPoint(1)) - localOrigin;
            }
            else if(edges[0].GetEndPoint(0).IsAlmostEqualTo(edges[1].GetEndPoint(1)))
            {
                localOrigin = Round(edges[0].GetEndPoint(0));
                p0 = Round(edges[0].GetEndPoint(1)) - localOrigin;
                p1 = Round(edges[1].GetEndPoint(0)) - localOrigin;
            }
            else if(edges[0].GetEndPoint(1).IsAlmostEqualTo(edges[1].GetEndPoint(0)))
            {
                localOrigin = Round(edges[0].GetEndPoint(1));
                p0 = Round(edges[0].GetEndPoint(0)) - localOrigin;
                p1 = Round(edges[1].GetEndPoint(1)) - localOrigin;
            }
            else//if(edges[0].GetEndPoint(1).IsAlmostEqualTo(edges[1].GetEndPoint(1)))
            {
                localOrigin = Round(edges[0].GetEndPoint(1));
                p0 = Round(edges[0].GetEndPoint(0)) - localOrigin;
                p1 = Round(edges[1].GetEndPoint(0)) - localOrigin;
            }

            if (InFirstQuadrant(p0) && InSecondQuadrant(p1) || InSecondQuadrant(p0) && InThirdQuadrant(p1))
            {
                baseEdge = 0;
                props.rotation = ComputeNormal(edges[baseEdge]).AngleTo(rotationVector);
            }

            else if (InFirstQuadrant(p1) && InSecondQuadrant(p0) || InSecondQuadrant(p1) && InThirdQuadrant(p0))
            {
                baseEdge = 1;
                props.rotation = ComputeNormal(edges[baseEdge]).AngleTo(rotationVector);
            }

            else if (InThirdQuadrant(p0) && InFourthQuadrant(p1) || InFourthQuadrant(p0) && InFirstQuadrant(p1))
            {
                baseEdge = 0;
                props.rotation = -ComputeNormal(edges[baseEdge]).AngleTo(rotationVector);
            }
            else if (InThirdQuadrant(p1) && InFourthQuadrant(p0) || InFourthQuadrant(p1) && InFirstQuadrant(p0))
            {
                baseEdge = 1;
                props.rotation = -ComputeNormal(edges[baseEdge]).AngleTo(rotationVector);
            }

            props.width = edges[baseEdge].Length;
            props.depth = edges[baseEdge == 0 ?  1 : 0].Length;

            return props;
        }

        //   -t-
        //    _
        // | | |
        // d | |
        // | | |___
        // | |_____| t
        //   ---b---
        private LShapeProperties IsLShaped(List<XYZ> vertices)
        {
            LShapeProperties props = new LShapeProperties();

            List<Line> edges = GetEdges(vertices, 6);

            if (edges is null) return null;
            if (edges.Count != 6) return null;

            // identify "thickness edges", they are 3 indices away from eachother with equal length
            int tEdgeIndex0 = -1;
            int tEdgeIndex1 = -1;
            for(int i = 0; i < 3; i++)
            {
                if (edges[i].Length.RoundedEquals(edges[i+3].Length))
                {
                    tEdgeIndex0 = i;
                    tEdgeIndex1 = i + 3;
                }
            }

            if (tEdgeIndex0 == -1) return null;
            if (!AreOrthogonal(edges[tEdgeIndex0], edges[tEdgeIndex1])) return null;

            props.thickness = edges[tEdgeIndex0].Length;

            // Find the longest edge, this is either the depth or breadth edge.
            int longestEdge = -1;
            double longest = 0;
            for(int i = 0; i < 6; i++)
            {
                if (i == tEdgeIndex0 || i == tEdgeIndex1) continue;

                if (edges[i].Length > longest)
                {
                    longestEdge = i;
                    longest = edges[i].Length;
                }
            }

            // Find the edge adjacent to the longest edge which is not a "thickness edge". This is the other depth/breadth edge.
            int adjEdge = -1;

            if (!edges[mod(longestEdge - 1, 6)].Length.RoundedEquals(props.thickness))
                adjEdge = mod(longestEdge - 1, 6);
            else if (!edges[mod(longestEdge + 1, 6)].Length.RoundedEquals(props.thickness))
                adjEdge = mod(longestEdge + 1, 6);

            if (!AreOrthogonal(edges[longestEdge], edges[adjEdge])) return null;

            int oppAdjIndex = -1;
            int oppLongestIndex = -1;
            for(int i = 0; i < 6; i++)
            {
                if (i == tEdgeIndex0 || i == tEdgeIndex1 || i == longestEdge || i == adjEdge) continue;
                if (AreParallel(edges[i], edges[longestEdge])) oppLongestIndex = i;
                if (AreParallel(edges[i], edges[adjEdge])) oppAdjIndex = i;
            }

            if (oppLongestIndex == -1) return null;
            if (oppAdjIndex == -1) return null;
            if (!edges[adjEdge].Length.RoundedEquals(edges[oppAdjIndex].Length + props.thickness)) return null;
            if (!edges[longestEdge].Length.RoundedEquals(edges[oppLongestIndex].Length + props.thickness)) return null;

            // Get direction
            XYZ localOrigin;
            XYZ p0;
            XYZ p1;

            if (edges[longestEdge].GetEndPoint(0).IsAlmostEqualTo(edges[adjEdge].GetEndPoint(0)))
            {
                localOrigin = Round(edges[longestEdge].GetEndPoint(0));
                p0 = Round(edges[longestEdge].GetEndPoint(1)) - localOrigin;
                p1 = Round(edges[adjEdge].GetEndPoint(1)) - localOrigin;
            }
            else if(edges[longestEdge].GetEndPoint(0).IsAlmostEqualTo(edges[adjEdge].GetEndPoint(1)))
            {
                localOrigin = Round(edges[longestEdge].GetEndPoint(0));
                p0 = Round(edges[longestEdge].GetEndPoint(1)) - localOrigin;
                p1 = Round(edges[adjEdge].GetEndPoint(0)) - localOrigin;
            }
            else if(edges[longestEdge].GetEndPoint(1).IsAlmostEqualTo(edges[adjEdge].GetEndPoint(0)))
            {
                localOrigin = Round(edges[longestEdge].GetEndPoint(1));
                p0 = Round(edges[longestEdge].GetEndPoint(0)) - localOrigin;
                p1 = Round(edges[adjEdge].GetEndPoint(1)) - localOrigin;
            }
            else//if(edges[longestEdge].GetEndPoint(1).IsAlmostEqualTo(edges[adjEdge].GetEndPoint(1)))
            {
                localOrigin = Round(edges[longestEdge].GetEndPoint(1));
                p0 = Round(edges[longestEdge].GetEndPoint(0)) - localOrigin;
                p1 = Round(edges[adjEdge].GetEndPoint(0)) - localOrigin;
            }

            int baseEdge = -1;
            if (InFirstQuadrant(p0) && InSecondQuadrant(p1) || InSecondQuadrant(p0) && InThirdQuadrant(p1))
            {
                baseEdge = longestEdge;
                props.rotation = ComputeNormal(edges[baseEdge]).AngleTo(rotationVector);
            }

            else if (InFirstQuadrant(p1) && InSecondQuadrant(p0) || InSecondQuadrant(p1) && InThirdQuadrant(p0))
            {
                baseEdge = adjEdge;
                props.rotation = ComputeNormal(edges[baseEdge]).AngleTo(rotationVector);
            }

            else if (InThirdQuadrant(p0) && InFourthQuadrant(p1) || InFourthQuadrant(p0) && InFirstQuadrant(p1))
            {
                baseEdge = longestEdge;
                props.rotation = -ComputeNormal(edges[baseEdge]).AngleTo(rotationVector);
            }
            else if (InThirdQuadrant(p1) && InFourthQuadrant(p0) || InFourthQuadrant(p1) && InFirstQuadrant(p0))
            {
                baseEdge = adjEdge;
                props.rotation = -ComputeNormal(edges[baseEdge]).AngleTo(rotationVector);
            }

            props.breadth = edges[baseEdge].Length;
            props.depth = edges[baseEdge == longestEdge ?  adjEdge : longestEdge].Length;

            return props;
        }

        private bool InFirstQuadrant(XYZ p)
        {
            if(planeId == XY) return p.X >= 0 && p.Y >= 0;
            if(planeId == YZ) return p.Y >= 0 && p.Z >= 0;

            return false;
        }
        private bool InSecondQuadrant(XYZ p)
        {
            if (planeId == XY) return p.X <= 0 && p.Y >= 0;
            if (planeId == YZ) return p.Y <= 0 && p.Z >= 0;

            return false;
        }
        private bool InThirdQuadrant(XYZ p)
        {
            if (planeId == XY) return p.X <= 0 && p.Y <= 0;
            if (planeId == YZ) return p.Y <= 0 && p.Z <= 0;

            return false;
        }
        private bool InFourthQuadrant(XYZ p)
        {
            if (planeId == XY) return p.X >= 0 && p.Y <= 0;
            if (planeId == YZ) return p.Y >= 0 && p.Z <= 0;

            return false;
        }

        //         ----b--- (breadth of flange)
        //          _______
        //        ||___ ___| tf (thickness of flange)
        //        |   | |     |
        // (depth)d   | |     (web)
        //        |   | |     |
        //        | __| |__   |
        //        ||_______|
        //           -tw-
        //     (thickness of web)

        // TODO: Fix H shaped column in http://app.snaptru.de/model/AJUD87/
        private HShapeProperties IsHShaped(List<XYZ> vertices)
        {

            HShapeProperties props = new HShapeProperties();

            List<Line> edges = GetEdges(vertices, 12);
            if (edges is null) return null;
            if (edges.Count != 12) return null;

            // find flange thickness edges
            int tf0 = -1;
            int tf1 = -1;
            int tf2 = -1;
            int tf3 = -1;

            for (int i = 0; i < 8; i++)
            {
                if( edges[i].Length.RoundedEquals(edges[mod(i + 2, 12)].Length)
                 && edges[i].Length.RoundedEquals(edges[mod(i + 6, 12)].Length)
                 && edges[i].Length.RoundedEquals(edges[mod(i + 8, 12)].Length) )
                {
                    tf0 = i; tf1 = i + 2; tf2 = i + 6; tf3 = i + 8;
                }
                else if( edges[i].Length.RoundedEquals(edges[mod(i + 4, 12)].Length)
                      && edges[i].Length.RoundedEquals(edges[mod(i + 6, 12)].Length)
                      && edges[i].Length.RoundedEquals(edges[mod(i + 10, 12)].Length) )
                {
                    tf0 = i; tf1 = i + 4; tf2 = i + 6; tf3 = i + 10;
                }
            }
            if (tf0 == -1 || tf1 == -1 || tf2 == -1 || tf3 == -1) return null;

            double flangeThickness = edges[tf0].Length;

            // Find the edges which are adjacent to flange thickness edges which are the longest.
            // These will be the flange breadth edges

            int bf0 = edges[mod(tf0 - 1, 12)].Length > edges[mod(tf0 + 1, 12)].Length ? mod(tf0 - 1, 12) : mod(tf0 + 1, 12);
            int bf1 = edges[mod(tf2 - 1, 12)].Length > edges[mod(tf2 + 1, 12)].Length ? mod(tf2 - 1, 12) : mod(tf2 + 1, 12);

            double flangeBreadth = edges[bf0].Length; 
            if (!edges[bf0].Length.RoundedEquals(edges[bf1].Length)) return null;
            if (!AreParallel(edges[bf0], edges[bf1])) return null;

            // Find web edges

            int w0 = mod(bf0 + 3, 12);
            int w1 = mod(bf0 - 3, 12);

            if (!edges[w0].Length.RoundedEquals(edges[w1].Length)) return null;
            if (!AreParallel(edges[w0], edges[w1])) return null;

            double depth = edges[w0].Length + (flangeThickness * 2);

            props.depth = depth;
            props.flangeBreadth = flangeBreadth;
            props.flangeThickness = flangeThickness;
            props.webThickness = Distance(edges[w0], edges[w1]);

            Line lower = GetLowerLine(edges[bf0], edges[bf1]) == 0 ? edges[bf0] : edges[bf1];


            // Get direction
            XYZ localOrigin = GetMidPoint(lower);
            XYZ p0 = Round(lower.GetEndPoint(0)) - localOrigin;
            XYZ p1 = Round(lower.GetEndPoint(1)) - localOrigin;

            if (InSecondQuadrant(p0) && InFourthQuadrant(p1) || InSecondQuadrant(p1) && InFourthQuadrant(p0))
            {
                props.rotation = -ComputeNormal(lower).AngleTo(rotationVector);
            }
            else
            {
                props.rotation = ComputeNormal(lower).AngleTo(rotationVector);
            }

            return props;
        }

        private XYZ GetMidPoint(Line line)
        {
            return new XYZ
            (
                (line.GetEndPoint(0).X + line.GetEndPoint(1).X) / 2,
                (line.GetEndPoint(0).Y + line.GetEndPoint(1).Y) / 2,
                (line.GetEndPoint(0).Z + line.GetEndPoint(1).Z) / 2
            );
        }

        //           (breadth flange)
        //           ---bf---
        //            _______
        //         | |       | tf (thickness flange)
        //         | |   ____|
        //         | |  |    
        // (depth) d |  |
        //         | |  |
        //         | |  |____
        //         | |       |
        //         | |_______|
        //           -bw-
        //           (breadth of web)
        private CShapeProperties IsCShaped(List<XYZ> vertices)
        {

            CShapeProperties props = new CShapeProperties();

            List<Line> edges = GetEdges(vertices, 8);

            if (edges is null) return null;
            if (edges.Count != 8) return null;

            // Find flange thickness edges
            int tf0 = -1;
            int tf1 = -1;
            for(int i = 0; i < 8; i++)
            {
                if (edges[i].Length.RoundedEquals(edges[mod(i + 4, 8)].Length))
                {
                    if (AreParallel(edges[i], edges[mod(i + 4, 8)]))
                    {
                        tf0 = i;
                        tf1 = mod(i + 4, 8);
                        break;
                    }
                }
            }

            if (tf0 == -1) return null;

            double flangeThickness = edges[tf1].Length;

            // find the flange breadth edges, these are  the longer edges
            // which are adjacent to the flange thickness edges 
            int bf0 = edges[mod(tf0 - 1, 8)].Length > edges[mod(tf0 + 1, 8)].Length ? mod(tf0 - 1, 8) : mod(tf0 + 1, 8);
            int bf1 = edges[mod(tf1 - 1, 8)].Length > edges[mod(tf1 + 1, 8)].Length ? mod(tf1 - 1, 8) : mod(tf1 + 1, 8);

            double flangeBreadth = edges[bf0].Length;
            if (!edges[bf0].Length.RoundedEquals(edges[bf1].Length)) return null;
            if (!AreParallel(edges[bf0], edges[bf1])) return null;

            // find web edges
            int wOuter; 
            int wInner;
            if(edges[mod(tf0 + 2, 8)].Length > edges[mod(tf0 - 2, 8)].Length)
            {
                wOuter = mod(tf0 + 2, 8);
                wInner = mod(tf0 - 2, 8);
            }
            else
            {
                wOuter = mod(tf0 - 2, 8);
                wInner = mod(tf0 + 2, 8);
            }

            double webThickness = Distance(edges[wOuter], edges[wInner]);

            // Get inner flange edges to check if shape is correct

            for (int i = 0; i < 8; i++)
            {
                if (i == tf0 || i == tf1 || i == bf0 || i == bf1 || i == wOuter || i == wInner) continue;

                if (!(edges[i].Length + webThickness).RoundedEquals(flangeBreadth)) return null;
            }

            props.depth = edges[wOuter].Length;
            props.flangeBreadth = flangeBreadth;
            props.flangeThickness = flangeThickness;
            props.webThickness = Distance(edges[wOuter], edges[wInner]);

            // Get direction
            // The method used here is same as the one used in L shape,
            // the web outer edge and one of the flange outer edges form the L shape.
            XYZ localOrigin;
            int baseEdge = -1;

            XYZ pWOuter;
            XYZ pBf;

            if (edges[wOuter].GetEndPoint(0).IsAlmostEqualTo(edges[bf0].GetEndPoint(0)))
            {
                localOrigin = Round(edges[wOuter].GetEndPoint(0));
                pWOuter = Round(edges[wOuter].GetEndPoint(1)) - localOrigin;
                pBf = Round(edges[bf0].GetEndPoint(1)) - localOrigin;
            }
            else if(edges[wOuter].GetEndPoint(0).IsAlmostEqualTo(edges[bf0].GetEndPoint(1)))
            {
                localOrigin = Round(edges[wOuter].GetEndPoint(0));
                pWOuter = Round(edges[wOuter].GetEndPoint(1)) - localOrigin;
                pBf = Round(edges[bf0].GetEndPoint(0)) - localOrigin;
            }
            else if(edges[wOuter].GetEndPoint(1).IsAlmostEqualTo(edges[bf0].GetEndPoint(0)))
            {
                localOrigin = Round(edges[wOuter].GetEndPoint(1));
                pWOuter = Round(edges[wOuter].GetEndPoint(0)) - localOrigin;
                pBf = Round(edges[bf0].GetEndPoint(1)) - localOrigin;
            }
            else
            {
                localOrigin = Round(edges[wOuter].GetEndPoint(1));
                pWOuter = Round(edges[wOuter].GetEndPoint(0)) - localOrigin;
                pBf = Round(edges[bf0].GetEndPoint(0)) - localOrigin;
            }

            if (InFirstQuadrant(pWOuter) && InSecondQuadrant(pBf) || InSecondQuadrant(pWOuter) && InThirdQuadrant(pBf))
            {
                baseEdge = bf1;
            }

            else if (InFirstQuadrant(pBf) && InSecondQuadrant(pWOuter) || InSecondQuadrant(pBf) && InThirdQuadrant(pWOuter))
            {
                baseEdge = bf0;
            }

            else if (InThirdQuadrant(pWOuter) && InFourthQuadrant(pBf) || InFourthQuadrant(pWOuter) && InFirstQuadrant(pBf))
            {
                baseEdge = bf1;
            }
            else if (InThirdQuadrant(pBf) && InFourthQuadrant(pWOuter) || InFourthQuadrant(pBf) && InFirstQuadrant(pWOuter))
            {
                baseEdge = bf0;
            }

            if (edges[wOuter].GetEndPoint(0).IsAlmostEqualTo(edges[baseEdge].GetEndPoint(0)))
            {
                localOrigin = Round(edges[wOuter].GetEndPoint(0));
                pWOuter = Round(edges[wOuter].GetEndPoint(1)) - localOrigin;
                pBf = Round(edges[baseEdge].GetEndPoint(1)) - localOrigin;
            }
            else if(edges[wOuter].GetEndPoint(0).IsAlmostEqualTo(edges[baseEdge].GetEndPoint(1)))
            {
                localOrigin = Round(edges[wOuter].GetEndPoint(0));
                pWOuter = Round(edges[wOuter].GetEndPoint(1)) - localOrigin;
                pBf = Round(edges[baseEdge].GetEndPoint(0)) - localOrigin;
            }
            else if(edges[wOuter].GetEndPoint(1).IsAlmostEqualTo(edges[baseEdge].GetEndPoint(0)))
            {
                localOrigin = Round(edges[wOuter].GetEndPoint(1));
                pWOuter = Round(edges[wOuter].GetEndPoint(0)) - localOrigin;
                pBf = Round(edges[baseEdge].GetEndPoint(1)) - localOrigin;
            }
            else//if(edges[wOuter].GetEndPoint(1).IsAlmostEqualTo(edges[bf].GetEndPoint(1)))
            {
                localOrigin = Round(edges[wOuter].GetEndPoint(1));
                pWOuter = Round(edges[wOuter].GetEndPoint(0)) - localOrigin;
                pBf = Round(edges[baseEdge].GetEndPoint(0)) - localOrigin;
            }

            if (InFirstQuadrant(pWOuter) && InSecondQuadrant(pBf) || InSecondQuadrant(pWOuter) && InThirdQuadrant(pBf))
            {
                props.rotation = ComputeNormal(edges[baseEdge]).AngleTo(rotationVector);
            }
            else if (InFirstQuadrant(pBf) && InSecondQuadrant(pWOuter) || InSecondQuadrant(pBf) && InThirdQuadrant(pWOuter))
            {
                props.rotation = ComputeNormal(edges[baseEdge]).AngleTo(rotationVector);
            }
            else if (InThirdQuadrant(pWOuter) && InFourthQuadrant(pBf) || InFourthQuadrant(pWOuter) && InFirstQuadrant(pBf))
            {
                props.rotation = -ComputeNormal(edges[baseEdge]).AngleTo(rotationVector);
            }
            else if (InThirdQuadrant(pBf) && InFourthQuadrant(pWOuter) || InFourthQuadrant(pBf) && InFirstQuadrant(pWOuter))
            {
                props.rotation = -ComputeNormal(edges[baseEdge]).AngleTo(rotationVector);
            }

            return props;
        }

        private int mod(int x, int m)
        {
            return (x % m + m) % m;
        }

        private bool AreOrthogonal(Line a, Line b)
        {
            double angle = Math.Abs(Math.Round(Angle(a, b), 3));
            return  angle == Math.Round(Math.PI / 2, 3);
        }
        private bool AreParallel(Line a, Line b)
        {
            double angle = Math.Abs(Math.Round(Angle(a, b), 3));
            return angle == Math.Round(Math.PI, 3) || angle == 0 ;
        }

        private double Angle(Line a, Line b)
        {
            return ComputeNormal(a).AngleTo(ComputeNormal(b));
        }

        private XYZ Round(XYZ xyz, int precision = 3)
        {
            return new XYZ(Math.Round(xyz.X, precision), Math.Round(xyz.Y, precision), Math.Round(xyz.Z, precision));
        }

        // TODO: The joinLines is a quick fix to test H-shape identification, fix the getEdges method for H-shape
        private List<Line> GetEdges(List<XYZ> points, int maxEdges = -1, bool joinLines = true)
        {
            List<Line> lines = new List<Line>();

            for (int currIndex = 0; currIndex < points.Count; currIndex++)
            {
                int prevIndex = currIndex == 0 ? points.Count - 1 : currIndex - 1;

                if (ShouldJoinLastLine(lines, points[currIndex]) && joinLines)
                {
                    Line lastLine = lines.Last();
                    if (lastLine.GetEndPoint(0).RoundedEquals(points[currIndex])) continue;

                    Line joinedLine = Line.CreateBound(lastLine.GetEndPoint(0), points[currIndex]);
                    lines[lines.Count - 1] = joinedLine;
                }
                else if (ShouldJoinFirstLine(lines, points[prevIndex]) && currIndex == points.Count - 1 && joinLines)
                {
                    Line firstLine = lines.First();
                    if (points[prevIndex].RoundedEquals(firstLine.GetEndPoint(1))) continue;

                    Line joinedLine = Line.CreateBound(points[prevIndex], firstLine.GetEndPoint(1));
                    lines[0] = joinedLine;
                }
                else
                {
                    if (points[prevIndex].RoundedEquals(points[currIndex])) continue;

                    lines.Add(Line.CreateBound(points[prevIndex], points[currIndex]));
                }

                if (lines.Count > maxEdges + 1 && maxEdges != -1) return null;
            }

            // check if first and last line should be joined
            Line newLine = Line.CreateBound(lines.Last().GetEndPoint(0), lines.First().GetEndPoint(1));
            if(newLine.ContainsLine(lines.First()))
            {
                lines[0] = newLine;
                lines.RemoveAt(lines.Count - 1);
            }

            return lines;
        }

        private bool ShouldJoinLastLine(List<Line> lines, XYZ newPoint)
        {
            if (lines.Count == 0) return false;

            Line lastLine = lines.Last();
            try
            {
                Line joinedLine = Line.CreateBound(lastLine.GetEndPoint(0), newPoint);

                return joinedLine.ContainsLine(lastLine);
            }
            catch
            {
                return false;
            }
        }
        private bool ShouldJoinFirstLine(List<Line> lines, XYZ newPoint)
        {
            if (lines.Count == 0) return false;

            Line firstLine = lines.First();

            if (firstLine.GetEndPoint(1).RoundedEquals(newPoint)) return false;

            Line joinedLine = Line.CreateBound(newPoint, firstLine.GetEndPoint(1));

            return joinedLine.ContainsLine(firstLine);
        }

        private XYZ ComputeNormal(Line line)
        {
            XYZ startPoint = line.GetEndPoint(0);
            XYZ endPoint = line.GetEndPoint(1);

            XYZ direction = (endPoint - startPoint).Normalize();
            return Round(direction);
        }

        private double Distance(Line a, Line b)
        {
            double d1 = a.Distance(b.GetEndPoint(0));
            double d2 = b.Distance(a.GetEndPoint(0));
            return d1 < d2 ? d1 : d2;
        }

        private int GetLowerLine(Line a, Line b)
        {
            double[] allPoints = { a.GetEndPoint(0).Y, a.GetEndPoint(1).Y, b.GetEndPoint(0).Y, b.GetEndPoint(1).Y };
            double min = allPoints.Min();

            if (a.GetEndPoint(0).Y == min || a.GetEndPoint(1).Y == min) return 0;

            return 1;
        }
    }
}
