using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace TrudeImporter
{
    public class TrudeDoor : TrudeModel
    {
        public static DoorTypeStore TypeStore = new DoorTypeStore();

        public FamilyInstance instance;

        public FamilyInstance CreateDoor(Document doc, FamilySymbol familySymbol, ElementId levelId, Wall wall, XYZ direction)
        {

            Level level = doc.GetElement(levelId) as Level;

            XYZ xyz = new XYZ(Position.X, Position.Y, 0.0);

            if (wall is null)
            {
                wall = GetProximateWall(xyz, doc, level.Id);
            }

            BoundingBoxXYZ bbox =  wall.get_BoundingBox(null);

            XYZ loc = new XYZ(Position.X, Position.Y, bbox.Min.Z);

            instance = doc.Create.NewFamilyInstance(loc, familySymbol, wall, (Level)doc.GetElement(wall.LevelId), StructuralType.NonStructural);

            // Done to make sure door is cutting the wall
            // See https://forums.autodesk.com/t5/revit-api-forum/create-doors-but-not-cutting-through-wall/td-p/5564330
            instance.flipFacing();
            doc.Regenerate();

            instance.flipFacing();
            doc.Regenerate();

            if (!instance.FacingOrientation.IsAlmostEqualTo(direction))
            {
                instance.flipFacing();
                instance.flipHand();
            }

            return instance;
        }
    }
}
