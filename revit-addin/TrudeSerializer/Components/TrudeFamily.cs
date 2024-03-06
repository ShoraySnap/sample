using System;
using System.Collections.Generic;
using System.Text;
using TrudeSerializer.Utils;

namespace TrudeSerializer.Components
{
    internal class TrudeFamily: TrudeComponent
    {
        public Dimensions dimension;
        public TransformObject transform;
        public bool hasParentElement;
        public List<string> subComponents;
        public string subType;
        public string subCategory;


        public TrudeFamily(string elementId, string category, string level, string family, string subType, string subCategory, Dimensions dimension, TransformObject transform, List<string> subComponent) : base(elementId, category, family, level)
        {
            this.subType = subType;
            this.subCategory = subCategory;
            this.dimension = dimension;
            this.transform = transform;
            this.subComponents = subComponent;
        }
    }
}
