using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculiX.GH
{
    public abstract class WriteableProperty
    {
        public int ElementId;
        public abstract void Write(TextWriter writer);
        public abstract void Write(BinaryWriter writer);

    }

    public class ElementOrientationProperty : WriteableProperty
    {
        public Vector3d XAxis;
        public Vector3d YAxis;

        public ElementOrientationProperty() 
        { 
        }

        public override void Write(TextWriter writer)
        {
            writer.WriteLine($"{ElementId} {XAxis.X} {XAxis.Y} {XAxis.Z} {YAxis.X} {YAxis.Y} {YAxis.Z}");
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(ElementId);
            writer.Write(XAxis.X);
            writer.Write(XAxis.Y);
            writer.Write(XAxis.Z);
            writer.Write(YAxis.X);
            writer.Write(YAxis.Y);
            writer.Write(YAxis.Z);
        }

    }

    public class PropertyMap
    {
        public List<WriteableProperty> Properties = new List<WriteableProperty>();
        public void Write(string filepath)
        {
            using (FileStream stream = new FileStream(filepath, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
                {
                    writer.Write(Properties.Count);
                    for (int i = 0; i < Properties.Count; i++)
                    {
                        Properties[i].Write(writer);
                    }

                }
            }
        }
    }
}
