using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
using Material = Autodesk.Revit.DB.Material;

namespace TrudeImporter
{
    public class TrudeMass : TrudeModel
    {
        private int[] indices;

        public EulerAngles eulerAngles;
        bool HasRotationQuaternion;

        public Element element = null;

        public String FamilyName;
        public String FamilyTypeName;

        public TrudeMass(JToken massData)
        {
            JToken furnitureMeshData = massData["meshes"].First;

            Name = ((string)furnitureMeshData["name"]).RemoveIns();
            Position = TrudeRepository.GetPosition(massData);
            Scaling = TrudeRepository.GetScaling(massData);
            Rotation = TrudeRepository.GetRotation(massData);
            HasRotationQuaternion = TrudeRepository.HasRotationQuaternion(massData);
            eulerAngles = TrudeRepository.GetEulerAnglesFromRotationQuaternion(massData);
            levelNumber = TrudeRepository.GetLevelNumber(massData);

            FamilyName = TrudeRepository.GetFamilyName(massData); 
            FamilyTypeName = TrudeRepository.GetFamilyTypeName(massData); 
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
            if (origin == null) origin = (element.Location as LocationPoint).Point;

            //XYZ normal = new XYZ(0, 0, 1);
            XYZ normal = new XYZ(0, 1, 0);

            Plane pl = Plane.CreateByNormalAndOrigin(normal, origin);

            GlobalVariables.Document.Regenerate();
            var ids = ElementTransformUtils.MirrorElements(GlobalVariables.Document, new List<ElementId>() { element.Id }, pl, false);
        }

        public void CreateWithFamilySymbol(FamilySymbol familySymbol, double familyRotation, double instanceRotation, bool isFacingFlip, XYZ originOffset = null)
        {
            bool isSnaptrudeFlipped = Scaling.Z < 0;

            FamilyInstance instance = GlobalVariables.Document.Create.NewFamilyInstance(
                XYZ.Zero,
                familySymbol,
                Autodesk.Revit.DB.Structure.StructuralType.UnknownFraming);

            if (isFacingFlip && instance.FacingFlipped == false) FlipFacing(instance);

            if (isSnaptrudeFlipped) SnaptrudeFlip(instance);

            Transform offsetRotationTransform = Transform.CreateRotation(XYZ.BasisZ, familyRotation);

            originOffset = XYZ.Zero; // TODO: Investigate!!! I'm too burnt out to do it now.  Why does this work? We are using the offset for furniture and it seems to work, but doesn't work here.

            if (isSnaptrudeFlipped)
                originOffset = offsetRotationTransform.OfPoint(originOffset);
            else
                originOffset = offsetRotationTransform.OfPoint(-originOffset);

            Line rotationAxis = Line.CreateBound(originOffset, originOffset + XYZ.BasisZ);

            double rotationAngle = HasRotationQuaternion
                ? -eulerAngles.heading
                : -Rotation.Z;

            if (isSnaptrudeFlipped)
            {
                instance.Location.Rotate(rotationAxis, -familyRotation);
                instance.Location.Rotate(rotationAxis, rotationAngle);
            }
            else
            {
                instance.Location.Rotate(rotationAxis, familyRotation);
                instance.Location.Rotate(rotationAxis, rotationAngle);
            }

            instance.Location.Move(Position);

            element = instance;
        }

        public FamilyInstance CreateCopyFromInstance(FamilyInstance existingGroup, XYZ localOriginOffset, Boolean isFacingFlip)
        {
            XYZ originalPoint = ((LocationPoint)existingGroup.Location).Point;

            BoundingBoxXYZ bbox = existingGroup.get_BoundingBox(null);

            //XYZ center = (bbox.Max + bbox.Min).Divide(2);
            XYZ center = ((LocationPoint)existingGroup.Location).Point;

            ElementId newId = ElementTransformUtils.CopyElement(
                GlobalVariables.Document,
                existingGroup.Id,
                center.Multiply(-1))
                .First();

            this.element = GlobalVariables.Document.GetElement(newId);

            BoundingBoxXYZ bboxNew = this.element.get_BoundingBox(null);
            //XYZ centerNew = (bboxNew.Max + bboxNew.Min).Divide(2);

            //double originalRotation = ((LocationPoint)existingAssemblyInstance.Location).Rotation;

            LocationPoint pt = (LocationPoint)this.element.Location;
            XYZ centerNew = pt.Point;
            //ElementTransformUtils.RotateElement(GlobalVariables.Document, newId, Line.CreateBound(centerNew, centerNew + XYZ.BasisZ), -originalRotation);
            ElementTransformUtils.MoveElement(GlobalVariables.Document, newId, this.Position - localOriginOffset);

            XYZ centerAfterMove = ((LocationPoint)this.element.Location).Point;

            if (this.Scaling.Z < 0)
            {
                ElementTransformUtils.RotateElement(
                    GlobalVariables.Document,
                    newId,
                    Line.CreateBound(this.Position, this.Position + XYZ.BasisZ),
                    this.eulerAngles.heading);
            }
            else
            {
                ElementTransformUtils.RotateElement(
                    GlobalVariables.Document,
                    newId,
                    Line.CreateBound(this.Position, this.Position + XYZ.BasisZ),
                    -this.eulerAngles.heading);
            }


            if (isFacingFlip)
            {
                XYZ normal = (this.element as FamilyInstance).FacingOrientation;
                XYZ origin = (this.element.Location as LocationPoint).Point;
                Plane pl = Plane.CreateByNormalAndOrigin(normal, centerAfterMove);
                var ids = ElementTransformUtils.MirrorElements(GlobalVariables.Document, new List<ElementId>() { newId }, pl, false);
            }

            if (this.Scaling.Z < 0)
            {
                this.SnaptrudeFlip(this.element, this.Position);
            }

            return element as FamilyInstance;
        }

        public void Rotate(Location position)
        {
            if (HasRotationQuaternion)
            {

                LocationPoint pt = position as LocationPoint;

                Line localXAxis = Line.CreateBound(pt.Point, pt.Point + new XYZ(1, 0, 0));
                Line localYAxis = Line.CreateBound(pt.Point, pt.Point + new XYZ(0, 1, 0));
                Line localZAxis = Line.CreateBound(pt.Point, pt.Point + new XYZ(0, 0, 1));

                position.Rotate(localXAxis, -eulerAngles.bank);
                position.Rotate(localZAxis, -eulerAngles.heading);
                position.Rotate(localYAxis, -eulerAngles.attitude);
            }
            else
            {
                position.Rotate(Line.CreateBound(XYZ.Zero, XYZ.BasisX), -Rotation.X);
                position.Rotate(Line.CreateBound(XYZ.Zero, XYZ.BasisY), -Rotation.Y);
                position.Rotate(Line.CreateBound(XYZ.Zero, XYZ.BasisZ), -Rotation.Z);
            }
        }
    }
}
