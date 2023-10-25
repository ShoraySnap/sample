using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TrudeImporter
{
    public class TrudeFloor : TrudeModel
    {
        private List<XYZ> faceVertices = new List<XYZ>();
        FloorType existingFloorType = null;
        private float thickness;
        private TrudeLayer[] Layers;
        public static FloorTypeStore TypeStore = new FloorTypeStore();
        private Floor floor { get; set; }
        private XYZ centerPosition;
        private string baseType = null;

        /// <summary>
        /// Imports floors into revit from snaptrude json data
        /// </summary>
        /// <param name="floor"></param>
        /// <param name="levelId"></param>
        /// <param name="forForge"></param>
        public TrudeFloor(FloorProperties floor, ElementId levelId, bool forForge = false)
        {
            thickness = floor.Thickness;
            baseType = floor.BaseType;
            centerPosition = floor.CenterPosition;
            // To fix height offset issue, this can fixed from snaptude side by sending top face vertices instead but that might or might not introduce further issues
            foreach (var v in floor.FaceVertices)
            {
                faceVertices.Add(v + new XYZ(0, 0, thickness));
            }

            // get existing floor id from revit meta data if already exists else set it to null
            if (floor.ExistingElementId != null)
            {
                Floor existingFloor = GlobalVariables.Document.GetElement(new ElementId((int)floor.ExistingElementId)) as Floor;
                existingFloorType = existingFloor.FloorType;
            }
            var _layers = new List<TrudeLayer>();
            //you can improve this section 
            // --------------------------------------------
            if (floor.Layers != null)
            {
                foreach (var layer in floor.Layers)
                {
                    _layers.Add(
                      new TrudeLayer(
                        floor.BaseType, 
                        layer.Name, 
                        layer.ThicknessInMm, 
                        layer.IsCore
                      )
                    );
                }
            }
            Layers = _layers.ToArray();
            setCoreLayerIfNotExist(Math.Abs(thickness));


            // TODO : Fix on Snaptrude end, when floor thickness is changed, its layers's thickness should be adjusted
            // It is not being done now, so here the core layer's thickness is increased by appropriate length
            double sumOfLayersThickness = 0;
            Array.ForEach(Layers, (l) => sumOfLayersThickness += l.ThicknessInMm);

            if(UnitsAdapter.FeetToMM(thickness) != sumOfLayersThickness)
            {
                TrudeLayer coreLayer = Layers.FirstOrDefault(layer => layer.IsCore);
                coreLayer.ThicknessInMm += UnitsAdapter.FeetToMM(thickness) - sumOfLayersThickness;
            }

            // --------------------------------------------
            CreateFloor(levelId, int.Parse(GlobalVariables.RvtApp.VersionNumber) >= 2023);
            CreateHoles(floor.Holes);
        }

        private void setCoreLayerIfNotExist(double fallbackThickness)
        {
            if (Layers.Length == 0)
            {
                Layers = new TrudeLayer[] { new TrudeLayer("Floor", "screed" + Utils.RandomString(4), UnitsAdapter.FeetToMM(fallbackThickness), true) };

                return;
            }

            TrudeLayer coreLayer = Layers.FirstOrDefault(layer => layer.IsCore);

            if (coreLayer != null)
            {
                return;
            }

            foreach (TrudeLayer layer in Layers)
            {
                if (layer.Name.ToLower() == "screed")
                {
                    layer.IsCore = true;
                    return;
                }
            }

            int coreIndex = Layers.Count() / 2;
            Layers[coreIndex].IsCore = true;
        }

        private void CreateFloor(ElementId levelId, bool depricated = false)
        {
            CurveArray profile = getProfile(faceVertices);
            FloorType floorType = existingFloorType;
            
            var Doc = GlobalVariables.Document;
            if (floorType is null)
            {
                FilteredElementCollector collector = new FilteredElementCollector(Doc).OfClass(typeof(FloorType));
                FloorType defaultFloorType = collector.Where(type => ((FloorType)type).FamilyName == "Floor").First() as FloorType;
                floorType = defaultFloorType;
            }
            
            try
            {
                var newFloorType = TypeStore.GetType(Layers, Doc, floorType);
                floor = Doc.Create.NewFloor(profile, newFloorType, Doc.GetElement(levelId) as Level, false);
            }
            catch
            {
                floor = Doc.Create.NewFloor(profile, floorType, Doc.GetElement(levelId) as Level, false);
            }

            // Rotate and move the slab
            //rotate();

            //bool result = floor.Location.Move(centerPosition);

            //if (!result) throw new Exception("Move floor location failed.");

            //this.setType(floorType);

            Level level = Doc.GetElement(levelId) as Level;
            //setHeight(level);
            Doc.Regenerate();
        }

        private void CreateHoles(List<List<XYZ>> holes)
        {
            foreach (var hole in holes)
            {
                var holeProfile = getProfile(hole);
                try
                {
                    GlobalVariables.Document.Create.NewOpening(floor, holeProfile, true);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Could not create hole with error: ", e);
                }
            }
        }

        private CurveArray getProfile(List<XYZ> vertices)
        {
            CurveArray curves = new CurveArray();

            for (int i = 0; i < vertices.Count(); i++)
            {
                int currentIndex = i.Mod(vertices.Count());
                int nextIndex = (i + 1).Mod(vertices.Count());

                XYZ pt1 = vertices[currentIndex];
                XYZ pt2 = vertices[nextIndex];
                bool samePoint = false;

                while (pt1.DistanceTo(pt2) <= GlobalVariables.RvtApp.ShortCurveTolerance)
                {
                    i++;
                    if (i > vertices.Count() + 3) break;
                    else if (pt1.X == pt2.X && pt1.Y == pt2.Y && pt1.Z == pt2.Z)
                    {
                        samePoint = true;
                        break;
                    }

                    nextIndex = (i + 1).Mod(vertices.Count());
                    pt2 = vertices[nextIndex];
                }
                if (samePoint) break;
                curves.Append(Line.CreateBound(pt1, pt2));
            }
            return curves;
        }

        //private void rotate()
        //{
        //    Location position = this.floor.Location;
        //    if (roofData["meshes"][0] != null)
        //    {
        //        Line localXAxis = Line.CreateBound(new XYZ(0, 0, 0), XYZ.BasisX);
        //        Line localYAxis = Line.CreateBound(new XYZ(0, 0, 0), XYZ.BasisY);
        //        Line localZAxis = Line.CreateBound(new XYZ(0, 0, 0), XYZ.BasisZ);

        //        // Why am i rotating them in this particular order? I wish i knew.
        //        if (!TrudeRepository.HasRotationQuaternion(roofData))
        //        {
        //            position.Rotate(localZAxis, -double.Parse(roofData["meshes"][0]["rotation"][1].ToString()));
        //            position.Rotate(localYAxis, -double.Parse(roofData["meshes"][0]["rotation"][2].ToString()));
        //            position.Rotate(localXAxis, -double.Parse(roofData["meshes"][0]["rotation"][0].ToString()));
        //        }
        //        else
        //        {
        //            EulerAngles rotation = TrudeRepository.GetEulerAnglesFromRotationQuaternion(roofData);

        //            // Y and Z axis are swapped moving from snaptrude to revit.
        //            position.Rotate(localXAxis, -rotation.bank);
        //            position.Rotate(localZAxis, -rotation.heading);
        //            position.Rotate(localYAxis, -rotation.attitude);
        //        }
        //    }
        //}

        private void setHeight(Level level)
        {
            double bottomZ = faceVertices[0].Z/* * Scaling.Z*/;
            double slabHeightAboveLevel = centerPosition.Z + bottomZ - level.ProjectElevation + thickness;

            floor
                .get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM)
                .Set(slabHeightAboveLevel);
        }

        private List<TrudeLayer> createLayers(double fallbackThickness = 25)
        {

            // TODO: handle existing revit data

            List<TrudeLayer> stLayers = new List<TrudeLayer>();
            
            return null;
        }
    }
}