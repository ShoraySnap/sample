using System;
using System.Collections.Generic;
using System.Linq;

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
            List<double> roundPosition = position.Select(x => Math.Round(x, Constants.ROUND_DIGITS)).ToList();
            List<double> roundCenter = center.Select(x => Math.Round(x, Constants.ROUND_DIGITS)).ToList();

            this.position = roundPosition;
            this.rotation = Math.Round(rotation, Constants.ROUND_DIGITS);
            this.center = roundCenter;
            this.isMirrored = isMirrored;
            this.isFaceFlipped = isFaceFlipped;
            this.isHandFlipped = isHandFlipped;
        }

        public TransformObject(List<double> center)
        {
            List<double> roundCenter = center.Select(x => Math.Round(x, Constants.ROUND_DIGITS)).ToList();
            this.center = roundCenter;
        }
    }
}