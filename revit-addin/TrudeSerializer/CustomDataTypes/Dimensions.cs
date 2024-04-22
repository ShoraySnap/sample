using System;

namespace TrudeSerializer.Utils
{
    class Dimensions
    {
        public double width;
        public double height;
        public double length;

        public Dimensions(double width, double height, double length)
        {
            this.width = Math.Round(width, Constants.ROUND_DIGITS);
            this.height = Math.Round(height, Constants.ROUND_DIGITS);
            this.length = Math.Round(length, Constants.ROUND_DIGITS);
        }
    }
}