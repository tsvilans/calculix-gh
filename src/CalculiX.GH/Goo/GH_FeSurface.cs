using Grasshopper.Kernel.Types;

namespace CalculiX.GH
{
    public class GH_FeSurface : GH_Goo<Surface>
    {
        public GH_FeSurface(Surface surface)
            : base(surface)
        {

        }

        public override bool IsValid => Value != null;

        public override string TypeName => Value.ToString();

        public override string TypeDescription => "An FE surface.";

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
            if (typeof(Q).IsAssignableFrom(typeof(Surface)))
            {
                object results = Value;

                target = (Q)results;
                return true;
            }
            return base.CastTo(ref target);
        }
    }
}
