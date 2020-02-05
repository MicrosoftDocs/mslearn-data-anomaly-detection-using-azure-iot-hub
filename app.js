// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
'use strict';

const chalk = require('chalk');
console.log(chalk.yellow.inverse('Vibration sensor device app'));

// The device connection string to authenticate the device with your IoT hub.
var connectionString = '<your device connection string>';

// The sample connects to a device-specific MQTT endpoint on your IoT Hub.
var Mqtt = require('azure-iot-device-mqtt').Mqtt;
var DeviceClient = require('azure-iot-device').Client
var Message = require('azure-iot-device').Message;

// Create the IoT Hub client.
var client = DeviceClient.fromConnectionString(connectionString, Mqtt);

// Conveyor belt globals.
var packageCount = 0;                                   // Count of packages leaving the conveyor belt.
var speedEnum = Object.freeze({ "stopped": "stopped", "slow": "slow", "fast": "fast" });
var beltSpeed = speedEnum.stopped;                      // Initial state of the conveyor belt.
var slowPackagesPerSecond = 1;                          // Packages completed at slow speed/ per second
var fastPackagesPerSecond = 2;                          // Packages completed at fast speed/ per second
var beltStoppedSeconds = 0;                             // Time the belt has been stopped.
var temperature = 60;                                   // Ambient temperature of the facility.

// Vibration globals.
var sensorID = "VibrationSensorID";
var intervalInMilliseconds = 2000;                      // Time interval required by setInterval function.
var intervalInSeconds = intervalInMilliseconds / 1000;  // Time interval in seconds.
var seconds = 0;                                        // Time conveyor belt is running.   
var forcedSeconds = 0;                                  // Time since forced vibration started.
var increasingSeconds = 0;                              // Time since increasing vibration started.

var basicConstant = 2 + 2 * Math.random();              // A number between 2 and 4.
var forcedConstant;                                     // Constants identifing the severity of the vibration.
var increasingConstant;


function greenMessage(text) {
    console.log(chalk.green(text));
}

function redMessage(text) {
    console.log(chalk.red(text));
}

// Create a message and send it to the IoT hub every interval.
setInterval(function () {

    // Simulate the vibration telemetry of a conveyor belt.
    var vibration;

    // Randomly adjust belt speed.
    switch (beltSpeed) {
        case speedEnum.fast:
            if (Math.random() < 0.01) {
                beltSpeed = speedEnum.stopped;
            }
            if (Math.random() > 0.95) {
                beltSpeed = speedEnum.slow;
            }
            break;

        case speedEnum.slow:
            if (Math.random() < 0.01) {
                beltSpeed = speedEnum.stopped;
            }
            if (Math.random() > 0.95) {
                beltSpeed = speedEnum.fast;
            }
            break;

        case speedEnum.stopped:
            if (Math.random() > 0.75) {
                beltSpeed = speedEnum.slow;
            }
            break;
    }

    // Randomly vary the ambient temperature.
    temperature += Math.random() - 0.5;

    // Set vibration levels.

    if (beltSpeed == speedEnum.stopped) {

        // If the belt is stopped, all vibration comes to a halt.
        forcedConstant = 0;
        increasingConstant = 0;
        vibration = 0;

        // Record how much time the belt is stopped, in case we need to send an alert.
        beltStoppedSeconds += intervalInSeconds;
    }
    else {
        // Conveyor belt is running.
        beltStoppedSeconds = 0;

        // Check for random starts in unwanted vibrations.

        // Check forced vibration.
        if (forcedConstant == 0) {
            if (Math.random() < 0.1) {

                // Forced vibration starts.
                forcedConstant = 1 + 6 * Math.random();             // A number between 1 and 7.
                if (beltSpeed == speedEnum.slow)
                    forcedConstant /= 2;                            // Lesser vibration if slower speeds.
                forcedSeconds = 0;
            }
        } else {
            if (Math.random() > 0.99) {
                forcedConstant = 0;
                greenMessage("Forced vibration stopped");
            }
        }

        // Check increasing vibration.
        if (increasingConstant == 0) {
            if (Math.random() < 0.05) {

                // Increasing vibration starts.
                increasingConstant = 100 + 100 * Math.random();         // A number between 100 and 200.
                if (beltSpeed == speedEnum.slow)
                    increasingConstant *= 2;                            // Longer period if slower speeds.
                increasingSeconds = 0;
            }
        } else {
            if (Math.random() > 0.99) {
                increasingConstant = 0;
                greenMessage("Increasing vibration stopped");
            }
        }

        // Apply the vibrations, starting with basic (normal) vibration.
        vibration = basicConstant * Math.sin(seconds);

        if (forcedConstant > 0) {

            var forcedVibration = forcedConstant * Math.sin(0.75 * forcedSeconds) * Math.sin(10 * forcedSeconds);

            // Add forced vibration.
            vibration += forcedVibration;
            forcedSeconds += intervalInSeconds;
            redMessage("Forced vibration = " + forcedVibration.toFixed(2));
        }

        if (increasingConstant > 0) {

            var increasingVibration = (increasingSeconds / increasingConstant) * Math.sin(increasingSeconds);

            // Add increasing vibration.
            vibration += increasingVibration;
            increasingSeconds += intervalInSeconds;
            redMessage("Increasing vibration = " + increasingVibration.toFixed(2));
        }
    }

    // Increment the time since the conveyor belt app started.
    seconds += intervalInSeconds;

    // Count the packages that have completed their journey.
    switch (beltSpeed) {
        case speedEnum.fast:
            packageCount += fastPackagesPerSecond * intervalInSeconds;
            break;

        case speedEnum.slow:
            packageCount += slowPackagesPerSecond * intervalInSeconds;
            break;

        case speedEnum.stopped:
            // No packages!
            break;
    }

    // Create two messages:
    // 1. Vibration telemetry
    // 2. Logging information
    var telemetryDataPoint = new Message(JSON.stringify({
        vibration: vibration.toFixed(2),
    }));

    // Add a custom application property to the message.
    // An IoT hub can filter on these properties without access to the message body.
    telemetryDataPoint.properties.add('sensorID', sensorID);

    // Send an alert, if the conveyor belt is stopped for more than five seconds.
    telemetryDataPoint.properties.add('beltAlert', (beltStoppedSeconds > 5) ? 'true' : 'false');

    console.log('\nTelemetry data: ' + telemetryDataPoint.getData());

    // Send the message.
    client.sendEvent(telemetryDataPoint, function (err) {
        if (err) {
            redMessage('Telemetry send error: ' + err.toString());
        } else {
            var date = new Date();
            greenMessage('Telemetry sent ' + date.toLocaleTimeString());
        }
    });

    // Create the logging JSON message.
    var loggingDataPoint = new Message(JSON.stringify({
        vibration: vibration.toFixed(2),
        packages: packageCount,
        speed: beltSpeed,
        temp: temperature.toFixed(2),
    }));

    // Add a custom application property to the message.
    // An IoT hub can filter on these properties without access to the message body.
    loggingDataPoint.properties.add('sensorID', sensorID);

    // Send an alert, if the conveyor belt is stopped for more than five seconds.
    loggingDataPoint.properties.add('beltAlert', (beltStoppedSeconds > 5) ? 'true' : 'false');

    console.log('Logging data:   ' + loggingDataPoint.getData());

    // Send the message.
    client.sendEvent(loggingDataPoint, function (err) {
        if (err) {
            redMessage('Log send error: ' + err.toString());
        } else {
            greenMessage('Log sent');
        }
    });

    // Set the interval in milliseconds.
}, intervalInMilliseconds);