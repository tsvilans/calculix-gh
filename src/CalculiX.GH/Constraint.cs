using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculiX.GH
{
    public abstract class Constraint : IWriteable
    {
        public string Name { get; set; }
        public ReferencePoint ReferencePoint { get; set; }

        public abstract void Write(TextWriter tw);

        public Constraint()
        {

        }

    }

    public class RigidBody : Constraint
    {
        public RigidBody() { }
        public RigidBody(string name, ReferencePoint refPoint)
        {
            Name = name;
            ReferencePoint = refPoint;
        }

        public override string ToString()
        {
            return $"RigidBody (\"{Name}\", {ReferencePoint.Name})";
        }

        public override void Write(TextWriter tw)
        {
            tw.WriteLine($"*Rigid body, Nset={ReferencePoint.NodeSet}, Ref node={ReferencePoint.RefNodeId}, Rot node={ReferencePoint.RotNodeId}");
        }
    }
}
