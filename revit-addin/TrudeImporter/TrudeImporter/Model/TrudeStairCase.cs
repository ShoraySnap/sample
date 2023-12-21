using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
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

            //print the layers
            foreach (var layer in Layers)
            {
                System.Diagnostics.Debug.WriteLine("{0}={1}", layer.Name,layer.IsCore);
            }


            //print all the properties
            System.Diagnostics.Debug.WriteLine("UniqueId={0}", UniqueId);




            // Create the staircase in Revit on the specified storey
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
            Level baseLevel = doc.GetElement(new ElementId(Storey)) as Level;
            Level topLevel = doc.GetElement(new ElementId(Storey + 1)) as Level;

            // Get the StairsType to be used for stairs creation
            StairsType stairsType = new FilteredElementCollector(doc)
                .OfClass(typeof(StairsType))
                .OfType<StairsType>()
                .FirstOrDefault(st => st.Name.Equals(StaircaseType, StringComparison.OrdinalIgnoreCase)); // Use a default stairs type or match by name

            if (stairsType == null)
            {
                StairsType stairsTypeTemplate = new FilteredElementCollector(doc).OfClass(typeof(StairsType)).Cast<StairsType>().FirstOrDefault();

                // If a template is available, duplicate it to create a new StairsType
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

            ElementId stairsId = null;
            using (StairsEditScope stairsScope = new StairsEditScope(doc, "Create Stairs"))
            {
                stairsId = stairsScope.Start(topLevel.Id);

                using (Transaction trans = new Transaction(doc, "Create Stairs Run"))
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
                    stairsRun.get_Parameter(BuiltInParameter.STAIRS_ATTR_MINIMUM_TREAD_DEPTH).Set(Tread); // Use appropriate parameters
                    stairsRun.get_Parameter(BuiltInParameter.STAIRS_ATTR_MAX_RISER_HEIGHT).Set(Riser);

                    // Create the landing if necessary
                    // ...

                    trans.Commit();
                }

                stairsScope.Commit(new StairsFailurePreprocessor());
            }

            // Retrieve the stairs instance after creation
            CreatedStaircase = doc.GetElement(stairsId) as Stairs;

            // Set the base offset if needed
            if (BaseOffset != 0 && CreatedStaircase != null)
            {
                Parameter baseOffsetParam = CreatedStaircase.get_Parameter(BuiltInParameter.STAIRS_BASE_OFFSET);
                baseOffsetParam.Set(BaseOffset);
            }

            // Apply materials to the staircase based on the layers
            // ...
        }

    }
}



//        //STAIRCASES......................................................................
//        //ST_Staircase st_staircase = new ST_Staircase();
//        //JToken stairs = geometryParent["staircases"];
//        //foreach (var stair in stairs)
//        //{
//        //    break;
//        //    processedElements++;
//        //    LogProgress(processedElements, totalElements);

//        //    try
//        //    {
//        //        var stairData = stair.First;
//        //        if (IsThrowAway(stairData))
//        //        {
//        //            continue;
//        //        }
//        //        ST_Staircase stairObj = new ST_Staircase();
//        //        stairObj.Props = stairData["dsProps"];
//        //        stairObj.Mesh = stairData["meshes"].First;
//        //        stairObj.Scaling = stairObj.Mesh["scaling"].Select(jv => (double)jv).ToArray();
//        //        stairObj.SnaptrudePosition = stairObj.Mesh["position"].Select(jv => UnitsAdapter.convertToRevit((double)jv)).ToArray();
//        //        stairObj.Type = stairObj.Props["staircaseType"].ToString();
//        //        stairObj.levelBottom = (from lvl in new FilteredElementCollector(GlobalVariables.Document).
//        //            OfClass(typeof(Level)).
//        //            Cast<Level>()
//        //                                where (lvl.Id == GlobalVariables.LevelIdByNumber[int.Parse(stairObj.Props["storey"].ToString())])
//        //                                select lvl).First();
//        //        stairObj.levelTop = (from lvl in new FilteredElementCollector(GlobalVariables.Document).
//        //            OfClass(typeof(Level)).
//        //            Cast<Level>()
//        //                             where (lvl.Id == GlobalVariables.LevelIdByNumber[int.Parse(stairObj.Props["storey"].ToString()) + 1])
//        //                             select lvl).First();

//        //        ElementId staircase = stairObj.CreateStairs(GlobalVariables.Document);
//        //        Stairs currStair;
//        //        using (StairsEditScope newStairsScope = new StairsEditScope(GlobalVariables.Document, "edit Stairs"))
//        //        {
//        //            ElementId newStairsId = newStairsScope.Start(staircase);
//        //            using (SubTransaction stairsTrans = new SubTransaction(GlobalVariables.Document))
//        //            {
//        //                stairsTrans.Start();
//        //                currStair = GlobalVariables.Document.GetElement(newStairsId) as Stairs;
//        //                currStair.DesiredRisersNumber = int.Parse(stairObj.Props["steps"].ToString());
//        //                StairsType stairsType = GlobalVariables.Document.GetElement(currStair.GetTypeId()) as StairsType;

//        //                StairsType newStairsType = stairsType.Duplicate("stairs_" + RandomString(5)) as StairsType;

//        //                newStairsType.MaxRiserHeight = UnitsAdapter.convertToRevit(stairObj.Props["riser"]);
//        //                newStairsType.MinRunWidth = UnitsAdapter.convertToRevit(stairObj.Props["width"]);
//        //                newStairsType.MinTreadDepth = UnitsAdapter.convertToRevit(stairObj.Props["tread"]);

//        //                currStair.ChangeTypeId(newStairsType.Id);

//        //                currStair
//        //                    .get_Parameter(BuiltInParameter.STAIRS_ACTUAL_TREAD_DEPTH)
//        //                    .Set(UnitsAdapter.convertToRevit(stairObj.Props["tread"]));

//        //                stairsTrans.Commit();
//        //            }
//        //            newStairsScope.Commit(new StairsFailurePreprocessor());
//        //        }

//        //        // DELETE EXISTING RAILINGS
//        //        using(SubTransaction transactionDeleteRailings = new SubTransaction(GlobalVariables.Document))
//        //        {
//        //            transactionDeleteRailings.Start();
//        //            try
//        //            {

//        //                ICollection<ElementId> railingIds = currStair.GetAssociatedRailings();
//        //                foreach (ElementId railingId in railingIds)
//        //                {
//        //                    GlobalVariables.Document.Delete(railingId);
//        //                }
//        //                transactionDeleteRailings.Commit();

//        //            }
//        //            catch (Exception e)
//        //            {
//        //                LogTrace("Error in deleting staircase railings", e.ToString());
//        //            }
//        //        }
//        //    }
//        //    catch (Exception exception)
//        //    {
//        //        LogTrace("Error in creating staircase", exception.ToString());
//        //    }
//        //}
//        //LogTrace("staircases created");
//        // ......................................................................
