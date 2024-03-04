using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
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
                StairsType stairsTypeTemplate = new FilteredElementCollector(doc).OfClass(typeof(StairsType)).Cast<StairsType>().FirstOrDefault();

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

                using (Transaction trans = new Transaction(GlobalVariables.Document, "Create Stairs"))
                {
                    trans.Start();
                    
                    foreach (StaircaseBlockProperties props in StaircaseBlocks)
                    {
                        switch (props.Type)
                        {
                            case "FlightLanding":
                                RunCreator_FlightLanding(props);
                                break;
                            case "LandingFlightLanding":
                                RunCreator_LandingFlightLanding(props);
                                break;
                            default:
                                System.Diagnostics.Debug.WriteLine("Skipping staircase block: " + props.Type);
                                break;
                        }
                    }


                    trans.Commit();
                }
                stairsScope.Commit(new StairsFailurePreprocessor());
            }
            GlobalVariables.Transaction.Start();

            CreatedStaircase = doc.GetElement(stairsId) as Stairs;
            ICollection<ElementId> railingIds = CreatedStaircase.GetAssociatedRailings();
            foreach (ElementId railingId in railingIds)
            {
                doc.Delete(railingId);
            }
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

        private void RunCreator_FlightLanding(StaircaseBlockProperties props)
        {
            XYZ pathEnd0 = ComputePoints(props.StartPoint, props.Translation, props.Rotation); 
            double horizontal = props.Steps * props.Tread;
            XYZ pathEnd1 = new XYZ(pathEnd0.X + horizontal, pathEnd0.Y, pathEnd0.Z );
            Line locationLine = Line.CreateBound(pathEnd0, pathEnd1);
                
            System.Diagnostics.Debug.WriteLine("pathEnd0: " + pathEnd0);
            System.Diagnostics.Debug.WriteLine("pathEnd1: " + pathEnd1);

            Line geomLine = Line.CreateBound(pathEnd0, pathEnd1);
            SketchPlane sketchPlane = SketchPlane.Create(doc, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, pathEnd1));
            ModelCurve modelCurve = doc.Create.NewModelCurve(geomLine, sketchPlane);

            StairsRun stairsRun = StairsRun.CreateStraightRun(doc, stairsId,locationLine, StairsRunJustification.Center);
            stairsRun.EndsWithRiser = false;
            stairsRun.ActualRunWidth = Width;
            //stairsRun.TopElevation = topLevel.Elevation + staircaseheight;
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

    }
}


