using Grasshopper.Kernel.Types;

namespace CalculiX.GH
{
    public class GH_FeReferencePoint : GH_Goo<ReferencePoint>
    {
        public GH_FeReferencePoint(ReferencePoint reference_point) 
            : base(reference_point)
        {

        }

        public override bool IsValid => Value != null;

        public override string TypeName => Value.ToString();

        public override string TypeDescription => "A Reference Point for an FE model.";

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
            if (typeof(Q).IsAssignableFrom(typeof(ReferencePoint)))
            {
                object results = Value;

                target = (Q)results;
                return true;
            }
            return base.CastTo(ref target);
        }
    }
}
