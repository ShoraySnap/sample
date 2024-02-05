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

        public List<StaircaseBlockProperties> StaircaseBlocks { get; set; }
        public List<LayerProperties> Layers { get; set; }
        public Stairs CreatedStaircase { get; private set; }

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
            CenterPosition = staircaseProps.CenterPosition;
            Type = staircaseProps.Type;
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
            Autodesk.Revit.DB.Document doc = GlobalVariables.Document;
            int finalStorey = Storey + 1;

            Level topLevel = (from lvl in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>() where (lvl.Id == GlobalVariables.LevelIdByNumber[finalStorey])select lvl).First();
            Level bottomLevel = (from lvl in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()where (lvl.Id == GlobalVariables.LevelIdByNumber[Storey])select lvl).First();

            StairsType stairsType = new FilteredElementCollector(doc)
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

            ElementId stairsId = null;

            GlobalVariables.Transaction.Commit();
            using (StairsEditScope stairsScope = new StairsEditScope(doc, "Create Stairs"))
            {
                stairsId = stairsScope.Start(bottomLevel.Id, topLevel.Id);

                using (Transaction trans = new Transaction(GlobalVariables.Document, "Create Stairs"))
                {
                    trans.Start();
                    StaircaseBlockProperties staircaseBlockProperties = StaircaseBlocks[0];

                    XYZ p1 = ComputePoints(staircaseBlockProperties.StartPoint, staircaseBlockProperties.Translation, staircaseBlockProperties.Rotation);
                    System.Diagnostics.Debug.WriteLine("p1: " + p1);
                    XYZ p2 = ComputePoints(new XYZ(staircaseBlockProperties.StartPoint.X, staircaseBlockProperties.StartPoint.Y + Width, staircaseBlockProperties.StartPoint.Z), staircaseBlockProperties.Translation, staircaseBlockProperties.Rotation);
                    System.Diagnostics.Debug.WriteLine("p2: " + p2);
                    Line runLine = Line.CreateBound(p1, p2);
                    StairsRun stairsRun = StairsRun.CreateStraightRun(doc, stairsId, runLine, StairsRunJustification.Center);
                    
                    stairsType.MinTreadDepth = Tread;
                    stairsType.MaxRiserHeight = Riser;
                    stairsType.get_Parameter(BuiltInParameter.STAIRS_ATTR_MINIMUM_TREAD_DEPTH).Set(Tread);
                    stairsType.get_Parameter(BuiltInParameter.STAIRS_ATTR_MAX_RISER_HEIGHT).Set(Riser);
                    //stairsType.get_Parameter(BuiltInParameter.STAIRS_ATTR_TREAD_THICKNESS).Set(StairThickness);

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
                CreatedStaircase.get_Parameter(BuiltInParameter.STAIRS_DESIRED_NUMBER_OF_RISERS).Set(Steps);
                //CreatedStaircase.get_Parameter(BuiltInParameter.STAIRS_ACTUAL_NUMBER_OF_RISERS).Set(Steps);
                //CreatedStaircase.get_Parameter(BuiltInParameter.STAIRS_ACTUAL_NUMBER_OF_RISERS).Set(Steps);
            }
        }

        private XYZ ComputePoints(XYZ startingPoint, double[] translation, double[] rotation)
        {
            XYZ currPoint = startingPoint;
            //apply translation
            currPoint = new XYZ(currPoint.X + (double)translation.GetValue(0), currPoint.Y + (double)translation.GetValue(1), currPoint.Z + (double)translation.GetValue(2));
            //apply rotation
            currPoint = new XYZ(currPoint.X * Math.Cos((double)rotation.GetValue(0)) - currPoint.Y * Math.Sin((double)rotation.GetValue(0)), currPoint.X * Math.Sin((double)rotation.GetValue(0)) + currPoint.Y * Math.Cos((double)rotation.GetValue(0)), currPoint.Z);
            return currPoint;
        }
    }
}


