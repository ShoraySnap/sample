using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Material = Autodesk.Revit.DB.Material;

namespace TrudeImporter
{
    public class TrudeStaircase : TrudeModel
    {
        // Staircase properties
        public int Storey { get; set; }
        public int UniqueId { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }
        public double Tread { get; set; }
        public double Riser { get; set; }
        public double LandingWidth { get; set; }
        public double StairThickness { get; set; }
        public int Steps { get; set; }
        public double BaseOffset { get; set; }
        public string StaircaseType { get; set; }

        public Level topLevel = null;
        public Level bottomLevel = null;
        public double staircaseheight= 0;

        public List<StaircaseBlockProperties> StaircaseBlocks { get; set; }
        public List<LayerProperties> Layers { get; set; }
        public Stairs CreatedStaircase { get; private set; }

        public ElementId stairsId = null;
        public StairsType stairsType = null;

        public Autodesk.Revit.DB.Document doc = GlobalVariables.Document;

        // Constructor to parse properties and create the staircase
        public TrudeStaircase(StairCaseProperties staircaseProps, ElementId levelId)
        {
            // Parse the properties
            Storey = staircaseProps.Storey;
            UniqueId = staircaseProps.UniqueId;
            Position = staircaseProps.Position;
            Rotation = staircaseProps.Rotation;
            Height =  staircaseProps.Height;
            Width = staircaseProps.Width;
            Tread = staircaseProps.Tread;
            Riser = staircaseProps.Storey;
            LandingWidth = staircaseProps.LandingWidth;
            StairThickness = staircaseProps.StairThickness;
            Steps = staircaseProps.Steps;
            BaseOffset = staircaseProps.BaseOffset;
            Name = staircaseProps.Name;
            StaircaseType = staircaseProps.StaircaseType;
            StaircaseBlocks = staircaseProps.StaircaseBlocks;
            Layers = staircaseProps.Layers;

            CreateStaircase();
        }

        class StairsFailurePreprocessor : IFailuresPreprocessor
        {
            public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
            {
                // Use default failure processing
                return FailureProcessingResult.Continue;
            }
        }

        private void CreateStaircase()
        {
            int finalStorey = Storey + 1;

            topLevel = (from lvl in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>() where (lvl.Id == GlobalVariables.LevelIdByNumber[finalStorey])select lvl).First();
            bottomLevel = (from lvl in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()where (lvl.Id == GlobalVariables.LevelIdByNumber[Storey])select lvl).First();

            stairsType = new FilteredElementCollector(doc)
                .OfClass(typeof(StairsType))
                .OfType<StairsType>()
                .FirstOrDefault(st => st.Name.Equals(StaircaseType, StringComparison.OrdinalIgnoreCase));

            if (stairsType == null)
            {
                StairsType stairsTypeTemplate = new FilteredElementCollector(doc).OfClass(typeof(StairsType)).Cast<StairsType>().FirstOrDefault(x => x.ConstructionMethod == StairsConstructionMethod.CastInPlace);

                if (stairsTypeTemplate != null)
                {
                    stairsType = stairsTypeTemplate.Duplicate(StaircaseType) as StairsType;
                }
                else
                {
                    throw new InvalidOperationException("No StairsType template found to duplicate.");
                }
            }

            GlobalVariables.Transaction.Commit();
            using (StairsEditScope stairsScope = new StairsEditScope(doc, "Create Stairs"))
            {
                stairsId = stairsScope.Start(bottomLevel.Id, topLevel.Id);
                CreatedStaircase = doc.GetElement(stairsId) as Stairs;
                using (Transaction trans = new Transaction(GlobalVariables.Document, "Create Stairs"))
                {
                    trans.Start();
                    CreatedStaircase.ChangeTypeId(stairsType.Id);
                    CreatedStaircase.ActualTreadDepth = Tread;
                    CreatedStaircase.get_Parameter(BuiltInParameter.STAIRS_DESIRED_NUMBER_OF_RISERS).Set(Steps);
                    trans.Commit();

                    if (StaircaseType == "straight" && StaircaseBlocks.Count > 1)
                    {
                        if (StaircaseBlocks.Sum(b => b.StartLandingWidth) == 0)
                        {
                            StaircaseBlocks[0].Steps = StaircaseBlocks.Sum(b => b.Steps);
                            StaircaseBlocks = new List<StaircaseBlockProperties> { StaircaseBlocks[0] };
                        }
                    }

                    List<StaircaseBlockProperties> StairRunBlocks = StaircaseBlocks.Where(b => b.Type != "Landing").ToList();

                    List<ElementId> createdRunIds = new List<ElementId>();

                    trans.Start();
                    for (int i = 0; i < StairRunBlocks.Count; i++)
                    {
                        StaircaseBlockProperties props = StairRunBlocks[i];
                        string typeFromBlockBefore = i == 0 ? "" : StairRunBlocks[i - 1].Type;
                        StaircaseBlockProperties lastBlockProps = i == 0 ? null : StairRunBlocks[i - 1];
                        ElementId runId = RunCreator_Simple(props, lastBlockProps);
                        createdRunIds.Add(runId);
                    }
                    for (int i = 1; i < createdRunIds.Count; i++)
                    {
                        try
                        {
                            StairsLanding.CreateAutomaticLanding(GlobalVariables.Document, createdRunIds[i - 1], createdRunIds[i]);
                        }
                        catch (Exception)
                        {
                        }
                    }

                    trans.Commit();
                }
                stairsScope.Commit(new StairsFailurePreprocessor());
            }
            GlobalVariables.Transaction.Start();

            ICollection<ElementId> railingIds = CreatedStaircase.GetAssociatedRailings();
            ElementTransformUtils.RotateElement(GlobalVariables.Document, stairsId, Line.CreateBound(XYZ.Zero, XYZ.Zero + XYZ.BasisZ), -Rotation.Z);
            ElementTransformUtils.MoveElement(GlobalVariables.Document, stairsId, Position);
            //ICollection<ElementId> supportIds = CreatedStaircase.GetStairsSupports();
            foreach (ElementId railingId in railingIds)
            {
                doc.Delete(railingId);
            }
            //foreach (ElementId supportId in supportIds)
            //{
            //    doc.Delete(supportId);
            //}
            if (CreatedStaircase != null)
            {
                CreatedStaircase.get_Parameter(BuiltInParameter.STAIRS_BASE_OFFSET).Set(BaseOffset);
                //CreatedStaircase.get_Parameter(BuiltInParameter.STAIRS_DESIRED_NUMBER_OF_RISERS).Set(Steps);
                //CreatedStaircase.get_Parameter(BuiltInParameter.STAIRS_ACTUAL_NUMBER_OF_RISERS).Set(Steps);
            }
        }

        private XYZ ComputePoints(XYZ startingPoint, double[] translation, double[] rotation)
        {
            XYZ currPoint = startingPoint;
            currPoint = new XYZ(currPoint.X + (double)translation.GetValue(0), currPoint.Y + (double)translation.GetValue(1), currPoint.Z + (double)translation.GetValue(2) + bottomLevel.Elevation + staircaseheight);
            //currPoint = new XYZ(currPoint.X * Math.Cos((double)rotation.GetValue(0)) - currPoint.Y * Math.Sin((double)rotation.GetValue(0)), currPoint.X * Math.Sin((double)rotation.GetValue(0)) + currPoint.Y * Math.Cos((double)rotation.GetValue(0)), currPoint.Z);
            return currPoint;
        }

        private ElementId RunCreator_Simple(StaircaseBlockProperties props, StaircaseBlockProperties lastProps)
        {
            Transform transform = Transform.CreateRotation(XYZ.BasisZ, -props.Rotation.Z);
            XYZ direction = transform.OfVector(new XYZ(-1, 0, 0));
            XYZ startPoint = props.StartPoint - new XYZ(props.Translation.X, -props.Translation.Y, -props.Translation.Z);
            startPoint += direction.CrossProduct(XYZ.BasisZ) * (3.28084 - Width) / 2;
            if (props.StartLandingWidth != 0)
            {
                startPoint += direction * props.StartLandingWidth;
            }
            double blockLength = props.Steps * props.Tread;
            XYZ endPoint = startPoint + blockLength * direction;
            Line rightLine = Line.CreateBound(startPoint, endPoint);
            StairsRun run = StairsRun.CreateStraightRun(GlobalVariables.Document, stairsId, rightLine, StairsRunJustification.Right);
            run.ActualRunWidth = Width;
            run.EndsWithRiser = false;
            double height = props.Steps * props.Riser;
            run.TopElevation = props.Translation.Z + height;
            return run.Id;
        }
        /*
        private void RunCreator_FlightLanding(StaircaseBlockProperties props)
        {
            double tread_depth = props.Tread;
            double riserNum = props.Steps;
            double runWidth = Width;
            XYZ startPos = ComputePoints(props.StartPoint, props.Translation, props.Rotation);
            System.Diagnostics.Debug.WriteLine("StartPos: " + startPos);
            startPos = new XYZ(startPos.X, 0, startPos.Z);

            IList<Curve> bdryCurves = new List<Curve>();
            IList<Curve> riserCurves = new List<Curve>();
            IList<Curve> pathCurves = new List<Curve>();

            XYZ pnt1 = new XYZ(startPos[0], startPos[1], startPos[2]);
            XYZ pnt2 = new XYZ(startPos[0] - riserNum * tread_depth, startPos[1], startPos[2]);
            XYZ pnt3 = new XYZ(startPos[0], startPos[1] - runWidth, startPos[2]);
            XYZ pnt4 = new XYZ(startPos[0] - riserNum * tread_depth, startPos[1] - runWidth, startPos[2]);

            System.Diagnostics.Debug.WriteLine("pnt1: " + pnt1);
            System.Diagnostics.Debug.WriteLine("pnt2: " + pnt2);
            System.Diagnostics.Debug.WriteLine("pnt3: " + pnt3);
            System.Diagnostics.Debug.WriteLine("pnt4: " + pnt4);

            // boundaries
            bdryCurves.Add(Line.CreateBound(pnt2, pnt1));
            bdryCurves.Add(Line.CreateBound(pnt4, pnt3));

            double interval = (pnt2.X - pnt1.X) / riserNum;
            for (int ii = 0; ii <= riserNum; ii++)
            {
                XYZ end0 = new XYZ(pnt1.X + ii * interval, pnt1.Y, pnt1.Z);
                XYZ end1 = new XYZ(pnt1.X + ii * interval, pnt3.Y, pnt3.Z);
                riserCurves.Add(Line.CreateBound(end0, end1));
            }

            //stairs path curves
            XYZ pathEnd0 = (pnt1 + pnt3) / 2.0;
            XYZ pathEnd1 = (pnt2 + pnt4) / 2.0;

            pathCurves.Add(Line.CreateBound(pathEnd1, pathEnd0));

            Line geomLine = Line.CreateBound(pathEnd1, pathEnd0);
            SketchPlane sketchPlane = SketchPlane.Create(doc, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, pathEnd0));
            ModelCurve modelCurve = doc.Create.NewModelCurve(geomLine, sketchPlane);

            StairsRun stairsRun = StairsRun.CreateSketchedRun(doc, stairsId, bottomLevel.Elevation, bdryCurves, riserCurves, pathCurves);
            stairsRun.EndsWithRiser = false;

            stairsRun.BaseElevation = bottomLevel.Elevation + staircaseheight;
            stairsType.MinTreadDepth = props.Tread;
            stairsType.MaxRiserHeight = props.Riser;
            stairsType.MinRunWidth = props.Depth;
            System.Diagnostics.Debug.WriteLine("Risers: " + stairsRun.ActualRisersNumber);
            System.Diagnostics.Debug.WriteLine("Treads: " + stairsRun.ActualTreadsNumber);

            stairsType.get_Parameter(BuiltInParameter.STAIRS_ATTR_MINIMUM_TREAD_DEPTH).Set(Tread);
            stairsType.get_Parameter(BuiltInParameter.STAIRS_ATTR_MAX_RISER_HEIGHT).Set(Riser);
            staircaseheight += stairsRun.get_Parameter(BuiltInParameter.STAIRS_RUN_HEIGHT).AsDouble();
            //rotate along z axis
            //ElementTransformUtils.RotateElement(doc, stairsRun.Id, Line.CreateBound(new XYZ(0,0,0),new XYZ(0,0,1)), 1.309);
        }


        private void RunCreator_Landing(StaircaseBlockProperties props)
        {
            double tread_depth = props.Depth;
            double runWidth = props.LandingWidth;
            XYZ startPos = ComputePoints(props.StartPoint, props.Translation, props.Rotation);
            System.Diagnostics.Debug.WriteLine("StartPos: " + startPos);
            startPos = new XYZ(startPos.X, 0, startPos.Z);


            CurveLoop landingLoop = new CurveLoop();
            XYZ p1 = new XYZ(startPos[0], startPos[1], startPos[2]);
            XYZ p2 = new XYZ(startPos[0] - runWidth, startPos[1], startPos[2]);
            XYZ p3 = new XYZ(startPos[0] - runWidth, startPos[1] - tread_depth, startPos[2]);
            XYZ p4 = new XYZ(startPos[0], startPos[1] - tread_depth, startPos[2]);
            Line curve_1 = Line.CreateBound(p1, p2);
            Line curve_2 = Line.CreateBound(p2, p3);
            Line curve_3 = Line.CreateBound(p3, p4);
            Line curve_4 = Line.CreateBound(p4, p1);

            landingLoop.Append(curve_1);
            landingLoop.Append(curve_2);
            landingLoop.Append(curve_3);
            landingLoop.Append(curve_4);
            StairsLanding newLanding = StairsLanding.CreateSketchedLanding(doc, stairsId, landingLoop, bottomLevel.Elevation + staircaseheight);
            //staircaseheight += 

        }


        private void RunCreator_LandingFlightLanding(StaircaseBlockProperties props)
        {
            XYZ pathEnd0 = ComputePoints(props.StartPoint, props.Translation, props.Rotation);
            pathEnd0 = new XYZ(pathEnd0.X, 0, pathEnd0.Z);
            double horizontal = props.Steps * props.Tread;
            XYZ pathEnd1 = new XYZ(pathEnd0.X + horizontal, 0, pathEnd0.Z);
            Line locationLine = Line.CreateBound(pathEnd0, pathEnd1);

            System.Diagnostics.Debug.WriteLine("pathEnd0: " + pathEnd0);
            System.Diagnostics.Debug.WriteLine("pathEnd1: " + pathEnd1);

            Line geomLine = Line.CreateBound(pathEnd0, pathEnd1);
            SketchPlane sketchPlane = SketchPlane.Create(doc, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, pathEnd1));
            ModelCurve modelCurve = doc.Create.NewModelCurve(geomLine, sketchPlane);

            StairsRun stairsRun = StairsRun.CreateStraightRun(doc, stairsId, locationLine, StairsRunJustification.Center);
            //stairsRun.EndsWithRiser = false;
            stairsRun.ActualRunWidth = Width;
            stairsRun.TopElevation = topLevel.Elevation;

            System.Diagnostics.Debug.WriteLine("Risers: " + stairsRun.ActualRisersNumber);
            System.Diagnostics.Debug.WriteLine("Treads: " + stairsRun.ActualTreadsNumber);
            stairsType.MinTreadDepth = props.Tread;
            stairsType.MaxRiserHeight = props.Riser;
            stairsType.MinRunWidth = props.Depth;

            stairsType.get_Parameter(BuiltInParameter.STAIRS_ATTR_MINIMUM_TREAD_DEPTH).Set(Tread);
            stairsType.get_Parameter(BuiltInParameter.STAIRS_ATTR_MAX_RISER_HEIGHT).Set(Riser);
            staircaseheight += stairsRun.get_Parameter(BuiltInParameter.STAIRS_RUN_HEIGHT).AsDouble();

        }
        */
    }
}




