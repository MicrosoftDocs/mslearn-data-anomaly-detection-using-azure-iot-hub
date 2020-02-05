// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;

namespace vibration_device
{
    class SimulatedDevice
    {
        // Telemetry globals.
        private const int intervalInMilliseconds = 2000;                                // Time interval required by wait function.
        private static readonly int intervalInSeconds = intervalInMilliseconds / 1000;  // Time interval in seconds.

        // Conveyor belt globals.
        enum SpeedEnum
        {
            stopped,
            slow,
            fast
        }
        private static int packageCount = 0;                                        // Count of packages leaving the conveyor belt.
        private static SpeedEnum beltSpeed = SpeedEnum.stopped;                     // Initial state of the conveyor belt.
        private static readonly double slowPackagesPerSecond = 1;                   // Packages completed at slow speed/ per second
        private static readonly double fastPackagesPerSecond = 2;                   // Packages completed at fast speed/ per second
        private static double beltStoppedSeconds = 0;                               // Time the belt has been stopped.
        private static double temperature = 60;                                     // Ambient temperature of the facility.
        private static double seconds = 0;                                          // Time conveyor belt is running.

        // Vibration globals.
        private static double forcedSeconds = 0;                                    // Time since forced vibration started.
        private static double increasingSeconds = 0;                                // Time since increasing vibration started.
        private static double naturalConstant;                                      // Constant identifying the severity of natural vibration.
        private static double forcedConstant = 0;                                   // Constant identifying the severity of forced vibration.
        private static double increasingConstant = 0;                               // Constant identifying the severity of increasing vibration.

        // IoT Hub global variables.
        private static DeviceClient s_deviceClient;

        // The device connection string to authenticate the device with your IoT hub.
        private readonly static string s_deviceConnectionString = "<your device connection string>";

        private static void colorMessage(string text, ConsoleColor clr)
        {
            Console.ForegroundColor = clr;
            Console.WriteLine(text);
            Console.ResetColor();
        }
        private static void greenMessage(string text)
        {
            colorMessage(text, ConsoleColor.Green);
        }

        private static void redMessage(string text)
        {
            colorMessage(text, ConsoleColor.Red);
        }

        // Async method to send simulated telemetry.
        private static async void SendDeviceToCloudMessagesAsync(Random rand)
        {
            try
            {
                // Simulate the vibration telemetry of a conveyor belt.
                double vibration;

                while (true)
                {
                    // Randomly adjust belt speed.
                    switch (beltSpeed)
                    {
                        case SpeedEnum.fast:
                            if (rand.NextDouble() < 0.01)
                            {
                                beltSpeed = SpeedEnum.stopped;
                            }
                            if (rand.NextDouble() > 0.95)
                            {
                                beltSpeed = SpeedEnum.slow;
                            }
                            break;

                        case SpeedEnum.slow:
                            if (rand.NextDouble() < 0.01)
                            {
                                beltSpeed = SpeedEnum.stopped;
                            }
                            if (rand.NextDouble() > 0.95)
                            {
                                beltSpeed = SpeedEnum.fast;
                            }
                            break;

                        case SpeedEnum.stopped:
                            if (rand.NextDouble() > 0.75)
                            {
                                beltSpeed = SpeedEnum.slow;
                            }
                            break;
                    }

                    // Set vibration levels.
                    if (beltSpeed == SpeedEnum.stopped)
                    {
                        // If the belt is stopped, all vibration comes to a halt.
                        forcedConstant = 0;
                        increasingConstant = 0;
                        vibration = 0;

                        // Record how much time the belt is stopped, in case we need to send an alert.
                        beltStoppedSeconds += intervalInSeconds;
                    }
                    else
                    {
                        // Conveyor belt is running.
                        beltStoppedSeconds = 0;

                        // Check for random starts in unwanted vibrations.

                        // Check forced vibration.
                        if (forcedConstant == 0)
                        {
                            if (rand.NextDouble() < 0.1)
                            {
                                // Forced vibration starts.
                                forcedConstant = 1 + 6 * rand.NextDouble();             // A number between 1 and 7.
                                if (beltSpeed == SpeedEnum.slow)
                                    forcedConstant /= 2;                                // Lesser vibration if slower speeds.
                                forcedSeconds = 0;
                                redMessage($"Forced vibration starting with severity: {Math.Round(forcedConstant, 2)}");
                            }
                        }
                        else
                        {
                            if (rand.NextDouble() > 0.99)
                            {
                                forcedConstant = 0;
                                greenMessage("Forced vibration stopped");
                            }
                            else
                            {
                                redMessage($"Forced vibration: {Math.Round(forcedConstant, 1)} started at: {DateTime.Now.ToShortTimeString()}");
                            }
                        }

                        // Check increasing vibration.
                        if (increasingConstant == 0)
                        {
                            if (rand.NextDouble() < 0.05)
                            {
                                // Increasing vibration starts.
                                increasingConstant = 100 + 100 * rand.NextDouble();     // A number between 100 and 200.
                                if (beltSpeed == SpeedEnum.slow)
                                    increasingConstant *= 2;                            // Longer period if slower speeds.
                                increasingSeconds = 0;
                                redMessage($"Increasing vibration starting with severity: {Math.Round(increasingConstant, 2)}");
                            }
                        }
                        else
                        {
                            if (rand.NextDouble() > 0.99)
                            {
                                increasingConstant = 0;
                                greenMessage("Increasing vibration stopped");
                            }
                            else
                            {
                                redMessage($"Increasing vibration: {Math.Round(increasingConstant, 1)} started at: {DateTime.Now.ToShortTimeString()}");
                            }
                        }

                        // Apply the vibrations, starting with natural vibration.
                        vibration = naturalConstant * Math.Sin(seconds);

                        if (forcedConstant > 0)
                        {
                            // Add forced vibration.
                            vibration += forcedConstant * Math.Sin(0.75 * forcedSeconds) * Math.Sin(10 * forcedSeconds);
                            forcedSeconds += intervalInSeconds;
                        }

                        if (increasingConstant > 0)
                        {
                            // Add increasing vibration.
                            vibration += (increasingSeconds / increasingConstant) * Math.Sin(increasingSeconds);
                            increasingSeconds += intervalInSeconds;
                        }
                    }

                    // Increment the time since the conveyor belt app started.
                    seconds += intervalInSeconds;

                    // Count the packages that have completed their journey.
                    switch (beltSpeed)
                    {
                        case SpeedEnum.fast:
                            packageCount += (int)(fastPackagesPerSecond * intervalInSeconds);
                            break;

                        case SpeedEnum.slow:
                            packageCount += (int)(slowPackagesPerSecond * intervalInSeconds);
                            break;

                        case SpeedEnum.stopped:
                            // No packages!
                            break;
                    }

                    // Randomly vary ambient temperature.
                    temperature += rand.NextDouble() - 0.5d;

                    // Create two messages:
                    // 1. Vibration telemetry
                    // 2. Logging information

                    // Create the telemetry JSON message.
                    var telemetryDataPoint = new
                    {
                        vibration = Math.Round(vibration, 2),
                    };
                    var telemetryMessageString = JsonConvert.SerializeObject(telemetryDataPoint);
                    var telemetryMessage = new Message(Encoding.ASCII.GetBytes(telemetryMessageString));

                    // Add a custom application property to the message. This can be used to route the message.
                    telemetryMessage.Properties.Add("sensorID", "VSTel");

                    // Send an alert if the belt has been stopped for more than five seconds.
                    telemetryMessage.Properties.Add("beltAlert", (beltStoppedSeconds > 5) ? "true" : "false");

                    Console.WriteLine($"Telemetry data: {telemetryMessageString}");

                    // Send the telemetry message.
                    await s_deviceClient.SendEventAsync(telemetryMessage);
                    greenMessage($"Telemetry sent {DateTime.Now.ToShortTimeString()}");

                    // Create the logging JSON message.
                    var loggingDataPoint = new
                    {
                        vibration = Math.Round(vibration, 2),
                        packages = packageCount,
                        speed = beltSpeed.ToString(),
                        temp = Math.Round(temperature, 2),
                    };
                    var loggingMessageString = JsonConvert.SerializeObject(loggingDataPoint);
                    var loggingMessage = new Message(Encoding.ASCII.GetBytes(loggingMessageString));

                    // Add a custom application property to the message. This can be used to route the message.
                    loggingMessage.Properties.Add("sensorID", "VSLog");

                    // Send an alert if the belt has been stopped for more than five seconds.
                    loggingMessage.Properties.Add("beltAlert", (beltStoppedSeconds > 5) ? "true" : "false");

                    Console.WriteLine($"Log data: {loggingMessageString}");

                    // Send the logging message.
                    await s_deviceClient.SendEventAsync(loggingMessage);
                    greenMessage("Log data sent\n");

                    await Task.Delay(intervalInMilliseconds);
                }
            }
            catch (Exception ex)
            {
                redMessage(ex.Message);
            }
        }

        private static void Main(string[] args)
        {
            Random rand = new Random();
            colorMessage("Vibration sensor device app.\n", ConsoleColor.Yellow);

            // Create the IoT hub device client, specifying the MQTT protocol.
            s_deviceClient = DeviceClient.CreateFromConnectionString(s_deviceConnectionString, TransportType.Mqtt);

            // Create a number between 2 and 4, as a constant for normal vibration levels.
            naturalConstant = 2 + 2 * rand.NextDouble();

            SendDeviceToCloudMessagesAsync(rand);
            Console.ReadLine();
        }
    }
}