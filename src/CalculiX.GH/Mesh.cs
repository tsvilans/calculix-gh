using FrdReader;
using Grasshopper;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CalculiX.GH
{
    public static partial class Extension
    {
        public static List<Line> ExtractCreases(this Mesh mesh, double threshold)
        {
            if (mesh.FaceNormals.Count < 1)
                mesh.RebuildNormals();

            var edges = new List<Line>();
            var edgeMap = new Dictionary<int[], List<int>>(new CompareIntArray());

            for (int i = 0; i < mesh.Faces.Count; ++i)
            {
                var face = mesh.Faces[i];

                int[][] faceEdges = null;

                if (face.IsTriangle)
                    faceEdges = new int[][]
                  {
                    new int[]{face.A, face.B},
                    new int[]{face.B, face.C},
                    new int[]{face.C, face.A}
                  };
                else
                    faceEdges = new int[][]
                  {
                    new int[]{face.A, face.B},
                    new int[]{face.B, face.C},
                    new int[]{face.C, face.D},
                    new int[]{face.D, face.A}
                  };

                for (int j = 0; j < faceEdges.Length; ++j)
                {
                    Array.Sort(faceEdges[j]);

                    if (!edgeMap.ContainsKey(faceEdges[j]))
                    {
                        edgeMap[faceEdges[j]] = new List<int>();
                    }

                    edgeMap[faceEdges[j]].Add(i);
                }
            }

            foreach (var kvp in edgeMap)
            {
                var v0 = mesh.Vertices[kvp.Key[0]];
                var v1 = mesh.Vertices[kvp.Key[1]];

                var fi = kvp.Value;
                if (fi.Count == 1)
                {
                    edges.Add(new Line(v0, v1));
                }
                else if (fi.Count == 2)
                {
                    var n0 = mesh.FaceNormals[fi[0]];
                    var n1 = mesh.FaceNormals[fi[1]];

                    var dot = n0 * n1;

                    if (Math.Acos(Math.Abs(dot)) > threshold)
                    {
                        edges.Add(new Line(v0, v1));
                    }
                }
                else if (fi.Count > 2)
                    throw new Exception("Non-manifold faces detected!");
            }

            return edges;

        }
    }

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

        public static Mesh CreateShellMesh(Dictionary<int, Point3d> nodes, List<int[]> faces, Dictionary<int, System.Drawing.Color> colors = null)
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

            foreach (var face in faces)
            {
                if (face.Length == 3)
                {
                    int a = face[0];
                    int b = face[1];
                    int c = face[2];

                    mesh.Faces.AddFace(map[a], map[b], map[c]);
                }
                else if (face.Length > 3)
                {
                    int a = face[0];
                    int b = face[1];
                    int c = face[2];
                    int d = face[3];

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
                        new int[]{ fi[9], fi[10], fi[8]}, //new int[]{ fi[9], fi[11], fi[8]},
                        new int[]{ fi[11], fi[8], fi[10]}, //new int[]{ fi[11], fi[9], fi[10]},

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
                case (6): // C3D10
                    return new int[][]
                    {
                        new int[]{ fi[0], fi[4], fi[7]},
                        new int[]{ fi[4], fi[1], fi[8]},
                        new int[]{ fi[8], fi[3], fi[7]},
                        new int[]{ fi[7], fi[4], fi[8]},

                        new int[]{ fi[1], fi[5], fi[8]},
                        new int[]{ fi[5], fi[2], fi[9]},
                        new int[]{ fi[9], fi[3], fi[8]},
                        new int[]{ fi[8], fi[5], fi[9]},

                        new int[]{ fi[2], fi[6], fi[9]},
                        new int[]{ fi[6], fi[0], fi[7]},
                        new int[]{ fi[7], fi[3], fi[9]},
                        new int[]{ fi[9], fi[6], fi[7]},

                        new int[]{ fi[4], fi[0], fi[6]},
                        new int[]{ fi[6], fi[2], fi[5]},
                        new int[]{ fi[5], fi[1], fi[4]},
                        new int[]{ fi[4], fi[6], fi[5]},
                    };

                case (12): // 27-node hex
                    return new int[][]
                    {
                        new int[]{ fi[0], fi[8], fi[20], fi[9] },
                        new int[] { fi[8], fi[1], fi[11], fi[20] },
                        new int[] { fi[11], fi[2], fi[13], fi[20] },
                        new int[] { fi[13], fi[3], fi[9], fi[20] },

                        new int[] { fi[0], fi[10], fi[21], fi[8] },
                        new int[] { fi[10], fi[4], fi[16], fi[21] },
                        new int[] { fi[16], fi[5], fi[12], fi[21] },
                        new int[] { fi[12], fi[1], fi[8], fi[21] },

                        new int[] { fi[4], fi[17], fi[25], fi[16] },
                        new int[] { fi[17], fi[7], fi[19], fi[25] },
                        new int[] { fi[19], fi[6], fi[18], fi[25] },
                        new int[] { fi[18], fi[5], fi[16], fi[25] },

                        new int[] { fi[7], fi[15], fi[24], fi[19] },
                        new int[] { fi[15], fi[3], fi[13], fi[24] },
                        new int[] { fi[13], fi[2], fi[14], fi[24] },
                        new int[] { fi[14], fi[6], fi[19], fi[24] },

                        new int[] { fi[0], fi[9], fi[22], fi[10] },
                        new int[] { fi[9], fi[3], fi[15], fi[22] },
                        new int[] { fi[15], fi[7], fi[17], fi[22] },
                        new int[] { fi[17], fi[4], fi[10], fi[22] },

                        new int[] { fi[12], fi[5], fi[18], fi[23] },
                        new int[] { fi[18], fi[6], fi[14], fi[23] },
                        new int[] { fi[14], fi[2], fi[11], fi[23] },
                        new int[] { fi[11], fi[1], fi[12], fi[23] }
                    };
                default:
                    return new int[][]
                    {
                        new int[]{ }
                    };
            }
        }

        public static int[][] GetElementVisualizationFaces(int[] tags, int elementType)
        {
            var fi = tags;
            switch (elementType)
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
                        new int[]{ fi[9], fi[10], fi[8]}, //new int[]{ fi[9], fi[11], fi[8]},
                        new int[]{ fi[11], fi[8], fi[10]}, //new int[]{ fi[11], fi[9], fi[10]},

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
                case (6): // C3D10
                    return new int[][]
                    {
                        new int[]{ fi[0], fi[4], fi[7]},
                        new int[]{ fi[4], fi[1], fi[8]},
                        new int[]{ fi[8], fi[3], fi[7]},
                        new int[]{ fi[7], fi[4], fi[8]},

                        new int[]{ fi[1], fi[5], fi[8]},
                        new int[]{ fi[5], fi[2], fi[9]},
                        new int[]{ fi[9], fi[3], fi[8]},
                        new int[]{ fi[8], fi[5], fi[9]},

                        new int[]{ fi[2], fi[6], fi[9]},
                        new int[]{ fi[6], fi[0], fi[7]},
                        new int[]{ fi[7], fi[3], fi[9]},
                        new int[]{ fi[9], fi[6], fi[7]},

                        new int[]{ fi[4], fi[0], fi[6]},
                        new int[]{ fi[6], fi[2], fi[5]},
                        new int[]{ fi[5], fi[1], fi[4]},
                        new int[]{ fi[4], fi[6], fi[5]},
                    };

                case (12): // 27-node hex
                    return new int[][]
                    {
                        new int[]{ fi[0], fi[8], fi[20], fi[9] },
                        new int[] { fi[8], fi[1], fi[11], fi[20] },
                        new int[] { fi[11], fi[2], fi[13], fi[20] },
                        new int[] { fi[13], fi[3], fi[9], fi[20] },

                        new int[] { fi[0], fi[10], fi[21], fi[8] },
                        new int[] { fi[10], fi[4], fi[16], fi[21] },
                        new int[] { fi[16], fi[5], fi[12], fi[21] },
                        new int[] { fi[12], fi[1], fi[8], fi[21] },

                        new int[] { fi[4], fi[17], fi[25], fi[16] },
                        new int[] { fi[17], fi[7], fi[19], fi[25] },
                        new int[] { fi[19], fi[6], fi[18], fi[25] },
                        new int[] { fi[18], fi[5], fi[16], fi[25] },

                        new int[] { fi[7], fi[15], fi[24], fi[19] },
                        new int[] { fi[15], fi[3], fi[13], fi[24] },
                        new int[] { fi[13], fi[2], fi[14], fi[24] },
                        new int[] { fi[14], fi[6], fi[19], fi[24] },

                        new int[] { fi[0], fi[9], fi[22], fi[10] },
                        new int[] { fi[9], fi[3], fi[15], fi[22] },
                        new int[] { fi[15], fi[7], fi[17], fi[22] },
                        new int[] { fi[17], fi[4], fi[10], fi[22] },

                        new int[] { fi[12], fi[5], fi[18], fi[23] },
                        new int[] { fi[18], fi[6], fi[14], fi[23] },
                        new int[] { fi[14], fi[2], fi[11], fi[23] },
                        new int[] { fi[11], fi[1], fi[12], fi[23] }
                    };
                default:
                    return new int[][]
                    {
                        new int[]{ }
                    };
            }
        }
    }
}
