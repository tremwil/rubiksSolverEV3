using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;

namespace RubiksSolveEV3
{
    class Posterize3GrayScale
    {
        public byte turnPoint1;
        public byte turnPoint2;

        public Posterize3GrayScale()
        {
            turnPoint1 = 85;
            turnPoint2 = 171;
        }

        public Posterize3GrayScale(byte turnPt1, byte turnPt2)
        {
            turnPoint1 = turnPt1; turnPoint2 = turnPt2;
        }

        private byte postFunc1Channel(byte src)
        {
            if (src < turnPoint1) return 0;
            else if (src < turnPoint2) return 128;
            else return 255;
        }

        private Color postFuncFullColor(Color c)
        {
            return Color.FromArgb(
                c.A,
                postFunc1Channel(c.R),
                postFunc1Channel(c.G),
                postFunc1Channel(c.B)
            );
        }

        public void ApplyInPlace(UnmanagedImage image)
        {
            Color col;

            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    col = image.GetPixel(x, y);
                    // Grayscale
                    if (image.PixelFormat == PixelFormat.Format8bppIndexed)
                        image.SetPixel(x, y, postFunc1Channel(col.R));

                    // Normal
                    else image.SetPixel(x, y, postFuncFullColor(col));
                }
            }
        }

        public UnmanagedImage Apply(UnmanagedImage source)
        {
            // Copy source to new img
            UnmanagedImage img = source.Clone();

            //apply in place, then return
            ApplyInPlace(img);
            return img;
        }
    }
}
