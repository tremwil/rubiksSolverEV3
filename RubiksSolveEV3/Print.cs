using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwoPhaseSolver;
using MonoBrick.EV3;
using System.Diagnostics;
using ConsoleExtender;

namespace RubiksSolveEV3
{
    enum RobotState
    {
        Setup,
        Error,
        Scanning,
        Solving,
        Moving,
        Done,
    }

    partial class Program
    {
        static string[] times = new string[4]
        {
            "Scan  : ",
            "Solve : ",
            "Move  : ",
            "Total : "
        };

        /// <summary>
        /// Set the solving state of the EV3.
        /// </summary>
        /// <param name="inf">Solve state</param>
        static void setState(RobotState state)
        {
            ConsoleHelper.WriteLineColor("\nSTATE: " + state.ToString() + "\n", ConsoleColor.Yellow);
            ev3.Mailbox.Send("lcd", state.ToString());

            switch (state)
            {
                case RobotState.Setup:
                    EV3Lights.setOn(LightColors.Red, true);
                    break;
                case RobotState.Error:
                    EV3Lights.setOn(LightColors.Red, false);
                    break;
                case RobotState.Scanning:
                    EV3Lights.setOn(LightColors.Orange, true);
                    break;
                case RobotState.Moving:
                    EV3Lights.setOn(LightColors.Orange, false);
                    break;
                case RobotState.Done:
                    EV3Lights.setOn(LightColors.Green, false);
                    break;
            }
        }

        static void printTimeData()
        {
            tTotal = tScan + tSolve + tMove;
            var tvals = new long[4] { tScan, tSolve, tMove, tTotal };

            var strings = tvals.Select(x => string.Format(
                "{0}m : {1}.{2}s\n",
                (x / 60000).ToString().PadLeft(2, '0'),
                (x / 1000 % 60).ToString().PadLeft(2, '0'),
                (x % 1000).ToString().PadLeft(3, '0')
            )).ToArray();

            string delim = "".PadRight(Console.BufferWidth, '-');

            Console.WriteLine("");
            ConsoleHelper.WriteLineColor("FINAL STATS".PadLeft(Console.BufferWidth / 2 + 5), ConsoleColor.Green);

            Console.WriteLine(delim);
            for (int i = 0; i < 4; i++)
            {
                ConsoleHelper.WriteColor(times[i], ConsoleColor.Green);

                Console.Write("".PadLeft(Console.BufferWidth / 2 - strings[i].Length / 2 - times[i].Length));

                ConsoleHelper.WriteLineColor(strings[i], ConsoleColor.Magenta);
                Console.WriteLine(delim);
            };

            Console.Write("\n");
        }

        static string square = "\u2588\u2588";

        static ConsoleColor[] cubeColors = new ConsoleColor[7]
        {
            ConsoleColor.White,
            ConsoleColor.Blue,
            ConsoleColor.Red,
            ConsoleColor.DarkGreen,
            ConsoleColor.DarkYellow, // Actually orange
            ConsoleColor.Yellow,
            ConsoleColor.Black
        };

        static int[] idmap = new int[6] { 0, 4, 3, 2, 1, 5 };

        static int[] layerMult = new int[6] { 0, 3, 3, 3, 3, 6 };
        static int[] layer = new int[8] { 0, 0, 0, 1, 2, 2, 2, 1 };

        static int[] posMult = new int[6] { 6, 0, 3, 6, 9, 6 };
        static int[] pos = new int[8] { 0, 1, 2, 2, 2, 1, 0, 0 };

        public static void printFacelets(byte[] fc)
        {
            int[,] lines = new int[9, 12];
            int i, j, k, sqr;

            // Initialize with 6
            k = 0;
            for (i = 0; i < 9; i++)
            {
                for (j = 0; j < 12; j++)
                {
                    if (i % 3 == 1 && j % 3 == 1)
                    {   // Center block
                        if ((i / 3 == 0 || i / 3 == 2) && j / 3 != 2) // Not on cube
                        {
                            lines[i, j] = 6;
                            continue;
                        }

                        lines[i, j] = idmap[k++];
                    }
                    else lines[i, j] = 6;
                }
            }

            // Order line by line
            for (i = 0; i < 6; i++)
            {
                k = idmap[i];

                for (j = 0; j < 8; j++)
                {
                    sqr = fc[i * 8 + j];
                    lines[layerMult[k] + layer[j], posMult[k] + pos[j]] = sqr;
                }
            }

            // Print
            for (i = 0; i < 9; i++)
            {
                for (j = 0; j < 12; j++)
                {
                    ConsoleHelper.WriteColor(square, cubeColors[lines[i, j]]);
                }
                Console.WriteLine();
            }
        }

        public static void updateFacelets(byte[] fc, int col, int row)
        {
            Console.SetCursorPosition(col, row);
            printFacelets(fc);
        }
    }

    public static class MoveExt
    {
        public static void printColor(this Move me, int progress)
        {
            ConsoleColor col = ConsoleColor.Magenta;

            int i = 0;
            foreach (byte m in me.moveList)
            {
                if (i > progress)
                {
                    col = ConsoleColor.DarkMagenta;
                }

                ConsoleHelper.WriteColor(Move.strmove[m] + " ", col);

                i++;
            }
        }
    }
}
