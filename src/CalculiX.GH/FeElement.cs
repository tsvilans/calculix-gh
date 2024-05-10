using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculiX
{
    public enum GmshType
    {
        Line1 = 1,
        Triangle1 = 2,
        Quadrangle1 = 3,
        Tetrahedron1 = 4,
        Hexahedron1 = 5,
        Prism1 = 6,
        Pyramid1 = 7,
        Line2 = 8,
        Triangle2 = 9,
        Quadrangle2 = 10,
        Tetrahedron2 = 11,
        Hexahedron2 = 12,
        Prism2 = 13,
        Pyramid2 = 14,
        Point1 = 15,
        Quadrangle2R = 16,
        Hexahedron2R = 17, 
        Prism2R = 18,
        Pyramid2R = 19
    }
    public abstract class FeElement
    {
        public int Id;
        public int[] Indices;
        public abstract string Type { get; }
        public abstract GmshType GmshType { get; }

        public override string ToString()
        {
            return $"{Type} ({Id})";
        }

        public virtual int[][] GetFaceIndices()
        {
            throw new NotImplementedException("Element type has no faces.");
        }
    }


public class ElementB31 : FeElement
    {
        public override string Type
        {
            get { return "B31"; }
        }

        public override GmshType GmshType => GmshType.Line1;

        public ElementB31(int id, int a, int b)
        {
            Id = id;
            Indices = new int[] { a, b };
        }
    }

    public class ElementB32 : FeElement
    {
        public override string Type
        {
            get { return "B32"; }
        }

        public override GmshType GmshType => GmshType.Line2;

        public ElementB32(int id, int a, int b, int c)
        {
            Id = id;
            Indices = new int[] { a, b, c };
        }
    }

    // First-order triangle
    public class ElementS3 : FeElement
    {
        public override string Type
        {
            get { return "S3"; }
        }

        public override GmshType GmshType => GmshType.Prism1;

        public ElementS3(int id, int[] indices)
        {
            if (indices.Length != 3) throw new Exception("S3 element needs 3 nodes!");
            Id = id;
            Indices = new int[3];
            Array.Copy(indices, Indices, Indices.Length);
        }

        public ElementS3(int id, int a, int b, int c)
        {
            Id = id;
            Indices = new int[] { a, b, c };
        }

        public override int[][] GetFaceIndices()
        {
            return new int[][]
            {
                new int[]{Indices[0], Indices[1], Indices[2] }
            };
        }
    }

    // Second-order triangle
    public class ElementS6 : FeElement
    {
        public override string Type
        {
            get { return "S6"; }
        }

        public override GmshType GmshType => GmshType.Triangle2;

        public ElementS6(int id, int[] indices)
        {
            if (indices.Length != 6) throw new Exception($"{Type} element needs 6 nodes!");
            Id = id;
            Indices = new int[6];
            Array.Copy(indices, Indices, Indices.Length);
        }

        public override int[][] GetFaceIndices()
        {
            return new int[][]
            {
                new int[]{Indices[0], Indices[1], Indices[2] }
            };
        }
    }

    // First-order quadrangle
    public class ElementS4 : FeElement
    {
        public override string Type
        {
            get { return "S4"; }
        }

        public override GmshType GmshType => GmshType.Quadrangle1;

        public ElementS4(int id, int[] indices)
        {
            if (indices.Length != 4) throw new Exception($"{Type} element needs 4 nodes!");
            Id = id;
            Indices = new int[4];
            Array.Copy(indices, Indices, Indices.Length);
        }

        public ElementS4(int id, int a, int b, int c, int d)
        {
            Id = id;
            Indices = new int[] { a, b, c, d };
        }

        public override int[][] GetFaceIndices()
        {
            return new int[][]
            {
                new int[]{Indices[0], Indices[1], Indices[2], Indices[4] }
            };
        }
    }

    // Second-order quadrangle
    public class ElementS8 : FeElement
    {
        public override string Type
        {
            get { return "S8"; }
        }

        public override GmshType GmshType => GmshType.Quadrangle2R;

        public ElementS8(int id, int[] indices)
        {
            if (indices.Length != 8) throw new Exception($"{Type} element needs 6 nodes!");
            Id = id;
            Indices = new int[8];
            Array.Copy(indices, Indices, Indices.Length);
        }

        public override int[][] GetFaceIndices()
        {
            return new int[][]
            {
                new int[]{Indices[0], Indices[1], Indices[2], Indices[3], Indices[4], Indices[5], Indices[6], Indices[7] }
            };
        }
    }

    // First-order wedge
    public class ElementC3D6 : FeElement
    {
        public override string Type
        {
            get { return "C3D6"; }
        }

        public override GmshType GmshType => GmshType.Prism1;

        public ElementC3D6(int id, int[] indices)
        {
            if (indices.Length != 6) throw new Exception("C3D6 element needs 6 nodes!");
            Id = id;
            Indices = new int[6];
            Array.Copy(indices, Indices, Indices.Length);
        }

        public override int[][] GetFaceIndices()
        {
            return new int[][]
            {
                new int[]{Indices[0], Indices[2], Indices[1] },
                new int[]{Indices[3], Indices[4], Indices[5] },
                new int[]{Indices[1], Indices[4], Indices[3], Indices[0] },
                new int[]{Indices[2], Indices[5], Indices[4], Indices[1] },
                new int[]{Indices[0], Indices[3], Indices[5], Indices[2] }
            };
        }
    }

    // Second-order wedge
    public class ElementC3D15 : FeElement
    {
        public override string Type
        {
            get { return "C3D6"; }
        }

        public override GmshType GmshType => GmshType.Prism2;

        public ElementC3D15(int id, int[] indices)
        {
            if (indices.Length != 15) throw new Exception("C3D15 element needs 15 nodes!");
            Id = id;
            Indices = new int[15];
            Array.Copy(indices, Indices, Indices.Length);
        }

        public override int[][] GetFaceIndices()
        {
            return new int[][]
            {
                new int[]{Indices[0], Indices[2], Indices[1], Indices[8], Indices[7], Indices[6] },
                new int[]{Indices[3], Indices[4], Indices[5], Indices[9], Indices[10], Indices[11] },
                new int[]{Indices[0], Indices[1], Indices[4], Indices[3], 
                    Indices[6], Indices[13], Indices[9], Indices[12] },
                new int[]{Indices[1], Indices[2], Indices[5], Indices[4],
                    Indices[7], Indices[14], Indices[10], Indices[13]},
                new int[]{Indices[2], Indices[0], Indices[3], Indices[5],
                    Indices[8], Indices[12], Indices[11], Indices[14]}
            };
        }
    }
    // First-order tetra
    public class ElementC3D4 : FeElement
    {
        public override string Type
        {
            get { return "C3D10"; }
        }

        public override GmshType GmshType => GmshType.Tetrahedron1;

        public ElementC3D4(int id, int[] indices)
        {
            if (indices.Length != 4) throw new Exception("C3D4 element needs 4 nodes!");
            Id = id;
            Indices = new int[4];
            Array.Copy(indices, Indices, Indices.Length);
        }

        public ElementC3D4(int id, int a, int b, int c, int d)
        {
            Id = id;
            Indices = new int[] { a, b, c, d };
        }

        public override int[][] GetFaceIndices()
        {
            return new int[][]
            {
                new int[]{Indices[0], Indices[2], Indices[1] },
                new int[]{Indices[0], Indices[1], Indices[3] },
                new int[]{Indices[1], Indices[2], Indices[3] },
                new int[]{Indices[2], Indices[0], Indices[3] }
            };
        }
    }

    // Second-order tetra
    public class ElementC3D10 : FeElement
    {
        public override string Type
        {
            get { return "C3D10"; }
        }

        public override GmshType GmshType => GmshType.Tetrahedron2;

        public ElementC3D10(int id, int[] indices)
        {
            if (indices.Length != 10) throw new Exception("C3D10 element needs 10 nodes!");
            Id = id;
            Indices = new int[10];
            Array.Copy(indices, Indices, Indices.Length);
        }

        public override int[][] GetFaceIndices()
        {
            return new int[][]
            {
                new int[]{Indices[0], Indices[2], Indices[1], Indices[6], Indices[5], Indices[4] },
                new int[]{Indices[0], Indices[1], Indices[3], Indices[4], Indices[8], Indices[7] },
                new int[]{Indices[1], Indices[2], Indices[3], Indices[5], Indices[9], Indices[8] },
                new int[]{Indices[2], Indices[0], Indices[3], Indices[6], Indices[7], Indices[9] }
            };
        }
    }

    // First-order brick
    public class ElementC3D8 : FeElement
    {
        public override string Type
        {
            get { return "C3D8"; }
        }

        public override GmshType GmshType => GmshType.Hexahedron1;

        public ElementC3D8(int id, int[] indices)
        {
            if (indices.Length != 8) throw new Exception("C3D8 element needs 8 nodes!");
            Id = id;
            Indices = new int[8];
            Array.Copy(indices, Indices, Indices.Length);
        }

        public override int[][] GetFaceIndices()
        {
            return new int[][]
            {
                new int[]{Indices[0], Indices[3], Indices[2], Indices[1] },
                new int[]{Indices[4], Indices[5], Indices[6], Indices[7] },
                new int[]{Indices[0], Indices[1], Indices[5], Indices[4] },
                new int[]{Indices[1], Indices[2], Indices[6], Indices[5] },
                new int[]{Indices[2], Indices[3], Indices[7], Indices[6] },
                new int[]{Indices[3], Indices[0], Indices[4], Indices[7] }
            };
        }

    }

    // Second-order brick
    public class ElementC3D20 : FeElement
    {
        public override string Type
        {
            get { return "C3D20"; }
        }

        public override GmshType GmshType => GmshType.Hexahedron2R;

        public ElementC3D20(int id, int[] indices)
        {
            if (indices.Length != 20) throw new Exception("C3D20 element needs 8 nodes!");
            Id = id;
            Indices = new int[20];
            Array.Copy(indices, Indices, Indices.Length);
        }

        public override int[][] GetFaceIndices()
        {
            return new int[][]
            {
                new int[]{Indices[0], Indices[3], Indices[2], Indices[1],
                    Indices[11], Indices[10], Indices[9], Indices[8] },
                new int[]{Indices[4], Indices[5], Indices[6], Indices[7],
                    Indices[12], Indices[13], Indices[14], Indices[15]},
                new int[]{Indices[0], Indices[1], Indices[5], Indices[4],
                    Indices[8], Indices[17], Indices[12], Indices[16]},
                new int[]{Indices[1], Indices[2], Indices[6], Indices[5],
                    Indices[9], Indices[18], Indices[13], Indices[17],},
                new int[]{Indices[2], Indices[3], Indices[7], Indices[6],
                    Indices[10], Indices[19], Indices[14], Indices[18]},
                new int[]{Indices[3], Indices[0], Indices[4], Indices[7],
                    Indices[11], Indices[16], Indices[15], Indices[19]}
            };
        }
    }

    public class ElementSPRING2: FeElement
    {
        public override string Type => "SPRING2";

        public override GmshType GmshType => GmshType.Line1;

        public ElementSPRING2(int id, int[] indices)
        {
            if (indices.Length != 2) throw new Exception("SPRING2 element needs 2 nodes!");
            Id = id;
            Indices = new int[2];
            Array.Copy(indices, Indices, Indices.Length);
        }

        public ElementSPRING2(int id, int a, int b)
        {
            Id = id;
            Indices = new int[] { a, b};
        }
    }

}
