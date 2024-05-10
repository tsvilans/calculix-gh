using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculiX.GH
{
    public abstract class GenericSet
    {
        public string Name;
        public List<int> Tags;

        public GenericSet(string name, List<int> tags)
        {
            Name = name;
            Tags = tags;
        }

        public GenericSet(string name)
        {
            Name = name;
            Tags = new List<int>();
        }
    }

    public class NodeSet : GenericSet
    {
        public NodeSet(string name, List<int> tags) : base(name, tags)
        { 
        }

        public NodeSet(string name) : base(name)
        {
        }

        public override string ToString()
        {
            return $"NodeSet (\"{Name}\", {Tags.Count} nodes)";
        }
    }

    public class ElementSet : GenericSet
    {
        public ElementSet(string name, List<int> tags) : base(name, tags)
        {
        }

        public ElementSet(string name) : base(name)
        {
        }

        public override string ToString()
        {
            return $"ElementSet (\"{Name}\", {Tags.Count} elements)";
        }
    }
}
