using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculiX.GH
{
    public class ReferencePoint
    {
        public string Name;
        public Point3d Location;
        public string NodeSet;

        /// <summary>
        /// These must be set while writing the export file, as the reference
        /// point will be added as a node after all other nodes, therefore
        /// the IDs are unknown.
        /// </summary>
        internal int RefNodeId = -1;
        internal int RotNodeId = -1;

        public ReferencePoint(string name, Point3d location, string nodeSet)
        {
            Name = name;
            Location = location;
            NodeSet = nodeSet;
        }

        public override string ToString()
        {
            return $"ReferencePoint (\"{Name}\", {NodeSet})";
        }
    }
}
