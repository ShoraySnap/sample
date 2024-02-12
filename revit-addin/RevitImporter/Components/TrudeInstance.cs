using TrudeSerializer.Utils;

namespace TrudeSerializer.Components
{
    internal class TrudeInstance : TrudeComponent
    {
        public Dimensions dimension;
        public TransformObject transform;
        public string subType;
        public string subCategory;

        public TrudeInstance(string elementId, string level, string family, string subType, string subCategory, Dimensions dimension, TransformObject transform) : base(elementId, "Furniture", family, level)
        {
            this.subType = subType;
            this.subCategory = subCategory;
            this.dimension = dimension;
            this.transform = transform;
            this.isInstance = true;
        }
    }




}