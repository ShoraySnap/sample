using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;

namespace TrudeImporter
{
    public class TrudeRoof : TrudeModel
    {
        public TrudeLayer[] Layers;

        public static FloorTypeStore TypeStore = new FloorTypeStore();

        public Floor floor { get; set; }

        private void setThickness(double thickness)
        {
            // TODO: Remove after thickness of layers is fixed on snaptrude
            bool foundCore = false;
            for (int i = 0; i < this.Layers.Length; i++)
            {
                if (this.Layers[i].IsCore)
                {
                    if (thickness != 0) this.Layers[i].ThicknessInMm = UnitsAdapter.FeetToMM(thickness);
                    else thickness = UnitsAdapter.MMToFeet(this.Layers[i].ThicknessInMm);
                    foundCore = true;
                }
            }
            if (this.Layers.Length == 0)
            {
                this.Layers = new TrudeLayer[] { new TrudeLayer("Floor", "screed" + Utils.RandomString(4), UnitsAdapter.FeetToMM(thickness), true) };
            }
            if (!foundCore)
            {
                int i = this.Layers.Length / 2;
                this.Layers[i].IsCore = true;
                this.Layers[i].ThicknessInMm = UnitsAdapter.FeetToMM(thickness);
            }
        }

        private List<Point3D> jtokenListToPoint3d(JToken vertices)
        {
            List<Point3D> verticesList = new List<Point3D>();
            for (int i = 0; i < vertices.Count(); i++)
            {
                var point = vertices[i].Select(jv => UnitsAdapter.convertToRevit((double)jv)).ToArray();
                Point3D vector = new Point3D(point[0], point[2], point[1]);
                verticesList.Add(vector);
            }

            return verticesList;
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
                if (!TrudeRepository.HasRotationQuaternion(roofData))
                {
                    XYZ origin = new XYZ(0, 0, 0);
                    XYZ xinf = new XYZ(10, 0, 0);
                    XYZ yinf = new XYZ(0, 10, 0);
                    XYZ zinf = new XYZ(0, 0, 10);
                    Line xaxis = Line.CreateBound(origin, xinf);
                    Line yaxis = Line.CreateBound(origin, yinf);
                    Line zaxis = Line.CreateBound(origin, zinf);

                    position.Rotate(zaxis, -double.Parse(roofData["meshes"][0]["rotation"][1].ToString()));
                    position.Rotate(yaxis, -double.Parse(roofData["meshes"][0]["rotation"][2].ToString()));
                    position.Rotate(xaxis, -double.Parse(roofData["meshes"][0]["rotation"][0].ToString()));
                }
                else
                {
                    EulerAngles rotation = TrudeRepository.GetEulerAnglesFromRotationQuaternion(roofData);

                    Line localXAxis = Line.CreateBound(new XYZ(0, 0, 0), new XYZ(1, 0, 0));
                    Line localYAxis = Line.CreateBound(new XYZ(0, 0, 0), new XYZ(0, 1, 0));
                    Line localZAxis = Line.CreateBound(new XYZ(0, 0, 0), new XYZ(0, 0, 1));

                    // Y and Z axis are swapped moving from snaptrude to revit.
                    position.Rotate(localXAxis, -rotation.bank);
                    position.Rotate(localZAxis, -rotation.heading);
                    position.Rotate(localYAxis, -rotation.attitude);
                }
            }

        }

        private void setHeight(double thickness, double bottomZ, Level level)
        {
            double slabHeightAboveLevel = this.Position.Z + bottomZ - level.ProjectElevation + thickness;

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

        public TrudeRoof(JToken roofData, Document newDoc, ElementId levelId, FloorType existingFloorType = null)
        {
            this.Name = roofData["meshes"].First["name"].ToString();
            this.Position = TrudeRepository.GetPosition(roofData);
            this.Scaling = TrudeRepository.GetScaling(roofData);
            this.levelNumber = TrudeRepository.GetLevelNumber(roofData);
            this.Layers = TrudeRepository.GetLayers(roofData);

            double thickness = UnitsAdapter.convertToRevit((double)roofData["thickness"]);
            this.setThickness(Math.Abs(thickness));

            if (int.Parse(GlobalVariables.RvtApp.VersionNumber) >= 2023)
            {
                //Create(newDoc, roofData, thickness);
            }
            else
            {
                CreateDepricated(newDoc, roofData, thickness, levelId, existingFloorType);
            }
        }

        private void CreateDepricated(Document newDoc, JToken roofData, double thickness, ElementId levelId, FloorType existingFloorType = null)
        {
            JToken topVerticesNormal = roofData["topVerticesNormal"];
            JToken topVerticesUntitNormal = roofData["topVerticesUnitNormal"];

            List<Point3D> topVertices = jtokenListToPoint3d(roofData["topVertices"])
                .Distinct()
                .ToList();

            Point3D topLowestPoint = topVertices.Aggregate(topVertices[0], (least, next) => least.Z < next.Z ? least : next);

            List<Point3D> bottomVertices = jtokenListToPoint3d(roofData["bottomVertices"])
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
            this.floor = newDoc.Create.NewFloor(profile, false);

            this.floor.get_Parameter(BuiltInParameter.LEVEL_PARAM).Set(levelId);

            // Rotate and move the slab

            this.rotate(roofData);

            bool result = this.floor.Location.Move(this.Position);

            if (!result) throw new Exception("Move floor location failed.");

            this.setType(existingFloorType);

            Level level = newDoc.GetElement(levelId) as Level;
            double bottomZ = bottomVertices[0].Z * Scaling.Z;
            this.setHeight(thickness, bottomZ, level);
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
        //    //this.floor = newDoc.Create.NewFloor(profile, false);

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

        private void setType(FloorType defaultFloorType = null)
        {
            if (defaultFloorType is null)
            {
                FilteredElementCollector collector = new FilteredElementCollector(GlobalVariables.Document).OfClass(typeof(FloorType));
                defaultFloorType = collector.Where(type => ((FloorType)type).FamilyName == "Floor" && ((FloorType)type).Category.Name.ToLower().Contains("floor")).Last() as FloorType;
            }

            FloorType floorType = TypeStore.GetType(Layers, GlobalVariables.Document, defaultFloorType); 

            if (floorType.IsValidObject)
            {
                this.floor.FloorType = floorType;
            }
        }
    }
}
