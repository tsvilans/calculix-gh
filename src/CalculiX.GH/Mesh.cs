using FrdReader;
using Grasshopper;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
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
        public static Mesh CreateShellMesh(Dictionary<int, Point3d> nodes, GH_Structure<GH_Integer> faces, Dictionary<int, System.Drawing.Color> colors = null)
        {
            var mesh = new Mesh();

            var map = new Dictionary<int, int>();

            foreach (var kvp in nodes)
            {
                map[kvp.Key] = mesh.Vertices.Count;

                mesh.Vertices.Add(kvp.Value);

                if (colors != null)
                    mesh.VertexColors.Add(colors[kvp.Key]);
            }

            foreach (var branch in faces.Branches)
            {
                if (branch.Count == 3)
                {
                    int a = branch[0].Value;
                    int b = branch[1].Value;
                    int c = branch[2].Value;

                    mesh.Faces.AddFace(map[a], map[b], map[c]);
                }
                else if (branch.Count > 3)
                {
                    int a = branch[0].Value;
                    int b = branch[1].Value;
                    int c = branch[2].Value;
                    int d = branch[3].Value;

                    mesh.Faces.AddFace(map[a], map[b], map[c], map[d]);
                }
            }

            return mesh;

        }

        public static int[][] GetElementVisualizationFaces(FrdElement element)
        {
            var fi = element.Indices;
            switch(element.Type)
            {
                case (1): // B31
                    return new int[][]
                    {
                        new int[]{ fi[0], fi[1], fi[2], fi[3]},
                        new int[]{ fi[4], fi[5], fi[6], fi[7]},
                        new int[]{ fi[0], fi[4], fi[5], fi[1]},
                        new int[]{ fi[6], fi[2], fi[3], fi[7]},
                        new int[]{ fi[0], fi[3], fi[7], fi[4]},
                        new int[]{ fi[1], fi[5], fi[6], fi[2]},
                    };
                case (3): // C3D4
                    return new int[][]
                    {
                        new int[]{ fi[0], fi[1], fi[2]},
                        new int[]{ fi[1], fi[2], fi[3]},
                        new int[]{ fi[2], fi[3], fi[0]},
                        new int[]{ fi[3], fi[0], fi[1]},
                    };
                case (4): // B32
                    return new int[][]
                    {
                        // 1
                        new int[]{ fi[11], fi[0], fi[8]},
                        new int[]{ fi[8], fi[1], fi[9]},
                        new int[]{ fi[9], fi[2], fi[10]},
                        new int[]{ fi[10], fi[3], fi[11]},
                        new int[]{ fi[9], fi[11], fi[8]},
                        new int[]{ fi[11], fi[9], fi[10]},

                        // 2
                        new int[]{ fi[16], fi[4], fi[19]},
                        new int[]{ fi[19], fi[7], fi[18]},
                        new int[]{ fi[18], fi[6], fi[17]},
                        new int[]{ fi[17], fi[5], fi[16]},
                        new int[]{ fi[18], fi[16], fi[19]},
                        new int[]{ fi[16], fi[18], fi[17]},

                        // 3
                        new int[]{ fi[12], fi[0], fi[8]},
                        new int[]{ fi[8], fi[1], fi[13]},
                        new int[]{ fi[13], fi[5], fi[16]},
                        new int[]{ fi[16], fi[4], fi[12]},
                        new int[]{ fi[16], fi[8], fi[13]},
                        new int[]{ fi[8], fi[16], fi[12]},

                        // 4
                        new int[]{ fi[13], fi[1], fi[9]},
                        new int[]{ fi[9], fi[2], fi[14]},
                        new int[]{ fi[14], fi[6], fi[17]},
                        new int[]{ fi[17], fi[5], fi[13]},
                        new int[]{ fi[17], fi[9], fi[14]},
                        new int[]{ fi[9], fi[17], fi[13]},

                        // 5
                        new int[]{ fi[14], fi[2], fi[10]},
                        new int[]{ fi[10], fi[3], fi[15]},
                        new int[]{ fi[15], fi[7], fi[18]},
                        new int[]{ fi[18], fi[6], fi[14]},
                        new int[]{ fi[18], fi[10], fi[15]},
                        new int[]{ fi[10], fi[18], fi[14]},

                        // 6
                        new int[]{ fi[15], fi[3], fi[11]},
                        new int[]{ fi[11], fi[0], fi[12]},
                        new int[]{ fi[12], fi[4], fi[19]},
                        new int[]{ fi[19], fi[7], fi[15]},
                        new int[]{ fi[19], fi[11], fi[12]},
                        new int[]{ fi[11], fi[19], fi[15]},
                    };


                default:
                    return null;
            }
        }
    }
}
