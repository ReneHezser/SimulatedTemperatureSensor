using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace SimulatedTemperatureSensorModule
{
    class Program
    {
        static int counter;

        static DesiredProperties desiredProperties;
        static DataGenerationPolicy generationPolicy = new DataGenerationPolicy();

        private static volatile bool IsReset = false;

        static void Main(string[] args)
        {
            Init().Wait();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }


        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init()
        {
            var mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
            ModuleClient ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine("IoT Hub module client initialized.");

            var moduleTwin = await ioTHubModuleClient.GetTwinAsync();
            var moduleTwinCollection = moduleTwin.Properties.Desired;
            desiredProperties = new DesiredProperties();
            desiredProperties.UpdateDesiredProperties(moduleTwinCollection);

            // callback for updating desired properties through the portal or rest api
            await ioTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync(onDesiredPropertiesUpdate, null);

            // this direct method will allow to reset the temperature sensor values back to their initial state
            await ioTHubModuleClient.SetMethodHandlerAsync("reset", ResetMethod, null);

            // we don't pass ioTHubModuleClient as we're not sending any messages out to the message bus
            await ioTHubModuleClient.SetInputMessageHandlerAsync("control", ControlMessageHandler, null);

            while (true)
            {
                try
                {
                    if (desiredProperties.SendData)
                    {
                        for (int i = 0; i < desiredProperties.InstanceCount; i++)
                        {
                            counter++;
                            if (counter == 1)
                            {
                                // first time execution needs to reset the data factory
                                IsReset = true;
                            }

                            var messageBody = TemperatureDataFactory.CreateTemperatureData(counter, i, generationPolicy, IsReset);
                            IsReset = false;
                            var messageString = JsonConvert.SerializeObject(messageBody);
                            var messageBytes = Encoding.UTF8.GetBytes(messageString);
                            var message = new Message(messageBytes);

                            await ioTHubModuleClient.SendEventAsync("temperatureOutput", message);
                            Console.WriteLine($"\t{DateTime.UtcNow.ToShortDateString()} {DateTime.UtcNow.ToLongTimeString()}> Sending message: {counter}, Body: {messageString}");
                        }
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(desiredProperties.SendInterval));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Unexpected Exception {ex.Message}");
                    Console.WriteLine($"\t{ex.ToString()}");
                }
            }
        }

        private static Task onDesiredPropertiesUpdate(TwinCollection twinCollection, object userContext)
        {
            desiredProperties.UpdateDesiredProperties(twinCollection);
            return Task.CompletedTask;
        }

        private static Task<MethodResponse> ResetMethod(MethodRequest request, object userContext)
        {
            var response = new MethodResponse((int)HttpStatusCode.OK);
            Console.WriteLine("Received reset command via direct method invocation");
            Console.WriteLine("Resetting temperature sensor...");
            IsReset = true;
            return Task.FromResult(response);
        }

        private static Task<MessageResponse> ControlMessageHandler(Message message, object userContext)
        {
            var messageBytes = message.GetBytes();
            var messageString = Encoding.UTF8.GetString(messageBytes);

            Console.WriteLine($"Received message Body: [{messageString}]");

            try
            {
                var messages = JsonConvert.DeserializeObject<ControlCommand[]>(messageString);
                foreach (ControlCommand messageBody in messages)
                {
                    if (messageBody.Command == ControlCommandEnum.Reset)
                    {
                        Console.WriteLine("Resetting temperature sensor..");
                        IsReset = true;
                    }
                    else
                    {
                        //NoOp
                        Console.WriteLine("Received NOOP message");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to deserialize control command with exception: [{ex.Message}]");
            }

            return Task.FromResult(MessageResponse.Completed);
        }
    }
}
