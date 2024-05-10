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
    public class GH_FeSection : GH_Goo<Section>
    {
        public GH_FeSection(Section section)
            : base(section)
        {

        }

        public override bool IsValid => Value != null;

        public override string TypeName => Value.ToString();

        public override string TypeDescription => "An FE section.";

        public override IGH_Goo Duplicate()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return Value.ToString();
            //return $"Load (\"{Value.ToString}\", {Value.Tags.Count} nodes)";
        }

        public override bool CastTo<Q>(ref Q target)
        {
            if (typeof(Q).IsAssignableFrom(typeof(Section)))
            {
                object results = Value;

                target = (Q)results;
                return true;
            }
            return base.CastTo(ref target);
        }
    }
}
