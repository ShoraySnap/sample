using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;

namespace TrudeImporter
{
    public class XyzConverter : JsonCreationConverter<XYZ>
    {
        protected override XYZ Create(Type objectType, JToken jToken)
        {
            return TrudeRepository.ArrayToXYZ(jToken, false);
        }
    }
}
