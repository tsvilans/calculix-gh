using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculiX
{
    public abstract class FeElement
    {
        public int Id;
        public int[] Indices;
        public abstract string Type { get; }
    }

    public class ElementB31 : FeElement
    {
        public override string Type
        {
            get { return "B31"; }
        }

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

        public ElementB32(int id, int a, int b, int c)
        {
            Id = id;
            Indices = new int[] { a, b, c };
        }
    }

    class BeamSection
    {
        public string Name;
        public double Width, Height;
        public string ElementSet;
        public string Material;

        public Vector3d Direction = Vector3d.ZAxis;

        public BeamSection(string name, double width, double height, string material = "", string eset = "")
        {
            Name = name;
            Width = width;
            Height = height;
            ElementSet = eset;
            Material = material;
        }
    }

    class BeamSolver
    {
        public bool BinaryOutput = true;
        public Vector3d WindVector = Vector3d.Zero;

        public Dictionary<int, Point3d> Nodes = new Dictionary<int, Point3d>();
        public Dictionary<int, FeElement> Elements = new Dictionary<int, FeElement>();
        public Dictionary<string, int[]> ElementSets = new Dictionary<string, int[]>();
        public Dictionary<string, int[]> NodeSets = new Dictionary<string, int[]>();

        public Dictionary<string, BeamSection> Sections = new Dictionary<string, BeamSection>();

        public BeamSolver()
        {
        }

        public void Export(string outputPath, Action<string> outputDebugging)
        {
            using (StreamWriter file = new StreamWriter(outputPath))
            {
                // Write heading
                file.WriteLine("**");
                file.WriteLine("*Heading");
                file.WriteLine("Hash: 6CDwskLW, Date: 11/05/2023, Unit system: M_KG_S_C");

                // Write nodes
                file.WriteLine("**");
                file.WriteLine("** Nodes +++++++++++++++++++++++++++++++++++++++++++++++++++");
                file.WriteLine("**");

                file.WriteLine("*Node");

                foreach (var kvp in Nodes)
                {
                    file.WriteLine("{0}, {1:0.000000}, {2:0.000000}, {3:0.000000}", kvp.Key, kvp.Value.X, kvp.Value.Y, kvp.Value.Z);
                }

                var elementsB31 = Elements.Where(x => x.Value.GetType() == typeof(ElementB31)).ToArray();
                var elementsB32 = Elements.Where(x => x.Value.GetType() == typeof(ElementB32)).ToArray();

                // Write elements
                file.WriteLine("**");
                file.WriteLine("** Elements ++++++++++++++++++++++++++++++++++++++++++++++++");
                file.WriteLine("**");


                if (elementsB31.Length > 0)
                {
                    file.WriteLine("*ELEMENT, Type={0}", "B31");
                    foreach (var kvp in elementsB31)
                    {
                        var indicesString = string.Join(", ", kvp.Value.Indices);
                        file.WriteLine("{0}, {1}", kvp.Key, indicesString);
                    }
                }

                if (elementsB32.Length > 0)
                {
                    file.WriteLine("*ELEMENT, Type={0}", "B32");
                    foreach (var kvp in elementsB32)
                    {
                        var indicesString = string.Join(", ", kvp.Value.Indices);
                        file.WriteLine("{0}, {1}", kvp.Key, indicesString);
                    }
                }

                int counter = 0;

                if (true)
                {
                    file.WriteLine("**");
                    file.WriteLine("** Element normals +++++++++++++++++++++++++++++++++++++++++");
                    file.WriteLine("**");
                    file.WriteLine("*NORMAL");

                    foreach (var kvp in Elements)
                    {
                        var element = kvp.Value;
                        var axis = Nodes[element.Indices[0]] - Nodes[element.Indices[1]];

                        Vector3d binormal = Vector3d.Unset;
                        if (Math.Abs(axis * Vector3d.ZAxis) > 0.9999)
                            binormal = Vector3d.CrossProduct(Vector3d.XAxis, axis);
                        else
                            binormal = Vector3d.CrossProduct(Vector3d.ZAxis, axis);

                        var normal = Vector3d.CrossProduct(binormal, axis);

                        if (normal.IsTiny())
                        {
                            normal = Vector3d.ZAxis;
                        }

                        //normal = -axis;
                        normal.Unitize();

                        foreach (var index in element.Indices)
                        {
                            file.WriteLine("{0}, {1}, {2}, {3}, {4}", element.Id, index, normal.X, normal.Y, normal.Z);
                        }
                        //file.WriteLine("{0}, {1}, {2}, {3}, {4}", element.Id, 1, normal.X, normal.Y, normal.Z);
                        //file.WriteLine("{0}, {1}, {2}, {3}, {4}", element.Id, 2, normal.X, normal.Y, normal.Z);
                        outputDebugging(string.Format("Normal ({0}): {1:0.000000}", element.Id, normal));
                    }
                }

                file.WriteLine("**");
                file.WriteLine("** Element sets ++++++++++++++++++++++++++++++++++++++++++++");
                file.WriteLine("**");

                foreach (var eset in ElementSets)
                {
                    file.Write("*Elset, Elset={0}", eset.Key);
                    counter = 0;
                    foreach (var index in eset.Value)
                    {
                        if (counter % 10 == 0) file.WriteLine();
                        file.Write("{0}, ", index);

                        counter++;
                    }

                    file.WriteLine();
                }

                file.WriteLine("**");
                file.WriteLine("** Node sets +++++++++++++++++++++++++++++++++++++++++++++++");
                file.WriteLine("**");

                foreach (var nset in NodeSets)
                {
                    file.Write("*Nset, Nset={0}", nset.Key);
                    counter = 0;
                    foreach (var index in nset.Value)
                    {
                        if (counter % 10 == 0) file.WriteLine();
                        file.Write("{0}, ", index);
                        counter++;
                    }

                    file.WriteLine();
                }

                file.WriteLine("**");
                file.WriteLine("** Materials +++++++++++++++++++++++++++++++++++++++++++++++");
                file.WriteLine("**");

                // Hardcoded material
                file.WriteLine("*Material, Name=Wood");
                /*
                file.WriteLine("*Elastic, Type = Engineering Constants");
                file.WriteLine("9.7E+09, 4E+08, 2.2E+08, 0.35, 0.6, 0.55, 4E+08, 2.5E+08,");
                file.WriteLine("2.5E+07");
                */
                file.WriteLine("*Elastic");
                file.WriteLine("9.7E+09, 0.45");

                file.WriteLine("*Density");
                file.WriteLine("480");

                file.WriteLine("**");
                file.WriteLine("** Sections ++++++++++++++++++++++++++++++++++++++++++++++++");
                file.WriteLine("**");

                foreach (var kvp in Sections)
                {
                    var section = kvp.Value;
                    file.WriteLine("*BEAM SECTION, Material={0}, Elset={1}, Section=RECT", section.Material, section.ElementSet);
                    file.WriteLine("{0}, {1}", section.Height, section.Width);
                    file.WriteLine("{0}, {1}, {2}", section.Direction.X, section.Direction.Y, section.Direction.Z);
                }


                file.WriteLine("**");
                file.WriteLine("** Steps ++++++++++++++++++++++++++++++++++++++++++++++++");
                file.WriteLine("**");

                //file.WriteLine("*Step, NLGEOM=ON");
                file.WriteLine("*Step");

                file.WriteLine("*Static");
                file.WriteLine("**");
                file.WriteLine("**Output frequency++++++++++++++++++++++++++++++++++++++++");
                file.WriteLine("**");
                file.WriteLine("*Output, Frequency = 1");
                file.WriteLine("**");
                file.WriteLine("**Boundary conditions+++++++++++++++++++++++++++++++++++++");
                file.WriteLine("**");
                //file.WriteLine("*Boundary, op = New");
                //file.WriteLine("**Name: Fixed-1");

                file.WriteLine("*Boundary");
                file.WriteLine("Supports, 1, 3");
                //file.WriteLine("**Name: Displacement_Rotation-1");

                file.WriteLine("**");
                file.WriteLine("**Loads++++++++++++++++++++++++++++++++++++++++++++++++++ +");
                file.WriteLine("**");
                file.WriteLine("*Cload, op = New");

                //file.WriteLine("***Cload");
                //file.WriteLine("**3, 1, 1000000");
                //file.WriteLine("**3, 3, -500000");

                file.WriteLine("*Dload, op = New");

                //file.WriteLine("*Dload");
                //file.WriteLine("All, P1, 10");

                file.WriteLine("*DLOAD");
                file.WriteLine("All, GRAV, 9.81, 0.0, 0.0, -1.0");

                if (!WindVector.IsZero)
                {
                    file.WriteLine("*Dload, op = New");
                    var windDir = WindVector;
                    windDir.Unitize();

                    file.WriteLine("*DLOAD");
                    file.WriteLine("All, GRAV, {0}, {1}, {2}, {3}", WindVector.Length, windDir.X, windDir.Y, windDir.Z);

                }

                file.WriteLine("**");
                file.WriteLine("**Outputs ++++++++++++++++++++++++++++++++++++++++++++++++ +");
                file.WriteLine("**");

                if (BinaryOutput)
                {
                    file.WriteLine("*Node output");
                    file.WriteLine("RF, U");

                    file.WriteLine("*Element output");
                    file.WriteLine("S, E");
                }
                else
                {
                    file.WriteLine("*Node file");
                    file.WriteLine("RF, U");

                    file.WriteLine("*El file");
                    file.WriteLine("S, E");

                }
                file.WriteLine("**");
                file.WriteLine("**End step++++++++++++++++++++++++++++++++++++++++++++++++");
                file.WriteLine("**");
                file.WriteLine("*End step");

            }
        }
    }
}
