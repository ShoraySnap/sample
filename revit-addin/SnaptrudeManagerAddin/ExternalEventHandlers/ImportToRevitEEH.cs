using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB.Visual;
using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using TrudeCommon.Events;
using TrudeImporter;

namespace SnaptrudeManagerAddin
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ImportToRevitEEH : IExternalEventHandler
    {
        public string GetName()
        {
            return GetType().Name;
        }
        public void Execute(UIApplication uiapp)
        {
            Document doc = uiapp.ActiveUIDocument.Document;

            GlobalVariables.Document = doc;
            GlobalVariables.RvtApp = uiapp.Application;
            GlobalVariables.ForForge = false;

            uiapp.Application.FailuresProcessing += Application_FailuresProcessing;
            try
            {
                bool status = false;
                using (TransactionGroup tg = new TransactionGroup(doc, "Parse Trude"))
                {
                    tg.Start();
                    using (Transaction t = new Transaction(doc, "Parse Trude"))
                    {
                        GlobalVariables.Transaction = t;
                        t.Start();
                        status = ImportTrude();
                        if (!t.HasEnded()) t.Commit();
                    }
                    if (TrudeImporterMain.Abort)
                    {
                        tg.RollBack();
                    }
                    else
                    {
                        tg.Assimilate();
                    }
                }

                if (status && !TrudeImporterMain.Abort)
                {
                    TrudeEventEmitter.EmitEvent(TRUDE_EVENT.REVIT_PLUGIN_IMPORT_TO_REVIT_SUCCESS);
                }
                else
                {
                    TrudeEventEmitter.EmitEvent(TRUDE_EVENT.REVIT_PLUGIN_IMPORT_TO_REVIT_ABORTED);
                }
                GlobalVariables.cleanGlobalVariables();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("catch", ex.ToString());
                GlobalVariables.cleanGlobalVariables();
            }
            finally
            {
                uiapp.Application.FailuresProcessing -= Application_FailuresProcessing;
                GlobalVariables.cleanGlobalVariables();
            }
        }

        void Application_FailuresProcessing(object sender, Autodesk.Revit.DB.Events.FailuresProcessingEventArgs e)
        {
            FailuresAccessor fa = e.GetFailuresAccessor();

            fa.DeleteAllWarnings();
        }

        private bool ImportTrude()
        {
            GlobalVariables.LevelIdByNumber.Clear();
            FamilyLoader.LoadedFamilies.Clear();

            _ = new FetchTextures.FetchTextures();
            deleteRemovedElements(GlobalVariables.TrudeProperties.DeletedElements);

            TrudeImporterMain.Import(GlobalVariables.TrudeProperties);

            FamilyLoader.LoadedFamilies.Clear();
            GlobalVariables.LevelIdByNumber.Clear();

            TrudeWall.TypeStore.Clear();
            TrudeFloor.TypeStore.Clear();
            TrudeBeam.types.Clear();
            TrudeColumn.types.Clear();
            TrudeColumn.NewLevelsByElevation.Clear();
            TrudeSlab.TypeStore.Clear();
            TrudeDoor.TypeStore.Clear();
            TrudeWindow.TypeStore.Clear();

            return true;
        }

        private void deleteRemovedElements(List<int> elementIds)
        {
            foreach (int elementId in elementIds)
            {
                try
                {
                    #if REVIT2019 || REVIT2020 || REVIT2021 || REVIT2022 || REVIT2023
                    ElementId id = new ElementId((int)elementId);
                    #else
                    ElementId id = new ElementId((Int64)elementId);
                    #endif
                    Element element = GlobalVariables.Document.GetElement(id);
                    if (!element.GroupId.Equals(ElementId.InvalidElementId))
                        TrudeImporterMain.deleteIfInGroup(element);
                    else
                        GlobalVariables.Document.Delete(id);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Exception in removing deleted elements:" + e.Message);
                }
            }
        }

        private static int countTotalElement(JObject jObject)
        {
            int totalElements = 0;

            if (jObject["metadata"] != null && jObject["metadata"]["revitDeleteIds"] != null)
            {

                using (SubTransaction t = new SubTransaction(GlobalVariables.Document))
                {
                    t.Start();
                    foreach (JToken elementId in jObject["metadata"]["revitDeleteIds"])
                    {
                        try
                        {
                            #if REVIT2019 || REVIT2020 || REVIT2021 || REVIT2022 || REVIT2023
                            ElementId id = new ElementId((int)elementId);
                            #else
                            ElementId id = new ElementId((Int64)elementId);
                            #endif
                            GlobalVariables.Document.Delete(id);
                        }
                        catch (Exception e)
                        {
                            System.Console.WriteLine(e.Message);
                        }
                    }
                    t.Commit();
                }
            }

            foreach (JToken structure in jObject.GetValue("structures"))
            {
                JToken structureData = structure.First;

                // STOREYS
                JToken storeys = structureData["storeys"];
                if (!storeys.HasValues) continue;
                totalElements += storeys.Count();

                JToken geometryParent = structureData["01"];
                if (geometryParent is null) continue;

                //WALLS
                JToken walls = geometryParent["walls"];
                totalElements += walls.Count();

                //FLOORS
                JToken floors = geometryParent["floors"];
                totalElements += floors.Count();

                //BASE FLOORS AND INTERMEDIATE FLOORS
                JToken roofs = geometryParent["roofs"];
                totalElements += roofs.Count();

                // Columns and Beams
                JToken masses = geometryParent["masses"];
                totalElements += masses.Count();

                //DOORS
                JToken doors = geometryParent["doors"];
                totalElements += doors.Count();

                //WINDOWS
                JToken windows = geometryParent["windows"];
                totalElements += windows.Count();

                //STAIRCASES
                JToken stairs = geometryParent["staircases"];
                totalElements += stairs.Count();

                //FURNITURES
                JToken furnitures = geometryParent["furnitures"];
                totalElements += furnitures.Count();
            }

            return totalElements;
        }

        /// <summary>
        /// Create a data structure, attach it to a wall, 
        /// populate it with data, and retrieve the data 
        /// back from the wall
        /// </summary>
        public void StoreDataInElement(Element element, string dataToStore)
        {
            //Transaction createSchemaAndStoreData = new Transaction(wall.Document, "tCreateAndStore");

            //createSchemaAndStoreData.Start();
            SchemaBuilder schemaBuilder = new SchemaBuilder(new Guid("720080CB-DA99-40DC-9415-E53F280AA1F0"));

            // allow anyone to read the object
            schemaBuilder.SetReadAccessLevel(AccessLevel.Public);

            // restrict writing to this vendor only
            schemaBuilder.SetWriteAccessLevel(AccessLevel.Public);

            // required because of restricted write-access
            schemaBuilder.SetVendorId("ADSK");

            // create a field to store an XYZ
            FieldBuilder fieldBuilder = schemaBuilder.AddSimpleField("snaptrudeId", typeof(String));

            //fieldBuilder.SetUnitType(UnitType.UT_Length);

            fieldBuilder.SetDocumentation("A stored location value representing a wiring splice in a wall.");

            schemaBuilder.SetSchemaName("snaptrudeId");

            Schema schema = schemaBuilder.Finish(); // register the Schema object

            // create an entity (object) for this schema (class)
            Entity entity = new Entity(schema);

            // get the field from the schema
            Field fieldSpliceLocation = schema.GetField("snaptrudeId");

            entity.Set<string>(fieldSpliceLocation, dataToStore); // set the value for this entity

            element.SetEntity(entity); // store the entity in the element

            // get the data back from the wall
            Entity retrievedEntity = element.GetEntity(schema);

            string retrievedData = retrievedEntity.Get<string>(schema.GetField("snaptrudeId"));

            //createSchemaAndStoreData.Commit();
        }

        private string GetDataFromElement(Element element)
        {
            return null;

            SchemaBuilder schemaBuilder = new SchemaBuilder(new Guid("720080CB-DA99-40DC-9415-E53F280AA1F0"));

            // allow anyone to read the object
            schemaBuilder.SetReadAccessLevel(AccessLevel.Public);

            // restrict writing to this vendor only
            schemaBuilder.SetWriteAccessLevel(AccessLevel.Public);

            // required because of restricted write-access
            schemaBuilder.SetVendorId("ADSK");

            // create a field to store an XYZ
            FieldBuilder fieldBuilder = schemaBuilder.AddSimpleField("snaptrudeId", typeof(String));

            //fieldBuilder.SetUnitType(UnitType.UT_Length);

            fieldBuilder.SetDocumentation("A stored location value representing a wiring splice in a wall.");

            schemaBuilder.SetSchemaName("snaptrudeId");

            Schema schema = schemaBuilder.Finish(); // register the Schema object
            Entity retrievedEntity = element.GetEntity(schema);

            try
            {
                string retrievedData = retrievedEntity.Get<string>(schema.GetField("snaptrudeId"));

                return retrievedData;
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException e)
            {
                System.Console.WriteLine(e.Message);
                return null;
            }
        }
        public bool AreLayersSame(TrudeLayer[] stLayers, FloorType wallType)
        {
            CompoundStructure compoundStructure = wallType.GetCompoundStructure();
            IList<CompoundStructureLayer> layers = compoundStructure.GetLayers();

            if (stLayers.Length != layers.Count) return false;

            bool areSame = true;
            for (int i = 0; i < stLayers.Length; i++)
            {
                TrudeLayer stLayer = stLayers[i];
                CompoundStructureLayer layer = layers[i];

                if (Math.Round(stLayer.ThicknessInMm) != UnitsAdapter.FeetToMM(layer.Width))
                {
                    //if (stLayer.IsCore && layer.Function == MaterialFunctionAssignment.Structure) continue; // TODO: remove this after core thickness is fixed on snaptrude end. 

                    areSame = false;
                    break;
                }
            }

            return areSame;
        }

        public ExternalDBApplicationResult OnShutdown(ControlledApplication application)
        {
            return ExternalDBApplicationResult.Succeeded;
        }

        /// <summary>
        /// Class representing a staircase.
        /// </summary>        
        public class ST_Staircase
        {
            /// <summary>
            /// Represents snaptrude dsprops.
            /// </summary>
            /// <value>
            /// Gets JToken of dsprops of current stair.
            /// </value>
            public JToken Props { get; set; }
            /// <summary>
            /// Represents snaptrude meshes.
            /// </summary>
            /// <value>
            /// Gets JToken of meshes of current stair.
            /// </value>
            public JToken Mesh { get; set; }
            /// <summary>
            /// Represents bottom level of the stair.
            /// </summary>
            /// <value>
            /// Gets bottom level of current stair.
            /// </value>
            public Level levelBottom { get; set; }
            /// <summary>
            /// Represents top level of the stair.
            /// </summary>
            /// <value>
            /// Gets top level of current stair.
            /// </value>
            public Level levelTop { get; set; }
            public string Name { get; set; }
            /// <summary>
            /// Represents type of the stair. (straight, dogLegged, lShaped or square)
            /// </summary>
            /// <value>
            /// Gets type of current stair.
            /// </value>
            public string Type { get; set; }
            /// <summary>
            /// Represents position of the stair as in Snaptrude.
            /// </summary>
            /// <value>
            /// Gets position of current stair. (double array)
            /// </value>
            public double[] SnaptrudePosition { get; set; }
            /// <summary>
            /// Represents scaling values of the stair. 
            /// </summary>
            /// <value>
            /// Gets scaling values of current stair.
            /// </value>
            public double[] Scaling { get; set; }
            /// <summary>
            /// Constructs a scaling transformation matrix.
            /// </summary>
            /// <param name="centre"> Point w.r.t which scaling will happen. A revit XYZ point.</param>
            /// <returns>Scaling Matrix.</returns>
            public double[,] getScaleMatrix(XYZ centre)
            {
                double[,] scaleMat = new double[4, 4];
                scaleMat[0, 0] = this.Scaling[0];
                scaleMat[1, 1] = this.Scaling[2];
                scaleMat[2, 2] = this.Scaling[1];
                scaleMat[3, 3] = 1.0;
                scaleMat[0, 3] = centre[0] * (1 - this.Scaling[0]);
                scaleMat[1, 3] = centre[1] * (1 - this.Scaling[2]);
                scaleMat[2, 3] = centre[2] * (1 - this.Scaling[1]);
                scaleMat[0, 1] = scaleMat[0, 2] = 0.0;
                scaleMat[1, 0] = scaleMat[1, 2] = 0.0;
                scaleMat[2, 0] = scaleMat[2, 1] = 0.0;
                scaleMat[3, 0] = scaleMat[3, 1] = scaleMat[3, 2] = 0.0;

                return scaleMat;
            }
            /// <summary>
            /// Process the given point using the given transformation matrix.
            /// </summary>
            /// <param name="point">Point to be processed. A revit XYZ point.</param>
            /// <param name="Matrix">Transformation matrix to be used. A 2D double array.</param>
            /// <returns>The processed or transformed point.</returns>
            public XYZ getProcessedPts(XYZ point, double[,] Matrix)
            {
                double[,] currPt = { { point.X }, { point.Y }, { point.Z }, { 1 } };
                double[,] prod = new double[4, 1];
                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 1; j++)
                    {
                        prod[i, j] = 0;
                        for (int k = 0; k < 4; k++)
                            prod[i, j] += Matrix[i, k] * currPt[k, j];
                    }
                }
                XYZ processed = new XYZ(prod[0, 0], prod[1, 0], prod[2, 0]);
                return processed;
            }
            /// <summary>
            /// Constructs a rotation transformation matrix.
            /// </summary>
            /// <param name="q">Rotation in terms of a Quaternion quantity. A Quaternion type object.</param>
            /// <param name="centre">Point w.r.t which rotation will happen. A revit XYZ point.</param>
            /// <returns>The rotation matrix.</returns>
            private double[,] getRotMatrix(Quaternion q, XYZ centre)
            {
                double[,] rotMatrix = new double[4, 4];
                q.Normalize();
                double sqw = q.W * q.W;
                double sqx = q.X * q.X;
                double sqy = q.Y * q.Y;
                double sqz = q.Z * q.Z;
                rotMatrix[0, 0] = sqx - sqy - sqz + sqw; // since sqw + sqx + sqy + sqz =1
                rotMatrix[1, 1] = -sqx + sqy - sqz + sqw;
                rotMatrix[2, 2] = -sqx - sqy + sqz + sqw;

                double tmp1 = q.X * q.Y;
                double tmp2 = q.Z * q.W;
                rotMatrix[0, 1] = 2.0 * (tmp1 + tmp2);
                rotMatrix[1, 0] = 2.0 * (tmp1 - tmp2);

                tmp1 = q.X * q.Z;
                tmp2 = q.Y * q.W;
                rotMatrix[0, 2] = 2.0 * (tmp1 - tmp2);
                rotMatrix[2, 0] = 2.0 * (tmp1 + tmp2);

                tmp1 = q.Y * q.Z;
                tmp2 = q.X * q.W;
                rotMatrix[1, 2] = 2.0 * (tmp1 + tmp2);
                rotMatrix[2, 1] = 2.0 * (tmp1 - tmp2);

                double a1, a2, a3;

                a1 = centre.X;
                a2 = centre.Y;
                a3 = centre.Z;

                rotMatrix[0, 3] = a1 - a1 * rotMatrix[0, 0] - a2 * rotMatrix[0, 1] - a3 * rotMatrix[0, 2];
                rotMatrix[1, 3] = a2 - a1 * rotMatrix[1, 0] - a2 * rotMatrix[1, 1] - a3 * rotMatrix[1, 2];
                rotMatrix[2, 3] = a3 - a1 * rotMatrix[2, 0] - a2 * rotMatrix[2, 1] - a3 * rotMatrix[2, 2];
                rotMatrix[3, 0] = rotMatrix[3, 1] = rotMatrix[3, 2] = 0.0;
                rotMatrix[3, 3] = 1.0;

                return rotMatrix;

            }
            /// <summary>
            /// The method used to actually create stairs.
            /// </summary>
            /// <param name="document">The revit document to which the stair will be added.</param>
            /// <returns>ElementId of the stair created.</returns>
            public ElementId CreateStairs(Document document)
            {
                if (this.Type == "straight")
                {
                    return this.CreateStraightStairs(document);
                }
                else if (this.Type == "square")
                {
                    return this.CreateSquareStairs(document);
                }
                else if (this.Type == "dogLegged")
                {
                    return this.CreateDogLeggedStairs(document);
                }
                else if (this.Type == "lShaped")
                {
                    return this.CreateLShapedStairs(document);
                }
                else return null;
            }
            /// <summary>
            /// The method used to create a straight type of stair.
            /// </summary>
            /// <param name="document">The revit document to which the stair will be added.</param>
            /// <returns>ElementId of the stair created.</returns>
            private ElementId CreateStraightStairs(Document document)
            {
                ElementId newStairsId = null;
                using (StairsEditScope newStairsScope = new StairsEditScope(document, "New Stairs"))
                {
                    newStairsId = newStairsScope.Start(this.levelBottom.Id, this.levelTop.Id);
                    using (SubTransaction stairsTrans = new SubTransaction(document))
                    {
                        stairsTrans.Start();
                        double tread_depth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["tread"].ToString()));
                        // 'Y' coordinate in snaptrude is z coordinate in revit
                        // as revit draws in xy plane and snaptrude in xz plane.
                        double[] position = { this.SnaptrudePosition[0], this.SnaptrudePosition[2], this.SnaptrudePosition[1] };
                        int riserNum = int.Parse(this.Props["steps"].ToString());
                        position[2] = 0;
                        double runWidth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["width"].ToString()));
                        double[,] rotMatrix = new double[4, 4];
                        if (this.Mesh["rotationQuaternion"] == null)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                for (int j = 0; j < 4; j++)
                                {
                                    if (i == j) rotMatrix[i, j] = 1.0;
                                    else rotMatrix[i, j] = 0.0;
                                }
                            }
                        }
                        else
                        {
                            double[] quat = this.Mesh["rotationQuaternion"].Select(jv => UnitsAdapter.convertToRevit((double)jv)).ToArray();
                            Quaternion q = new Quaternion(quat[0], quat[2], quat[1], quat[3]);
                            XYZ pos = new XYZ(position[0], position[1], position[2]);
                            double[,] tmp = this.getRotMatrix(q, pos);
                            rotMatrix = tmp;
                        }

                        double[] scale = this.Mesh["scaling"].Select(jv => (double)jv).ToArray();
                        XYZ pos1 = new XYZ(position[0], position[1], position[2]);
                        double[,] scaleMatrix = this.getScaleMatrix(pos1);

                        int[] stepsArrange = this.Props["stepsArrangement"].First.Select(jv => (int)jv).ToArray();
                        double[] landings = this.Props["landings"].First.Select(jv => UnitsAdapter.convertToRevit((double)jv)).ToArray();

                        int landingInd = 0;
                        double[] topElevArr = new double[landings.Length + 2];
                        topElevArr[0] = 0.0;
                        XYZ[] landingCor = new XYZ[landings.Length + 1];
                        for (int i = 0; i < stepsArrange.Length; i++)
                        {
                            var stepsNo = stepsArrange[i];
                            IList<Curve> bdryCurves = new List<Curve>();
                            IList<Curve> riserCurves = new List<Curve>();
                            IList<Curve> pathCurves = new List<Curve>();

                            XYZ pnt1 = new XYZ(position[0], position[1], position[2]);
                            XYZ pnt2 = new XYZ(position[0] - stepsNo * tread_depth, position[1], position[2]);
                            XYZ pnt3 = new XYZ(position[0], position[1] - runWidth, position[2]);
                            XYZ pnt4 = new XYZ(position[0] - stepsNo * tread_depth, position[1] - runWidth, position[2]);

                            XYZ rpnt1 = this.getProcessedPts(this.getProcessedPts(pnt1, scaleMatrix), rotMatrix);
                            XYZ rpnt2 = this.getProcessedPts(this.getProcessedPts(pnt2, scaleMatrix), rotMatrix);
                            XYZ rpnt3 = this.getProcessedPts(this.getProcessedPts(pnt3, scaleMatrix), rotMatrix);
                            XYZ rpnt4 = this.getProcessedPts(this.getProcessedPts(pnt4, scaleMatrix), rotMatrix);

                            // boundaries
                            bdryCurves.Add(Line.CreateBound(rpnt1, rpnt2));
                            bdryCurves.Add(Line.CreateBound(rpnt3, rpnt4));

                            // riser curves
                            double interval = (pnt2.X - pnt1.X) / stepsNo;
                            for (int ii = 0; ii <= stepsNo; ii++)
                            {
                                XYZ end0 = new XYZ(pnt1.X + ii * interval, pnt1.Y, pnt1.Z);
                                XYZ end1 = new XYZ(pnt1.X + ii * interval, pnt3.Y, pnt3.Z);
                                XYZ rend0 = this.getProcessedPts(this.getProcessedPts(end0, scaleMatrix), rotMatrix);
                                XYZ rend1 = this.getProcessedPts(this.getProcessedPts(end1, scaleMatrix), rotMatrix);
                                riserCurves.Add(Line.CreateBound(rend0, rend1));
                            }

                            //stairs path curves
                            XYZ pathEnd0 = (pnt1 + pnt3) / 2.0;
                            XYZ pathEnd1 = (pnt2 + pnt4) / 2.0;
                            XYZ rpathEnd0 = this.getProcessedPts(this.getProcessedPts(pathEnd0, scaleMatrix), rotMatrix);
                            XYZ rpathEnd1 = this.getProcessedPts(this.getProcessedPts(pathEnd1, scaleMatrix), rotMatrix);

                            pathCurves.Add(Line.CreateBound(rpathEnd0, rpathEnd1));

                            StairsRun newRun1 = StairsRun.CreateSketchedRun(document, newStairsId, topElevArr[landingInd], bdryCurves, riserCurves, pathCurves);
                            newRun1.EndsWithRiser = false;
                            topElevArr[landingInd + 1] = newRun1.TopElevation;
                            XYZ nextPos = pnt2;
                            if (landingInd < landings.Length)
                            {
                                XYZ tmp = new XYZ(pnt2.X - landings[landingInd], pnt2.Y, pnt2.Z);
                                nextPos = tmp;
                                landingCor[landingInd] = pnt2;
                                landingInd++;
                            }
                            position[0] = nextPos.X;
                            position[1] = nextPos.Y;
                            position[2] = nextPos.Z;
                        }

                        for (int i = 0; i < landings.Length; i++)
                        {
                            // Add a landing between the runs
                            CurveLoop landingLoop = new CurveLoop();
                            XYZ p1 = landingCor[i];
                            XYZ p2 = new XYZ(p1.X - landings[i], p1.Y, p1.Z);
                            XYZ p3 = new XYZ(p1.X, p1.Y - runWidth, p1.Z);
                            XYZ p4 = new XYZ(p1.X - landings[i], p1.Y - runWidth, p1.Z);

                            XYZ rp1 = this.getProcessedPts(this.getProcessedPts(p1, scaleMatrix), rotMatrix);
                            XYZ rp2 = this.getProcessedPts(this.getProcessedPts(p2, scaleMatrix), rotMatrix);
                            XYZ rp3 = this.getProcessedPts(this.getProcessedPts(p3, scaleMatrix), rotMatrix);
                            XYZ rp4 = this.getProcessedPts(this.getProcessedPts(p4, scaleMatrix), rotMatrix);

                            Line curve_1 = Line.CreateBound(rp1, rp2);
                            Line curve_2 = Line.CreateBound(rp2, rp4);
                            Line curve_3 = Line.CreateBound(rp4, rp3);
                            Line curve_4 = Line.CreateBound(rp3, rp1);

                            landingLoop.Append(curve_1);
                            landingLoop.Append(curve_2);
                            landingLoop.Append(curve_3);
                            landingLoop.Append(curve_4);
                            StairsLanding newLanding = StairsLanding.CreateSketchedLanding(document, newStairsId, landingLoop, topElevArr[i + 1]);
                        }
                        stairsTrans.Commit();
                    }
                    // A failure preprocessor is to handle possible failures during the edit mode commitment process.
                    newStairsScope.Commit(new StairsFailurePreprocessor());
                }

                return newStairsId;
            }
            /// <summary>
            /// The method used to create a lShaped type of stair.
            /// </summary>
            /// <param name="document">The revit document to which the stair will be added.</param>
            /// <returns>ElementId of the stair created.</returns>
            private ElementId CreateLShapedStairs(Document document)
            {
                ElementId newStairsId = null;
                using (StairsEditScope newStairsScope = new StairsEditScope(document, "New Stairs"))
                {
                    newStairsId = newStairsScope.Start(this.levelBottom.Id, this.levelTop.Id);
                    using (SubTransaction stairsTrans = new SubTransaction(document))
                    {
                        stairsTrans.Start();
                        double tread_depth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["tread"].ToString()));
                        // 'Y' coordinate in snaptrude is z coordinate in revit
                        // as revit draws in xy plane and snaptrude in xz plane.
                        double[] position = { this.SnaptrudePosition[0], this.SnaptrudePosition[2], this.SnaptrudePosition[1] };
                        int riserNum = int.Parse(this.Props["steps"].ToString());
                        position[2] = 0;
                        double runWidth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["width"].ToString()));
                        double[,] rotMatrix = new double[4, 4];
                        if (this.Mesh["rotationQuaternion"] == null)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                for (int j = 0; j < 4; j++)
                                {
                                    if (i == j) rotMatrix[i, j] = 1.0;
                                    else rotMatrix[i, j] = 0.0;
                                }
                            }
                        }
                        else
                        {
                            double[] quat = this.Mesh["rotationQuaternion"].Select(jv => (double)jv).ToArray();
                            Quaternion q = new Quaternion(quat[0], quat[2], quat[1], quat[3]);
                            XYZ pos = new XYZ(position[0], position[1], position[2]);
                            double[,] tmp = this.getRotMatrix(q, pos);
                            rotMatrix = tmp;
                        }

                        double[] scale = this.Mesh["scaling"].Select(jv => (double)jv).ToArray();
                        XYZ pos1 = new XYZ(position[0], position[1], position[2]);
                        double[,] scaleMatrix = this.getScaleMatrix(pos1);

                        int[][] stepsArrange = this.Props["stepsArrangement"].Select(jv => jv.Select(jv1 => (int)jv1).ToArray()).ToArray();
                        double[][] landings = this.Props["landings"].Select(jv => jv.Select(jv1 => UnitsAdapter.convertToRevit((double)jv1)).ToArray()).ToArray();

                        int landingInd = 0;
                        double[] topElevArr = new double[stepsArrange.Length + 2];
                        topElevArr[0] = 0.0;
                        XYZ[] landingCor = new XYZ[stepsArrange.Length + 1];
                        for (int i = 0; i < stepsArrange.Length; i++)
                        {
                            for (int j = 0; j < stepsArrange[i].Length; j++)
                            {
                                var stepsNo = stepsArrange[i][j];
                                IList<Curve> bdryCurves = new List<Curve>();
                                IList<Curve> riserCurves = new List<Curve>();
                                IList<Curve> pathCurves = new List<Curve>();
                                XYZ pnt1, pnt2, pnt3, pnt4;
                                if (i == 0)
                                {
                                    pnt1 = new XYZ(position[0], position[1], position[2]);
                                    pnt2 = new XYZ(position[0] - stepsNo * tread_depth, position[1], position[2]);
                                    pnt3 = new XYZ(position[0], position[1] - runWidth, position[2]);
                                    pnt4 = new XYZ(position[0] - stepsNo * tread_depth, position[1] - runWidth, position[2]);
                                }
                                else
                                {
                                    pnt1 = new XYZ(position[0], position[1], position[2]);
                                    pnt2 = new XYZ(position[0], position[1] - stepsNo * tread_depth, position[2]);
                                    pnt3 = new XYZ(position[0] - runWidth, position[1], position[2]);
                                    pnt4 = new XYZ(position[0] - runWidth, position[1] - stepsNo * tread_depth, position[2]);
                                }


                                XYZ rpnt1 = this.getProcessedPts(this.getProcessedPts(pnt1, scaleMatrix), rotMatrix);
                                XYZ rpnt2 = this.getProcessedPts(this.getProcessedPts(pnt2, scaleMatrix), rotMatrix);
                                XYZ rpnt3 = this.getProcessedPts(this.getProcessedPts(pnt3, scaleMatrix), rotMatrix);
                                XYZ rpnt4 = this.getProcessedPts(this.getProcessedPts(pnt4, scaleMatrix), rotMatrix);

                                // boundaries
                                bdryCurves.Add(Line.CreateBound(rpnt1, rpnt2));
                                bdryCurves.Add(Line.CreateBound(rpnt3, rpnt4));

                                // riser curves
                                double interval;
                                if (i == 0)
                                {
                                    interval = (pnt2.X - pnt1.X) / stepsNo;
                                }
                                else
                                {
                                    interval = (pnt2.Y - pnt1.Y) / stepsNo;
                                }

                                for (int ii = 0; ii <= stepsNo; ii++)
                                {
                                    XYZ end0;
                                    XYZ end1;
                                    if (i == 0)
                                    {
                                        end0 = new XYZ(pnt1.X + ii * interval, pnt1.Y, pnt1.Z);
                                        end1 = new XYZ(pnt1.X + ii * interval, pnt3.Y, pnt3.Z);
                                    }
                                    else
                                    {
                                        end0 = new XYZ(pnt1.X, pnt1.Y + ii * interval, pnt1.Z);
                                        end1 = new XYZ(pnt3.X, pnt1.Y + ii * interval, pnt3.Z);
                                    }

                                    XYZ rend0 = this.getProcessedPts(this.getProcessedPts(end0, scaleMatrix), rotMatrix);
                                    XYZ rend1 = this.getProcessedPts(this.getProcessedPts(end1, scaleMatrix), rotMatrix);
                                    riserCurves.Add(Line.CreateBound(rend0, rend1));
                                }

                                //stairs path curves
                                XYZ pathEnd0 = (pnt1 + pnt3) / 2.0;
                                XYZ pathEnd1 = (pnt2 + pnt4) / 2.0;
                                XYZ rpathEnd0 = this.getProcessedPts(this.getProcessedPts(pathEnd0, scaleMatrix), rotMatrix);
                                XYZ rpathEnd1 = this.getProcessedPts(this.getProcessedPts(pathEnd1, scaleMatrix), rotMatrix);

                                pathCurves.Add(Line.CreateBound(rpathEnd0, rpathEnd1));

                                StairsRun newRun1 = StairsRun.CreateSketchedRun(document, newStairsId, topElevArr[landingInd], bdryCurves, riserCurves, pathCurves);
                                newRun1.EndsWithRiser = false;
                                topElevArr[landingInd + 1] = newRun1.TopElevation;
                                XYZ nextPos = pnt2;
                                double offset = 0;
                                if (i == 0 && j == stepsArrange[i].Length - 1) nextPos = pnt4;
                                else if (i == 0) offset = landings[0][j];
                                landingCor[landingInd] = pnt2;
                                landingInd++;
                                position[0] = nextPos.X - offset;
                                position[1] = nextPos.Y;
                                position[2] = nextPos.Z;
                            }
                        }
                        int tmpIndex = 0;
                        for (int i = 0; i < landings.Length; i++)
                        {
                            for (int j = 0; j < landings[i].Length; j++)
                            {
                                // Add a landing between the runs
                                CurveLoop landingLoop = new CurveLoop();
                                XYZ p1, p2, p3, p4;
                                if (i == 0)
                                {
                                    p1 = landingCor[tmpIndex];
                                    p2 = new XYZ(p1.X - landings[i][j], p1.Y, p1.Z);
                                    p3 = new XYZ(p1.X, p1.Y - runWidth, p1.Z);
                                    p4 = new XYZ(p1.X - landings[i][j], p1.Y - runWidth, p1.Z);
                                }
                                else
                                {
                                    p1 = landingCor[tmpIndex];
                                    p2 = new XYZ(p1.X, p1.Y - landings[i][j], p1.Z);
                                    p3 = new XYZ(p1.X - runWidth, p1.Y, p1.Z);
                                    p4 = new XYZ(p1.X - runWidth, p1.Y - landings[i][j], p1.Z);
                                }

                                XYZ rp1 = this.getProcessedPts(this.getProcessedPts(p1, scaleMatrix), rotMatrix);
                                XYZ rp2 = this.getProcessedPts(this.getProcessedPts(p2, scaleMatrix), rotMatrix);
                                XYZ rp3 = this.getProcessedPts(this.getProcessedPts(p3, scaleMatrix), rotMatrix);
                                XYZ rp4 = this.getProcessedPts(this.getProcessedPts(p4, scaleMatrix), rotMatrix);

                                Line curve_1 = Line.CreateBound(rp1, rp2);
                                Line curve_2 = Line.CreateBound(rp2, rp4);
                                Line curve_3 = Line.CreateBound(rp4, rp3);
                                Line curve_4 = Line.CreateBound(rp3, rp1);

                                landingLoop.Append(curve_1);
                                landingLoop.Append(curve_2);
                                landingLoop.Append(curve_3);
                                landingLoop.Append(curve_4);
                                StairsLanding newLanding = StairsLanding.CreateSketchedLanding(document, newStairsId, landingLoop, topElevArr[tmpIndex + 1]);
                                tmpIndex++;
                            }
                        }
                        stairsTrans.Commit();
                    }
                    // A failure preprocessor is to handle possible failures during the edit mode commitment process.
                    newStairsScope.Commit(new StairsFailurePreprocessor());
                }

                return newStairsId;
            }
            /// <summary>
            /// The method used to create a dogLegged type of stair.
            /// </summary>
            /// <param name="document">The revit document to which the stair will be added.</param>
            /// <returns>ElementId of the stair created.</returns>
            private ElementId CreateDogLeggedStairs(Document document)
            {
                ElementId newStairsId = null;
                using (StairsEditScope newStairsScope = new StairsEditScope(document, "New Stairs"))
                {
                    newStairsId = newStairsScope.Start(this.levelBottom.Id, this.levelTop.Id);
                    using (SubTransaction stairsTrans = new SubTransaction(document))
                    {
                        stairsTrans.Start();
                        double tread_depth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["tread"].ToString()));
                        // 'Y' coordinate in snaptrude is z coordinate in revit
                        // as revit draws in xy plane and snaptrude in xz plane.
                        double[] position = { this.SnaptrudePosition[0], this.SnaptrudePosition[2], this.SnaptrudePosition[1] };
                        int riserNum = int.Parse(this.Props["steps"].ToString());
                        position[2] = 0;
                        double runWidth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["width"].ToString()));
                        double[,] rotMatrix = new double[4, 4];
                        if (this.Mesh["rotationQuaternion"] == null)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                for (int j = 0; j < 4; j++)
                                {
                                    if (i == j) rotMatrix[i, j] = 1.0;
                                    else rotMatrix[i, j] = 0.0;
                                }
                            }
                        }
                        else
                        {
                            double[] quat = this.Mesh["rotationQuaternion"].Select(jv => (double)jv).ToArray();
                            Quaternion q = new Quaternion(quat[0], quat[2], quat[1], quat[3]);
                            XYZ pos = new XYZ(position[0], position[1], position[2]);
                            double[,] tmp = this.getRotMatrix(q, pos);
                            rotMatrix = tmp;
                        }

                        double[] scale = this.Mesh["scaling"].Select(jv => (double)jv).ToArray();
                        XYZ pos1 = new XYZ(position[0], position[1], position[2]);
                        double[,] scaleMatrix = this.getScaleMatrix(pos1);

                        int[][] stepsArrange = this.Props["stepsArrangement"].Select(jv => jv.Select(jv1 => jv1.ToString() == "" ? 0 : (int)jv1).ToArray()).ToArray();

                        int landingInd = 0;
                        double[] topElevArr = new double[stepsArrange.Length + 2];
                        topElevArr[0] = 0.0;
                        XYZ[] landingCor = new XYZ[stepsArrange.Length + 1];
                        bool singleLanding = false;
                        for (int i = 0; i < stepsArrange.Length; i++)
                        {
                            for (int j = 0; j < stepsArrange[i].Length; j++)
                            {
                                if (stepsArrange[i][j] == 0)
                                {
                                    singleLanding = true;
                                    position[0] = position[0];
                                    position[1] = position[1] - 0.5905511811023623; // approx 150mm
                                    position[2] = position[2];
                                    continue;
                                }

                                var stepsNo = stepsArrange[i][j];
                                IList<Curve> bdryCurves = new List<Curve>();
                                IList<Curve> riserCurves = new List<Curve>();
                                IList<Curve> pathCurves = new List<Curve>();
                                XYZ pnt1, pnt2, pnt3, pnt4;
                                if (i == 0)
                                {
                                    pnt3 = new XYZ(position[0], position[1], position[2]);
                                    pnt4 = new XYZ(position[0] - stepsNo * tread_depth, position[1], position[2]);
                                    pnt1 = new XYZ(position[0], position[1] - runWidth, position[2]);
                                    pnt2 = new XYZ(position[0] - stepsNo * tread_depth, position[1] - runWidth, position[2]);
                                }
                                else if (i == 1)
                                {
                                    pnt1 = new XYZ(position[0], position[1], position[2]);
                                    pnt2 = new XYZ(position[0], position[1] - stepsNo * tread_depth, position[2]);
                                    pnt3 = new XYZ(position[0] - runWidth, position[1], position[2]);
                                    pnt4 = new XYZ(position[0] - runWidth, position[1] - stepsNo * tread_depth, position[2]);
                                }
                                else
                                {
                                    pnt1 = new XYZ(position[0], position[1], position[2]);
                                    pnt2 = new XYZ(position[0] + stepsNo * tread_depth, position[1], position[2]);
                                    pnt3 = new XYZ(position[0], position[1] - runWidth, position[2]);
                                    pnt4 = new XYZ(position[0] + stepsNo * tread_depth, position[1] - runWidth, position[2]);
                                }

                                XYZ rpnt1 = this.getProcessedPts(this.getProcessedPts(pnt1, scaleMatrix), rotMatrix);
                                XYZ rpnt2 = this.getProcessedPts(this.getProcessedPts(pnt2, scaleMatrix), rotMatrix);
                                XYZ rpnt3 = this.getProcessedPts(this.getProcessedPts(pnt3, scaleMatrix), rotMatrix);
                                XYZ rpnt4 = this.getProcessedPts(this.getProcessedPts(pnt4, scaleMatrix), rotMatrix);

                                // boundaries
                                bdryCurves.Add(Line.CreateBound(rpnt1, rpnt2));
                                bdryCurves.Add(Line.CreateBound(rpnt3, rpnt4));

                                // riser curves
                                double interval;
                                if (i == 0 || i == 2)
                                {
                                    interval = (pnt2.X - pnt1.X) / stepsNo;
                                }
                                else
                                {
                                    interval = (pnt2.Y - pnt1.Y) / stepsNo;
                                }

                                for (int ii = 0; ii <= stepsNo; ii++)
                                {
                                    XYZ end0;
                                    XYZ end1;
                                    if (i == 0 || i == 2)
                                    {
                                        end0 = new XYZ(pnt1.X + ii * interval, pnt1.Y, pnt1.Z);
                                        end1 = new XYZ(pnt1.X + ii * interval, pnt3.Y, pnt3.Z);
                                    }
                                    else
                                    {
                                        end0 = new XYZ(pnt1.X, pnt1.Y + ii * interval, pnt1.Z);
                                        end1 = new XYZ(pnt3.X, pnt1.Y + ii * interval, pnt3.Z);
                                    }

                                    XYZ rend0 = this.getProcessedPts(this.getProcessedPts(end0, scaleMatrix), rotMatrix);
                                    XYZ rend1 = this.getProcessedPts(this.getProcessedPts(end1, scaleMatrix), rotMatrix);
                                    riserCurves.Add(Line.CreateBound(rend0, rend1));
                                }

                                //stairs path curves
                                XYZ pathEnd0 = (pnt1 + pnt3) / 2.0;
                                XYZ pathEnd1 = (pnt2 + pnt4) / 2.0;
                                XYZ rpathEnd0 = this.getProcessedPts(this.getProcessedPts(pathEnd0, scaleMatrix), rotMatrix);
                                XYZ rpathEnd1 = this.getProcessedPts(this.getProcessedPts(pathEnd1, scaleMatrix), rotMatrix);

                                pathCurves.Add(Line.CreateBound(rpathEnd0, rpathEnd1));

                                StairsRun newRun1 = StairsRun.CreateSketchedRun(document, newStairsId, topElevArr[landingInd], bdryCurves, riserCurves, pathCurves);
                                newRun1.EndsWithRiser = false;
                                topElevArr[landingInd + 1] = newRun1.TopElevation;
                                XYZ nextPos = pnt2;
                                landingCor[landingInd] = pnt2;
                                landingInd++;

                                position[0] = nextPos.X;
                                position[1] = nextPos.Y;
                                position[2] = nextPos.Z;
                            }
                        }

                        for (int i = 0; i < landingInd - 1; i++)
                        {
                            // Add a landing between the runs
                            CurveLoop landingLoop = new CurveLoop();
                            XYZ p1, p2, p3, p4;

                            if (singleLanding == true)
                            {
                                XYZ tmp = landingCor[i];
                                p1 = new XYZ(tmp.X, tmp.Y + runWidth, tmp.Z);
                                p2 = new XYZ(p1.X - runWidth, p1.Y, p1.Z);
                                p3 = new XYZ(p1.X, p1.Y - (2 * runWidth + 0.5905511811023623), p1.Z);
                                p4 = new XYZ(p1.X - runWidth, p1.Y - (2 * runWidth + 0.5905511811023623), p1.Z);
                            }
                            else if (i == 0 && singleLanding == false)
                            {
                                p1 = landingCor[i];
                                p2 = new XYZ(p1.X - runWidth, p1.Y, p1.Z);
                                p3 = new XYZ(p1.X, p1.Y + runWidth, p1.Z);
                                p4 = new XYZ(p1.X - runWidth, p1.Y + runWidth, p1.Z);
                            }
                            else if (i == 1 && singleLanding == false)
                            {
                                p1 = landingCor[i];
                                p2 = new XYZ(p1.X, p1.Y - runWidth, p1.Z);
                                p3 = new XYZ(p1.X - runWidth, p1.Y, p1.Z);
                                p4 = new XYZ(p1.X - runWidth, p1.Y - runWidth, p1.Z);
                            }
                            else break;


                            XYZ rp1 = this.getProcessedPts(this.getProcessedPts(p1, scaleMatrix), rotMatrix);
                            XYZ rp2 = this.getProcessedPts(this.getProcessedPts(p2, scaleMatrix), rotMatrix);
                            XYZ rp3 = this.getProcessedPts(this.getProcessedPts(p3, scaleMatrix), rotMatrix);
                            XYZ rp4 = this.getProcessedPts(this.getProcessedPts(p4, scaleMatrix), rotMatrix);

                            Line curve_1 = Line.CreateBound(rp1, rp2);
                            Line curve_2 = Line.CreateBound(rp2, rp4);
                            Line curve_3 = Line.CreateBound(rp4, rp3);
                            Line curve_4 = Line.CreateBound(rp3, rp1);

                            landingLoop.Append(curve_1);
                            landingLoop.Append(curve_2);
                            landingLoop.Append(curve_3);
                            landingLoop.Append(curve_4);
                            StairsLanding newLanding = StairsLanding.CreateSketchedLanding(document, newStairsId, landingLoop, topElevArr[i + 1]);
                        }

                        stairsTrans.Commit();
                    }
                    // A failure preprocessor is to handle possible failures during the edit mode commitment process.
                    newStairsScope.Commit(new StairsFailurePreprocessor());
                }

                return newStairsId;
            }
            /// <summary>
            /// The method to create a square type of stair.
            /// </summary>
            /// <param name="document">The revit document to which the stair will be added.</param>
            /// <returns>ElementId of the stair created.</returns>
            private ElementId CreateSquareStairs(Document document)
            {
                ElementId newStairsId = null;
                using (StairsEditScope newStairsScope = new StairsEditScope(document, "New Stairs"))
                {
                    newStairsId = newStairsScope.Start(this.levelBottom.Id, this.levelTop.Id);
                    using (SubTransaction stairsTrans = new SubTransaction(document))
                    {
                        stairsTrans.Start();
                        double tread_depth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["tread"].ToString()));
                        // 'Y' coordinate in snaptrude is z coordinate in revit
                        // as revit draws in xy plane and snaptrude in xz plane.
                        double[] position = { this.SnaptrudePosition[0], this.SnaptrudePosition[2], this.SnaptrudePosition[1] };
                        int riserNum = int.Parse(this.Props["steps"].ToString());
                        position[2] = 0;
                        double runWidth = UnitsAdapter.convertToRevit(Convert.ToDouble(this.Props["width"].ToString()));
                        double[,] rotMatrix = new double[4, 4];
                        if (this.Mesh["rotationQuaternion"] == null)
                        {
                            for (int i = 0; i < 4; i++)
                            {
                                for (int j = 0; j < 4; j++)
                                {
                                    if (i == j) rotMatrix[i, j] = 1.0;
                                    else rotMatrix[i, j] = 0.0;
                                }
                            }
                        }
                        else
                        {
                            double[] quat = this.Mesh["rotationQuaternion"].Select(jv => (double)jv).ToArray();
                            Quaternion q = new Quaternion(quat[0], quat[2], quat[1], quat[3]);
                            XYZ pos = new XYZ(position[0], position[1], position[2]);
                            double[,] tmp = this.getRotMatrix(q, pos);
                            rotMatrix = tmp;
                        }

                        double[] scale = this.Mesh["scaling"].Select(jv => (double)jv).ToArray();
                        XYZ pos1 = new XYZ(position[0], position[1], position[2]);
                        double[,] scaleMatrix = this.getScaleMatrix(pos1);

                        int[][] stepsArrange = this.Props["stepsArrangement"].Select(jv => jv.Select(jv1 => (int)jv1).ToArray()).ToArray();
                        //double[] landings = this.Props["landings"].First.Select(jv => (double)jv * 10 / 12).ToArray();

                        int landingInd = 0;
                        double[] topElevArr = new double[stepsArrange.Length + 2];
                        topElevArr[0] = 0.0;
                        XYZ[] landingCor = new XYZ[stepsArrange.Length + 1];
                        for (int i = 0; i < stepsArrange.Length; i++)
                        {
                            var stepsNo = stepsArrange[i][0];
                            IList<Curve> bdryCurves = new List<Curve>();
                            IList<Curve> riserCurves = new List<Curve>();
                            IList<Curve> pathCurves = new List<Curve>();
                            XYZ pnt1, pnt2, pnt3, pnt4;
                            if (i == 0)
                            {
                                pnt3 = new XYZ(position[0], position[1], position[2]);
                                pnt4 = new XYZ(position[0] - stepsNo * tread_depth, position[1], position[2]);
                                pnt1 = new XYZ(position[0], position[1] - runWidth, position[2]);
                                pnt2 = new XYZ(position[0] - stepsNo * tread_depth, position[1] - runWidth, position[2]);
                            }
                            else if (i == 1)
                            {
                                pnt1 = new XYZ(position[0], position[1], position[2]);
                                pnt2 = new XYZ(position[0], position[1] - stepsNo * tread_depth, position[2]);
                                pnt3 = new XYZ(position[0] - runWidth, position[1], position[2]);
                                pnt4 = new XYZ(position[0] - runWidth, position[1] - stepsNo * tread_depth, position[2]);
                            }
                            else if (i == 2)
                            {
                                pnt1 = new XYZ(position[0], position[1], position[2]);
                                pnt2 = new XYZ(position[0] + stepsNo * tread_depth, position[1], position[2]);
                                pnt3 = new XYZ(position[0], position[1] - runWidth, position[2]);
                                pnt4 = new XYZ(position[0] + stepsNo * tread_depth, position[1] - runWidth, position[2]);
                            }
                            else
                            {
                                pnt1 = new XYZ(position[0], position[1], position[2]);
                                pnt2 = new XYZ(position[0], position[1] + stepsNo * tread_depth, position[2]);
                                pnt3 = new XYZ(position[0] + runWidth, position[1], position[2]);
                                pnt4 = new XYZ(position[0] + runWidth, position[1] + stepsNo * tread_depth, position[2]);
                            }

                            XYZ rpnt1 = this.getProcessedPts(this.getProcessedPts(pnt1, scaleMatrix), rotMatrix);
                            XYZ rpnt2 = this.getProcessedPts(this.getProcessedPts(pnt2, scaleMatrix), rotMatrix);
                            XYZ rpnt3 = this.getProcessedPts(this.getProcessedPts(pnt3, scaleMatrix), rotMatrix);
                            XYZ rpnt4 = this.getProcessedPts(this.getProcessedPts(pnt4, scaleMatrix), rotMatrix);

                            // boundaries
                            bdryCurves.Add(Line.CreateBound(rpnt1, rpnt2));
                            bdryCurves.Add(Line.CreateBound(rpnt3, rpnt4));

                            // riser curves
                            double interval;
                            if (i == 0 || i == 2)
                            {
                                interval = (pnt2.X - pnt1.X) / stepsNo;
                            }
                            else
                            {
                                interval = (pnt2.Y - pnt1.Y) / stepsNo;
                            }

                            for (int ii = 0; ii <= stepsNo; ii++)
                            {
                                XYZ end0;
                                XYZ end1;
                                if (i == 0 || i == 2)
                                {
                                    end0 = new XYZ(pnt1.X + ii * interval, pnt1.Y, pnt1.Z);
                                    end1 = new XYZ(pnt1.X + ii * interval, pnt3.Y, pnt3.Z);
                                }
                                else
                                {
                                    end0 = new XYZ(pnt1.X, pnt1.Y + ii * interval, pnt1.Z);
                                    end1 = new XYZ(pnt3.X, pnt1.Y + ii * interval, pnt3.Z);
                                }

                                XYZ rend0 = this.getProcessedPts(this.getProcessedPts(end0, scaleMatrix), rotMatrix);
                                XYZ rend1 = this.getProcessedPts(this.getProcessedPts(end1, scaleMatrix), rotMatrix);
                                riserCurves.Add(Line.CreateBound(rend0, rend1));
                            }

                            //stairs path curves
                            XYZ pathEnd0 = (pnt1 + pnt3) / 2.0;
                            XYZ pathEnd1 = (pnt2 + pnt4) / 2.0;
                            XYZ rpathEnd0 = this.getProcessedPts(this.getProcessedPts(pathEnd0, scaleMatrix), rotMatrix);
                            XYZ rpathEnd1 = this.getProcessedPts(this.getProcessedPts(pathEnd1, scaleMatrix), rotMatrix);

                            pathCurves.Add(Line.CreateBound(rpathEnd0, rpathEnd1));

                            StairsRun newRun1 = StairsRun.CreateSketchedRun(document, newStairsId, topElevArr[landingInd], bdryCurves, riserCurves, pathCurves);
                            newRun1.EndsWithRiser = false;
                            topElevArr[landingInd + 1] = newRun1.TopElevation;
                            XYZ nextPos = pnt2;
                            landingCor[landingInd] = pnt2;
                            landingInd++;
                            position[0] = nextPos.X;
                            position[1] = nextPos.Y;
                            position[2] = nextPos.Z;
                        }

                        for (int i = 0; i < stepsArrange.Length; i++)
                        {
                            // Add a landing between the runs
                            CurveLoop landingLoop = new CurveLoop();
                            XYZ p1, p2, p3, p4;

                            if (i == 0)
                            {
                                p1 = landingCor[i];
                                p2 = new XYZ(p1.X - runWidth, p1.Y, p1.Z);
                                p3 = new XYZ(p1.X, p1.Y + runWidth, p1.Z);
                                p4 = new XYZ(p1.X - runWidth, p1.Y + runWidth, p1.Z);
                            }
                            else if (i == 1)
                            {
                                p1 = landingCor[i];
                                p2 = new XYZ(p1.X, p1.Y - runWidth, p1.Z);
                                p3 = new XYZ(p1.X - runWidth, p1.Y, p1.Z);
                                p4 = new XYZ(p1.X - runWidth, p1.Y - runWidth, p1.Z);
                            }
                            else if (i == 2)
                            {
                                p1 = landingCor[i];
                                p2 = new XYZ(p1.X + runWidth, p1.Y, p1.Z);
                                p3 = new XYZ(p1.X, p1.Y - runWidth, p1.Z);
                                p4 = new XYZ(p1.X + runWidth, p1.Y - runWidth, p1.Z);
                            }
                            else
                            {
                                p1 = landingCor[i];
                                p2 = new XYZ(p1.X, p1.Y + runWidth, p1.Z);
                                p3 = new XYZ(p1.X + runWidth, p1.Y, p1.Z);
                                p4 = new XYZ(p1.X + runWidth, p1.Y + runWidth, p1.Z);
                            }

                            XYZ rp1 = this.getProcessedPts(this.getProcessedPts(p1, scaleMatrix), rotMatrix);
                            XYZ rp2 = this.getProcessedPts(this.getProcessedPts(p2, scaleMatrix), rotMatrix);
                            XYZ rp3 = this.getProcessedPts(this.getProcessedPts(p3, scaleMatrix), rotMatrix);
                            XYZ rp4 = this.getProcessedPts(this.getProcessedPts(p4, scaleMatrix), rotMatrix);

                            Line curve_1 = Line.CreateBound(rp1, rp2);
                            Line curve_2 = Line.CreateBound(rp2, rp4);
                            Line curve_3 = Line.CreateBound(rp4, rp3);
                            Line curve_4 = Line.CreateBound(rp3, rp1);

                            landingLoop.Append(curve_1);
                            landingLoop.Append(curve_2);
                            landingLoop.Append(curve_3);
                            landingLoop.Append(curve_4);
                            StairsLanding newLanding = StairsLanding.CreateSketchedLanding(document, newStairsId, landingLoop, topElevArr[i + 1]);
                        }
                        stairsTrans.Commit();
                    }
                    // A failure preprocessor is to handle possible failures during the edit mode commitment process.
                    newStairsScope.Commit(new StairsFailurePreprocessor());
                }

                return newStairsId;
            }
        }
        /// <summary>
        /// FailurePreprocessor class required for StairsEditScope
        /// </summary>
        class StairsFailurePreprocessor : IFailuresPreprocessor
        {
            public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
            {
                // Use default failure processing
                return FailureProcessingResult.Continue;
            }
        }

        // MATERIAL IMPLMENTATION WRAPPER
        public class ST_Material
        {
            private const string urlDomain = "http://www.snaptru.de/";
            private const string redundantUrl1 = "http://127.0.0.1:8000";
            private const string redundantUrl2 = "http://127.0.0.1:8000/D:Snaptrudestagingsnaptrudestaging..";
            private const string fileLoc = @"D:\RevitExport\Textures\";
            public static bool CheckIfMultiMaterial(string materialId, JToken materialJSON)
            {
                if (materialJSON[materialId] != null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            public static JObject CreateMaterialsJSON(JToken materialParam)
            {
                JObject materials = new JObject();

                for (int i = 0; i < materialParam.Count(); i++)
                {
                    bool checkFlag = CheckIfMaterialExists(materialParam, materialParam[i]["name"].ToString(), i);
                    if (!checkFlag)
                    {
                        materials.Add(materialParam[i]["name"].ToString(), materialParam[i]);
                    }
                }
                return materials;
            }
            public static void LoadFamilyMaterial(Autodesk.Revit.ApplicationServices.Application app, Document doc, JToken material)
            {
                string fileTarget = "";
                Asset asset = app.GetAssets(AssetType.Appearance)[0];
                if (material["diffuse"] != null)
                {
                    if (material["diffuseTexture"] != null)
                    {
                        if (material["diffuseTexture"]["url"] != null)
                        {
                            // Download Texture Image
                            //fileTarget = DownloadTextures(material, material["diffuseTexture"]["url"].ToString());
                            fileTarget = "/texture.png";
                            // Create material
                            // GenerateMaterial(doc, material, asset, fileTarget, "diffuse");
                        }
                    }
                    else
                    {
                        // Create material without texture
                        // GenerateMaterial(doc, material, asset, fileTarget, "diffuse");
                    }
                }
                else if (material["albedo"] != null)
                {
                    if (material["albedoTexture"] != null)
                    {
                        if (material["albedoTexture"]["url"] != null)
                        {
                            // Download Texture Image
                            //fileTarget = DownloadTextures(material, material["albedoTexture"]["url"].ToString());
                            fileTarget = "/texture.png";
                            // Create material
                            // GenerateMaterial(doc, material, asset, fileTarget, "albedo");
                        }
                    }
                    else
                    {
                        // Create material without texture
                        // GenerateMaterial(doc, material, asset, fileTarget, "albedo");
                    }
                }
            }
            public static void LoadMaterials(Autodesk.Revit.ApplicationServices.Application app, Document doc, JToken mats)
            {
                for (int i = 0; i < mats.Count(); i++)
                {
                    string fileTarget = "";
                    Asset asset = app.GetAssets(AssetType.Appearance)[0];
                    var material = mats[i];
                    string matName = material["name"].ToString();
                    bool materialCheckFlag = false;
                    bool nameCheckFlag;


                    // Check if material already exists
                    materialCheckFlag = CheckIfMaterialExists(mats, matName, i);
                    nameCheckFlag = NamingUtils.IsValidName(matName);

                    if (materialCheckFlag || !nameCheckFlag)
                    {
                        continue;
                    }
                    else
                    {
                        if (material["diffuse"] != null)
                        {
                            if (material["diffuseTexture"] != null)
                            {
                                if (material["diffuseTexture"]["url"] != null)
                                {
                                    // Download Texture Image
                                    //fileTarget = DownloadTextures(material, material["diffuseTexture"]["url"].ToString());
                                    fileTarget = "https://app.snaptrude.com/media/media/materials/RAL_2005.jpg";
                                    // Create material
                                    // GenerateMaterial(doc, material, asset, fileTarget, "diffuse");
                                }
                            }
                            else
                            {
                                // Create material without texture
                                // GenerateMaterial(doc, material, asset, fileTarget, "diffuse");
                            }
                        }
                        else if (material["albedo"] != null)
                        {
                            if (material["albedoTexture"] != null)
                            {
                                if (material["albedoTexture"]["url"] != null)
                                {
                                    // Download Texture Image
                                    //fileTarget = DownloadTextures(material, material["albedoTexture"]["url"].ToString());
                                    fileTarget = "https://app.snaptrude.com/media/media/materials/RAL_2005.jpg";
                                    // Create material
                                    // GenerateMaterial(doc, material, asset, fileTarget, "albedo");
                                }
                            }
                            else
                            {
                                // Create material without texture
                                // GenerateMaterial(doc, material, asset, fileTarget, "albedo");
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }
            public static void LoadMultiMaterials(Autodesk.Revit.ApplicationServices.Application app, Document doc, JToken multiMats, JObject matsJSON)
            {
                for (int i = 0; i < multiMats.Count(); i++)
                {
                    string fileTarget = "";
                    Asset asset = app.GetAssets(AssetType.Appearance)[0];
                    var material = multiMats[i];
                    string multiMatName = material["name"].ToString();
                    bool materialCheckFlag = false;
                    bool nameCheckFlag;

                    // Check if material already exists
                    materialCheckFlag = CheckIfMaterialExists(multiMats, multiMatName, i);
                    nameCheckFlag = NamingUtils.IsValidName(multiMatName);

                    if (materialCheckFlag || !nameCheckFlag)
                    {
                        continue;
                    }
                    else
                    {
                        string matName = material["materials"].Last.ToString();
                        material = matsJSON[matName];
                        material["name"] = multiMatName;
                        if (material["diffuse"] != null)
                        {
                            if (material["diffuseTexture"] != null)
                            {
                                if (material["diffuseTexture"]["url"] != null)
                                {
                                    // Download Texture Image
                                    //fileTarget = DownloadTextures(material, material["diffuseTexture"]["url"].ToString());
                                    fileTarget = "https://app.snaptrude.com/media/media/materials/RAL_2005.jpg";
                                    // Create material
                                    // GenerateMaterial(doc, material, asset, fileTarget, "diffuse");
                                }
                            }
                            else
                            {
                                // Create material without texture
                                // GenerateMaterial(doc, material, asset, fileTarget, "diffuse");
                            }
                        }
                        else if (material["albedo"] != null)
                        {
                            if (material["albedoTexture"] != null)
                            {
                                if (material["albedoTexture"]["url"] != null)
                                {
                                    // Download Texture Image
                                    //fileTarget = DownloadTextures(material, material["albedoTexture"]["url"].ToString());
                                    fileTarget = "https://app.snaptrude.com/media/media/materials/RAL_2005.jpg";
                                    // Create material
                                    // GenerateMaterial(doc, material, asset, fileTarget, "albedo");
                                }
                            }
                            else
                            {
                                // Create material without texture
                                // GenerateMaterial(doc, material, asset, fileTarget, "albedo");
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }
                }
            }
            private static bool CheckIfMaterialExists(JToken mats, string name, int matIdx)
            {
                for (int i = 0; i < matIdx; i++)
                {
                    string checkName = mats[i]["name"].ToString();
                    if (checkName == name)
                    {
                        return true;
                    }
                }
                return false;
            }
            private static string DownloadTextures(JToken material, string matURL)
            {
                string url_ = matURL;
                string imageName = url_.Split('/').Last();
                string url = null;
                string targetLocation = fileLoc + imageName;

                if (!(File.Exists(targetLocation)))
                {
                    // Temporary Logic
                    if (url_.StartsWith(redundantUrl1))
                    {
                        if (url_.StartsWith(redundantUrl2))
                        {
                            url_ = url_.Replace(redundantUrl2, "/");
                            url = urlDomain + url_;
                        }
                        else
                        {
                            url_ = url_.Replace(redundantUrl1, "");
                            url = urlDomain + url_;
                        }
                    }
                    else
                    {
                        url = urlDomain + url_;
                    }

                    using (WebClient client = new WebClient())
                    {
                        client.DownloadFile(new Uri(url), targetLocation);
                    }

                    return targetLocation;
                }
                else
                {
                    return targetLocation;
                }
            }
        }

        private static void LogProgress(int processed, int total)
        {
            if (processed % 10 == 0)
            {
                System.Console.WriteLine(
                  "!ACESAPI:acesHttpOperation({0},\"\",\"\",{1},null)",
                  "onProgress",
                  "{ \"current-progress\": " + processed + ", \"total\": " + total + " }"
                );
            }
        }

        private static Random random = new Random();
        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private static bool IsThrowAway(string meshType)
        {
            return meshType.Contains("throwAway");
        }
        private static bool IsThrowAway(JToken data)
        {
            return IsThrowAway(data["meshes"][0]["type"].ToString());
        }

        private static bool ShouldImport(JToken data)
        {
            if (data.First["dsProps"]["revitMetaData"].IsNullOrEmpty()) return true;

            if (data.First["dsProps"]["revitMetaData"]["elementId"].IsNullOrEmpty())
            {
                if (data.First["dsProps"]["revitMetaData"]["isStackedWall"].IsNullOrEmpty()) return true;
            }

            if (data.First["dsProps"]["revitMetaData"]["isModified"].IsNullOrEmpty()) return false;

            return (bool)data.First["dsProps"]["revitMetaData"]["isModified"];
        }

        private static bool IsStackedWall(JToken data)
        {
            if (data.First["dsProps"]["revitMetaData"]["isStackedWall"].IsNullOrEmpty()) return false;

            return (bool)data.First["dsProps"]["revitMetaData"]["isStackedWall"];
        }
        private static bool IsParentStackedWall(JToken data)
        {
            if (IsStackedWall(data))
            {
                return data.First["dsProps"]["revitMetaData"]["stackedWallParentId"].IsNullOrEmpty();
            }

            return false;
        }

        private void ShowSuccessDialogue()
        {
            TaskDialog mainDialog = new TaskDialog("Snaptrude Import Status");
            mainDialog.MainInstruction = "Snaptrude Import Status";
            mainDialog.MainContent = "Finished importing your Snaptrude file!";

            mainDialog.CommonButtons = TaskDialogCommonButtons.Close;
            mainDialog.DefaultButton = TaskDialogResult.Close;

            TaskDialogResult tResult = mainDialog.Show();
        }
        private void ShowTrudeFileNotSelectedDialogue()
        {
            TaskDialog mainDialog = new TaskDialog("Snaptrude Import Status");
            mainDialog.MainInstruction = "Snaptrude Import Status";
            mainDialog.MainContent = "File not selected, import cancelled.";

            mainDialog.CommonButtons = TaskDialogCommonButtons.Close;
            mainDialog.DefaultButton = TaskDialogResult.Close;

            TaskDialogResult tResult = mainDialog.Show();
        }

        private void ShowReadOnlyDialogue()
        {
            TaskDialog mainDialog = new TaskDialog("Snaptrude Import Status");
            mainDialog.MainInstruction = "Snaptrude Import Status";
            mainDialog.MainContent = "Revit file is in read-only mode.";

            mainDialog.CommonButtons = TaskDialogCommonButtons.Close;
            mainDialog.DefaultButton = TaskDialogResult.Close;

            TaskDialogResult tResult = mainDialog.Show();
        }

    }
}