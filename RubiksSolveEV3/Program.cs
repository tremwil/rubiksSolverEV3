using System;
using System.Collections.Generic;
using System.Linq;
using MonoBrick.EV3;
using System.Threading;
using System.Diagnostics;
using TwoPhaseSolver;
using System.Security.Permissions;
using ConsoleExtender;
using System.IO;
using System.Runtime.InteropServices;

namespace RubiksSolveEV3
{

    partial class Program
    {
        static Stopwatch watch = new Stopwatch();

        static bool DEBUG_MODE = false;
        static bool canExit = true;
        static bool canListen = false;
        static bool closeAfterKey = false;
        static bool isReady = false;
        static bool manualStart = false;

        static long tTotal = 0;
        static uint size = 8;

        public static Brick<TouchSensor, Sensor, Sensor, Sensor> ev3;

        /// <summary>
        /// Main program.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            char chr, chrl;
            SetScreenColorsClass.SetColor(ConsoleColor.DarkYellow, 0xFF, 0x8F, 0x17);

            // Change title/font
            Console.Title = "EV3 DogeCub3r V1.2.0";
            ConsoleHelper.SetConsoleFont(size);

            ConsoleHelper.WriteLineColor(
                "\n[EV3 DogeCub3r V1.2.0] Press I to show controls.",
                ConsoleColor.Green
            );

            // Set up font-size array
            var fonts = ConsoleHelper.ConsoleFonts;
            var sfont = fonts.OrderByDescending(x => x.SizeY * x.SizeY + x.SizeX * x.SizeX).ToArray();
            ConsoleHelper.SetConsoleFont(sfont[size].Index);

            // Start checkthread
            robotCheck.Start();

            // Wait for established connection
            while (!canListen)
            {
                Thread.Sleep(50);
                Console.ReadKey(true);
            }

            isReady = true;
            ConsoleHelper.WriteColor("Ready\n\n", ConsoleColor.Green);

            // Main loop
            while (true)
            {
                // Listen for user input
                chr = Console.ReadKey(true).KeyChar;
                chrl = char.ToLower(chr);

                // Close if required
                if (closeAfterKey) return;

                // Close (Min q)
                if (chr == 'q' && canExit)
                {
                    robotCheck.Abort();
                    return;
                }
                // Force Close (Maj Q)
                if (chr == 'Q')
                {
                    robotCheck.Abort();
                    return;
                }
                // Clear console (Min c)
                if (chr == 'c' && canExit)
                {
                    Console.Clear();
                    ConsoleHelper.WriteLineColor(
                        "\n[EV3 DogeCub3r V1.2.0] Press I to show controls.",
                        ConsoleColor.Green
                    );
                }
                // Force clear (Maj c)
                if (chr == 'C')
                {
                    Console.Clear();
                    ConsoleHelper.WriteLineColor(
                        "\n[EV3 DogeCub3r V1.2.0] Press I to show controls.",
                        ConsoleColor.Green
                    );
                }
                // Change font (+-)
                if (chr == '+')
                {
                    if (size < 11) size++;
                    ConsoleHelper.SetConsoleFont(sfont[size].Index);
                }
                // Change font (+-)
                if (chr == '-')
                {
                    if (size > 0) size--;
                    ConsoleHelper.SetConsoleFont(sfont[size].Index);
                }
                // Save pattern
                if (chrl == 's')
                {
                    PatternLoader.askSavePattern();
                }
                // Load pattern
                if (chrl == 'l')
                {
                    PatternLoader.askLoadPattern();
                }
                // View patterns
                if (chrl == 'p')
                {
                    ConsoleHelper.WriteLineColor("AVAILABLE PATTERNS:\n", ConsoleColor.Yellow);
                    var fnames = Directory.GetFiles("patterns").Select(x => "   " + x.Split('\\')[1]);
                    ConsoleHelper.WriteColor(string.Join("\n", fnames) + "\n\n", ConsoleColor.Magenta);
                }
                // Print info
                if (chrl == 'i')
                {
                    string format = 
                        "\nKEYBOARD ACTIONS\n\n" + 
                        "   C   : Clear console\n" +
                        "   Q   : Close window\n" + 
                        "   S   : Save a pattern\n" + 
                        "   L   : Load a pattern\n" + 
                        "   P   : View patterns\n" + 
                        "   M   : Manual start\n" +
                        "   +/- : Change font size\n" +
                        "   D   : Toggle debug mode (let errors pass)\n" +

                        "\n\nINFO\n\n" + 
                        "   ROBOT NAME        : {0}\n" + 
                        "   COM PORT          : {1}\n" +
                        "   CURRENT FONT SIZE : {2}\n" + 
                        "   CURRENT PATTERN   : {3}\n";

                    ConsoleHelper.WriteLineColor(
                        string.Format(format, robotName, robotPort, size, PatternLoader.currentPattern), 
                        ConsoleColor.Yellow
                    );
                }
                if (chrl == 'm')
                {
                    manualStart = true;
                }
                if (chrl == 'd')
                {
                    DEBUG_MODE = !DEBUG_MODE;
                    ConsoleHelper.WriteLineColor("Debug mode set to " + DEBUG_MODE.ToString(), ConsoleColor.Cyan);
                }

                Thread.Sleep(50);
            }
        }
    }
}