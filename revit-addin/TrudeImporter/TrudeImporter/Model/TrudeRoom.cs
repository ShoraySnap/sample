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
        public int UniqueID; // Unique id of associated snaptrude mass/floor.
        public Solid Solid;
        public bool RoomMatched;
        public bool IsDirectShape;
        public CurveArray CurveArray;

        public TrudeRoom(string label, ElementId id, CurveArray curveArray, bool isDirectShape, int uniqueID)
        {
            RoomMatched = false;
            Solid = null;
            Label = label;
            Id = id;
            CurveArray = curveArray;
            IsDirectShape = isDirectShape;
            UniqueID = uniqueID;
        }
        public TrudeRoom(string label, ElementId id, CurveArray curveArray, bool isDirectShape)
        {
            RoomMatched = false;
            Solid = null;
            Label = label;
            Id = id;
            CurveArray = curveArray;
            IsDirectShape = isDirectShape;
        }
        public TrudeRoom(string label, ElementId id, List<XYZ> vertices)
        {
            RoomMatched = false;
            Solid = null;
            Label = label;
            Id = id;
            if (vertices != null) CurveArray = getProfile(vertices);
            IsDirectShape = true;
        }

        public static CurveArray getProfile(List<XYZ> vertices)
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

        public static void StoreRoomData(ElementId levelId, string roomType, Element element, CurveArray profile, int uniqueId = -1)
        {
            if (roomType != "Default")
            {
                element.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set(roomType);
                bool isDirectShape = element is DirectShape;
                TrudeRoom trudeRoom = new TrudeRoom(roomType, element.Id, profile, isDirectShape, uniqueId);
                if (GlobalVariables.CreatedFloorsByLevel.ContainsKey(levelId))
                    GlobalVariables.CreatedFloorsByLevel[levelId].Add(trudeRoom);
                else
                    GlobalVariables.CreatedFloorsByLevel.Add(levelId, new List<TrudeRoom> { trudeRoom });
            }
        }
    }
}
