using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
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
        public double StoreyHeight { get; set; }
        public double Riser { get; set; }
        public double LandingWidth { get; set; }
        public double StairThickness { get; set; }
        public int Steps { get; set; }
        public double BaseOffset { get; set; }
        public string StaircaseType { get; set; }
        public string StaircasePreset { get; set; }

        public Level topLevel = null;
        public Level bottomLevel = null;
        public double staircaseheight = 0;
        public Dictionary<ElementId, Tuple<XYZ, XYZ>> runStartEndPoints = new Dictionary<ElementId, Tuple<XYZ, XYZ>>();
        public List<StaircaseBlockProperties> StaircaseBlocks { get; set; }
        public List<LayerProperties> Layers { get; set; }
        public Stairs CreatedStaircase { get; private set; }

        public ElementId stairsId = null;
        public StairsType stairsType = null;
        public StairsRunType stairsRunType = null;
        List<StaircaseBlockProperties> StairRunBlocks = null;
        

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
            Height = staircaseProps.Height;
            Width = staircaseProps.Width;
            Tread = staircaseProps.Tread;
            Riser = staircaseProps.Storey;
            StoreyHeight = staircaseProps.StoreyHeight;
            LandingWidth = staircaseProps.LandingWidth;
            StairThickness = staircaseProps.StairThickness;
            Steps = staircaseProps.Steps;
            BaseOffset = staircaseProps.BaseOffset;
            Name = staircaseProps.Name;
            StaircaseType = staircaseProps.StaircaseType;
            StaircasePreset = staircaseProps.StaircasePreset;
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
            Utils.TryStartTransaction();

            bottomLevel = (from lvl in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>() where (lvl.Id == GlobalVariables.LevelIdByNumber[Storey]) select lvl).First();
            if (StoreyHeight != Height)
            {
                topLevel=createTempLevel(Storey, Height + bottomLevel.ProjectElevation + BaseOffset);
            }
            else { 
                int finalStorey = Storey + 1;
                if (finalStorey == 0)
                {
                    finalStorey = 1;
                }

                if (!GlobalVariables.LevelIdByNumber.ContainsKey(finalStorey))
                {
                    createLevel(finalStorey, Height + bottomLevel.ProjectElevation + BaseOffset);
                }
                topLevel = (from lvl in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>() where (lvl.Id == GlobalVariables.LevelIdByNumber[finalStorey]) select lvl).First();
            }
            double stairThicknessInRevit = StairThickness * 304.802581;
            stairThicknessInRevit = Math.Round(stairThicknessInRevit, 2);

            string typeName = "Snaptrude-" + StaircasePreset + "-" + stairThicknessInRevit + "mm";
            stairsType = new FilteredElementCollector(doc)
                .OfClass(typeof(StairsType))
                .OfType<StairsType>()
                .FirstOrDefault(st => st.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));

            if (stairsType == null)
            {
                StairsType stairsTypeTemplate = new FilteredElementCollector(doc).OfClass(typeof(StairsType)).Cast<StairsType>().FirstOrDefault(x => x.ConstructionMethod == StairsConstructionMethod.CastInPlace);

                if (stairsTypeTemplate != null)
                {
                    stairsType = stairsTypeTemplate.Duplicate(typeName) as StairsType;

                    ElementId runTypeId = stairsType.RunType;
                    StairsRunType runType = doc.GetElement(runTypeId) as StairsRunType;
                    string runTypeName = "Snaptrude-Run-Thickness-" + stairThicknessInRevit + "mm";
                    if (runType.StructuralDepth != StairThickness)
                    {
                        StairsRunType existingRunType = new FilteredElementCollector(doc)
                            .OfClass(typeof(StairsRunType))
                            .OfType<StairsRunType>()
                            .FirstOrDefault(st => st.Name.Equals(runTypeName, StringComparison.OrdinalIgnoreCase));
                        if (existingRunType != null)
                        {
                            stairsType.RunType = existingRunType.Id;
                        }
                        else
                        {
                            StairsRunType duplicateRunType = runType.Duplicate(runTypeName) as StairsRunType;
                            duplicateRunType.StructuralDepth = StairThickness;
                            stairsType.RunType = duplicateRunType.Id;
                        }
                    }

                    ElementId landingTypeId = stairsType.LandingType;
                    StairsLandingType landingType = doc.GetElement(landingTypeId) as StairsLandingType;
                    string landingTypeName = "Snaptrude-Landing-Thickness-" + stairThicknessInRevit + "mm";
                    if (landingType.Thickness != StairThickness)
                    {
                        StairsLandingType existingLandingType = new FilteredElementCollector(doc)
                            .OfClass(typeof(StairsLandingType))
                            .OfType<StairsLandingType>()
                            .FirstOrDefault(st => st.Name.Equals(landingTypeName, StringComparison.OrdinalIgnoreCase));
                        if (existingLandingType != null)
                        {
                            stairsType.LandingType = existingLandingType.Id;
                        }
                        else
                        {
                            StairsLandingType duplicateLandingType = landingType.Duplicate(landingTypeName) as StairsLandingType;
                            duplicateLandingType.Thickness = StairThickness;
                            stairsType.LandingType = duplicateLandingType.Id;
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException("No StairsType template found to duplicate.");
                }
            }
            GlobalVariables.Transaction.Commit();

            stairsId = GlobalVariables.StairsEditScope.Start(bottomLevel.Id, topLevel.Id);
            CreatedStaircase = doc.GetElement(stairsId) as Stairs;

            Utils.TryStartTransaction();
            CreatedStaircase.ChangeTypeId(stairsType.Id);
            CreatedStaircase.ActualTreadDepth = Tread;
            if (StoreyHeight != Height)
            {
                CreatedStaircase.get_Parameter(BuiltInParameter.STAIRS_TOP_LEVEL_PARAM).Set(new ElementId(-1));
                CreatedStaircase.get_Parameter(BuiltInParameter.STAIRS_STAIRS_HEIGHT).Set(Height);
            }
            CreatedStaircase.get_Parameter(BuiltInParameter.STAIRS_DESIRED_NUMBER_OF_RISERS).Set(Steps);
            GlobalVariables.Transaction.Commit();

            if (StaircaseType == "straight" && StaircaseBlocks.Count > 1)
            {
                if (StaircaseBlocks.Sum(b => b.StartLandingWidth) == 0)
                {
                    StaircaseBlocks[0].Steps = StaircaseBlocks.Sum(b => b.Steps);
                    StaircaseBlocks = new List<StaircaseBlockProperties> { StaircaseBlocks[0] };
                }
            }
            List<StaircaseBlockProperties> StairRunBlocks = StaircaseBlocks.Where(b => b.Type != "Landing").ToList();
            List<StaircaseBlockProperties> StairRunLandingBlocks = StaircaseBlocks.Where(b => b.Type == "Landing").ToList();
            List<ElementId> createdRunIds = new List<ElementId>();

            Utils.TryStartTransaction();
            for (int i = 0; i < StairRunBlocks.Count; i++)
            {
                StaircaseBlockProperties props = StairRunBlocks[i];
                ElementId runId = RunCreator_Simple(props, bottomLevel, i != StairRunBlocks.Count - 1);
                if (runId != null)
                    createdRunIds.Add(runId);
            }
            for (int i = 1; i < createdRunIds.Count; i++)
            {
                try
                {
                    IList<ElementId> createdAutoLandings= StairsLanding.CreateAutomaticLanding(GlobalVariables.Document, createdRunIds[i - 1], createdRunIds[i]);
                    if (StaircaseType == "dogLegged")
                    {
                        Tuple<XYZ, XYZ> startEndPoints = runStartEndPoints[createdRunIds[i - 1]];
                        XYZ direction = (startEndPoints.Item2 - startEndPoints.Item1).Normalize();
                        foreach (ElementId landingId in createdAutoLandings)
                        {
                            StairsLanding landing = doc.GetElement(landingId) as StairsLanding;
                            if (landing != null)
                            {
                                CurveLoop landingLoop = landing.GetFootprintBoundary();
                                IDictionary<int, Line> landingBoundaries = new Dictionary<int, Line>();
                                CurveLoop extendedLandingBoundary = new CurveLoop();
                                int landingBoundaryIndex = 0;

                                double firstCurveLength = GetFirstCurve(landingLoop).ApproximateLength;
                                double fifthCurveLength = GetFifthCurve(landingLoop).ApproximateLength;
                                XYZ absoluteDirection = new XYZ(Math.Abs(direction.X), Math.Abs(direction.Y), Math.Abs(direction.Z));
                                XYZ scalar = absoluteDirection * (1 + ((LandingWidth - fifthCurveLength) / fifthCurveLength));

                                foreach (Curve curve in landingLoop)
                                {
                                    XYZ startPoint = curve.GetEndPoint(0);
                                    XYZ endPoint = curve.GetEndPoint(1);
                                    XYZ newStartPoint = new XYZ(startPoint.X, startPoint.Y, startPoint.Z);
                                    XYZ newEndPoint = new XYZ(endPoint.X, endPoint.Y, endPoint.Z);

                                    if (landingBoundaryIndex == 3)
                                    {
                                        double xDiff = Math.Abs(startPoint.X - endPoint.X)- firstCurveLength;
                                        double additive = xDiff * scalar.X - xDiff;
                                        if (direction.X == 1 || direction.X == -1)
                                        {
                                            newEndPoint = new XYZ(endPoint.X >= 0 ? endPoint.X + additive : endPoint.X - additive, endPoint.Y, endPoint.Z);
                                        }
                                    }
                                    else if (landingBoundaryIndex == 4 || landingBoundaryIndex == 5)
                                    {
                                        if (landingBoundaryIndex == 4)
                                        {
                                            newStartPoint = new XYZ(landingBoundaries[3].GetEndPoint(1).X, startPoint.Y, startPoint.Z);
                                            newEndPoint = new XYZ(newStartPoint.X, endPoint.Y, endPoint.Z);
                                        }
                                        
                                        if (landingBoundaryIndex == 5)
                                        {
                                            newStartPoint = new XYZ(landingBoundaries[4].GetEndPoint(1).X, startPoint.Y, startPoint.Z);
                                            newEndPoint = new XYZ(endPoint.X, endPoint.Y, endPoint.Z);
                                        }
                                    }
                                    landingBoundaries.Add(landingBoundaryIndex, Line.CreateBound(newStartPoint, newEndPoint));
                                    landingBoundaryIndex++;
                                }

                                foreach (KeyValuePair<int, Line> boundary in landingBoundaries)
                                {
                                    extendedLandingBoundary.Append(boundary.Value);
                                }
                                StairsLanding newLanding = StairsLanding.CreateSketchedLanding(doc, stairsId, extendedLandingBoundary, landing.BaseElevation);
                                doc.Delete(landingId);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to create automatic landing. Creating manual landing instead.");
                    CreateManualLanding(createdRunIds[i - 1], createdRunIds[i]);
                }
            }

            if (StaircaseType == "square")
            {
                System.Diagnostics.Debug.WriteLine("Creating edge landing.");
                CreateEdgeLanding(createdRunIds[createdRunIds.Count - 1], createdRunIds[createdRunIds.Count % 4]);
            }

            GlobalVariables.Transaction.Commit();
            GlobalVariables.StairsEditScope.Commit(new StairsFailurePreprocessor());
            Utils.TryStartTransaction();

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
                CreatedStaircase.get_Parameter(BuiltInParameter.STAIRS_DESIRED_NUMBER_OF_RISERS).Set(Steps);
            }

            if (StoreyHeight != Height)
            {
                doc.Delete(topLevel.Id);
            }
            GlobalVariables.Transaction.Commit();
        }

        private ElementId RunCreator_Simple(StaircaseBlockProperties props, Level bottomLevel, bool endWithRiser)
        {
            Transform transform = Transform.CreateRotation(XYZ.BasisZ, -props.Rotation.Z);
            XYZ direction = transform.OfVector(new XYZ(-1, 0, 0));
            XYZ startPoint = props.StartPoint - new XYZ(props.Translation.X, -props.Translation.Y, -props.Translation.Z);
            startPoint += XYZ.BasisZ * bottomLevel.ProjectElevation;
            startPoint += direction.CrossProduct(XYZ.BasisZ) * (3.2808398950131235 - Width) / 2;
            if (props.StartLandingWidth != 0)
                startPoint += direction * props.StartLandingWidth;
            double blockLength =  props.Tread * (endWithRiser && props.Steps != 1 ? (props.Steps - 1) : props.Steps);
            XYZ endPoint = startPoint + blockLength * direction;
            if (startPoint.IsAlmostEqualTo(endPoint))
            {
                return null;
            }
            Line rightLine = Line.CreateBound(startPoint, endPoint);
            StairsRun run = StairsRun.CreateStraightRun(GlobalVariables.Document, stairsId, rightLine, StairsRunJustification.Right);
            run.ActualRunWidth = Width;
            run.EndsWithRiser = endWithRiser && props.Steps != 1;
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

            if (xDiff > yDiff)
            {
                supportOffset = yDiff % width;
            }
            else
            {
                supportOffset = xDiff % width;
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
            XYZ straightDirection = rotatedDirectionCW.Normalize();
            XYZ perpendicularDirection = rotatedDirectionCCW.Normalize();

            // straightDirection = new XYZ(Math.Round(rotatedDirectionCW.X, 2), Math.Round(rotatedDirectionCW.Y, 2), 0);
            // perpendicularDirection = new XYZ(Math.Round(rotatedDirectionCCW.X, 2), Math.Round(rotatedDirectionCCW.Y, 2), 0);

            XYZ offsetVectorCCW = perpendicularDirection * width;

            XYZ corner1 = startPoint;
            XYZ corner2 = startPoint + offsetVectorCCW;
            XYZ corner3 = endPoint;
            XYZ corner4 = endPoint - offsetVectorCCW;

            XYZ adjustmentVector = straightDirection * supportOffset;
            XYZ adjustedCorner3 = corner3 - adjustmentVector;
            XYZ adjustedCorner4 = corner4 - adjustmentVector;

            XYZ adjustmentVector2 = perpendicularDirection * supportOffset * 0.99;

            XYZ adjustedCorner2 = corner2 + adjustmentVector2;
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
            StairsRun runAfter = doc.GetElement(runIdAfter) as StairsRun;
            StairsRun runBefore = doc.GetElement(runIdBefore) as StairsRun;

            if (runBefore == null || runAfter == null)
                throw new InvalidOperationException("Invalid stair runs for manual landing.");

            double elevation = Height + bottomLevel.ProjectElevation;

            XYZ startPoint = runStartEndPoints[runAfter.Id].Item1;
            XYZ endPoint = runStartEndPoints[runAfter.Id].Item2;

            startPoint = new XYZ(startPoint.X, startPoint.Y, elevation);
            endPoint = new XYZ(endPoint.X, endPoint.Y, elevation);
            Line rightLine = Line.CreateBound(startPoint, endPoint);
            StairsRun run = StairsRun.CreateStraightRun(GlobalVariables.Document, stairsId, rightLine, StairsRunJustification.Right);
            run.ActualRunWidth = Width;
            run.EndsWithRiser = false;
            try
            {
                StairsLanding.CreateAutomaticLanding(GlobalVariables.Document, runBefore.Id, run.Id);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Failed to create automatic landing. Creating manual landing instead." + ex.Message);
                CreateManualLanding(runAfter.Id, run.Id);
            }
            doc.Delete(run.Id);

        }

        private void createLevel(int storey, double elevation)
        {
            Level level = Level.Create(doc, elevation);
            level.Name = "Level " + storey;
            GlobalVariables.LevelIdByNumber.Add(storey, level.Id);
        }
        private Level createTempLevel(int storey, double elevation)
        {
            Level level = Level.Create(doc, elevation);
            level.Name = "Temp Level " + storey;
            return level;
        }

        private Curve GetFirstCurve(CurveLoop landingLoop)
        {
            CurveLoopIterator curveLoopIterator = landingLoop.GetCurveLoopIterator();
            for (int i = 0; i < 2; i++)
            {
                curveLoopIterator.MoveNext();
            }
            return curveLoopIterator.Current;
        }
        private Curve GetFifthCurve(CurveLoop landingLoop)
        {
            CurveLoopIterator curveLoopIterator = landingLoop.GetCurveLoopIterator();
            for (int i = 0; i < 6; i++)
            {
                curveLoopIterator.MoveNext();
            }
            return curveLoopIterator.Current;
        }
    }
}



