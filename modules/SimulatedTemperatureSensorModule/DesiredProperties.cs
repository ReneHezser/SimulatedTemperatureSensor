// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

namespace SimulatedTemperatureSensorModule
{
    public class DesiredProperties
    {
        private bool _sendData = true;
        // in milliseconds
        private int _sendInterval = 5000;
        private int _instanceCount = 1;

        public void UpdateDesiredProperties(TwinCollection twinCollection)
        {
            Console.WriteLine($"Updating desired properties {twinCollection.ToJson(Formatting.Indented)}");
            try
            {
                if (twinCollection.Contains("SendData") && twinCollection["SendData"] != null)
                {
                    _sendData = twinCollection["SendData"];
                }

                if (twinCollection.Contains("SendInterval") && twinCollection["SendInterval"] != null)
                {
                    _sendInterval = twinCollection["SendInterval"];
                }

                if (twinCollection.Contains("InstanceCount") && twinCollection["InstanceCount"] != null)
                {
                    _instanceCount = twinCollection["InstanceCount"];
                }
            }
            catch (AggregateException aexc)
            {
                foreach (var exception in aexc.InnerExceptions)
                {
                    Console.WriteLine($"[ERROR] Could not retrieve desired properties {aexc.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Reading desired properties failed with {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"Value for SendData = {_sendData}");
                Console.WriteLine($"Value for SendInterval = {_sendInterval}ms");
                Console.WriteLine($"Value for InstanceCount = {_instanceCount}");
            }
        }

        public bool SendData
        {
            get { return _sendData; }
        }
        /// <summary>
        /// in milliseconds
        /// </summary>
        /// <value></value>
        public int SendInterval
        {
            get { return _sendInterval; }
        }
        public int InstanceCount
        {
            get { return _instanceCount; }
        }
    }
}