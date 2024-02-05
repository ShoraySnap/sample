using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
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
        public new string Name { get; set; }
        public XYZ CenterPosition { get; set; }
        public string Type { get; set; }
        public string StaircaseType { get; set; }
        public string StaircasePreset { get; set; }
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
            Document doc = GlobalVariables.Document;
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
                    // Handle the case where no template is available
                    // This might involve loading a template from a file or handling the error
                    throw new InvalidOperationException("No StairsType template found to duplicate.");
                }
            }
            System.Diagnostics.Debug.WriteLine("Staircase type: " + stairsType);
            System.Diagnostics.Debug.WriteLine("topLevel: " + topLevel.Elevation);
            System.Diagnostics.Debug.WriteLine("bottomLevel: " + bottomLevel.Elevation);

            ElementId stairsId = null;

            GlobalVariables.Transaction.Commit();
            using (StairsEditScope stairsScope = new StairsEditScope(doc, "Create Stairs"))
            {
                stairsId = stairsScope.Start(bottomLevel.Id, topLevel.Id);

                using (Transaction trans = new Transaction(GlobalVariables.Document, "Create Stairs"))
                {
                    trans.Start();

                    // Create the stairs run using the previously calculated points and curves
                    // Example points for the run's sketch lines - replace with actual points from your stair design
                    XYZ p1 = new XYZ(0, 0, 0);
                    XYZ p2 = new XYZ(0, Width, 0);
                    Line runLine = Line.CreateBound(p1, p2);
                    StairsRun stairsRun = StairsRun.CreateStraightRun(doc, stairsId, runLine, StairsRunJustification.Center);

                    // Set the tread depth and riser height on the StairsType
                    stairsType.MinTreadDepth = Tread;
                    stairsType.MaxRiserHeight = Riser;
                    //stairsRun.get_Parameter(BuiltInParameter.STAIRS_ATTR_MINIMUM_TREAD_DEPTH).Set(Tread); // Use appropriate parameters
                    //stairsRun.get_Parameter(BuiltInParameter.STAIRS_ATTR_MAX_RISER_HEIGHT).Set(Riser);

                    trans.Commit();
                }
                stairsScope.Commit(new StairsFailurePreprocessor());
            }
            GlobalVariables.Transaction.Start();

            //// Retrieve the stairs instance after creation
            //CreatedStaircase = doc.GetElement(stairsId) as Stairs;

            //// Set the base offset if needed
            //if (BaseOffset != 0 && CreatedStaircase != null)
            //{
            //    Parameter baseOffsetParam = CreatedStaircase.get_Parameter(BuiltInParameter.STAIRS_BASE_OFFSET);
            //    baseOffsetParam.Set(BaseOffset);
            //}

            // Apply materials to the staircase based on the layers
            // ...
        }

    }
}


