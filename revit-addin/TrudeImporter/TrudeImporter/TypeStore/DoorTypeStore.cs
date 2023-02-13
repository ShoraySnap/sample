using Autodesk.Revit.DB;
using System;

namespace TrudeImporter
{
    public class DoorTypeStore : TypeStore<double[], FamilySymbol>
    {
        public override string KeyAdapter(double[] scaling, FamilySymbol defaultType)
        {
            double height = Math.Round(scaling[0], 3);
            double width = Math.Round(scaling[1], 3);
            return $"{defaultType.Name}_{height}x{width}";
        }

        public override void TypeModifier(double[] heightAndWidth, FamilySymbol type)
        {
            Parameter heightParam = type.get_Parameter(BuiltInParameter.DOOR_HEIGHT);
            Parameter widthParam = type.get_Parameter(BuiltInParameter.DOOR_WIDTH);

            if (heightParam == null || widthParam == null) return;

            if (!heightParam.IsReadOnly) heightParam.Set(heightAndWidth[0]);
            if (!widthParam.IsReadOnly) widthParam.Set(heightAndWidth[1]);
        }
    }
}
