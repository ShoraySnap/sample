using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrudeImporter
{
    public class TrudeWindowOld : TrudeModel
    {
        public static WindowTypeStore TypeStore = new WindowTypeStore();

        public FamilyInstance instance;

        public FamilyInstance CreateWindow(Document doc, FamilySymbol familySymbol, ElementId levelName, Wall wall, XYZ direction)
        {
            double windowHeight = familySymbol.get_Parameter(BuiltInParameter.WINDOW_HEIGHT).AsDouble();
            XYZ basePosition = new XYZ(Position.X, Position.Y, Position.Z - windowHeight / 2);

            Level level = doc.GetElement(levelName) as Level;

            if (wall is null)
            {
                wall = GetProximateWall(basePosition, doc, level.Id);
            }

            instance = doc.Create.NewFamilyInstance(basePosition, familySymbol, wall, level, StructuralType.NonStructural);

            // Done to make sure window is cutting the wall
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
