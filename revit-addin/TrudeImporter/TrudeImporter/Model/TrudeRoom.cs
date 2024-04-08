using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrudeImporter
{
    public class TrudeRoom
    {
        public string Label;
        public ElementId Id;
        public Solid Solid;
        public bool RoomMatched;
        public bool IsDirectShape;
        public CurveArray CurveArray;

        public TrudeRoom(string label, ElementId id, CurveArray curveLoop, bool isDirectShape)
        {
            RoomMatched = false;
            Solid = null;
            Label = label;
            Id = id;
            CurveArray = curveLoop;
            IsDirectShape = isDirectShape;
        }
        public TrudeRoom(string label, ElementId id, List<XYZ> vertices, bool isDirectShape)
        {
            RoomMatched = false;
            Solid = null;
            Label = label;
            Id = id;
            if (vertices != null) CurveArray = getProfile(vertices);
            IsDirectShape = isDirectShape;
        }

        private CurveArray getProfile(List<XYZ> vertices)
        {
            CurveArray curves = new CurveArray();

            for (int i = 0; i < vertices.Count(); i++)
            {
                int currentIndex = i.Mod(vertices.Count());
                int nextIndex = (i + 1).Mod(vertices.Count());

                XYZ pt1 = vertices[currentIndex];
                XYZ pt2 = vertices[nextIndex];
                bool samePoint = false;

                while (pt1.DistanceTo(pt2) <= GlobalVariables.RvtApp.ShortCurveTolerance)
                {
                    //This can be potentially handled on snaptrude side by sending correct vertices. Currently, some points are duplicate.
                    if (pt1.X == pt2.X && pt1.Y == pt2.Y && pt1.Z == pt2.Z)
                    {
                        samePoint = true;
                        break;
                    }

                    i++;
                    if (i > vertices.Count() + 3) break;

                    nextIndex = (i + 1).Mod(vertices.Count());
                    pt2 = vertices[nextIndex];
                }
                if (samePoint) continue;
                curves.Append(Line.CreateBound(pt1, pt2));
            }
            return curves;
        }
    }
}
