using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculiX.GH
{
    /// <summary>
    /// Order of elements doesn't matter.
    /// </summary>
    public class CompareIntArraySlow : IEqualityComparer<int[]>
    {
        public bool Equals(int[] x, int[] y)
        {
            if (x.Length != y.Length) return false;

            // More expensive version that avoids sorting the input arrays
            var xx = new int[x.Length]; 
            var yy = new int[y.Length];

            Array.Copy(x, xx, x.Length);
            Array.Copy(y, yy, y.Length);

            Array.Sort(xx);
            Array.Sort(yy);

            for (int i = 0; i < xx.Length; i++)
            {
                if (xx[i] != yy[i]) return false;
            }
            return true;
        }

        public int GetHashCode(int[] x)
        {
            unchecked
            {
                int hc = -1817952719;
                hc = (-1521134295) * hc + x[0];
                hc = (-1521134295) * hc + x[1];
                hc = (-1521134295) * hc + x[2];
                return hc;
            }

            int hash = 23;
            for (int i = 0; i < x.Length; i++)
            {
                hash = hash * 31 + x[i];
            }
            return hash;
        }
    }

    /// <summary>
    /// Requires arrays to be sorted for equality.
    /// </summary>
    public class CompareIntArray : IEqualityComparer<int[]>
    {
        public bool Equals(int[] x, int[] y)
        {
            if (x.Length != y.Length) return false;

            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i]) return false;
            }
            return true;
        }

        public int GetHashCode(int[] x)
        {
            unchecked
            {
                int hc = -1817952719;
                hc = (-1521134295) * hc + x[0];
                hc = (-1521134295) * hc + x[1];
                hc = (-1521134295) * hc + x[2];
                return hc;
            }

            int hash = 23;
            for (int i = 0; i < x.Length; i++)
            {
                hash = hash * 31 + x[i];
            }
            return hash;
        }
    }
}
