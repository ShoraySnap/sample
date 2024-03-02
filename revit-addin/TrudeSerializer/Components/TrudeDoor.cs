using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.IO.Packaging;
using TrudeSerializer.Importer;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Components
{
    internal class TrudeDoor: TrudeComponent
    {
        public static bool IsDoor(Element element)
        {
            string category = element?.Category?.Name;
            if (category == null)
            {
                return false;
            }
            return category.Contains("Doors");
        }

        public Dimensions dimension;
        public TransformObject transform;
        public string subType;
        public string subCategory;
        public bool hasParentElement;
        public List<string> subComponent;
        public string hostId;
        public List<TrudePlanViewIndicator> planViewIndicator;
        public double[] handOrientation;
        public double[] facingOrientation;

        public TrudeDoor(string elementId, string level, string family, string subType, string subCategory, Dimensions dimension, TransformObject transform, bool hasParentElement, List<string> subComponents, string hostId) : base(elementId, "Doors", family, level)
        {
            this.subType = subType;
            this.subCategory = subCategory;
            this.dimension = dimension;
            this.transform = transform;
            this.isInstance = true;
            this.subComponent = subComponents;
            this.hasParentElement = hasParentElement;
            this.hostId = hostId;
           
        }

        public void SetPlanViewIndicator(List<TrudePlanViewIndicator> planViewIndicator)
        {
            this.planViewIndicator = planViewIndicator;
        }

        public void SetHandOrientation(double[] handOrientation)
        {
            this.handOrientation = handOrientation;
        }

        public void SetFacingOrientation(double[] facingOrientation)
        {
            this.facingOrientation = facingOrientation;
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
            string hostId = InstanceUtility.GetHostId(element);

            bool isFamilyPresent = serializedData.Furniture.HasFamily(familyName);
            TrudeFamily door;
            if (!isFamilyPresent)
            {
                door = new TrudeFamily(elementId, "Doors",  level, family, subType, subCategory, dimension, transform, subComponents);
                CurrentFamily = door;
                serializedData.Doors.AddFamily(familyName, door);
            }

            TrudeDoor instance = new TrudeDoor(elementId, level, family, subType, subCategory, dimension, transform, hasParentElement, subComponents, hostId);

            List<TrudePlanViewIndicator> planViewIndicator = TrudePlanViewIndicator.GetPlanViewIndicator(element);
            instance.SetPlanViewIndicator(planViewIndicator);
            instance.SetHandOrientation(element);
            instance.SetFacingOrientation(element);

            return instance;
        }

        public void SetHandOrientation(Element element)
        {
            FamilyInstance familyInstance = element as FamilyInstance;
            if (familyInstance == null)
            {
                return;
            }
            double[] handOrientation = { familyInstance.HandOrientation.X, familyInstance.HandOrientation.Z, familyInstance.HandOrientation.Y };
            this.handOrientation = handOrientation;

        }

        public void SetFacingOrientation(Element element)
        {
            if (!(element is FamilyInstance familyInstance))
            {
                return;
            }
            double[] facingOrientation = { familyInstance.HandOrientation.X, familyInstance.HandOrientation.Z, familyInstance.HandOrientation.Y };
            this.facingOrientation = facingOrientation;

        }
    }
}
