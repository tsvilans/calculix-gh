using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculiX.GH
{
    public abstract class Gradient
    {
        public abstract System.Drawing.Color GetValue(double x);
        public System.Drawing.Color[] Stops { get; set; }

        public static Color Interpolate(Color a, Color b, double t)
        {
            return Color.FromArgb(
                ((int)(b.R * t) + (int)(a.R * (1 - t))),
                ((int)(b.G * t) + (int)(a.G * (1 - t))),
                ((int)(b.B * t) + (int)(a.B * (1 - t)))
                );
        }
    }
    public class UnsignedGradient : Gradient
    {
        public double MaxValue = 1.0;

        public UnsignedGradient(double maximum = 1.0) 
        {
            MaxValue = maximum;
        }

        public override Color GetValue(double x)
        {
            if (x <= 0) return Stops[0];
            if (x >= MaxValue) return Stops[Stops.Length - 1];

            double t = (x / MaxValue) * (Stops.Length - 1);
            int i = (int)Math.Floor(t);

            Color s0 = Stops[i], s1 = Stops[i + 1];

            double tt = t - i;

            return Interpolate(s0, s1, tt);
        }
    }

    public class SignedGradient : Gradient
    {
        public double MaxValue = 1.0;
        public double MinValue = -1.0;

        public SignedGradient(double minimum = -1.0, double maximum = 1.0)
        {
            MaxValue = maximum;
            MinValue = minimum;
        }

        public override Color GetValue(double x)
        {
            if (x <= MinValue) return Stops[0];
            if (x >= MaxValue) return Stops[Stops.Length - 1];

            double t = ((x - MinValue) / (MaxValue - MinValue)) * (Stops.Length - 1);
            int i = (int)Math.Floor(t);

            Color s0 = Stops[i], s1 = Stops[i + 1];

            double tt = t - i;

            return Interpolate(s0, s1, tt);
        }
    }
}
