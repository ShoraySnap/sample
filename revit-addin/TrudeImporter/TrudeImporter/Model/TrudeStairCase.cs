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
            System.Diagnostics.Debug.WriteLine("Storey={0}", Storey);
            System.Diagnostics.Debug.WriteLine("UniqueId={0}", UniqueId);
            System.Diagnostics.Debug.WriteLine("Height={0}", Height);
            System.Diagnostics.Debug.WriteLine("Width={0}", Width);
            System.Diagnostics.Debug.WriteLine("Tread={0}", Tread);
            System.Diagnostics.Debug.WriteLine("Riser={0}", Riser);
            System.Diagnostics.Debug.WriteLine("LandingWidth={0}", LandingWidth);
            System.Diagnostics.Debug.WriteLine("StairThickness={0}", StairThickness);
            System.Diagnostics.Debug.WriteLine("Steps={0}", Steps);
            System.Diagnostics.Debug.WriteLine("BaseOffset={0}", BaseOffset);
            System.Diagnostics.Debug.WriteLine("Name={0}", Name);
            System.Diagnostics.Debug.WriteLine("CenterPosition={0}", CenterPosition);
            System.Diagnostics.Debug.WriteLine("Type={0}", Type);
            System.Diagnostics.Debug.WriteLine("StaircaseType={0}", StaircaseType);
            System.Diagnostics.Debug.WriteLine("StaircasePreset={0}", StaircasePreset);




            // Create the staircase in Revit on the specified storey
            //CreateStaircase();
        }

    //    private void CreateStaircase()
    //    {
    //        Document doc = GlobalVariables.Document; // Assumes GlobalVariables.Document is the active Revit document
    //        Level baseLevel = doc.GetElement(new ElementId(Storey)) as Level; // Assumes Storey corresponds to the ElementId of the Level
    //        Level topLevel = doc.GetElement(new ElementId(Storey + 1)) as Level; // Assumes the top level is the next level up

    //        // Start a new transaction to create the staircase
    //        using (Transaction tx = new Transaction(doc, "Create Staircase"))
    //        {
    //            tx.Start();

    //            // Create the staircase
    //            Stairs stairs = Stairs.Create(doc, topLevel.Id, baseLevel.Id, Height);
    //            stairs.Name = Name;

    //            // Set the number of risers and the actual number of treads
    //            Parameter risersNumberParam = stairs.get_Parameter(BuiltInParameter.NUMBER_OF_RISERS);
    //            risersNumberParam.Set(Steps);
    //            Parameter treadsNumberParam = stairs.get_Parameter(BuiltInParameter.NUMBER_OF_TREADS);
    //            treadsNumberParam.Set(Steps - 1);

    //            // Create a sketch for the staircase
    //            StairsRun run = StairsRun.CreateStraightRun(doc, stairs.Id, Tread * Steps, Tread, Riser, Steps);
    //            run.Width = Width;

    //            // Add a landing if needed
    //            if (LandingWidth > 0)
    //            {
    //                // Here you would add code to create the landing at the appropriate position
    //            }

    //            // Set the base offset if needed
    //            if (BaseOffset != 0)
    //            {
    //                Parameter baseOffsetParam = stairs.get_Parameter(BuiltInParameter.STAIRS_BASE_OFFSET);
    //                baseOffsetParam.Set(BaseOffset);
    //            }

    //            // TODO: Apply materials to the staircase based on the layers
    //            // This requires a mapping from the layer values to actual Revit materials,
    //            // and then applying those materials to the stair components.

    //            // Commit the transaction
    //            tx.Commit();

    //            // Store the created staircase
    //            CreatedStaircase = stairs;
    //        }
    //    }
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
