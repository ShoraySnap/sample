using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Snaptrude
{
    public class ST_Wall : ST_Abstract
    {
        public string Material_ID { get; set; }
        public double[][] Wall_Polygons { get; set; }

        public ST_Layer[] Layers;

        public Wall wall { get; set; }

        public static IList<Curve> GetProfile(List<XYZ> points)
        {
            IList<Curve> profile = new List<Curve>();

            for (int i = 0; i < points.Count(); i++)
            {
                XYZ point0 = points[i];
                XYZ point1 = points[(i+1).Mod(points.Count())];

                if (point1.IsAlmostEqualTo(point0))
                {
                    point1 = points[(i + 2).Mod(points.Count())];
                }
                profile.Add(Line.CreateBound(point0, point1));
            }

            return profile;
        }

        public Wall CreateWall(Document newDoc, IList<Curve> profile, ElementId wallTypeId, ElementId levelIdForWall, double height)
        {
            wall = Wall.Create(newDoc, profile, wallTypeId, levelIdForWall, false);
            //wall = Wall.Create(newDoc, profile, false);
            //wall.ChangeTypeId(wallTypeId);

            Parameter top_constraint_param = wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
            top_constraint_param.Set("Unconnected");
            top_constraint_param.SetValueString("Unconnected");
            top_constraint_param.Set(ElementId.InvalidElementId);

            Parameter heightParam = wall.LookupParameter("Unconnected Height");
            heightParam.Set(height);

            return wall;
        }

        public static void JoinAllWalls(FilteredElementCollector walls, Wall wall, Document doc)
        {
            BoundingBoxXYZ bb = wall.get_BoundingBox(doc.ActiveView);
            Outline outline = new Outline(bb.Min, bb.Max);
            BoundingBoxIntersectsFilter bbfilter = new BoundingBoxIntersectsFilter(outline);
            walls.WherePasses(bbfilter);
            foreach (Wall w in walls)
            {
                if (!(JoinGeometryUtils.AreElementsJoined(doc, wall, w)))
                {
                    JoinGeometryUtils.JoinGeometry(doc, wall, w);
                }
            }
        }
        /// <summary>
        /// Paint wall faces.
        /// </summary>
        /// <param name="wall">Wall to be painted.</param>
        /// <param name="matId">ElementId of the material to paint on the wall.</param>
        /// <param name="doc">Revit document under process.</param>
        public void PaintWallFaces(Wall wall, ElementId matId, Document doc)
        {
            GeometryElement geometryElement = wall.get_Geometry(new Options());
            foreach (GeometryObject geometryObject in geometryElement)
            {
                if (geometryObject is Solid)
                {
                    Solid solid = geometryObject as Solid;
                    foreach (Face face in solid.Faces)
                    {
                        if (doc.IsPainted(wall.Id, face) == false)
                        {
                            doc.Paint(wall.Id, face, matId);
                        }
                    }
                }
            }
        }

        public GeometryElement GetGeometryElement()
        {
            Options geoOptions = new Options();
            geoOptions.DetailLevel = ViewDetailLevel.Fine;
            geoOptions.ComputeReferences = true;

            return wall.get_Geometry(geoOptions);
        }

        public FaceArray GetFaces()
        {
            IEnumerator<GeometryObject> geoObjectItor = GetGeometryElement().GetEnumerator();
            while (geoObjectItor.MoveNext())
            {
                Solid theSolid = geoObjectItor.Current as Solid;
                if (null != theSolid) return theSolid.Faces;
            }

            return null;
        }

        public void ApplyMaterialByFace( Document document, String materialNameWithId, JArray subMeshes, JArray materials, JArray multiMaterials, Wall wall )
        {
            //Dictionary that stores Revit Face And Its Normal
            IDictionary<String, Face> normalToRevitFace = new Dictionary<String, Face>();

            List<XYZ> revitFaceNormals = new List<XYZ>();

            IEnumerator<GeometryObject> geoObjectItor = GetGeometryElement().GetEnumerator();
            while (geoObjectItor.MoveNext())
            {
                Solid theSolid = geoObjectItor.Current as Solid;
                if (null != theSolid)
                {
                    foreach (Face face in theSolid.Faces)
                    {
                        PlanarFace p = (PlanarFace)face;
                        var normal = p.FaceNormal;
                        revitFaceNormals.Add(normal.Round(3));
                        if (!normalToRevitFace.ContainsKey(normal.Round(3).Stringify()))
                        {
                            normalToRevitFace.Add(normal.Round(3).Stringify(), face);
                        }
                    }
                }
            }

            //Dictionary that has Revit Face And The Material Index to Be Applied For It.
            IDictionary <Face, int> revitFaceAndItsSubMeshIndex = new Dictionary<Face, int>();

            foreach (JToken subMesh in subMeshes)
            {
                XYZ _normalInXYZFormat = STDataConverter.ArrayToXYZ(subMesh["normal"], false).Round(3);
                int _materialIndex = (int)subMesh["materialIndex"];

                String key = _normalInXYZFormat.Stringify();
                if (normalToRevitFace.ContainsKey(key))
                {
                    Face revitFace = normalToRevitFace[key];

                    if (!revitFaceAndItsSubMeshIndex.ContainsKey(revitFace)) revitFaceAndItsSubMeshIndex.Add(revitFace, _materialIndex);
                }
                else
                {
                    // find the closest key
                    double leastDistance = Double.MaxValue;
                    foreach (XYZ normal in revitFaceNormals)
                    {
                        double distance = _normalInXYZFormat.MultiplyEach(Scaling).DistanceTo(normal);
                        if (distance < leastDistance)
                        {
                            leastDistance = distance;
                            key = normal.Stringify();
                        }
                    }

                    Face revitFace = normalToRevitFace[key];

                    if (!revitFaceAndItsSubMeshIndex.ContainsKey(revitFace)) revitFaceAndItsSubMeshIndex.Add(revitFace, _materialIndex);
                }
            }

            FilteredElementCollector collector1 = new FilteredElementCollector(document).OfClass(typeof(Autodesk.Revit.DB.Material));
            IEnumerable<Autodesk.Revit.DB.Material> materialsEnum = collector1.ToElements().Cast<Autodesk.Revit.DB.Material>();
            
            
            foreach (var face in revitFaceAndItsSubMeshIndex)
            {
                String _materialName = null;
                _materialName = TrudeImporter.getMaterialNameFromMaterialId(materialNameWithId, subMeshes, materials, multiMaterials, face.Value);

                Autodesk.Revit.DB.Material _materialElement = null;

                foreach (var materialElement in materialsEnum)
                {
                    String matName = materialElement.Name;

                    if (matName.Replace("_", " ") == _materialName)
                    {
                        _materialElement = materialElement;
                    }
                }


                if ( _materialElement != null )
                {
                    document.Paint(wall.Id, face.Key, _materialElement.Id);
                }
            }

        }

        public void ApplyMaterialByObject(Document document, Wall wall, Autodesk.Revit.DB.Material material)
        {
            // Before acquiring the geometry, make sure the detail level is set to 'Fine'
            Options geoOptions = new Options();
            geoOptions.DetailLevel = ViewDetailLevel.Fine;

            // Obtain geometry for the given Wall element
            GeometryElement geoElem = wall.get_Geometry(geoOptions);

            // Find a face on the wall
            //Face wallFace = null;
            IEnumerator<GeometryObject> geoObjectItor = geoElem.GetEnumerator();
            List<Face> wallFaces = new List<Face>();

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
                        PlanarFace p = (PlanarFace)face;
                        var normal = p.FaceNormal;
                        wallFaces.Add(face);
                    }
                }
            }

            foreach (Face face in wallFaces)
            {
                document.Paint(wall.Id, face, material.Id);
            }
        }

        public static WallTypeStore TypeStore = new WallTypeStore();

        public static WallType GetWallTypeByWallLayers(ST_Layer[] layers, Document doc, WallType existingWallType=null)
        {
            WallType defaultType = null;
            if (!(existingWallType is null)) defaultType = existingWallType;

            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(WallType));
            if (defaultType is null) defaultType = collector.Where(wallType => ((WallType) wallType).Kind == WallKind.Basic) as WallType;
            if (defaultType is null) defaultType = collector.Where(wallType => ((WallType) wallType).Kind == WallKind.Stacked) as WallType;
            if (defaultType is null)
                foreach (WallType wt in collector.ToElements())
                {
                    if (wt.Kind == WallKind.Basic)
                    {
                        defaultType = wt;
                        break;
                    }
                }
            if (defaultType is null) defaultType = collector.First() as WallType;

            try
            {
                return TypeStore.GetType(layers, defaultType);
            } catch (Exception e)
            {
                return defaultType;
            }
        }
    }
}
