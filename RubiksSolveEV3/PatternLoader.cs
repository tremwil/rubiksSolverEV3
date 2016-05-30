using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwoPhaseSolver;
using ConsoleExtender;
using System.IO;

namespace RubiksSolveEV3
{
    static class PatternLoader
    {
        public static bool isLowerAlphanumeric(this string me)
        {
            return me.All(char.IsLetterOrDigit) && me.ToLower() == me;
        }

        public static Move currentPattern = Move.None;

        public static void askLoadPattern()
        {
            Move m = null;
            string o = "";

            while (m == null)
            {
                ConsoleHelper.WriteColor("Type pattern move / name: ", ConsoleColor.Cyan);
                o = Console.ReadLine();
                if (o == "")
                {
                    ConsoleHelper.WriteLineColor("Canceled", ConsoleColor.DarkYellow);
                    return;
                }
                m = parseInput(o);
            }

            currentPattern = m;
            ConsoleHelper.WriteLineColor("Pattern loaded", ConsoleColor.Green);
        }

        public static void askSavePattern()
        {
            Move m = null;
            string s = "_";
            string o = "";

            while (m == null)
            {
                ConsoleHelper.WriteColor("Type pattern move: ", ConsoleColor.Cyan);
                o = Console.ReadLine();
                if (o == "")
                {
                    ConsoleHelper.WriteLineColor("Canceled", ConsoleColor.DarkYellow);
                    return;
                }
                m = parseInput(o);
            }

            while (!s.isLowerAlphanumeric())
            {
                ConsoleHelper.WriteColor("Type pattern name: ", ConsoleColor.Cyan);
                s = Console.ReadLine();
                if (s == "")
                {
                    ConsoleHelper.WriteLineColor("Canceled", ConsoleColor.DarkYellow);
                    return;
                }
            }

            savePattern(m, s);
        }


        public static Move parseInput(string input)
        {
            if (input.isLowerAlphanumeric())
            {
                try
                {
                    return loadPattern(input);
                }
                catch
                {
                    ConsoleHelper.WriteLineColor("Pattern does not exist", ConsoleColor.Red);
                    return null;
                }
            }
            else
            {
                try
                {
                    return new Move(input);
                }
                catch
                {
                    ConsoleHelper.WriteLineColor("Incorrect move entered", ConsoleColor.Red);
                    return null;
                }
            }
        }

        public static void savePattern(Move pat, string name)
        {
            if (name.isLowerAlphanumeric())
            {
                File.WriteAllBytes("patterns/" + name, pat.moveList);
                ConsoleHelper.WriteLineColor("Pattern saved", ConsoleColor.Green);
                return;
            }

            ConsoleHelper.WriteLineColor("Pattern must be lower-alphanumeric", ConsoleColor.Red);
        }

        public static Move loadPattern(string name)
        {
            var bytes = File.ReadAllBytes("patterns/" + name);
            return new Move(bytes);
        }
    }
}
