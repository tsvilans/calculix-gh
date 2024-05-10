using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using FrdReader;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace CalculiX.GH
{
    /*
    public class GH_NodeSet : GH_Goo<NodeSet>
    {
        public GH_NodeSet(NodeSet nodeSet) 
            : base(nodeSet)
        {

        }

        public override bool IsValid => Value != null;

        public override string TypeName => "NodeSet";

        public override string TypeDescription => "A NodeSet for an FE model.";

        public override IGH_Goo Duplicate()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"NodeSet (\"{Value.Name}\", {Value.Tags.Count} nodes)";
        }

        public override bool CastTo<Q>(ref Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(NodeSet)))
            {
                object results = Value;

                target = (Q)results;
                return true;
            }
            return base.CastTo(ref target);
        }
    }
    */

    public class GH_FeSet : GH_Goo<GenericSet>
    {
        public GH_FeSet(GenericSet feSet)
            : base(feSet)
        {

        }

        public override bool IsValid => Value != null;

        public override string TypeName => "FeSet";

        public override string TypeDescription => "An set for an FE model.";

        public override IGH_Goo Duplicate()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public override bool CastTo<Q>(ref Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(GenericSet)))
            {
                object results = Value;

                target = (Q)results;
                return true;
            }
            return base.CastTo(ref target);
        }
    }
    /*
    public class GH_ElementSet : GH_Goo<ElementSet>
    {
        public GH_ElementSet(ElementSet elementSet)
            : base(elementSet)
        {

        }

        public override bool IsValid => Value != null;

        public override string TypeName => "ElementSet";

        public override string TypeDescription => "An ElementSet for an FE model.";

        public override IGH_Goo Duplicate()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"ElementSet (\"{Value.Name}\", {Value.Tags.Count} elements)";
        }

        public override bool CastTo<Q>(ref Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(ElementSet)))
            {
                object results = Value;

                target = (Q)results;
                return true;
            }
            return base.CastTo(ref target);
        }
    }
    */
}
