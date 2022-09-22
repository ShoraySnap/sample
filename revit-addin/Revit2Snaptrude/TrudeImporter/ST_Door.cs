using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snaptrude
{
    public class ST_Door : ST_Abstract
    {
        public static DoorTypeStore TypeStore = new DoorTypeStore();

        public FamilyInstance instance;

        public FamilyInstance CreateDoor(Document doc, FamilySymbol familySymbol, ElementId levelName, Wall wall, XYZ direction)
        {

            Level level = doc.GetElement(levelName) as Level;

            XYZ xyz = new XYZ(Position.X, Position.Y, 0.0);

            if (wall is null)
            {
                wall = GetProximateWall(xyz, doc, level.Id);
            }

            XYZ loc = new XYZ(Position.X, Position.Y, level.Elevation);

            instance = doc.Create.NewFamilyInstance(loc, familySymbol, wall, level, StructuralType.NonStructural);

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
