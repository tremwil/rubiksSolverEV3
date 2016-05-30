using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwoPhaseSolver;
using MonoBrick.EV3;
using System.Threading;
using System.Diagnostics;

namespace RubiksSolveEV3
{
    partial class Program
    {
        static int[] flipShift = new int[6] { 4, 1, 0, 3, 5, 2 };
        static int[] rollShift = new int[6] { 0, 4, 1, 2, 3, 5 };

        // faces are indexed URFLBD, but moves are URFDLB
        static int[] axToFace = new int[6] { 0, 1, 2, 5, 3, 4 };

        // Faces that can be moved using only cube flips
        static int[] flipOnly = new int[2] { 0, 4 };

        // Faces that can be rotated to the front
        static int[] rotOnly = new int[3] { 1, 2, 3 };

        static int[] axes = new int[6] { 0, 1, 2, 3, 4, 5 };
        static long tMove = 0;
        static int tgtTacho = 0;

        static int roll { get { return (tgtTacho / 270) % 4; } }

        /// <summary>
        /// Perform the move sequence on the cube.
        /// </summary>
        /// <param name="solution">The move to execute</param>
        static void initMoveSequence(Move solution)
        {
            watch.Restart();

            int col = Console.CursorLeft, row = Console.CursorTop;
            Cube c = new Cube(cube); // Copy cube
            byte[] mcopy = solution.moveList; // Moves to execute

            // Show the cube
            updateFacelets(c.getFaceletColors(), col, row);
            Console.WriteLine();
            solution.printColor(0);
            Console.WriteLine("  Move {0}/{1}", 0, mcopy.Length);

            for (int i = 0; i < mcopy.Length; i++)
            {
                byte m = mcopy[i];
                int ax = axToFace[m / 3], po = (m % 3 == 2) ? -1 : m % 3 + 1;
                int cpos = axes.ToList().IndexOf(ax); // Current index of the face

                // Cpos is in the rotation range
                if (rotOnly.Contains(cpos))
                {
                    releaseCube();

                    if (cpos == 3) rotate(-1);
                    else rotate(cpos);

                    flipCube(true);
                }
                // Cpos is in the flip range
                else if (flipOnly.Contains(cpos))
                    while (axes[5] != ax) flipCube(true);

                // Turn the face
                turn(po);

                c = Move.apply(c, m); // Update cube

                // Update the cube printing and status
                updateFacelets(c.getFaceletColors(), col, row);
                Console.WriteLine();
                solution.printColor(i);
                Console.WriteLine("  Move {0}/{1}", i + 1, mcopy.Length);
            }

            watch.Stop();
            tMove = watch.ElapsedMilliseconds;
        }

        public static void resetAll()
        {
            tgtTacho = 0;
            axes = new int[] { 0, 1, 2, 3, 4, 5 };
            int tc, ltc = 0;
            double dtc;

            ev3.MotorA.On(-20);
            Thread.Sleep(500);

            while (true)
            {
                tc = ev3.MotorA.GetTachoCount();
                dtc = Math.Abs(tc - ltc) * 20;

                // If hit something, derivative will be close to 0
                if (dtc < 10) break;

                ltc = tc;
                Thread.Sleep(50);
            }

            ev3.MotorA.Brake();
            ev3.MotorA.ResetTacho();
        }

        public static void holdCube()
        {
            ev3.MotorA.MoveTo(50, 110, true, true);
            ev3.MotorA.WaitUntilStop();
        }

        public static void releaseCube()
        {
            ev3.MotorA.MoveTo(50, 0, true, true);
            ev3.MotorA.WaitUntilStop();
        }

        public static void flipCube(bool hold = false)
        {
            int[] taxes = (int[])axes.Clone();
            for (int i = 0; i < 6; i++)
                taxes[flipShift[i]] = axes[i];

            axes = taxes;

            ev3.MotorA.MoveTo(50, 200, true, true);
            ev3.MotorA.WaitUntilStop();

            if (hold)
            {
                ev3.MotorA.MoveTo(50, 110, true, true);
                ev3.MotorA.WaitUntilStop();
            }
            else
            {
                ev3.MotorA.MoveTo(50, 0, true, true);
                ev3.MotorA.WaitUntilStop();
            }
        }

        public static void flipCube(int n, bool hold = false)
        {
            for (int i = 0; i < n - 1; i++)
                flipCube(true);

            flipCube(hold);
        }

        public static void rotate(int qtamount)
        {
            for (int k = 0; k < (-qtamount).divmod(4); k++)
            {
                int[] taxes = (int[])axes.Clone();

                for (int i = 1; i < 5; i++)
                    taxes[i % 4 + 1] = axes[i];

                axes = taxes;
            }

            tgtTacho -= qtamount * 270;
            ev3.MotorB.MoveTo(70, tgtTacho, true, true);
            ev3.MotorB.WaitUntilStop();
        }

        public static void turn(int qtamount)
        {
            tgtTacho -= qtamount * 270 + Math.Sign(qtamount) * 65;
            ev3.MotorB.MoveTo(70, tgtTacho, true, true);
            ev3.MotorB.WaitUntilStop();

            //int dir = tgtTacho - ev3.MotorB.GetTachoCount();
            //bool comp = tgtTacho > ev3.MotorB.GetTachoCount();

            //ev3.MotorB.On((sbyte)(100 * dir), true);

            //while (tgtTacho > ev3.MotorB.GetTachoCount() == comp)
            //    Thread.Sleep(20);

            tgtTacho += Math.Sign(qtamount) * 65;
            ev3.MotorB.MoveTo(20, tgtTacho, true, true);
            ev3.MotorB.WaitUntilStop();
        }
    }

    static class MotorExt
    {
        public static void WaitUntilStop(this Motor me, int timeBeforeLoop = 300)
        {
            Thread.Sleep(timeBeforeLoop);
            while (me.IsRunning()) { Thread.Sleep(50); }
        }
    }
}