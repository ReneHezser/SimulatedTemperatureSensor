# Simulated Temperature IoT Edge Module
This repository contains a tweaked Simulated Temperature Module for Azure IoT Edge. It is based on the [Simulated Temperature Sensor for Azure IoT Edge](https://github.com/Azure/iot-edge-v1/tree/a80857e05be0dc4bbc2de93555e6f83700c3d887/v2/samples/azureiotedge-simulated-temperature-sensor).

This module is an example of a temperature sensor simulation Azure IoT Edge module. You can see its usage in the [Azure IoT Edge documentation](https://docs.microsoft.com/en-us/azure/iot-edge/). It continuously creates simulated temperature data and sends the message to the ```temperatureOutput``` endpoint.
## Creating the images
1. Create an ```.env``` file in root directory, which contains credentials for the Azure Container Registry you want to use to upload the images.
    ```text
    CONTAINER_REGISTRY_URI=youracr.azurecr.io
    CONTAINER_REGISTRY_USERNAME=youracr
    CONTAINER_REGISTRY_PASSWORD=YourSecretPasswordFromACR
    ```
2. Set the desired architecture at the bottom of Visual Studio Code
3. Right click on the ```deployment.template.json``` in the root directory and hit **Build and push IoT Edge solution**
4. Right click ```config\deployment.yourarch.json``` and select **Create deployment for...**

## Available Endpoints

You can interact with the Temperature Simulator in several ways

## Output Queue Endpoints

The temperature simulator is producing a simulation on machine temperature / pressure and environmental parameters like temperature and humidity. The endpoint is `temperatureOutput` with the following payload

```json
{
    "InstanceId": 1,
    "machine": {
        "temperature": 55.651076260675254,
        "pressure": 4.9475909664060413
    },
    "ambient": {
        "temperature": 21.195752660602217,
        "humidity": 26
    },
    "timeCreated": "2018-02-09T10:53:32.2731850+00:00"
}
```

## Direct Method Invocation

The module provides a direct method handler which will **reset** the data to its initial values. To invoke this method you just create from another module or service a `CloudToDeviceMethod`

Here is sample code to showcase how such a method invocation would look like through the `ServiceClient`

```c#
...
var resetMethod = new CloudToDeviceMethod("reset");
var response = await serviceClient.InvokeDeviceMethodAsync(deviceId, moduleId, resetMethod);
...
```

## Input Queue Endpoints

You can also **reset** the data to its initial values via a message going through the EdgeHub routing system to the temperature simulation module. You have to send the payload to the input endpoint `control`.

```json
{
    "command": "reset"
}
```

## Desired Properties Support

The sending behavior can be configured using desired properties for the module via the module twin.

```json
{
    "properties": {
        "desired": {
            "SendData": true,
            "SendInterval": 5000,
            "InstanceCount": 1
        }
    }
}
```

Properties can be set during the set module process in the Azure Portal or via the Azure CLI with the [Azure IoT Extension for Azure CLI 2.0](https://github.com/Azure/azure-iot-cli-extension).

**SendData** = true | false
- Start or stops pushing messages to the `temperatureOutput` endpoint.

**SendInterval** = int value
- The interval in milliseconds between messages being pushed into the endpoint.

**InstanceCount** = int value
- The amount of simulated instances
