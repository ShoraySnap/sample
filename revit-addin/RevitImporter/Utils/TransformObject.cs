namespace TrudeSerializer.Utils
{
    class TransformObject
    {
        public double[] position;
        public double rotation;
        public double[] center;
        public bool isMirrored;
        public bool isFaceFlipped;
        public bool isHandFlipped;

        public TransformObject(double[] position, double rotation, double[] center, bool isMirrored, bool isFaceFlipped, bool isHandFlipped)
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