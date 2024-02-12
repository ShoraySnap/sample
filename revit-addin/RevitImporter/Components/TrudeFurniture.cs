using Autodesk.Revit.DB;
using RevitImporter.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;
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
            string category = element.Category.Name;
            return Array.Exists(funitureSubCategories, element.Category.Name.Contains);
        }

        public Dimensions dimension;
        public TransformObject transform;
        public bool hasParentElement;
        public string[] subComponents;

        public TrudeFurniture(string elementId, string level, string family) : base(elementId, "Furniture", family, level)
        {
        }

        public static TrudeComponent GetSerializedComponent(SerializedTrudeData serializedData, Element element)
        {
            string elementId = element.Id.ToString();
            string level = TrudeLevel.GetLevelName(element);

            string subType = element.Name;
            string family = InstanceUtility.GetFamily(element);
            string subCategory = element.Category.Name;

            double[] position = InstanceUtility.GetPosition(element);
            double rotation = InstanceUtility.GetRotation(element);
            double[] center = InstanceUtility.GetCustomCenterPoint(element);

            bool isFaceFlipped = InstanceUtility.IsFaceFlipped(element);
            bool isHandFlipped = InstanceUtility.IsHandFlipped(element);
            bool isMirrored = InstanceUtility.IsMirrored(element);

            TransformObject transform = new TransformObject(position, rotation, center, isFaceFlipped, isHandFlipped, isMirrored);

            double width = InstanceUtility.GetWidth(element);
            double height = InstanceUtility.GetHeight(element);
            double length = InstanceUtility.GetLength(element);

            Dimensions dimension = new Dimensions(width, height, length);

            bool hasParentElement = HasParentElement(element);
            string[] subComponents = GetSubComponentIds(element);
           

            string familyName = InstanceUtility.GetRevitName(subType, family, length, width, height, isFaceFlipped);

            bool isFamilyPresent = serializedData.Furniture.HasFamily(familyName);
            TrudeFurniture furniture;
            if (!isFamilyPresent)
            {
                furniture = new TrudeFurniture(elementId, level, family);
                //serializedData.Furniture.AddFamily(familyName);
            }

            TrudeInstance instance = new TrudeInstance(elementId, level, family, subType, subCategory, dimension, transform);

            return new TrudeFurniture(elementId, level, family);
        }

        public static bool HasParentElement(Element element)
        {
            bool hasParentElement = false;

            FamilyInstance familyInstance = element as FamilyInstance;
            if (familyInstance != null)
            {
                Element superComponent = familyInstance.SuperComponent;
                if (superComponent != null)
                {
                    hasParentElement = true;
                }

                ElementId assemblySuperComponent = familyInstance.AssemblyInstanceId;
                if (assemblySuperComponent != null || assemblySuperComponent.ToString() != "-1")
                {
                    hasParentElement = true;
                }

                ElementId groupId = familyInstance.GroupId;
                if (groupId != null || groupId.ToString() != "-1")
                {
                    hasParentElement = true;
                }
            }

            return hasParentElement;
        }

        public static string[] GetSubComponentIds(Element element)
        {
            string[] subComponentIds = new string[] { };

            IList<ElementId> dependantElements = element.GetDependentElements(null);

            if (dependantElements.Count > 0)
            {
                foreach (ElementId dependantElement in dependantElements)
                {
                    subComponentIds.Append(dependantElement.ToString());
                }

                return subComponentIds;
            }

            ICollection<ElementId> subComponents = (element as FamilyInstance).GetSubComponentIds();
            if (subComponents.Count > 0)
            {
                foreach (ElementId subComponent in subComponents)
                {
                    subComponentIds.Append(subComponent.ToString());
                }

                return subComponentIds;
            }

            return subComponentIds;
        }
    }
}