using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Snaptrude
{
    public class EulerAngles
    {
        public double attitude = 0;
        public double bank = 0;
        public double heading = 0;

        public EulerAngles() { }

        /// <summary>
        /// Method to convert Quaternions to Euler angles.
        /// Rotations should be done in the order of heading(Y-axis), attitude(Z-axis) and bank(X-axis)
        /// </summary>
        /// <param name="quaternion">double[4] representing qx, qy, qz and qw</param>
        /// <returns>EulerAngle instance</returns>
        public static EulerAngles FromQuaternion(double[] quaternion)
        {
            double qx = quaternion[0];
            double qy = quaternion[1];
            double qz = quaternion[2];
            double qw = quaternion[3];

            EulerAngles eulerAngles = new EulerAngles();

            //if (qw.RoundedEquals(0)) return eulerAngles;

            eulerAngles.heading = Math.Atan2(2 * qy * qw - 2 * qx * qz, 1 - 2 * Math.Pow(qy, 2) - 2 * Math.Pow(qz, 2));
            eulerAngles.attitude = Math.Asin(2 * qx * qy + 2 * qz * qw);
            eulerAngles.bank = Math.Atan2(2 * qx * qw - 2 * qy * qz, 1 - 2 * Math.Pow(qx, 2) - 2 * Math.Pow(qz, 2));

            return eulerAngles;
        }

        public string Stringify()
        {
            return $"{attitude}x{bank}x{heading}";
        }
    }
}
