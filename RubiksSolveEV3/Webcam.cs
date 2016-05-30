using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AForge.Video.DirectShow;
using AForge.Video;
using AForge.Imaging;
using AForge.Imaging.Filters;
using ColorMine.ColorSpaces;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using AForge.Math.Geometry;
using AForge;

namespace RubiksSolveEV3
{
    class Webcam
    {
        public static FilterInfoCollection devices = 
            new FilterInfoCollection(FilterCategory.VideoInputDevice);

        public const string deviceName = "LifeCam HD-3000";

        private VideoCaptureDevice videoDevice;
        private Bitmap lastFrame;

        public bool isRunning
        { get { return videoDevice.IsRunning; } }

        public Webcam()
        {
            var tgtDevice = devices.Cast<FilterInfo>().Where(x => x.Name.Contains(deviceName)).First();

            videoDevice = new VideoCaptureDevice(tgtDevice.MonikerString);
            videoDevice.NewFrame += onNewFrame;

            lastFrame = null;
        }

        private void onNewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            lastFrame = (Bitmap)eventArgs.Frame.Clone();
        }

        public void start() { videoDevice.Start(); }
        public void stop() { videoDevice.Stop(); }

        public Bitmap takePicture()
        {
            bool wasRunning = videoDevice.IsRunning;
            if (wasRunning && lastFrame != null) return (Bitmap)lastFrame.Clone();

            if (!wasRunning) videoDevice.Start();

            lastFrame = null;
            while (lastFrame == null) Thread.Sleep(15);

            if (!wasRunning) videoDevice.Stop();

            return (Bitmap)lastFrame.Clone();
        }

        public void calibrateExposure()
        {
            // Set auto
            videoDevice.SetCameraProperty(CameraControlProperty.Exposure, -6, CameraControlFlags.Auto);
            Thread.Sleep(3000);
        }

        public static Webcam Instance = new Webcam();
    }

    static class RubiksColorCompare
    {
        static ResizeBilinear resize = new ResizeBilinear(320, 240);
        static Threshold thresholder = new Threshold(1);
        static Grayscale grayscale = Grayscale.CommonAlgorithms.BT709;
        static ConservativeSmoothing smooth = new ConservativeSmoothing();
        static CannyEdgeDetector detect = new CannyEdgeDetector();
        static ContrastCorrection contrast = new ContrastCorrection(50);
        static FillHoles holefill = new FillHoles();


        //static BlobsFiltering filter = new BlobsFiltering(100, 100, int.MaxValue, int.MaxValue);
        static HoughLineTransformation lineDetector = new HoughLineTransformation();

        /// <summary>
        /// Divisor-signed mod function.
        /// </summary>
        /// <param name="me">Dividend</param>
        /// <param name="n">Divisor</param>
        /// <returns>The modulus (a % n)</returns>
        public static int divmod(this int me, int n)
        {
            return (me % n + n) % n;
        }

        public static float divmod(this float me, float n)
        {
            return (float)(me - Math.Floor(me / n) * n);
        }

        public static double divmod(this double me, double n)
        {
            return me - Math.Floor(me / n) * n;
        }

        /// <summary>
        /// Returns the angle (in degrees) from source to target
        /// </summary>
        /// <param name="source">The start angle</param>
        /// <param name="target">The target angle</param>
        /// <returns>The signed angle between source and target</returns>
        public static double deltaAngle(double source, double target)
        {
            return divmod(target - source + 180, 360) - 180;
        }

        public static bool isDiagonal(double deg, double sensivity)
        {
            var checkClose = new int[4] { 45, 135, 225, 315 };
            var dist = 45 * (1 - sensivity).clamp(0, 1);

            foreach (int v in checkClose)
                if (Math.Abs(deltaAngle(v, deg)) < dist)
                    return true;

            return false;
        }

        public static double clamp(this double me, double min, double max)
        {
            return Math.Min(Math.Max(min, me), max);
        }

        public static Hsb[] processCubePicture(Bitmap img)
        {
            int i, j, k;

            // make image unmanaged
            var lockRect = new Rectangle(0, 0, img.Width, img.Height);
            BitmapData dat = img.LockBits(lockRect, ImageLockMode.ReadOnly, img.PixelFormat);
            UnmanagedImage uimg = new UnmanagedImage(dat);

            // Resize image (divide size by 2) for 4x performance
            uimg = resize.Apply(uimg);

            #region get centers

            // Make grayscale & smooth
            smooth.ApplyInPlace(uimg);
            var uimg_gray = grayscale.Apply(uimg);
            contrast.ApplyInPlace(uimg_gray);
            // Run edge detect and make 2-color
            thresholder.ApplyInPlace(uimg_gray);
            var thresh = detect.Apply(uimg_gray);
            uimg_gray.ToManagedImage().Save("Snapshot_noprocess.png", ImageFormat.Png);
            // Get 50 possible lines
            lineDetector.ProcessImage(thresh);
            HoughLine[] lines = lineDetector.GetMostIntensiveLines(50);

            // Prepare the capture
            var manage_thresh_rgb = thresh.ToManagedImage().Clone(new Rectangle(0, 0, 320, 240), PixelFormat.Format24bppRgb);
            var thresh_rgb = new UnmanagedImage(
                manage_thresh_rgb.LockBits(
                    new Rectangle(0, 0, 320, 240), 
                    ImageLockMode.ReadWrite, 
                    PixelFormat.Format24bppRgb
            ));

            List<LineSegment> segs = new List<LineSegment>();
            List<HoughLine> segLines = new List<HoughLine>();

            // Set lines to segments
            foreach (HoughLine line in lines)
            {
                // get line's radius and theta values
                int r = line.Radius;
                double t = line.Theta;

                // check if line is in lower part of the image
                if (r < 0)
                {
                    t += 180;
                    r = -r;
                }

                // No diagonals lines to be accepted
                if (isDiagonal(t, 0.08)) continue;

                // convert degrees to radians
                t = (t / 180) * Math.PI;

                // get image centers (all coordinate are measured relative
                // to center)
                int w2 = thresh_rgb.Width / 2;
                int h2 = thresh_rgb.Height / 2;

                double x0 = 0, x1 = 0, y0 = 0, y1 = 0;

                if (line.Theta != 0)
                {
                    // none-vertical line
                    x0 = -w2; // most left point
                    x1 = w2;  // most right point

                    // calculate corresponding y values
                    y0 = (-Math.Cos(t) * x0 + r) / Math.Sin(t);
                    y1 = (-Math.Cos(t) * x1 + r) / Math.Sin(t);
                }
                else
                {
                    // vertical line
                    x0 = line.Radius;
                    x1 = line.Radius;

                    y0 = h2;
                    y1 = -h2;
                }

                var p1 = new IntPoint((int)x0 + w2, h2 - (int)y0);
                var p2 = new IntPoint((int)x1 + w2, h2 - (int)y1);

                // draw line on the image & add it to segs and segLines
                Drawing.Line(thresh_rgb, p1, p2, Color.Red);
                segs.Add(new LineSegment(p1, p2));
                segLines.Add(line);
            }

            List<IntPoint> intersects = new List<IntPoint>();
            List<int> avgAmount = new List<int>();
            LineSegment seg1, seg2;

            for (i = 0; i < segs.Count; i++)
            {
                seg1 = segs[i];

                for (j = 0; j < segs.Count; j++)
                {
                    if (j == i) continue; // Do not intersect with self
                    seg2 = segs[j];

                    // Calculate angle between A and B
                    var angleBetween = Math.Abs(deltaAngle(
                        segLines[i].Theta,
                        segLines[j].Theta
                    ));

                    // lines are parallel, don't intersect them
                    if (angleBetween < 20 || angleBetween > 160) continue;

                    // Get the intersection
                    var pnull = seg1.GetIntersectionWith(seg2);
                    if (!pnull.HasValue) continue;
                    var p = (IntPoint)pnull;

                    // Point is to close from sides, cannot be main
                    if (p.Y >= 210 || p.Y < 85 || p.X < 75 || p.X > 235) continue;

                    // If a close one (+-20px) exists, interpolate them
                    bool found = false;
                    for (k = 0; k < intersects.Count; k++)
                    {
                        var p2 = intersects[k];
                        if (p.SquaredDistanceTo(p2) < 400)
                        {
                            found = true;

                            var am = ++avgAmount[k];
                            intersects[k] = (p2 * (am - 1) + p) / am;
                            break;
                        }
                    }
                    if (found) continue;
                    
                    // Add the point to intersect list
                    intersects.Add(p);
                    avgAmount.Add(1);
                }
            }

            // Draw final intersects
            foreach(var pt in intersects)
            {
                Drawing.FillRectangle(
                    thresh_rgb,
                    new Rectangle(pt.X - 3, pt.Y - 3, 6, 6),
                    Color.Green
                );
            }

            thresh_rgb.ToManagedImage().Save("Snapshot.png", ImageFormat.Png);

            // Use a weighted euclidean distance to sort in order (UL, DL, UR, DR)
            var sortedPts = intersects.OrderBy(p => 2*p.X*p.X + p.Y*p.Y).ToArray();

            // Sometimes 3 points get detected on the first vertical line
            // Remove the 3rd if it exists
            var d1 = sortedPts[1].SquaredDistanceTo(sortedPts[2]);
            var d2 = sortedPts[0].SquaredDistanceTo(sortedPts[2]);
            if (d1 < d2) sortedPts = sortedPts.Where(p => p != sortedPts[2]).ToArray();

            // The middle can now be calculated as (p0x, p0y, p3x - p0x, p3y - p0y)
            var tangent = sortedPts[3] - sortedPts[0];
            Rectangle centerRect = new Rectangle(sortedPts[0].X, sortedPts[0].Y, tangent.X, tangent.Y);

            // Draw on the image to show the final result
            Drawing.Rectangle(thresh_rgb, centerRect, Color.Orange);

            // Save image & free resources
            thresh_rgb.Dispose();
            thresh.Dispose();

            #endregion get centers

            #region get colors

            // Declare color holding vars
            Hsb[] col = new Hsb[6];
            double[] s;
            Color c;
            Lab lab;

            // Declare scan location vars
            Size scanSize = new Size(centerRect.Size.Width / 2, centerRect.Size.Height / 2);
            int area = scanSize.Width * scanSize.Height;
            IntPoint offset = new IntPoint(scanSize.Width / 2, scanSize.Height / 2);

            Rectangle[] rectsToSee = new Rectangle[6];
            for (i = 0; i < 6; i++)
                // Stupid way of copying a rectangle
                rectsToSee[i] = Rectangle.Inflate(centerRect, 0, 0);

            // Offset rects by the good amount
            rectsToSee[0].Offset(-tangent.X, -tangent.Y);
            rectsToSee[1].Offset(0, -tangent.Y);
            rectsToSee[2].Offset(tangent.X, -tangent.Y);
            rectsToSee[3].Offset(-tangent.X, 0);
            rectsToSee[5].Offset(tangent.X, 0);

            for (k = 0; k < 6; k++)
            {
                var facelet = rectsToSee[k];
                var sx = facelet.Location.X + offset.X;
                var sy = facelet.Location.Y + offset.Y;

                s = new double[3];
                for (i = 0; i < scanSize.Width; i++)
                {
                    for (j = 0; j < scanSize.Height; j++)
                    {
                        // Sum all colors in the LAB color space
                        c = uimg.GetPixel(i + sx, j + sy);
                        lab = new Rgb { R = c.R, G = c.G, B = c.B }.To<Lab>();
                        s[0] += lab.L; s[1] += lab.A; s[2] += lab.B;
                    }
                }

                col[k] = new Lab { L = s[0] / area, A = s[1] / area, B = s[2] / area }.To<Hsb>();
            }

            #endregion get colors

            img.UnlockBits(dat);
            img.Dispose();
            return col;
        }
    }
}
