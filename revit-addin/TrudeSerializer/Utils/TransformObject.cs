using System.Collections.Generic;

namespace TrudeSerializer.Utils
{
    class TransformObject
    {
        public List<double> position;
        public double rotation;
        public List<double> center;
        public bool isMirrored;
        public bool isFaceFlipped;
        public bool isHandFlipped;

        public TransformObject(List<double> position, double rotation, List<double> center, bool isMirrored, bool isFaceFlipped, bool isHandFlipped)
        {
            this.position = position;
            this.rotation = rotation;
            this.center = center;
            this.isMirrored = isMirrored;
            this.isFaceFlipped = isFaceFlipped;
            this.isHandFlipped = isHandFlipped;
        }
    }
}