using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;

namespace TrudeImporter
{
    public class TrudeFloorOld : TrudeModel
    {
        public TrudeLayer[] Layers;

        public static FloorTypeStore TypeStore = new FloorTypeStore();

        public Floor floor { get; set; }

        private void setCoreLayerIfNotExist(double fallbackThickness)
        {
            if (this.Layers.Length == 0)
            {
                this.Layers = new TrudeLayer[] { new TrudeLayer("Floor", "screed" + Utils.RandomString(4), UnitsAdapter.FeetToMM(fallbackThickness), true) };

                return;
            }

            TrudeLayer coreLayer = this.Layers.FirstOrDefault(layer => layer.IsCore);

            if (coreLayer != null)
            {
                if (fallbackThickness != 0)
                {
                    coreLayer.ThicknessInMm = UnitsAdapter.FeetToMM(fallbackThickness, 1);
                }

                return;
            }

            foreach (TrudeLayer layer in this.Layers)
            {
                if (layer.Name.ToLower() == "screed")
                {
                    layer.IsCore = true;
                    return;
                }
            }

            int coreIndex = this.Layers.Count() / 2;
            Layers[coreIndex].IsCore = true;
        }

        private List<CurveLoop> getProfile(List<Point3D> vertices)
        {
            CurveLoop curveLoop = new CurveLoop();

            for (int i = 0; i < vertices.Count(); i++)
            {
                int currentIndex = i.Mod(vertices.Count());
                int nextIndex = (i + 1).Mod(vertices.Count());

                XYZ pt1 = vertices[currentIndex].ToXYZ();
                XYZ pt2 = vertices[nextIndex].ToXYZ();

                while (pt1.DistanceTo(pt2) <= GlobalVariables.RvtApp.ShortCurveTolerance)
                {
                    i++;
                    if (i > vertices.Count() + 3) break;
                    nextIndex = (i + 1).Mod(vertices.Count());
                    pt2 = vertices[nextIndex].ToXYZ();
                }

                curveLoop.Append(Line.CreateBound(pt1, pt2));
            }

            return new List<CurveLoop>() { curveLoop };
        }
        private CurveArray getDepricatedProfile(List<Point3D> vertices)
        {
            CurveArray curves = new CurveArray();

            for (int i = 0; i < vertices.Count(); i++)
            {
                int currentIndex = i.Mod(vertices.Count());
                int nextIndex = (i + 1).Mod(vertices.Count());

                XYZ pt1 = vertices[currentIndex].ToXYZ();
                XYZ pt2 = vertices[nextIndex].ToXYZ();

                while (pt1.DistanceTo(pt2) <= GlobalVariables.RvtApp.ShortCurveTolerance)
                {
                    i++;
                    if (i > vertices.Count() + 3) break;
                    nextIndex = (i + 1).Mod(vertices.Count());
                    pt2 = vertices[nextIndex].ToXYZ();
                }

                curves.Append(Line.CreateBound(pt1, pt2));
            }

            return curves;
        }

        private void rotate(JToken roofData)
        {
            Location position = this.floor.Location;
            if (roofData["meshes"][0] != null)
            {
                Line localXAxis = Line.CreateBound(new XYZ(0, 0, 0), XYZ.BasisX);
                Line localYAxis = Line.CreateBound(new XYZ(0, 0, 0), XYZ.BasisY);
                Line localZAxis = Line.CreateBound(new XYZ(0, 0, 0), XYZ.BasisZ);

                // Why am i rotating them in this particular order? I wish i knew.
                if (!TrudeRepository.HasRotationQuaternion(roofData))
                {
                    position.Rotate(localZAxis, -double.Parse(roofData["meshes"][0]["rotation"][1].ToString()));
                    position.Rotate(localYAxis, -double.Parse(roofData["meshes"][0]["rotation"][2].ToString()));
                    position.Rotate(localXAxis, -double.Parse(roofData["meshes"][0]["rotation"][0].ToString()));
                }
                else
                {
                    EulerAngles rotation = TrudeRepository.GetEulerAnglesFromRotationQuaternion(roofData);

                    // Y and Z axis are swapped moving from snaptrude to revit.
                    position.Rotate(localXAxis, -rotation.bank);
                    position.Rotate(localZAxis, -rotation.heading);
                    position.Rotate(localYAxis, -rotation.attitude);
                }
            }

        }

        private void setHeight(double thickness, double bottomZ, Level level)
        {
            double slabHeightAboveLevel = this.Position.Z + bottomZ - level.Elevation + thickness;

            this.floor
                .get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM)
                .Set(slabHeightAboveLevel);
        }

        private double getLeastZ(List<Point3D> vertices)
        {
            double zLeast = vertices[0].Z;

            foreach (Point3D v in vertices)
            {
                zLeast = v.X < zLeast ? v.X : zLeast;
            }

            return zLeast;
        }

        public TrudeFloorOld(JToken floorData, Document newDoc, ElementId levelId, FloorType floorType = null)
        {
            this.Name = floorData["meshes"].First["name"].ToString();
            this.Position = TrudeRepository.GetPosition(floorData);
            this.Scaling = TrudeRepository.GetScaling(floorData);
            this.levelNumber = TrudeRepository.GetLevelNumber(floorData);
            this.Layers = TrudeRepository.GetLayers(floorData);

            double thickness = UnitsAdapter.convertToRevit((double)floorData["thickness"]);
            this.setCoreLayerIfNotExist(Math.Abs(thickness));

            if (int.Parse(GlobalVariables.RvtApp.VersionNumber) >= 2023)
            {
                //Create(newDoc, roofData, thickness);
            }
            else
            {
                CreateDepricated(newDoc, floorData, thickness, levelId, floorType);
            }
        }

        private void CreateDepricated(Document newDoc, JToken roofData, double thickness, ElementId levelId, FloorType floorType = null)
        {
            JToken topVerticesNormal = roofData["topVerticesNormal"];
            JToken topVerticesUntitNormal = roofData["topVerticesUnitNormal"];

            List<Point3D> topVertices = TrudeRepository.ListToPoint3d(roofData["topVertices"])
                .Distinct()
                .ToList();

            Point3D topLowestPoint = topVertices.Aggregate(topVertices[0], (least, next) => least.Z < next.Z ? least : next);

            List<Point3D> bottomVertices = TrudeRepository.ListToPoint3d(roofData["bottomVertices"])
                .Distinct()
                .ToList();
            Point3D bottomLowestPoint = bottomVertices.Aggregate(bottomVertices[0], (least, next) => least.Z < next.Z ? least : next);

            for (int i = 0; i < bottomVertices.Count; i++)
            {
                Point3D newVertex = bottomVertices[i];
                newVertex.Z = bottomLowestPoint.Z;
                bottomVertices[i] = newVertex;
            }

            CurveArray profile = getDepricatedProfile(bottomVertices);
            //this.floor = newDoc.Create.NewFloor(profile, false);

            if (floorType is null)
            {
                FilteredElementCollector collector = new FilteredElementCollector(newDoc).OfClass(typeof(FloorType));
                FloorType defaultFloorType = collector.Where(type => ((FloorType)type).FamilyName == "Floor").First() as FloorType;
                floorType = defaultFloorType;
            }
            var newFloorType = TypeStore.GetType(this.Layers, GlobalVariables.Document, floorType);
            try
            {
                this.floor = newDoc.Create.NewFloor(profile, newFloorType, newDoc.GetElement(levelId) as Level, false);
            }
            catch
            {
                this.floor = newDoc.Create.NewFloor(profile, floorType, newDoc.GetElement(levelId) as Level, false);
            }

            // Rotate and move the slab

            this.rotate(roofData);

            bool result = this.floor.Location.Move(this.Position);

            if (!result) throw new Exception("Move floor location failed.");

            //this.setType(floorType);

            Level level = newDoc.GetElement(levelId) as Level;
            double bottomZ = bottomVertices[0].Z * Scaling.Z;
            this.setHeight(thickness, bottomZ, level);
        }

        public void ApplyPaintByMaterial(Document document, Floor floor, Autodesk.Revit.DB.Material material)
        {
            // Before acquiring the geometry, make sure the detail level is set to 'Fine'
            Options geoOptions = new Options();
            geoOptions.DetailLevel = ViewDetailLevel.Fine;

            // Obtain geometry for the given Wall element
            GeometryElement geoElem = floor.get_Geometry(geoOptions);

            // Find a face on the wall
            //Face wallFace = null;
            IEnumerator<GeometryObject> geoObjectItor = geoElem.GetEnumerator();
            List<Face> floorFaces = new List<Face>();

            while (geoObjectItor.MoveNext())
            {
                // need to find a solid first
                Solid theSolid = geoObjectItor.Current as Solid;
                if (null != theSolid)
                {
                    // Examine faces of the solid to find one with at least
                    // one region. Then take the geometric face of that region.
                    foreach (Face face in theSolid.Faces)
                    {
                        floorFaces.Add(face);
                    }
                }
            }

            foreach (Face face in floorFaces)
            {
                document.Paint(floor.Id, face, material.Id);
            }
        }

        //private void Create(Document newDoc, JToken roofData, double thickness)
        //{
        //    JToken topVerticesNormal = roofData["topVerticesNormal"];
        //    JToken topVerticesUntitNormal = roofData["topVerticesUnitNormal"];

        //    List<Point3D> topVertices = jtokenListToPoint3d(roofData["topVertices"])
        //        .Distinct()
        //        .ToList();

        //    Point3D topLowestPoint = topVertices.Aggregate(topVertices[0], (least, next) => least.Z < next.Z ? least : next);

        //    List<Point3D> bottomVertices = jtokenListToPoint3d(roofData["bottomVertices"])
        //        .Distinct()
        //        .ToList();
        //    Point3D bottomLowestPoint = bottomVertices.Aggregate(bottomVertices[0], (least, next) => least.Z < next.Z ? least : next);

        //    for (int i = 0; i < bottomVertices.Count; i++)
        //    {
        //        Point3D newVertex = bottomVertices[i];
        //        newVertex.Z = bottomLowestPoint.Z;
        //        bottomVertices[i] = newVertex;
        //    }

        //    List<CurveLoop> profile = getProfile(bottomVertices);

        //    var levelIdForfloor = Entry.LevelIdByNumber[this.levelNumber];
        //    this.floor = Floor.Create(newDoc, profile, ST_Floor.TypeStore.GetType(this.Layers, newDoc).Id, levelIdForfloor);

        //    this.floor.get_Parameter(BuiltInParameter.LEVEL_PARAM).Set(levelIdForfloor);

        //    // Rotate and move the slab

        //    this.rotate(roofData);

        //    bool result = this.floor.Location.Move(this.Position);

        //    if (!result) throw new Exception("Move floor location failed.");

        //    //this.setType();

        //    Level level = newDoc.GetElement(levelIdForfloor) as Level;
        //    double bottomZ = bottomVertices[0].Z * Scaling.Z;
        //    this.setHeight(thickness, bottomZ, level);
        //}

        private void setType(FloorType floorType = null)
        {
            try
            {
                this.floor.FloorType = TypeStore.GetType(Layers, GlobalVariables.Document);
            }
            catch (Exception e)
            {
                this.floor.FloorType = floorType;
            }
        }
    }
}
