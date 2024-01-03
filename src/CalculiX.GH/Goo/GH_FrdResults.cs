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
    public class GH_FrdResults : GH_Goo<FrdResults>
    {
        public GH_FrdResults(FrdResults results) 
            : base(results)
        {

        }

        public override bool IsValid => Value != null;

        public override string TypeName => "FrdResults";

        public override string TypeDescription => "Results from a CalculiX simulation.";

        public override IGH_Goo Duplicate()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"FrdResults ({Value.Nodes.Count} nodes, {Value.Elements.Count} elements)";
        }

        public override bool CastTo<Q>(ref Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(FrdResults)))
            {
                object results = Value;

                target = (Q)results;
                return true;
            }
            return base.CastTo(ref target);
        }
    }
}
