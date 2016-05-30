using ColorMine.ColorSpaces.Comparisons;
using ColorMine.ColorSpaces;
using MonoBrick.EV3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwoPhaseSolver;
using System.Drawing;
using ConsoleExtender;

namespace RubiksSolveEV3
{
    partial class Program
    {
        static long tScan = 0;

        /* OLD CODE BELOW */
        //// If saturation < threshold, must be a shade of gray
        //const float SAT_THRESHOLD = 0.3f;
        //// Hue is extremely constan
        //const float HUE_TOLERANCE = 13f;
        //// Saturation is not constant, so better not be sentitive to it
        //const float SAT_TOLERANCE = 0.7f;
        //// High coef makes a very bad detection of gray shades, but most cubes only have 1
        //const float LUM_TOLERANCE = 0.7f;

        //// CIEDE2000 threshold
        //static CieDe2000Comparison comparer = new CieDe2000Comparison();
        //const double CIE2000_THRESHOLD = 20;

        /// <summary>
        /// Scan the cube's facelets.
        /// </summary>
        /// <returns>The cube</returns>
        static Cube scanFacelets()
        {
            if (DEBUG_MODE) return new Cube();

            watch.Restart();
            Webcam.Instance.start();
            Webcam.Instance.calibrateExposure();

            Bitmap bmp;
            Hsb[,] colors = new Hsb[6, 9];
            int i, j;
            byte k;
            int[] kfix = new int[9] { 1, 2, 3, 8, 0, 4, 5, 6, 7 };

            // Take pictures and set colors
            for (j = 0; j < 4; j++)
            {
                for (i = 0; i < 4; i++)
                {
                    bmp = Webcam.Instance.takePicture();
                    var col6 = RubiksColorCompare.processCubePicture(bmp);

                    if ((axes[2] == 5) ? j % 2 == 1 : j % 2 == 0) // Primary
                        for (k = 0; k < 6; k++) colors[axes[2], kfix[k]] = col6[k];

                    else // Secondary
                        for (k = 0; k < 3; k++) colors[axes[2], kfix[k + 6]] = col6[k];

                    if (j > 1)
                    { rotate(2); i++; }
                    else rotate(1);

                    Thread.Sleep(300);
                }

                if (j == 1) flipCube();
                else if (j != 3) flipCube(2);

                Thread.Sleep(300);
            }

            Webcam.Instance.stop();

            List<string> toWrite = new List<string>();
            for (i = 0; i < 6; i++)
            {
                for (j = 0; j < 9; j++)
                    toWrite.Add(string.Format(
                        "Color {0},{1}: Hue {2}, Sat {3}, Bri {4}",
                        i, j,
                        colors[i, j].To<Hsb>().H,
                        colors[i, j].To<Hsb>().S,
                        colors[i, j].To<Hsb>().B
                    ));
                toWrite.Add("");
            }

            System.IO.File.WriteAllLines("colorlog.txt", toWrite);

            //  Intitliatize color matching variables
            byte[] facelets = new byte[48];
            for (i = 0; i < 48; i++) facelets[i] = 6;

            int besti = 0,
                bestcon = 0,
                match = 0,
                done = 0,
                considered,
                index,
                op;

            double dist, bestd;          
            Hsb facelet;
            bool forced;
            int[] taken = new int[6],
                  opsides = new int[6] { 5, 3, 4, 1, 2, 0 };

            byte[] ns;
            List<int>[] poss = new List<int>[48];
            for (i = 0; i < 48; i++) poss[i] = new List<int>() { 0, 1, 2, 3, 4, 5 };

            // Start search
            while (done < 48)
            {
                bestd = int.MaxValue;
                forced = false;
                
                for (i = 0; i < 6; i++)
                {
                    for (j = 0; j < 9; j++)
                    {
                        facelet = colors[i, j];
                        index = 8 * i + j - 1;

                        // Not center and not assigned
                        if (j != 0 && facelets[index] == 6 && !forced)
                        {
                            considered = 0;
                            foreach (int c in poss[index])
                            {
                                // if color all assigned
                                if (taken[c] == 8) continue;

                                dist = colorDiff(facelet, colors[c, 0]);
                                considered++;

                                // New best
                                if (dist < bestd)
                                {
                                    bestd = dist;
                                    besti = index;
                                    match = c;
                                }
                            }

                            // Best considered sticker
                            if (besti == index) bestcon = considered;

                            // Forced sticker, so lets stop the search
                            if (considered == 1) forced = true;
                        }
                    }
                }

                done++;
                facelets[besti] = (byte)match;

                op = opsides[match];
                ns = Cube.cubieFacelets(besti);

                foreach (byte n in ns)
                {
                    // Remove match & it's opposite face from the neighbors
                    poss[n].Remove(match);
                    poss[n].Remove(op);
                }

                taken[match]++;
            }

            watch.Stop();
            tScan = watch.ElapsedMilliseconds;

            ConsoleHelper.WriteLineColor("Scanning done, cube: \n", ConsoleColor.Cyan);
            printFacelets(facelets);
            return new Cube(facelets);
        }

        static double colorDiff(Hsb c1, Hsb c2)
        {
            var d = Math.Abs(RubiksColorCompare.deltaAngle(c1.H, c2.H));

            if (c1.S < 0.2 || c2.S < 0.2) return 300 + d;
            return d + Math.Abs(c1.S - c2.S) * 5 + Math.Abs(c1.B - c2.B) * 3;
        }
    }
}