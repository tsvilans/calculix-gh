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
    public class GH_FeBoundaryCondition : GH_Goo<BoundaryCondition>
    {
        public GH_FeBoundaryCondition(BoundaryCondition bc)
            : base(bc)
        {

        }

        public override bool IsValid => Value != null;

        public override string TypeName => Value.ToString();

        public override string TypeDescription => "A Boundary Condition for an FE model.";

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
            if (typeof(Q).IsAssignableFrom(typeof(BoundaryCondition)))
            {
                object results = Value;

                target = (Q)results;
                return true;
            }
            return base.CastTo(ref target);
        }
    }
}
