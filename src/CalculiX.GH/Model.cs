using CalculiX.GH;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace CalculiX
{
    public class Surface
    {
        public string Name = "Surface";
        public Dictionary<int, List<int>> Elements;

        public Surface(string name, Dictionary<int, List<int>> elements = null)
        {
            if (elements != null)
                Elements = elements;
            else
                Elements = new Dictionary<int, List<int>>();

            Name = name;
        }

        public void Write(TextWriter tw)
        {
            tw.WriteLine($"*Surface, Name={Name}, Type=Element");
            foreach (var kvp in Elements)
            {
                tw.WriteLine($"{Name}_S{kvp.Key}, S{kvp.Key}");
            }
        }

        public override string ToString()
        {
            return $"FeSurface ({Name})";
        }
    }


    // TODO: Flesh out properly
    public class SurfaceInteraction
    {
        public string Name;
        public SurfaceInteraction(string name)
        {
            Name = name;
        }

        public void Write(TextWriter tw)
        {
            tw.WriteLine($"*Surface interaction, Name={Name}");
            tw.WriteLine($"*Surface behaviour, Pressure-overclosure=Tied");
            tw.WriteLine($"{10000000000}");
            tw.WriteLine($"*Friction");
            tw.WriteLine($"{0.1}");
        }
    }

    public class ContactPair
    {
        public string Name;
        public string Interaction;
        public string MasterSurface, SlaveSurface;

        public ContactPair(string name, string interaction, string master, string slave)
        {
            Name = name;
            Interaction = interaction;
            MasterSurface = master;
            SlaveSurface = slave;
        }

        public void Write(TextWriter tw)
        {
            tw.WriteLine($"Contact pair, Interaction={Interaction}, Type=Surface to surface");
            tw.WriteLine($"{MasterSurface}, {SlaveSurface}");
        }
    }

    public abstract class MaterialProperty
    {
        public abstract void Write(TextWriter tw);
    }

    public class Elastic : MaterialProperty
    {
        public double[] Values = new double[2];
        public Elastic(double[] values)
        {
            if (values.Length != 2) throw new Exception("Elastic property needs 2 values!");
            Array.Copy(values, Values, 2);

        }

        public override void Write(TextWriter tw)
        {
            tw.WriteLine("*Elastic");
            tw.WriteLine($"{Values[0]}, {Values[1]}");
        }
    }

    public class EngineeringConstants : MaterialProperty
    {
        public double[] Values = new double[9];
        public EngineeringConstants(double[] values) 
        {
            if (values.Length != 9) throw new Exception("Engineering Constants property needs 9 values!");
            Array.Copy(values, Values, 9);
            
        }

        public override void Write(TextWriter tw)
        {
            tw.WriteLine("*Elastic, Type=Engineering Constants");
            tw.WriteLine($"{Values[0]}, {Values[1]}, {Values[2]}, {Values[3]}, {Values[4]}, {Values[5]}, {Values[6]}, {Values[7]},");
            tw.WriteLine($"{Values[8]}");
        }
    }

    public class UserMaterial : MaterialProperty
    {
        public double[] Values = null;
        public UserMaterial(double[] values)
        {
            Values = new double[values.Length];
            Array.Copy(values, Values, values.Length);
        }

        public override void Write(TextWriter tw)
        {
            tw.WriteLine($"*User material, Constants={Values.Length}");
            for (int i = 0; i < Values.Length - 1; i++)
            {
                tw.Write($"{Values[i]}, ");
                if ((i + 1) % 8 == 0) tw.WriteLine();
            }
            tw.WriteLine($"{Values[Values.Length - 1]}");
        }
    }

    public class Density : MaterialProperty
    {
        public double Value;
        public Density(double value)
        {
            Value = value;
        }
        public override void Write(TextWriter tw)
        {
            tw.WriteLine("*Density");
            tw.WriteLine($"{Value}");
        }
    }

    public class Expansion : MaterialProperty
    {
        public double[] Values;
        public Expansion(double[] values)
        {
            Values = new double[values.Length];
            Array.Copy(values, Values, values.Length);
        }

        public override void Write(TextWriter tw)
        {
            string type = Values.Length > 1 ? "ORTHO" : "ISO";
            tw.WriteLine($"*Expansion, Zero=20, Type={type}");

            // TODO: Make this more tweakable
            tw.WriteLine($"{string.Join(", ", Values)}, 20");
            /*
            foreach (var v in Values)
            {
                tw.WriteLine($"{v}");
            }
            */
        }
    }

    public class Material
    {
        public string Name { get; set; }
        public List<MaterialProperty> Properties;
        public Material(string name)
        {
            Name = name;
            Properties = new List<MaterialProperty>();
        }
    }

    public class BoundaryCondition
    {
        public string NodeSet;
        public int DofStart;
        public int DofEnd;
        public double Value;

        public BoundaryCondition(string nset, int dofStart, int dofEnd, double value = 0)
        {
            NodeSet = nset;
            DofStart = dofStart;
            DofEnd = dofEnd;
            Value = value;
        }
    }

    public abstract class Load
    {
        public abstract void Write(TextWriter tw);
    }

    public class CLoad : Load
    {
        public string NodeSet;
        public Vector3d Force;
        public CLoad(string nset, Vector3d force)
        {
            NodeSet = nset;
            Force = force;
        }

        public override void Write(TextWriter tw)
        {
            tw.WriteLine($"*Cload");
            if (Force.X != 0)
                tw.WriteLine($"{NodeSet}, 1, {Force.X}");
            if (Force.Y != 0)
                tw.WriteLine($"{NodeSet}, 2, {Force.Y}");
            if (Force.Z != 0)
                tw.WriteLine($"{NodeSet}, 3, {Force.Z}");
        }

        public override string ToString()
        {
            return $"CLoad ({NodeSet}, {Force:0.000})";
        }
    }

    public class GravityLoad : Load
    {
        public string ElementSet;
        public Vector3d Direction;
        public double Magnitude;
        
        public GravityLoad(string eset, Vector3d force)
        {
            ElementSet = eset;
            Direction = force;
            Magnitude = force.Length;
            Direction.Unitize();

        }

        public override void Write(TextWriter tw)
        {
            tw.WriteLine($"*Dload");
            tw.WriteLine($"{ElementSet}, GRAV, {Magnitude}, {Direction.X}, {Direction.Y}, {Direction.Z}");
        }

        public override string ToString()
        {
            return $"GravityLoad ({ElementSet}, {Direction:0.000})";
        }

    }

    public abstract class InitialCondition
    {
        public abstract void Write(TextWriter tw);
    }

    public class InitialTemperature : InitialCondition
    {
        string NodeSet = "";
        double Temperature = 0;
        public InitialTemperature(string nset, double temp = 20)
        {
            NodeSet = nset;
            Temperature = temp;
        }

        public override void Write(TextWriter tw)
        {
            tw.WriteLine($"*Initial conditions, Type=Temperature");
            tw.WriteLine($"{NodeSet}, {Temperature}");
        }
    }

    public class Spring
    {
        public string ElementSet;
        public double Elasticity;

        public Spring(string eset, double elasticity)
        {
            ElementSet = eset;
            Elasticity = elasticity;
        }

        /// <summary>
        /// TODO: See CalculiX manual
        /// </summary>
        /// <param name="tw"></param>
        public void Write(TextWriter tw)
        {
            tw.WriteLine($"*Spring, Elset={ElementSet}");
            tw.WriteLine();
            tw.WriteLine($"{Elasticity}");
        }
    }

    public class Step
    {
        public List<string> NodeOutput = new List<string> { "U" };
        public List<string> ElementOutput = new List<string> { "S", "E" };
        public bool BinaryOutput = true;

        public List<BoundaryCondition> BoundaryConditions = new List<BoundaryCondition>();
        public List<Load> Loads = new List<Load>();

        public string StepType = "Static";

        public Step(bool isStatic = true) 
        {
            if (!isStatic)
                StepType = "Coupled temperature-displacement, Steady state";
        }

        public void Write(TextWriter tw)
        {
            tw.WriteLine($"*Step");
            tw.WriteLine($"*{StepType}");
            tw.WriteLine($"*Output, Frequency={1}");

            tw.WriteLine("*Boundary");
            foreach (var bc in BoundaryConditions)
            {
                tw.WriteLine($"{bc.NodeSet}, {bc.DofStart}, {bc.DofEnd}, {bc.Value}");
            }

            foreach (var load in Loads)
            {
                load.Write(tw);
            }

            if (!BinaryOutput)
                tw.WriteLine($"*Node file");
            else
                tw.WriteLine($"*Node output");

            tw.WriteLine(string.Join(", ", NodeOutput));

            if (!BinaryOutput)
                tw.WriteLine($"*El file");
            else
                tw.WriteLine($"*Element output");

            tw.WriteLine(string.Join(", ", ElementOutput));


            tw.WriteLine($"*End step");
        }
    }

    public class Model
    {
        public string Name = "Model";
        public Dictionary<int, Point3d> Nodes = new Dictionary<int, Point3d>();
        public Dictionary<int, FeElement> Elements = new Dictionary<int, FeElement>();
        public List<Tuple<int, int, Vector3d>> Normals = new List<Tuple<int, int, Vector3d>>();
        //public Dictionary<int, Tuple<int, Vector3d>> Normals = new Dictionary<int, Tuple<int, Vector3d>>();

        public Dictionary<string, Dictionary<int, Plane>> Distributions = new Dictionary<string, Dictionary<int, Plane>>();
        public Dictionary<string, string> Orientations = new Dictionary<string, string>();

        public Dictionary<string, List<int>> ElementSets = new Dictionary<string, List<int>>();
        public Dictionary<string, List<int>> NodeSets = new Dictionary<string, List<int>>();

        public List<Material> Materials = new List<Material>();
        public List<Spring> Springs = new List<Spring>();

        public List<Surface> Surfaces = new List<Surface>();
        public List<SurfaceInteraction> SurfaceInteractions = new List<SurfaceInteraction>();
        public List<ContactPair> ContactPairs = new List<ContactPair>();

        public Dictionary<string, Section> Sections = new Dictionary<string, Section>();
        public List<InitialCondition> InitialConditions = new List<InitialCondition>();

        public List<Step> Steps = new List<Step>();

        public Model(string name)
        {
            Name = name;
        }

        public void NodeSetFromMeshProximity(Mesh mesh, string nsetName, double threshold, bool additive = true)
        {
            var nset = new List<int>();
            if (additive)
                if (NodeSets.ContainsKey(nsetName))
                    nset.AddRange(NodeSets[nsetName]);

            foreach (var node in Nodes)
            {
                if (mesh.ClosestPoint(node.Value, out Point3d pom, threshold) >= 0)
                {
                    nset.Add(node.Key);
                }
            }

            if (nset.Count > 0)
                NodeSets[nsetName] = nset;
        }

        public Plane GetOrientationFor1dElement(FeElement ele, Vector3d yaxis)
        {
            Point3d p0, p1;
            p0 = Nodes[ele.Indices[0]];
            if (ele is ElementB31) p1 = Nodes[ele.Indices[1]];
            else if (ele is ElementB32) p1 = Nodes[ele.Indices[2]];
            else
                throw new Exception("Not a valid element type for this method.");

            var xaxis = p1 - p0;
            xaxis.Unitize();

            if (yaxis == Vector3d.Unset || yaxis == Vector3d.Zero)
            {
                yaxis = Vector3d.YAxis;
            }
            if (Math.Abs(yaxis * xaxis) > (1.0-1e-6))
            {
                if (Math.Abs(xaxis * Vector3d.ZAxis) < 1)
                    yaxis = Vector3d.ZAxis;
                else
                    yaxis = Vector3d.YAxis;
            }

            return new Plane(p0, xaxis, yaxis);
        }

        public void Export(string outputPath)//, Action<string> outputDebugging)
        {
            // Preprocessing
            // Add surface element sets

            // Write element sets for surfaces
            foreach (var srf in Surfaces)
            {
                foreach (var kvp in srf.Elements)
                {
                    ElementSets.Add($"{srf.Name}_S{kvp.Key}", kvp.Value);
                }
            }

            using (StreamWriter file = new StreamWriter(outputPath))
            {
                // Write heading
                file.WriteLine("**");
                file.WriteLine("*Heading");
                file.WriteLine($"Hash: 6CDwskLW, Date: {DateTime.UtcNow}, Unit system: M_KG_S_C");

                // Write nodes
                file.WriteLine("**");
                file.WriteLine("** Nodes +++++++++++++++++++++++++++++++++++++++++++++++++++");
                file.WriteLine("**");

                file.WriteLine("*Node");

                foreach (var kvp in Nodes)
                {
                    file.WriteLine("{0}, {1:0.000000}, {2:0.000000}, {3:0.000000}", kvp.Key, kvp.Value.X, kvp.Value.Y, kvp.Value.Z);
                }

                // Write elements
                file.WriteLine("**");
                file.WriteLine("** Elements ++++++++++++++++++++++++++++++++++++++++++++++++");
                file.WriteLine("**");

                var elementTypes = new Dictionary<string, List<FeElement>>();
                foreach (var element in Elements.Values)
                {
                    if (!elementTypes.ContainsKey(element.Type))
                    {
                        elementTypes[element.Type] = new List<FeElement> { element };
                    }
                    else
                        elementTypes[element.Type].Add(element);
                }

                foreach (var kvp in elementTypes)
                {
                    file.WriteLine($"*Element, Type={kvp.Key}");
                    foreach (var element in kvp.Value)
                    {
                        var indicesString = string.Join(", ", element.Indices);
                        file.WriteLine($"{element.Id}, {indicesString}");
                    }
                }

                // Write element normals
                file.WriteLine("**");
                file.WriteLine("** Normals ++++++++++++++++++++++++++++++++++++++++++++++++");
                file.WriteLine("**");

                if (Normals.Count > 0)
                {
                    file.WriteLine("*Normal");
                    foreach (var kvp in Normals)
                    {
                        file.WriteLine($"{kvp.Item1}, {kvp.Item2}, {kvp.Item3.X}, {kvp.Item3.Y}, {kvp.Item3.Z}");
                    }

                }

                // Write distributions
                file.WriteLine("**");
                file.WriteLine("** Distributions ++++++++++++++++++++++++++++++++++++++++++");
                file.WriteLine("**");

                if (Distributions.Count > 0)
                {
                    foreach(var dist in Distributions)
                    {
                        file.WriteLine($"*Distribution, Name={dist.Key}");
                        file.WriteLine(", 1, 0, 0, 0, 1, 0");

                        foreach(var pl in dist.Value)
                        {
                            file.WriteLine($"{pl.Key}, {pl.Value.XAxis.X}, {pl.Value.XAxis.Y}, {pl.Value.XAxis.Z}, {pl.Value.YAxis.X}, {pl.Value.YAxis.Y}, {pl.Value.YAxis.Z}");
                        }
                    }
                }

                // Write orientations
                file.WriteLine("**");
                file.WriteLine("** Orientations ++++++++++++++++++++++++++++++++++++++++++");
                file.WriteLine("**");

                if (Orientations.Count > 0)
                {
                    foreach (var kvp in Orientations)
                    {
                        file.WriteLine($"*Orientation, Name={kvp.Key}");
                        file.WriteLine($"{kvp.Value}");
                    }
                }

                // Write node sets
                file.WriteLine("**");
                file.WriteLine("** Node sets +++++++++++++++++++++++++++++++++++++++++++++");
                file.WriteLine("**");

                foreach (var kvp in NodeSets)
                {
                    file.WriteLine($"*NSet, Nset={kvp.Key}");
                    StringBuilder sb = new StringBuilder();
                    int count = 0;

                    //Array.Sort(kvp.Value);
                    //
                    foreach (var nodeId in kvp.Value)
                    {
                        sb.Append(nodeId);
                        if (count < kvp.Value.Count - 1)
                        {
                            sb.Append(", ");
                            if (++count % 16 == 0) sb.AppendLine();
                        }
                    }
                    sb.AppendLine();
                    file.WriteLine(sb.ToString());
                }

                // Write element sets
                file.WriteLine("**");
                file.WriteLine("** Element sets ++++++++++++++++++++++++++++++++++++++++++");
                file.WriteLine("**");

                foreach (var kvp in ElementSets)
                {
                    file.WriteLine($"*Elset, Elset={kvp.Key}");
                    StringBuilder sb = new StringBuilder();
                    int count = 0;

                    //Array.Sort(kvp.Value);
                    //
                    foreach (var nodeId in kvp.Value)
                    {
                        sb.Append(nodeId);
                        if (count < kvp.Value.Count - 1)
                        {
                            sb.Append(", ");
                            if (++count % 16 == 0) sb.AppendLine();
                        }
                    }
                    sb.AppendLine();
                    file.WriteLine(sb.ToString());
                }

                // Write springs
                if (Springs.Count > 0)
                {
                    file.WriteLine("**");
                    file.WriteLine("** Springs ++++++++++++++++++++++++++++++++++++++++++++");
                    file.WriteLine("**");
                    foreach (var spring in Springs)
                    {
                        spring.Write(file);
                    }
                }

                // Write materials
                file.WriteLine("**");
                file.WriteLine("** Materials ++++++++++++++++++++++++++++++++++++++++++");
                file.WriteLine("**");

                foreach (var material in Materials)
                {
                    file.WriteLine($"*Material, Name={material.Name}");
                    foreach(var prop in material.Properties)
                    {
                        prop.Write(file);
                    }
                }

                // Write sections
                file.WriteLine("**");
                file.WriteLine("** Sections ++++++++++++++++++++++++++++++++++++++++++");
                file.WriteLine("**");

                foreach (var section in Sections)
                {
                    section.Value.Write(file);
                }

                // Write surface interactions
                if (SurfaceInteractions.Count > 0)
                {
                    file.WriteLine("**");
                    file.WriteLine("** Surface interactions +++++++++++++++++++++++++++++++");
                    file.WriteLine("**");

                    foreach (var si in SurfaceInteractions)
                    {
                        si.Write(file);
                    }
                }

                // Write contact pairs
                if (ContactPairs.Count > 0)
                {
                    file.WriteLine("**");
                    file.WriteLine("** Contact pairs ++++++++++++++++++++++++++++++++++++++");
                    file.WriteLine("**");

                    foreach (var cp in ContactPairs)
                    {
                        cp.Write(file);
                    }
                }

                // Write initial conditions
                if (InitialConditions.Count > 0)
                {
                    file.WriteLine("**");
                    file.WriteLine("** Initial conditions ++++++++++++++++++++++++++++++++");
                    file.WriteLine("**");

                    foreach (var condition in InitialConditions)
                    {
                        condition.Write(file);
                    }
                }

                // Write steps
                file.WriteLine("**");
                file.WriteLine("** Steps +++++++++++++++++++++++++++++++++++++++++++++");
                file.WriteLine("**");

                foreach (var step in Steps)
                {
                    step.Write(file);
                }
            }
        }
    }
}
