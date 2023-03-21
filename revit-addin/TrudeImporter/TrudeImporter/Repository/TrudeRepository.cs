using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;

namespace TrudeImporter
{
    class TrudeRepository
    {
        public static List<List<XYZ>> GetHoles(JToken data)
        {
            List<List<XYZ>> holes = new List<List<XYZ>>();

            if (data["holes"] is null) return holes;

            foreach (JToken holeJtoken in data["holes"])
            {
                List<XYZ> holePoints = holeJtoken.Select(point => TrudeRepository.ArrayToXYZ(point)).ToList();

                holes.Add(holePoints);
            }

            return holes;
        }

        public static string GetDataType(JToken data)
        {
            return data["meshes"][0]["type"].ToString();
        }
        public static string GetName(JToken data)
        {
            return data["meshes"].First()["name"].ToString();
        }

        public static XYZ GetPosition(JToken data)
        {
            double[] positionList = data["meshes"].First()["position"].Select(jv => UnitsAdapter.convertToRevit((double)jv)).ToArray();
            
            return new XYZ(positionList[0], positionList[2], positionList[1]);
        }

        public static XYZ ArrayToXYZ(JToken array, bool convertUnits = true)
        {
            if (convertUnits)
            {
                double[] positionList = array.Select(jv => UnitsAdapter.convertToRevit((double)jv)).ToArray();

                return new XYZ(positionList[0], positionList[2], positionList[1]);
            }

            double[] _positionList = array.Select(jv => (double)jv).ToArray();

            return new XYZ(_positionList[0], _positionList[2], _positionList[1]);
        }

        public static XYZ GetCenterPosition(JToken data)
        {
            double[] positionList = data["meshes"].First()["centerPosition"].Select(jv => UnitsAdapter.convertToRevit((double)jv)).ToArray();
            
            return new XYZ(positionList[0], positionList[2], positionList[1]);
        }
        public static XYZ GetScaling(JToken data)
        {
            double[] scalingList = data["meshes"].First()["scaling"].Select(jv => (double)jv).ToArray();
            
            return new XYZ(scalingList[0], scalingList[2], scalingList[1]);
        }
        public static XYZ GetRotation(JToken data)
        {
            double[] rotationList = data["meshes"].First()["rotation"].Select(jv => (double)jv).ToArray();
            
            return new XYZ(rotationList[0], rotationList[2], rotationList[1]);
        }

        public static int GetLevelNumber(JToken data)
        {
            return int.Parse(data["meshes"].First()["storey"].ToString());
        }

        public static List<XYZ> GetVertices(JToken data, int precision = 8, double[] scale = null)
        {
            JToken vertexData = data["geometries"]["vertexData"].First;
            double[] defaultScale = { 1, 1, 1 };
            if (scale is null) scale = defaultScale;

            List<Point3D> vertices = new List<Point3D>();

            double[] positions = vertexData["positions"].Select(gv => UnitsAdapter.convertToRevit((double)gv)).ToArray();
            for (int i = 0; i < positions.Length - 2; i += 3)
            {
                Point3D point = new Point3D(
                    Math.Round(positions[i] * scale[0], precision),
                    Math.Round(positions[i + 2] * scale[2], precision),
                    Math.Round(positions[i + 1] * scale[1], precision));
                vertices.Add(point);
            }
            return vertices.Distinct().Select((point, index) => new XYZ(point.X, point.Y, point.Z)).ToList();
        }

        public static EulerAngles GetEulerAnglesFromRotationQuaternion(JToken data)
        {
            EulerAngles rotation;
            if (data["meshes"][0]["rotationQuaternion"] is null)
            {
                rotation = new EulerAngles();
            }
            else
            {
                double[] quat = data["meshes"][0]["rotationQuaternion"].Values<double>().ToArray();
                rotation = EulerAngles.FromQuaternion(quat);
            }

            return rotation;
        }

        public static bool HasRotationQuaternion(JToken data)
        {
            return !(data["meshes"][0]["rotationQuaternion"] is null);
        }

        public static String GetFamilyName(JToken data)
        {
            JToken revitMetaData  = data["dsProps"]["revitMetaData"];

            return (string)revitMetaData["family"];
        }

        public static String GetFamilyTypeName(JToken data)
        {
            JToken revitMetaData  = data["dsProps"]["revitMetaData"];

            return (string)revitMetaData["type"];
        }

        public static TrudeLayer[] GetLayers(JToken data, double fallbackThickness = 25)
        {
            JToken revitMetaData  = data["dsProps"]["revitMetaData"];

            string baseType = null;
            JArray layers = null;

            if (data["baseType"].IsNullOrEmpty())
            {
                baseType = (string)data["dsProps"]["properties"]["_components"]["_name"];
            }
            else
            {
                baseType = (string) data["baseType"];
            }

            if (data["layers"].IsNullOrEmpty())
            {
                layers = (JArray)data["dsProps"]["properties"]["_components"]["_layers"];
            }
            else
            {
                layers = (JArray) data["layers"];
            }

            List<TrudeLayer> stLayers = new List<TrudeLayer>();

            bool shouldSetFunction =  false;
            if(!revitMetaData.IsNullOrEmpty())
            {
                if (revitMetaData["layersData"].IsNullOrEmpty())
                {
                    shouldSetFunction = false;
                }
                else if (revitMetaData["layersData"].Count() == layers.Count())
                {
                    shouldSetFunction = true;
                }
            }
            
            for (int i = 0; i < layers.Count(); i++)
            {
                if (shouldSetFunction)
                {
                    string function = (string)revitMetaData["layersData"][i]["function"];
                    TrudeLayer stLayer = new TrudeLayer(layers[i], baseType, function, fallbackThickness);
                    stLayers.Add(stLayer);
                }
                else
                {
                    TrudeLayer stLayer = new TrudeLayer(layers[i], baseType, null, fallbackThickness);
                    stLayers.Add(stLayer);
                }
            }

            return stLayers.ToArray();
        }

        public static List<Point3D> ListToPoint3d(JToken vertices)
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
    }
}
