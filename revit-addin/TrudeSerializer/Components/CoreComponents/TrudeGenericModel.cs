using Autodesk.Revit.DB;
using System.Collections.Generic;
using TrudeSerializer.Importer;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Components
{
    internal class TrudeGenericModel : TrudeComponent
    {
        public Dimensions dimension;
        public TransformObject transform;
        public string subType;
        public string subCategory;
        public bool hasParentElement;
        public List<string> subComponent;
        public double offset;
        public static bool IsGenericModel(Element element)
        {
            string category = element?.Category?.Name;
            if (category == null)
            {
                return false;
            }
            return category.Contains("Generic Models");
        }

        public TrudeGenericModel(string elementId, string level, string family, string subType, string subCategory, Dimensions dimension, TransformObject transform, bool hasParentElement, List<string> subComponents, double offset) : base(elementId, "GenericModel", family, level)
        {
            this.subType = subType;
            this.subCategory = subCategory;
            this.dimension = dimension;
            this.transform = transform;
            this.isInstance = true;
            this.subComponent = subComponents;
            this.hasParentElement = hasParentElement;
            this.offset = offset;
        }

        public static TrudeGenericModel GetSerializedComponent(SerializedTrudeData serializedData, Element element)
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

            bool hasParentElement = FamilyInstanceUtils.HasParentElement(element, true, true);
            double offset = InstanceUtility.GetOffset(element);

            List<string> subComponents = FamilyInstanceUtils.GetSubComponentIds(element);

            string familyName = InstanceUtility.GetRevitName(subType, family, dimension, isFaceFlipped);

            bool isFamilyPresent = serializedData.GenericModel.HasFamily(familyName);
            TrudeFamily genericModel = null;
            bool shouldUpdateFamily = false;
            if (isFamilyPresent)
            {
                genericModel = serializedData.GenericModel.GetFamily(familyName);
                shouldUpdateFamily = TrudeFamily.ShouldGetNewFamilyGeometry(element, genericModel);
                if (shouldUpdateFamily)
                {
                    ComponentHandler.Instance.RemoveFamily(serializedData, ComponentHandler.FamilyFolder.GenericModel, familyName);
                }
            }
            if (!isFamilyPresent || shouldUpdateFamily)
            {
                genericModel = new TrudeFamily(elementId, "GenericModel", level, family, subType, subCategory, dimension, transform, subComponents);
                CurrentFamily = genericModel;
                ComponentHandler.Instance.AddFamily(serializedData, ComponentHandler.FamilyFolder.GenericModel, familyName, genericModel);
            }

            TrudeGenericModel instance = new TrudeGenericModel(elementId, level, family, subType, subCategory, dimension, transform, hasParentElement, subComponents, offset);

            return instance;
        }
    }
}