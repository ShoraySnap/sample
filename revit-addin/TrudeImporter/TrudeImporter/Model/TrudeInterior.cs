using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
using Material = Autodesk.Revit.DB.Material;

namespace TrudeImporter
{
    public class TrudeInterior : TrudeModel
    {
        public int visiblity { get; set; }

        private int[] indices;
        private List<Point3D> verts = new List<Point3D>();

        private static Dictionary<string, TessellatedShapeBuilder> ShapeBuilders = new Dictionary<string, TessellatedShapeBuilder>();
        private string ShapeBuildersKey;

        public Element element = null;

        public double localBaseZ = 0;

        public String FamilyName;
        public String FamilyTypeName;
        public XYZ CenterPosition;

        public TrudeInterior(FurnitureProperties furnitureProperties)
        {
            Name = furnitureProperties.Name.RemoveIns();
            Position = furnitureProperties.Position;
            CenterPosition = furnitureProperties.CenterPosition;
            levelNumber = furnitureProperties.Storey;
            Rotation = furnitureProperties.Rotation;
            Scaling = furnitureProperties.Scaling;
            FamilyName = furnitureProperties.RevitFamilyName;
            FamilyTypeName = furnitureProperties.RevitFamilyType;
            localBaseZ = furnitureProperties.WorldBoundingBoxMin.Z - furnitureProperties.CenterPosition.Z;
        }

        public Parameter GetOffsetParameter(FamilyInstance instance)
        {
            if (instance == null) return null;

            Parameter offset = GlobalVariables.RvtApp.VersionNumber == "2019"
                ? instance.LookupParameter("Offset")
                : instance.LookupParameter("Offset from Host");

            return offset;
        }

        private void FlipFacing(FamilyInstance instance)
        {
            bool facingFlipResult = instance.flipFacing();

            if (!facingFlipResult)
            {
                XYZ normal = instance.FacingOrientation;
                XYZ origin = (instance.Location as LocationPoint).Point;
                Plane pl = Plane.CreateByNormalAndOrigin(normal, origin);
                var ids = ElementTransformUtils.MirrorElements(GlobalVariables.Document, new List<ElementId>() { instance.Id }, pl, false);
            }
        }

        public void SnaptrudeFlip(Element element, XYZ origin = null)
        {
            Parameter offset = GetOffsetParameter(element as FamilyInstance);

            if (origin == null) origin = (element.Location as LocationPoint).Point;

            //XYZ normal = new XYZ(0, 0, 1);
            XYZ normal = new XYZ(0, 1, 0);

            Plane pl = Plane.CreateByNormalAndOrigin(normal, origin);

            double originalOffset = 0;
            if (offset != null) originalOffset = offset.AsDouble();

            var ids = ElementTransformUtils.MirrorElements(GlobalVariables.Document, new List<ElementId>() { element.Id }, pl, false);

            if (offset != null) offset.Set(originalOffset);
        }

        public void CreateWithFamilySymbol(FamilySymbol familySymbol, Level level, double familyRotation, bool isFacingFlip, XYZ originOffset = null)
        {
            bool isSnaptrudeFlipped = Scaling.Z < 0;

            FamilyInstance instance = GlobalVariables.Document.Create.NewFamilyInstance(XYZ.Zero, familySymbol, level, level, Autodesk.Revit.DB.Structure.StructuralType.UnknownFraming);
            instance.LookupParameter("Length")?.Set(element.LookupParameter("Length").AsDouble());

            if (isFacingFlip && instance.FacingFlipped == false) FlipFacing(instance);
            GlobalVariables.Document.Regenerate();
            BoundingBoxXYZ boundingBox = instance.get_BoundingBox(null);
            XYZ boundingBoxCenter = (boundingBox.Max + boundingBox.Min)/2;

            if (isSnaptrudeFlipped) SnaptrudeFlip(instance);

            Transform offsetRotationTransform = Transform.CreateRotation(XYZ.BasisZ, familyRotation);

            if (isSnaptrudeFlipped)
                originOffset = offsetRotationTransform.OfPoint(originOffset);
            else
                originOffset = offsetRotationTransform.OfPoint(-originOffset);

            Line rotationAxis = Line.CreateBound(originOffset, originOffset + XYZ.BasisZ);
            if (instance.Category.Name == "Casework" || instance.Category.Name == "Furniture Systems")
                rotationAxis = Line.CreateBound(boundingBoxCenter, boundingBoxCenter + XYZ.BasisZ);

            instance.Location.Rotate(rotationAxis, -Rotation.Z);

            if (isSnaptrudeFlipped)
                instance.Location.Rotate(rotationAxis, -familyRotation);
            else
                instance.Location.Rotate(rotationAxis, familyRotation);

            XYZ positionRelativeToLevel = new XYZ(
                Position.X - originOffset.X,
                Position.Y - originOffset.Y,
                Position.Z - level.ProjectElevation);

            GlobalVariables.Document.Regenerate();

            if (instance.Category.Name == "Casework")
                instance.Location.Move(Position - boundingBoxCenter);
            else if (instance.Category.Name == "Furniture Systems")
            {
                XYZ position = Position - boundingBoxCenter;
                instance.Location.Move(new XYZ(position.X, position.Y, 0));
            }
            else
            instance.Location.Move(positionRelativeToLevel);

            element = instance;
        }

        public void CreateWithDirectShape(Document doc)
        {
            ElementId objectId = CreateDirectShape(doc);

            Location position = doc.GetElement(objectId).Location;

            Rotate(position);

            bool result = position.Move(Position);

            if (!result)
            {
                throw new Exception("Move furniture location failed.");
            }
        }

        public void Rotate(Location position)
        {
            position.Rotate(Line.CreateBound(XYZ.Zero, XYZ.BasisZ), -Rotation.Z);
        }

        private ElementId CreateDirectShape(Document doc)
        {


            TessellatedShapeBuilder builder = GetBuilder();

            ElementId categoryId = new ElementId(BuiltInCategory.OST_Furniture);
            DirectShape ds = DirectShape.CreateElement(doc, categoryId);
            ds.ApplicationId = "Application id";
            ds.ApplicationDataId = "Geometry object id";
            ds.Name = Name;

            builder.Target = TessellatedShapeBuilderTarget.Mesh;
            builder.Fallback = TessellatedShapeBuilderFallback.Salvage;
            builder.Build();
            var result = builder.GetBuildResult();
            ds.AppendShape(result.GetGeometricalObjects());

            return ds.Id;
        }

        private TessellatedShapeBuilder GetBuilder()
        {
            if (!ShapeBuilders.ContainsKey(ShapeBuildersKey))
            {
                TessellatedShapeBuilder builder = new TessellatedShapeBuilder();
                builder.OpenConnectedFaceSet(true);

                List<XYZ> loopVertices = new List<XYZ>(3);
                for (int i = 0; i < indices.Length; i += 6)
                {
                    // the vertices represent a degenrate triangle where meshPoint1 == meshPoint2,
                    // adding an offset to meshPoint1 so that the triangle won't be degenerate and can be used in TesseltatedShapeBuilder
                    // see https://forum.babylonjs.com/t/create-mesh-from-edgesrenderer/16464/5 for more info.
                    double offset = 0.003;
                    XYZ meshPoint1 = new XYZ(verts[indices[i]].X + offset, verts[indices[i]].Y + offset, verts[indices[i]].Z + offset);
                    XYZ meshPoint2 = new XYZ(verts[indices[i + 1]].X, verts[indices[i + 1]].Y, verts[indices[i + 1]].Z);
                    XYZ meshPoint3 = new XYZ(verts[indices[i + 2]].X, verts[indices[i + 2]].Y, verts[indices[i + 2]].Z);

                    loopVertices.Clear();
                    loopVertices.Add(meshPoint1);
                    loopVertices.Add(meshPoint2);
                    loopVertices.Add(meshPoint3);

                    TessellatedFace face = new TessellatedFace(loopVertices, GetMaterialId());
                    if (!face.IsValidObject) continue;

                    builder.AddFace(face);
                }
                builder.CloseConnectedFaceSet();

                ShapeBuilders.Add(ShapeBuildersKey, builder);
            }

            return ShapeBuilders[ShapeBuildersKey];
        }

        private static ElementId GetMaterialId(string materialName = "DefaultFurnitureMaterial", Color materialColor = null)
        {
            Material mat = Utils.FindElement(GlobalVariables.Document, typeof(Material), materialName) as Material;
            ElementId materialId;
            if (mat is null)
            {
                materialId = Material.Create(GlobalVariables.Document, materialName);

                mat = GlobalVariables.Document.GetElement(materialId) as Material;
                mat.Color = materialColor is null ? new Color(242, 242, 242) : materialColor;
            }
            else
            {
                materialId = mat.Id;
            }

            return materialId;
        }

    }
}
