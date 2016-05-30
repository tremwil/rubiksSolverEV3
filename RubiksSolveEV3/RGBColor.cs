using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoBrick.EV3;
using System.IO;
using System.Threading;

namespace RubiksSolveEV3
{
    struct RGBColor
    {
        public int r, g, b;

        public RGBColor(int r, int g, int b)
        {
            this.r = r;
            this.b = b;
            this.g = g;
        }

        public RGBColor(string rgb)
        {
            var sp = rgb.Split('.');
            r = Convert.ToInt32(sp[0]);
            g = Convert.ToInt32(sp[1]);
            b = Convert.ToInt32(sp[2]);
        }

        public RGBColor(IEnumerable<int> values)
        {
            var l = values.ToArray();
            r = l[0]; b = l[1]; g = l[2];
        }

        public override string ToString()
        {
            return string.Format("RGBColor({0}, {1}, {2})", r, g, b);
        }

        public static RGBColor operator +(RGBColor a, RGBColor b)
        {
            return new RGBColor(a.r + b.r, a.g + b.g, a.b + b.b);
        }

        public static RGBColor operator -(RGBColor a, RGBColor b)
        {
            return new RGBColor(
                Math.Abs(a.r - b.r),
                Math.Abs(a.g - b.g),
                Math.Abs(a.b - b.b)
           );
        }

        public IEnumerable<int> asIEnumerable()
        {
            yield return r;
            yield return g;
            yield return b;
        }

        public RGBColor normal
        {
            get
            {
                double magn = vectorMag / 255;

                return new RGBColor(
                    (int)Math.Round(r / magn),
                    (int)Math.Round(g / magn),
                    (int)Math.Round(b / magn)
                );
            }
        }

        public double magnitude
        {
            get { return (r + b + g) / 3; }
        }

        public double vectorMag
        {
            get { return Math.Sqrt(r * r + b * b + g * g); }
        }

        public System.Drawing.Color systemColorNorm
        {
            get { return (System.Drawing.Color)this; }
        }

        public static explicit operator System.Drawing.Color(RGBColor me)
        {
            RGBColor norm = me.normal;
            return System.Drawing.Color.FromArgb(norm.r, norm.g, norm.b);
        }

        public System.Drawing.Color systemColor(int ambientLvl)
        {
            throw new NotImplementedException();
        }
    }

    static class RGBExt
    {
        public static RGBColor ReadRGB(this ColorSensor me)
        {
            bool busy = true;
            while (busy)
            {
                try
                {
                    Program.ev3.FileSystem.ReadFile("/home/root/lms2012/prjs/Ev3 program/rgb.rtf", "out");
                    busy = false;
                }
                catch
                {
                    Thread.Sleep(10);
                }
            }

            string cont = File.ReadAllText("out");
            return new RGBColor(cont);
        }
    }
}
