using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace TrudeImporter
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ColorSchemeEnum
    {
        monochrome,
        texture,
        //label = 2,
        //spaceType = 3
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum LabelsEnum
    {
        objectLabels,
        areas,
        //Targets = 2,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum SheetSizeEnum
    {
        ANSI_A, ANSI_B, ANSI_C, ANSI_D, ANSI_E,
        ARCH_A, ARCH_B, ARCH_C, ARCH_D, ARCH_E, ARCH_E1,
        ISO_A4, ISO_A3, ISO_A2, ISO_A1, ISO_A0,
    }

    public class OrthoCameraSettings
    {
        [JsonProperty("bottomLeft")]
        public XYZ BottomLeft { get; set; }

        [JsonProperty("topRight")]
        public XYZ TopRight { get; set; }
    }

    public class LabelSettings
    {
        [JsonProperty("selected")]
        public List<LabelsEnum> Selected { get; set; }
    }

    public class SheetSettings
    {
        [JsonProperty("sheetSize")]
        public SheetSizeEnum SheetSize { get; set; }

        [JsonProperty("scale")]
        public int Scale;
    }

    public class ColorSettings
    {
        [JsonProperty("canvasBackground")]
        public string BackgroundColor;

        [JsonProperty("scheme")]
        public ColorSchemeEnum Scheme;
    }

    public class ViewElements
    {
        [JsonProperty("hiddenIds")]
        public List<int> HiddenIds { get; set; }
    }

    public class ViewProperties
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("camera")]
        public OrthoCameraSettings Camera { get; set; }

        [JsonProperty("sheetData")]
        public SheetSettings Sheet { get; set; }

        [JsonProperty("activeStorey")]
        public int Storey { get; set; }

        [JsonProperty("labelData")]
        public LabelSettings Label { get; set; }

        [JsonProperty("colorData")]
        public ColorSettings Color { get; set; }

        [JsonProperty("elements")]
        public ViewElements Elements { get; set; }
    }
}
