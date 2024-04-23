﻿using TrudeSerializer.Utils;

namespace TrudeSerializer.Components
{
    internal class TrudeInstance : TrudeComponent
    {
        public Dimensions dimension;
        public TransformObject transform;
        public string subType;
        public string subCategory;
        public bool hasParentElement;
        public string[] subComponent;
        public string hostId;
        public TrudePlanViewIndicator planViewIndicator;

        public TrudeInstance(string elementId, string level, string family, string subType, string subCategory, Dimensions dimension, TransformObject transform, bool hasParentElement, string[] subComponents, string hostId) : base(elementId, "Furniture", family, level)
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

        public void SetPlanViewIndicator(TrudePlanViewIndicator planViewIndicator)
        {
            this.planViewIndicator = planViewIndicator;
        }
    }
}