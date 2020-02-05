---
page_type: sample
languages:
- csharp
- javascript
products:
- azure-iot-hub
description: "This sample is the source code for the Data anomaly detection using Azure IoT Hub Learn module, and the Manage your Azure IoT Hub with alerts and metrics Learn module."
urlFragment: "data-anomaly-detection-using-azure-iot-hub"
---

# Data anomaly detection using Azure IoT Hub Learn module

The sample here provides the source code that is created in the Data anomaly detection using Azure IoT Hub Learn module. This module creates an IoT Hub, a remote device, and simulated telemetry for a conveyor belt is sent to the IoT Hub. The telemetry focuses on the vibration levels of the conveyor belt. Data is routed both to an Event Hub for real-time analysis, and to Blob storage for archiving. How to set up and manage message routing is one of the key components of the module. The telemetry is visualized using Microsoft Power BI.

The sample code is also used by the Learn module: Manage your Azure IoT Hub with alerts and metrics.

## Contents

| File/folder       | Description                                |
|-------------------|--------------------------------------------|
| `Program.cs`      | Sample C# source code.                     |
| `app.js`          | Sample Node.js source code.                |
| `.gitignore`      | Define what to ignore at commit time.      |
| `CHANGELOG.md`    | List of changes to the sample.             |
| `CONTRIBUTING.md` | Guidelines for contributing to the sample. |
| `README.md`       | This README file.                          |
| `LICENSE`         | The license for the sample.                |

## Prerequisites

The student of the module will need familiarity with the Azure IoT portal, and C# or Node.js programming. The code development can be done using Visual Studio, or Visual Studio Code. 

## Setup

The setup is explained in the text for the modules. The modules do not require the student to download the code from this location, the code is listed and explained in the Learn modules. The code here is a resource if the student needs it.

## Runnning the sample

Running the sample requires that the student go through all the steps of the Learn modules.

## Key concepts

### Data anomaly detection using Azure IoT Hub Learn module
An app is written in the module that shows how to write simulated vibration telemetry, in two messages. The first message is routed to Blob storage. The process of setting up Blob storage is explained, along with how to route data to it. The second message is routed to an IoT Event Hub. Both routes are sent to an Azure Stream Analytics job. The SQL query for the job sends the telemetry to a built-in ML model, that analyzes the data for anomalies. The output from this query is sent to Microsoft Power BI, for visualization.

### Manage your Azure IoT Hub with alerts and metrics
This Learn module uses the vibration telemetry output from a device app. The telemetry is used to examine the Monitoring features of an IoT Hub. The focus of the module is on metrics and alerts. The student creates a number of alerts, then runs the device app to test that the alerts trigger when expected.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
