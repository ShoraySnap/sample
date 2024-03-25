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
using Grid = Autodesk.Revit.DB.Grid;
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
        public Dictionary<ElementId, Tuple<XYZ, XYZ>> runStartEndPoints = new Dictionary<ElementId, Tuple<XYZ, XYZ>>();
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
            Scaling = staircaseProps.Scaling;
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
                        ElementId runId = RunCreator_Simple(props, bottomLevel);
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
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Failed to create automatic landing. Creating manual landing instead.");
                            CreateManualLanding(createdRunIds[i - 1], createdRunIds[i]);
                        }
                    }
                    
                    if (StaircaseType == "square")
                    {
                       CreateEdgeLanding(createdRunIds[createdRunIds.Count-1],createdRunIds[0]);
                    }
                    

                    trans.Commit();
                }
                stairsScope.Commit(new StairsFailurePreprocessor());
            }
            GlobalVariables.Transaction.Start();

            ICollection<ElementId> railingIds = CreatedStaircase.GetAssociatedRailings();
            if (Scaling.Z == -1)
                ElementTransformUtils.MirrorElements(GlobalVariables.Document, new List<ElementId> { stairsId }, Plane.CreateByNormalAndOrigin(XYZ.BasisX, XYZ.Zero), false);
            ElementTransformUtils.RotateElement(GlobalVariables.Document, stairsId, Line.CreateBound(XYZ.Zero, XYZ.Zero + XYZ.BasisZ), -Rotation.Z);
            ElementTransformUtils.MoveElement(GlobalVariables.Document, stairsId, Position);
            foreach (ElementId railingId in railingIds)
            {
                doc.Delete(railingId);
            }
            if (CreatedStaircase != null)
            {
                CreatedStaircase.get_Parameter(BuiltInParameter.STAIRS_BASE_OFFSET).Set(BaseOffset);
                CreatedStaircase.ChangeTypeId(stairsType.Id);
                CreatedStaircase.get_Parameter(BuiltInParameter.STAIRS_DESIRED_NUMBER_OF_RISERS).Set(Steps);
            }
        }

private ElementId RunCreator_Simple(StaircaseBlockProperties props, Level bottomLevel)
        {
            Transform transform = Transform.CreateRotation(XYZ.BasisZ, -props.Rotation.Z);
            XYZ direction = transform.OfVector(new XYZ(-1, 0, 0));
            XYZ startPoint = props.StartPoint - new XYZ(props.Translation.X, -props.Translation.Y, -props.Translation.Z);
            startPoint += XYZ.BasisZ * bottomLevel.ProjectElevation;
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
            if (Math.Abs(run.TopElevation - (props.Translation.Z + height)) > 0.01)
            run.TopElevation = props.Translation.Z + height;
            runStartEndPoints.Add(run.Id, new Tuple<XYZ, XYZ>(startPoint, endPoint));
            return run.Id;
        }

        private void CreateManualLanding(ElementId runIdBefore, ElementId runIdAfter)
        {
            StairsRun runBefore = doc.GetElement(runIdBefore) as StairsRun;
            StairsRun runAfter = doc.GetElement(runIdAfter) as StairsRun;

            if (runBefore == null || runAfter == null)
                throw new InvalidOperationException("Invalid stair runs for manual landing.");

            double elevation = runBefore.TopElevation;

            Level level = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
                          .OrderBy(lvl => Math.Abs(elevation - lvl.Elevation))
                          .FirstOrDefault();

            if (level == null)
                throw new InvalidOperationException("No suitable level found for landing creation.");

            XYZ startPoint = runStartEndPoints[runBefore.Id].Item2;
            XYZ endPoint = runStartEndPoints[runAfter.Id].Item1;
            startPoint = new XYZ(startPoint.X, startPoint.Y, level.Elevation);
            endPoint = new XYZ(endPoint.X, endPoint.Y, level.Elevation);

            XYZ direction = (endPoint - startPoint).Normalize();
            double width = this.Width;
            double supportOffset = 0;
            double xDiff = Math.Abs(startPoint.X - endPoint.X);
            double yDiff = Math.Abs(startPoint.Y - endPoint.Y);
            
            if  (xDiff > yDiff)
            {
                supportOffset = yDiff%width;
            }
            else
            {
                supportOffset = xDiff%width;
            }
            double angleRadians = Math.PI / 4;
            XYZ rotatedDirectionCW = new XYZ(
                direction.X * Math.Cos(angleRadians) + direction.Y * Math.Sin(angleRadians),
                -direction.X * Math.Sin(angleRadians) + direction.Y * Math.Cos(angleRadians),
                0);
            XYZ rotatedDirectionCCW = new XYZ(
                direction.X * Math.Cos(angleRadians) - direction.Y * Math.Sin(angleRadians),
                direction.X * Math.Sin(angleRadians) + direction.Y * Math.Cos(angleRadians),
                0);
            XYZ straightDirection = rotatedDirectionCW.Normalize() ;
            XYZ perpendicularDirection = rotatedDirectionCCW.Normalize() ;
            
            // straightDirection = new XYZ(Math.Round(rotatedDirectionCW.X, 2), Math.Round(rotatedDirectionCW.Y, 2), 0);
            // perpendicularDirection = new XYZ(Math.Round(rotatedDirectionCCW.X, 2), Math.Round(rotatedDirectionCCW.Y, 2), 0);
            
            XYZ offsetVectorCCW = perpendicularDirection * width ;

            XYZ corner1 = startPoint;
            XYZ corner2 = startPoint + offsetVectorCCW ; 
            XYZ corner3 = endPoint;
            XYZ corner4 = endPoint - offsetVectorCCW;
            
            XYZ adjustmentVector = straightDirection * supportOffset;
            XYZ adjustedCorner3 = corner3 - adjustmentVector;
            XYZ adjustedCorner4 = corner4 - adjustmentVector;
            
            XYZ adjustmentVector2 = perpendicularDirection * supportOffset * 0.99;
            
            XYZ adjustedCorner2 = corner2 + adjustmentVector2 ;
            XYZ adjustedCorner1 = corner1 + adjustmentVector2;
            
            CurveLoop landingLoop = new CurveLoop();

            landingLoop.Append(Line.CreateBound(adjustedCorner1, adjustedCorner4));
            landingLoop.Append(Line.CreateBound(adjustedCorner4, adjustedCorner3));
            landingLoop.Append(Line.CreateBound(adjustedCorner3, adjustedCorner2));
            landingLoop.Append(Line.CreateBound(adjustedCorner2, adjustedCorner1));

            StairsLanding newLanding = StairsLanding.CreateSketchedLanding(doc, stairsId, landingLoop, elevation);

        }

        private void CreateEdgeLanding(ElementId runIdBefore, ElementId runIdAfter)
        {
            // make a copy of the runAfter 
            StairsRun runAfter = doc.GetElement(runIdAfter) as StairsRun;
            StairsRun runBefore = doc.GetElement(runIdBefore) as StairsRun;
            
            if (runBefore == null || runAfter == null)
                throw new InvalidOperationException("Invalid stair runs for manual landing.");

            double elevation = runBefore.TopElevation;

            Level level = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
                .OrderBy(lvl => Math.Abs(elevation - lvl.Elevation))
                .FirstOrDefault();

            if (level == null)
                throw new InvalidOperationException("No suitable level found for landing creation.");
            
            XYZ startPoint = runStartEndPoints[runBefore.Id].Item2;
            XYZ endPoint = runStartEndPoints[runAfter.Id].Item1;
            startPoint = new XYZ(startPoint.X, startPoint.Y, level.Elevation);
            endPoint = new XYZ(endPoint.X, endPoint.Y, level.Elevation);
            
            XYZ direction = (endPoint - startPoint).Normalize();
            double width = this.Width;
            
        }

    }
}




