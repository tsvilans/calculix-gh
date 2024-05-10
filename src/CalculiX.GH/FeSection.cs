using Eto.Forms;
using Rhino.Geometry;
using Rhino.PlugIns;
using Rhino.Render.ChangeQueue;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculiX
{
    public abstract class Section
    {
        public string Name;
        public string ElementSet;
        public string Material;
        public string Orientation;

        public abstract void Write(TextWriter tw);
    }

    public class SolidSection : Section
    {
        public SolidSection(string name, string material = "", string eset = "", string orientation="")
        {
            Name = name;
            Orientation = orientation;
            ElementSet = eset;
            Material = material;
        }

        public override void Write(TextWriter tw)
        {
            tw.Write($"*Solid Section, Elset={ElementSet}, Material={Material}");
            if (!string.IsNullOrEmpty(Orientation))
                tw.Write($", Orientation={Orientation}");
            tw.WriteLine("");
        }
    }

    public class BeamSection : Section
    {
        public double Width, Height;
        public Vector3d Direction = Vector3d.ZAxis;
        public string Section;

        public BeamSection(string name, double width, double height, string material = "", string eset = "", string section = "RECT")
        {
            Name = name;
            Width = width;
            Height = height;
            ElementSet = eset;
            Material = material;
            Section = section;
        }

        public override void Write(TextWriter tw)
        {
            tw.Write($"*Beam Section, Elset={ElementSet}, Material={Material}, Section={Section}");
            if (!string.IsNullOrEmpty(Orientation)) tw.Write($", Orientation={Orientation}");
            tw.WriteLine("");

            tw.WriteLine($"{Height}, {Width}");
            tw.WriteLine($"{Direction.X}, {Direction.Y}, {Direction.Z}");
        }
    }


    public class ShellSection : Section
    {
        public double Thickness, Offset;
        public Vector3d Direction = Vector3d.ZAxis;

        public ShellSection(string name, double thickness, string material = "", string eset = "", double offset=0.0, string orientation = "")
        {
            Name = name;
            Thickness = thickness;
            ElementSet = eset;
            Material = material;
            Orientation = orientation;
            Offset = offset;
        }

        public override void Write(TextWriter tw)
        {
            tw.Write($"*Shell Section, Elset={ElementSet}, Material={Material}");

            if (!string.IsNullOrEmpty(Orientation)) tw.Write($", Orientation={Orientation}");
            if (Offset != 0.0) tw.Write($", Offset={Offset}");
            tw.WriteLine("");

            tw.WriteLine($"{Thickness}");
        }
    }
}
