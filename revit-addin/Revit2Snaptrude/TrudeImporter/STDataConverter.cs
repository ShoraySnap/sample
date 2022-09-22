using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace Snaptrude
{
    class STDataConverter
    {
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

        public static ST_Layer[] GetLayers(JToken data)
        {
            if (!data["dsProps"]["properties"]["_components"].HasValues) return null;

            JToken revitMetaData  = data["dsProps"]["revitMetaData"];

            string baseType = (string) data["dsProps"]["properties"]["_components"]["_name"];
            JArray layers = (JArray) data["dsProps"]["properties"]["_components"]["_layers"];

            if (data["layers"].IsNullOrEmpty())
            {
                layers = (JArray)data["dsProps"]["properties"]["_components"]["_layers"];
            }
            else
            {
                layers = (JArray) data["layers"];
            }

            List<ST_Layer> stLayers = new List<ST_Layer>();

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
                    ST_Layer stLayer = new ST_Layer(layers[i], baseType, function);
                    stLayers.Add(stLayer);
                }
                else
                {
                    ST_Layer stLayer = new ST_Layer(layers[i], baseType);
                    stLayers.Add(stLayer);
                }
            }

            return stLayers.ToArray();

            //return layers
            //    .Select(jv => new ST_Layer(jv, baseType))
            //    .ToArray();
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
