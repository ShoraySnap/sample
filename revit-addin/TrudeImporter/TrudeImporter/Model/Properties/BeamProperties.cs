﻿using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace TrudeImporter
{
    public class BeamProperties
    {
        [JsonProperty("height")]
        public float Height { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("storey")]
        public int Storey { get; set; }

        [JsonProperty("uniqueId")]
        public int UniqueId { get; set; }

        [JsonProperty("length")]
        public float Length { get; set; }

        [JsonProperty("existingElementId")]
        public int? ExistingElementId { get; set; }

        // these face vertices are the vertices of the face that was drawn first to extrude the beam from
        [JsonProperty("faceVertices")]
        public List<XYZ> FaceVertices { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("centerPosition")]
        public XYZ CenterPosition { get; set; }

        [JsonProperty("allFaceVertices")]
        public List<List<XYZ>> AllFaceVertices { get; set; }

        [JsonProperty("materialName")]
        public string MaterialName { get; set; }

        [JsonProperty("faceMaterialIds")]
        public List<int> FaceMaterialIds { get; set; }

        [JsonProperty("instances")]
        public List<BeamInstanceProperties> Instances { get; set; }

    }

    [Serializable]
    public class BeamInstanceProperties
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("storey")]
        public int Storey { get; set; }

        [JsonProperty("uniqueId")]
        public int UniqueId { get; set; }

        [JsonProperty("centerPosition")]
        public XYZ CenterPosition { get; set; }

        [JsonProperty("rotation")]
        public XYZ Rotation { get; set; }

        [JsonProperty("existingElementId")]
        public int? ExistingElementId { get; set; }
    }
}