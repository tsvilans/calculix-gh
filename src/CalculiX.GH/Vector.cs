using FrdReader;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculiX.GH
{
    public static partial class Utility
    {
        public static Vector3d[] GetVectors(FrdResults results, string field, string comp0, string comp1, string comp2)
        {
            if (!results.Fields.ContainsKey(field)) throw new Exception("Results don't contain " + field + " data.");

            var displacements = results.Fields[field];
            var dx = displacements[comp0];
            var dy = displacements[comp1];
            var dz = displacements[comp2];

            var vecs = new Vector3d[results.Nodes.Count];

            for (int i = 0; i < vecs.Length; ++i)
                vecs[i] = new Vector3d(dx[i], dy[i], dz[i]);

            return vecs;
        }
    }
}
