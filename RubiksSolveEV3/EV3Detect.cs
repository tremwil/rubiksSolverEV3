using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TwoPhaseSolver;
using MonoBrick.EV3;
using ConsoleExtender;
using System.Diagnostics;

namespace RubiksSolveEV3
{
    partial class Program
    {
        static Thread robotCheck = new Thread(roboMainLoop);
        static string robotName, robotPort = "";
        static long tSolve = 0;
        static Cube cube;

        /// <summary>
        /// Threaded method for executing all the robot-stuff.
        /// </summary>
        static void roboMainLoop()
        {
            /*  Ask for valid robot name
                Gets SerialPort from Win32_PnPEntity
                This is the ONLY way to get the SerialPort without crashing the
                (real) port for five minutes.  */

            while (robotPort == "")
            {
                // Ask for robot name
                ConsoleHelper.WriteColor("\nEnter robot name: ", ConsoleColor.Cyan);
                robotName = Console.ReadLine();
                ConsoleHelper.WriteLineColor("Establishing connection, please wait...", ConsoleColor.Cyan);

                // Wait for other task to finish
                Thread.Sleep(200);
                robotPort = PortInfo.getSerialPortID(robotName);

                // If empty, throw error
                if (robotPort.Contains("Error"))
                {
                    switch (robotPort.Last())
                    {
                        case '1':
                            ConsoleHelper.WriteLineColor("ERROR: Robot not found.", ConsoleColor.Red);
                            break;
                        case '2':
                            ConsoleHelper.WriteLineColor(
                                "ERROR: More than one robot of name '" + robotName + 
                                "'. Please rename the robot so it is different.", 
                                ConsoleColor.Red
                            );
                            break;
                        case '3':
                            ConsoleHelper.WriteLineColor("ERROR: Port not found.", ConsoleColor.Red);
                            break;
                    }
                    Console.Beep();
                    robotPort = "";
                }
            }

            // Show port
            ConsoleHelper.WriteLineColor("Found port " + robotPort + ". Connecting...", ConsoleColor.DarkCyan);

            // Initiate Bluethoot connection with the EV3 brick
            ev3 = new Brick<TouchSensor, Sensor, Sensor, Sensor>(robotPort);
            bool done = false;

            // Continiously attempt to connect until it works
            while (!done)
            {
                try
                {
                    ev3.Connection.Open();
                    ConsoleHelper.WriteLineColor("Connection success\n", ConsoleColor.Cyan);
                    done = true;
                }
                catch (MonoBrick.ConnectionException e)
                {
                    string s = string.Format("CONNECTION ERROR: {0}RETRY IN 5 SECONDS\n", e.InnerException.Message);
                    Console.Beep();
                    ConsoleHelper.WriteLineColor(s, ConsoleColor.Red);
                    Thread.Sleep(5000);
                }
            }

            Console.Beep(2000, 300);

            canListen = true;
            Console.WriteLine("Press any key to continue...");
            while (!isReady) Thread.Sleep(50);

            ev3.Sensor1 = new TouchSensor();

            try
            {
                while (true)
                {
                    // Wait for user to set click touch sensor.
                    // This way, you can let this program run on the 
                    // computer and all the user has to do to start the
                    // robot is to touch the sensor, like a 
                    // traditionnal EV3 program.
                    if (ev3.Sensor1.Read() == 1 || manualStart)
                    {
                        if (!manualStart)
                        {
                            // Wait for release or stop after 2sec press
                            watch.Restart();
                            Console.BackgroundColor = ConsoleColor.White;
                            ConsoleHelper.WriteLineColor("Start robot? Release to abort", ConsoleColor.Black);
                            Console.BackgroundColor = ConsoleColor.Black;

                            while (ev3.Sensor1.Read() == 1)
                            {
                                Thread.Sleep(50);
                                if (watch.ElapsedMilliseconds > 2000) { break; }
                            }
                            watch.Stop();
                            if (watch.ElapsedMilliseconds < 2000)
                            {
                                ConsoleHelper.WriteLineColor("Start aborted", ConsoleColor.DarkYellow);
                                continue;
                            }
                            Console.BackgroundColor = ConsoleColor.White;
                            ConsoleHelper.WriteLineColor("Starting robot...\n", ConsoleColor.Black);
                            Console.BackgroundColor = ConsoleColor.Black;
                        }
                        else
                        {
                            ConsoleHelper.WriteLineColor("Manual robot start...\n", ConsoleColor.Green);
                        }

                        // Prevent stewpid from exiting when program is running
                        canExit = false;

                        // Print info
                        ConsoleHelper.WriteLineColor("Starting EV3 program...", ConsoleColor.Cyan);
                        // Start EV3 program and
                        ev3.StartProgram("/home/root/lms2012/prjs/Ev3 program/solve cube.rbf");
                        // Print info
                        ConsoleHelper.WriteLineColor("Program started\n", ConsoleColor.Cyan);

                        // Setup
                        setState(RobotState.Setup);
                        ConsoleHelper.WriteColor("Resetting position...", ConsoleColor.DarkCyan);
                        resetAll();
                        ConsoleHelper.WriteLineColor("Done", ConsoleColor.DarkCyan);

                        // Get facelet data
                        setState(RobotState.Scanning);
                        cube = scanFacelets();

                        // Start solver
                        setState(RobotState.Solving);
                        // Reset clock
                        watch.Restart();
                        // Solve
                        Move instructions = Search.patternSolve(cube, PatternLoader.currentPattern, 22, printInfo: true);
                        // Get time
                        watch.Stop();
                        tSolve = watch.ElapsedMilliseconds;

                        // Apply moves
                        setState(RobotState.Moving);
                        initMoveSequence(instructions);

                        // Set done and do the winning dance
                        setState(RobotState.Done);
                        doDoneParade();

                        // Give control back to the brick and the user
                        ev3.MotorA.Off();
                        ev3.MotorB.Off();
                        ev3.MotorC.Off();
                        ev3.StopProgram();
                        canExit = true;
                        manualStart = false;

                        printTimeData();
                    }

                    Thread.Sleep(100);
                }
            }
            catch (Exception error) when (!DEBUG_MODE)
            {
                Console.Beep();

                // If error is not with robot, set error state
                if (!(error is MonoBrick.ConnectionException))
                    setState(RobotState.Error);

                // Set to scary error colors
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.BackgroundColor = ConsoleColor.White;

                Console.WriteLine("CRITICAL UNHANDLED ERROR. PLEASE RESTART.\nDETAILS BELOW:\n");
                Console.Error.WriteLine(error);

                Console.Write("Press any key to continue...");
                closeAfterKey = true;

                return;
            }
        }
    }
}
