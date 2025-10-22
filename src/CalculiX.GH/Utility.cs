using Rhino.Geometry;

namespace CalculiX.GH
{
    public static partial class Utility
    {
        private static readonly float _oneThird = 1f / 3f;
        private static readonly float _twoPiThirds = 2f * (float)Math.PI / 3f;
        private static readonly float _fourPiThirds = 4f * (float)Math.PI / 3f;
        //
        private static readonly float _radToDeg = (float)(180f / Math.PI);
        private static readonly float _degToRad = (float)(Math.PI / 180f);

        public static double CalculateVonMises(double xx, double yy, double zz, double xy, double yz, double zx) =>
            Math.Sqrt(0.5 * (
              (xx - yy) * (xx - yy) + (yy - zz) * (yy - zz) + (zz - xx) * (zz - xx)
                  + 6 * (xy * xy + yz * yz + zx * zx)
              )
        );

        public static float CalculateVonMises(float xx, float yy, float zz, float xy, float yz, float zx) =>
            (float)Math.Sqrt(0.5f * (
              (xx - yy) * (xx - yy) + (yy - zz) * (yy - zz) + (zz - xx) * (zz - xx)
                  + 6.0f * (xy * xy + yz * yz + zx * zx)
              )
        );


        public static float[] CalculateVonMises(float[] xx, float[] yy, float[] zz, float[] xy, float[] yz, float[] zx)
        {
            var N = xx.Length;
            if (yy.Length != N || zz.Length != N || xy.Length != N || yz.Length != N || zx.Length != N)
                throw new ArgumentException("All arrays must have the same length!");

            var vm = new float[N];
            for (int i = 0; i < N; ++i)
            {
                vm[i] = CalculateVonMises(xx[i], yy[i], zz[i], xy[i], yz[i], zx[i]);
            }

            return vm;
        }

        /// <summary>
        /// From PrePoMax
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="d"></param>
        /// <param name="x1"></param>
        /// <param name="x2"></param>
        /// <param name="x3"></param>
        public static void SolveQubicEquationDepressedCubicF(float a, float b, float c, float d,
                                                     ref float x1, ref float x2, ref float x3)
        {
            // https://en.wikipedia.org/wiki/Cubic_function
            float p;
            float q;
            float tmp1;
            float tmp2;
            float alpha;
            //
            p = (3f * a * c - b * b) / (3f * a * a);
            if (p > 0)
            {
                x1 = x2 = x3 = 0;
            }
            else
            {
                q = (2f * b * b * b - 9f * a * b * c + 27f * a * a * d) / (27f * a * a * a);
                //
                tmp1 = (3f * q) / (2f * p) * (float)Math.Sqrt(-3f / p);
                if (tmp1 > 1f) tmp1 = 1f;
                else if (tmp1 < -1f) tmp1 = -1f;
                alpha = _oneThird * (float)Math.Acos(tmp1);

                tmp1 = 2f * (float)Math.Sqrt(-p / 3f);
                tmp2 = b / (3f * a);
                x1 = tmp1 * (float)Math.Cos(alpha) - tmp2;
                x2 = tmp1 * (float)Math.Cos(alpha - _twoPiThirds) - tmp2;
                x3 = tmp1 * (float)Math.Cos(alpha - _fourPiThirds) - tmp2;
            }
        }

        /// <summary>
        /// Adapted from PrePoMax:
        /// https://gitlab.com/MatejB/PrePoMax/-/blob/master/CaeResults/FieldOutput/Field.cs
        /// </summary>
        /// <returns></returns>
        public static void ComputePrincipalInvariants(float[] array11, float[] array22, float[] array33, float[] array12, float[] array23, float[] array31, 
            out float[] signedMaxMin, out float[] max, out float[] mid, out float[] min)
        {
            float s11, s22, s33, s12, s23, s31;

            float[] s0 = new float[array11.Length];
            float[] s1 = new float[array11.Length];
            float[] s2 = new float[array11.Length];
            float[] s3 = new float[array11.Length];

            //
            float I1;
            float I2;
            float I3;
            //
            float sp1, sp2, sp3;
            sp1 = sp2 = sp3 = 0;
            //
            for (int i = 0; i < s1.Length; i++)
            {
                s11 = array11[i];
                s22 = array22[i];
                s33 = array33[i];
                s12 = array12[i];
                s23 = array23[i];
                s31 = array31[i];
                //
                I1 = s11 + s22 + s33;
                I2 = s11 * s22 + s22 * s33 + s33 * s11 - s12 * s12 - s23 * s23 - s31 * s31;
                I3 = s11 * s22 * s33 - s11 * s23 * s23 - s22 * s31 * s31 - s33 * s12 * s12 + 2f * s12 * s23 * s31;
                //
                SolveQubicEquationDepressedCubicF(1f, -I1, I2, -I3, ref sp1, ref sp2, ref sp3);

                float[] sort = new float[] { sp1, sp2, sp3 };
                Array.Sort(sort);
                sp1 = sort[2]; sp2 = sort[1]; sp3 = sort[0];

                //
                s0[i] = Math.Abs(sp1) > Math.Abs(sp3) ? sp1 : sp3;
                s1[i] = sp1;
                s2[i] = sp2;
                s3[i] = sp3;
                //
                if (float.IsNaN(s0[i])) s0[i] = 0;
                if (float.IsNaN(s1[i])) s1[i] = 0;
                if (float.IsNaN(s2[i])) s2[i] = 0;
                if (float.IsNaN(s3[i])) s3[i] = 0;
            }

            signedMaxMin = s0;
            max = s1;
            mid = s2;
            min = s3;
        }

        public static double CrossSectionArea(IEnumerable<Point3d> polygon)
        {
            var X = polygon.Select(point => point.X).ToArray();
            var Y = polygon.Select(point => point.Y).ToArray();

            var N = X.Length;
            double A = 0;

            for (int i = 0; i < N - 1; ++i)
            {
                A += X[i] * Y[i + 1] - X[i + 1] * Y[i];
            }
            return A * 0.5;
        }

        public static Point3d CrossSectionCentroid(IEnumerable<Point3d> polygon)
        {
            var x = polygon.Select(point => point.X).ToArray();
            var y = polygon.Select(point => point.Y).ToArray();

            var N = y.Length;
            double A = 0;
            double cx = 0, cy = 0;

            for (int i = 0; i < N - 1; ++i)
            {
                A += x[i] * y[i + 1] - x[i + 1] * y[i];
                cx += (x[i] + x[i + 1]) * (x[i] * y[i + 1] - x[i + 1] * y[i]);
                cy += (y[i] + y[i + 1]) * (x[i] * y[i + 1] - x[i + 1] * y[i]);
            }

            A *= 0.5;
            cx /= 6 * A;
            cy /= 6 * A;

            return new Point3d(cx, cy, 0);
        }

        public static void CrossSectionInertia(IEnumerable<Point3d> polygon, out double Ixx, out double Iyy, out double Ixy)
        {
            var x = polygon.Select(point => point.X).ToArray();
            var y = polygon.Select(point => point.Y).ToArray();

            var N = y.Length;
            double A = 0;
            double cx = 0, cy = 0;

            for (int i = 0; i < N - 1; ++i)
            {
                A += x[i] * y[i + 1] - x[i + 1] * y[i];
                cx += (x[i] + x[i + 1]) * (x[i] * y[i + 1] - x[i + 1] * y[i]);
                cy += (y[i] + y[i + 1]) * (x[i] * y[i + 1] - x[i + 1] * y[i]);
            }

            A *= 0.5;
            cx /= 6 * A;
            cy /= 6 * A;

            double sxx = 0, syy = 0, sxy = 0;

            for (int i = 0; i < N - 1; ++i)
            {
                sxx += (Math.Pow(y[i], 2) + y[i] * y[i + 1] + Math.Pow(y[i + 1], 2)) * (x[i] * y[i + 1] - x[i + 1] * y[i]);
                syy += (Math.Pow(x[i], 2) + x[i] * x[i + 1] + Math.Pow(x[i + 1], 2)) * (x[i] * y[i + 1] - x[i + 1] * y[i]);
                sxy += (x[i] * y[i + 1] + 2 * x[i] * y[i] + 2 * x[i + 1] * y[i + 1] + x[i + 1] * y[i]) * (x[i] * y[i + 1] - x[i + 1] * y[i]);
            }

            Ixx = sxx / 12 - A * Math.Pow(cy, 2);
            Iyy = syy / 12 - A * Math.Pow(cx, 2);
            Ixy = sxy / 24 - A * cx * cy;
        }

        public static void PrincipalDirections(double Ixx, double Iyy, double Ixy, out double I1, out double I2, out double theta)
        {
            double avg = (Ixx + Iyy) / 2;
            double diff = (Ixx - Iyy) / 2;
            double ddii = Math.Sqrt(diff * diff + Ixy * Ixy);
            I1 = avg + ddii;
            I2 = avg - ddii;
            theta = Math.Atan2(-Ixy, diff) / 2;
        }

    }
}
