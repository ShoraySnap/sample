﻿using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;
using System.Collections.Generic;

namespace TrudeImporter
{
    public class TrudeBeam : TrudeModel
    {
        private Plane countoursPlane;
        private bool inverseDirection = false;
        private Transform rotationTransform;
        private XYZ startFaceCentroid;
        private XYZ endFaceCentroid;
        private List<XYZ> LocalStartFaceVertices = new List<XYZ>();
        private string familyName;
        public static Dictionary<string, FamilySymbol> types = new Dictionary<string, FamilySymbol>();
        private BeamRfaGenerator beamRfaGenerator = new BeamRfaGenerator();
        private List<BeamInstanceProperties> instances;


        public TrudeBeam (BeamProperties beamProps, ElementId levelId)
        {
            instances = beamProps.Instances;
            this.countoursPlane = Plane.CreateByThreePoints(Extensions.Round(beamProps.FaceVertices[0]), Extensions.Round(beamProps.FaceVertices[1]), Extensions.Round(beamProps.FaceVertices[2]));

            // Get rotation angle required to align face plane with the YZ plane. (The faces are parallel to the YZ plane in rfa file)
            XYZ YZPlaneNormal = new XYZ(-1, 0, 0);

            XYZ axisOfRotation = XYZ.BasisZ;
            double rotationAngle = this.countoursPlane.Normal.AngleTo(YZPlaneNormal);

            if (this.countoursPlane.Normal.Z == 1 || this.countoursPlane.Normal.Z == -1)
            {
                axisOfRotation = XYZ.BasisY;
                rotationAngle = this.countoursPlane.Normal.AngleTo(YZPlaneNormal);
            }

            this.startFaceCentroid = (beamProps.FaceVertices[0] + beamProps.FaceVertices[1] + beamProps.FaceVertices[2] + beamProps.FaceVertices[3]) / 4;
            if (startFaceCentroid.Y > beamProps.CenterPosition.Y) this.inverseDirection = true;

            this.endFaceCentroid = beamProps.CenterPosition + (beamProps.CenterPosition - startFaceCentroid);

            this.rotationTransform = Transform.CreateRotation(axisOfRotation, inverseDirection ? rotationAngle : - rotationAngle);
          
            // Find local face vertices
            foreach (var point in beamProps.FaceVertices)
            {
                this.LocalStartFaceVertices.Add(point - startFaceCentroid);
            }

            CreateBeam(levelId);
        }

        private void CreateBeam(ElementId levelId)
        {
            List<XYZ> rotatedFaceVertices = RotateCountoursParallelToMemberRightPlane();

            ShapeIdentifier shapeIdentifier = new ShapeIdentifier(ShapeIdentifier.YZ);
            ShapeProperties shapeProperties = shapeIdentifier.GetShapeProperties(rotatedFaceVertices, inverseDirection);

            familyName = shapeProperties is null ? $"beam_custom_{Utils.RandomString(5)}" : $"beam_{shapeProperties.ToFamilyName()}";

            string baseDir = GlobalVariables.ForForge
                ? "."
                : $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/{Configs.CUSTOM_FAMILY_DIRECTORY}";

            CreateFamilyTypeIfNotExist(GlobalVariables.RvtApp, GlobalVariables.Document, familyName, shapeProperties, rotatedFaceVertices, baseDir);
            CreateFamilyInstances(GlobalVariables.Document, familyName, levelId, shapeProperties);

            BeamRfaGenerator.DeleteAll();
        }

        private List<XYZ> RotateCountoursParallelToMemberRightPlane()
        {
            const double REF_PLANE_MEMBER_LEFT_X = -4.101049869;

            List<XYZ> rotatedCountours = new List<XYZ>();
            foreach (XYZ point in LocalStartFaceVertices)
            {
                XYZ rotatedPoint = rotationTransform.OfPoint(point);
                rotatedCountours.Add(new XYZ(REF_PLANE_MEMBER_LEFT_X, rotatedPoint.Y, rotatedPoint.Z));
            }

            return rotatedCountours;
        }

        private void CreateFamilyTypeIfNotExist(Application app, Document doc, string familyName, ShapeProperties shapeProperties,
            List<XYZ> rotatedCountours, string baseDir)
        {
            if (!types.ContainsKey(familyName))
            {

                if (shapeProperties is null)
                {
                    beamRfaGenerator.CreateRFAFile(app, familyName, rotatedCountours);
                }
                else if (shapeProperties.GetType() == typeof(RectangularProperties))
                {
                    FamilyLoader.LoadCustomFamily("rectangular_beam", FamilyLoader.FamilyFolder.Beams);
                    
                    FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "rectangular_beam");
                    FamilySymbol newFamilyType = defaultFamilyType.Duplicate(familyName) as FamilySymbol;

                    newFamilyType.GetParameters("b")[0].Set((shapeProperties as RectangularProperties).width);
                    newFamilyType.GetParameters("d")[0].Set((shapeProperties as RectangularProperties).depth);

                    types.Add(familyName, newFamilyType);
                }
                else if (shapeProperties.GetType() == typeof(LShapeProperties))
                {
                    FamilyLoader.LoadCustomFamily("l_shaped_beam", FamilyLoader.FamilyFolder.Beams);

                    FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "l_shaped_beam");
                    FamilySymbol newFamilyType = defaultFamilyType.Duplicate(familyName) as FamilySymbol;

                    newFamilyType.GetParameters("d")[0].Set((shapeProperties as LShapeProperties).depth);
                    newFamilyType.GetParameters("b")[0].Set((shapeProperties as LShapeProperties).breadth);
                    newFamilyType.GetParameters("t")[0].Set((shapeProperties as LShapeProperties).thickness);

                    types.Add(familyName, newFamilyType);
                }
                else if (shapeProperties.GetType() == typeof(HShapeProperties))
                {
                    FamilyLoader.LoadCustomFamily("i_shaped_beam", FamilyLoader.FamilyFolder.Beams);
                    
                    FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "i_shaped_beam");
                    FamilySymbol newFamilyType = defaultFamilyType.Duplicate(familyName) as FamilySymbol;

                    newFamilyType.GetParameters("d")[0].Set((shapeProperties as HShapeProperties).depth);
                    newFamilyType.GetParameters("bf")[0].Set((shapeProperties as HShapeProperties).flangeBreadth);
                    newFamilyType.GetParameters("tf")[0].Set((shapeProperties as HShapeProperties).flangeThickness);
                    newFamilyType.GetParameters("tw")[0].Set((shapeProperties as HShapeProperties).webThickness);

                    types.Add(familyName, newFamilyType);
                }
                else if (shapeProperties.GetType() == typeof(CShapeProperties))
                {
                    FamilyLoader.LoadCustomFamily("c_shaped_beam", FamilyLoader.FamilyFolder.Beams);
                    
                    FamilySymbol defaultFamilyType = GetFamilySymbolByName(doc, "c_shaped_beam");
                    FamilySymbol newFamilyType = defaultFamilyType.Duplicate(familyName) as FamilySymbol;

                    newFamilyType.GetParameters("d")[0].Set((shapeProperties as CShapeProperties).depth);
                    newFamilyType.GetParameters("bf")[0].Set((shapeProperties as CShapeProperties).flangeBreadth);
                    newFamilyType.GetParameters("tf")[0].Set((shapeProperties as CShapeProperties).flangeThickness);
                    newFamilyType.GetParameters("tw")[0].Set((shapeProperties as CShapeProperties).webThickness);

                    types.Add(familyName, newFamilyType);
                }
            }
        }

        private void CreateFamilyInstances(Document doc, string familyName, ElementId levelId, ShapeProperties props)
        {
            FamilySymbol familySymbol;
            if (types.ContainsKey(familyName)) { familySymbol = types[familyName]; }
            else
            {
                doc.LoadFamily(beamRfaGenerator.fileName(familyName), out Family beamFamily);
                familySymbol = TrudeModel.GetFamilySymbolByName(doc, familyName);
                types.Add(familyName, familySymbol);
            }
            foreach (var instance in instances)
            {
                Curve curve = GetPositionCurve(props, instance.CenterPosition);
                Level level = doc.GetElement(levelId) as Level;
                double curveOffsetFromLevel = curve.GetEndPoint(0).Z - level.Elevation;
                Transform transform = Transform.CreateTranslation(XYZ.BasisZ * -curveOffsetFromLevel);
                Curve transformedCurve = curve.CreateTransformed(transform);

                FamilyInstance beam = doc.Create.NewFamilyInstance(transformedCurve, familySymbol, level, StructuralType.Beam);
                beam.Location.Rotate(Line.CreateBound(instance.CenterPosition, instance.CenterPosition - XYZ.BasisZ), instance.Rotation.Z);
                beam.GetParameters("Cross-Section Rotation")[0].Set(instance?.Rotation.Y ?? 0);
                beam.get_Parameter(BuiltInParameter.Z_JUSTIFICATION).Set((int)ZJustification.Center);
                GlobalVariables.Document.Regenerate();
                beam.get_Parameter(BuiltInParameter.STRUCTURAL_BEAM_END0_ELEVATION).Set(curveOffsetFromLevel);
                beam.get_Parameter(BuiltInParameter.STRUCTURAL_BEAM_END1_ELEVATION).Set(curveOffsetFromLevel);
            }
        }

        private Curve GetPositionCurve(ShapeProperties props, XYZ centerPosision)
        {
            Line line = Line.CreateBound(startFaceCentroid + centerPosision, endFaceCentroid + centerPosision);

            if (props is null)
            {
                return line;
            }
            else
            {
                return line.CreateReversed();
            }

        }
    }
}