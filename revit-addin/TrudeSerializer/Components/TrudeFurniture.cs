using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using TrudeSerializer.Importer;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Components
{
    internal class TrudeFurniture : TrudeComponent
    {
        private static readonly string[] funitureSubCategories = new string[] {
        "Lighting Fixtures",
        "Communication Devices",
        "Casework",
        "Furniture Systems",
        "Planting",
        "Security Devices",
        "Electrical Fixtures",
        "Furniture",
        "Fire Alarm Devices",
        "Data Devices",
        "Lighting Devices",
        "Specialty Equipment",
        "Assemblies",
        "Model Groups",
        "Mechanical Equipment",
        "Plumbing Fixtures",
        "Electrical Equipment",
        "Sprinklers"
        };

        public static bool IsFurnitureCategory(Element element)
        {
            string category = element?.Category?.Name;
            if (category == null)
            {
                return false;
            }
            return Array.Exists(funitureSubCategories, element.Category.Name.Contains);
        }

        public Dimensions dimension;
        public TransformObject transform;
        public string subType;
        public string subCategory;
        public bool hasParentElement;
        public List<string> subComponent;

        public TrudeFurniture(string elementId, string level, string family, string subType, string subCategory, Dimensions dimension, TransformObject transform, bool hasParentElement, List<string> subComponents) : base(elementId, "Furniture", family, level)
        {
            this.subType = subType;
            this.subCategory = subCategory;
            this.dimension = dimension;
            this.transform = transform;
            this.isInstance = true;
            this.subComponent = subComponents;
            this.hasParentElement = hasParentElement;
        }

        public static TrudeComponent GetSerializedComponent(SerializedTrudeData serializedData, Element element)
        {
            ClearCurrentFamily();
            string elementId = element.Id.ToString();
            string level = TrudeLevel.GetLevelName(element);

            string subType = element.Name;
            string family = InstanceUtility.GetFamily(element);
            string subCategory = element.Category.Name;

            List<double> position = InstanceUtility.GetPosition(element);
            double rotation = InstanceUtility.GetRotation(element);
            List<double> center = InstanceUtility.GetCustomCenterPoint(element);

            bool isFaceFlipped = InstanceUtility.IsFaceFlipped(element);
            bool isHandFlipped = InstanceUtility.IsHandFlipped(element);
            bool isMirrored = InstanceUtility.IsMirrored(element);

            TransformObject transform = new TransformObject(position, rotation, center, isMirrored, isFaceFlipped, isHandFlipped);

            double width = InstanceUtility.GetWidth(element);
            double height = InstanceUtility.GetHeight(element);
            double length = InstanceUtility.GetLength(element);

            Dimensions dimension = new Dimensions(width, height, length);

            bool hasParentElement = FamilyInstanceUtils.HasParentElement(element);
            List<string> subComponents = FamilyInstanceUtils.GetSubComponentIds(element);

            string familyName = InstanceUtility.GetRevitName(subType, family, length, width, height, isFaceFlipped);

            bool isFamilyPresent = serializedData.Furniture.HasFamily(familyName);
            TrudeFamily furniture;
            bool shouldUpdateFamily = false;
            if (isFamilyPresent)
            {
                furniture = serializedData.Furniture.GetFamily(familyName);
                shouldUpdateFamily = InstanceUtility.ShouldGetNewFamilyGeometry(element, furniture);
                if (shouldUpdateFamily)
                {
                    serializedData.Furniture.RemoveFamily(familyName);
                }
            }
            if (!isFamilyPresent || shouldUpdateFamily)
            {
                furniture = new TrudeFamily(elementId, "Furniture", level, family, subType, subCategory, dimension, transform, subComponents);
                CurrentFamily = furniture;
                serializedData.Furniture.AddFamily(familyName, furniture);
            }

            TrudeFurniture instance = new TrudeFurniture(elementId, level, family, subType, subCategory, dimension, transform, hasParentElement, subComponents);

            return instance;
        }
    }
}