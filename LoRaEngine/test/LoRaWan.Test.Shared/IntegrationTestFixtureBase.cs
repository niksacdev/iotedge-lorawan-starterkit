// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Azure.EventHubs;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Xunit;

    public abstract partial class IntegrationTestFixtureBase : IDisposable, IAsyncLifetime
    {
        public const string MESSAGE_IDENTIFIER_PROPERTY_NAME = "messageIdentifier";

        RegistryManager registryManager;
        private UdpLogListener udpLogListener;

        public TestConfiguration Configuration { get; }

        public EventHubDataCollector IoTHubMessages { get; private set; }

        public IntegrationTestFixtureBase()
        {
            this.Configuration = TestConfiguration.GetConfiguration();
            TestLogger.Log($"[INFO] {nameof(this.Configuration.IoTHubAssertLevel)}: {this.Configuration.IoTHubAssertLevel}");
            TestLogger.Log($"[INFO] {nameof(this.Configuration.NetworkServerModuleLogAssertLevel)}: {this.Configuration.NetworkServerModuleLogAssertLevel}");

            AppDomain.CurrentDomain.UnhandledException += this.OnUnhandledException;

            this.SetupTestDevices();

            // Fix device ID if a prefix was defined (DO NOT MOVE THIS LINE ABOVE DEVICE CREATION)
            foreach (var d in this.GetAllDevices())
            {
                if (!string.IsNullOrEmpty(this.Configuration.DevicePrefix))
                {
                    d.DeviceID = string.Concat(this.Configuration.DevicePrefix, d.DeviceID.Substring(this.Configuration.DevicePrefix.Length, d.DeviceID.Length - this.Configuration.DevicePrefix.Length));
                    if (!string.IsNullOrEmpty(d.AppEUI))
                    {
                        d.AppEUI = string.Concat(this.Configuration.DevicePrefix, d.AppEUI.Substring(this.Configuration.DevicePrefix.Length, d.AppEUI.Length - this.Configuration.DevicePrefix.Length));
                    }

                    if (!string.IsNullOrEmpty(d.AppKey))
                    {
                        d.AppKey = string.Concat(this.Configuration.DevicePrefix, d.AppKey.Substring(this.Configuration.DevicePrefix.Length, d.AppKey.Length - this.Configuration.DevicePrefix.Length));
                    }

                    if (!string.IsNullOrEmpty(d.AppSKey))
                    {
                        d.AppSKey = string.Concat(this.Configuration.DevicePrefix, d.AppSKey.Substring(this.Configuration.DevicePrefix.Length, d.AppSKey.Length - this.Configuration.DevicePrefix.Length));
                    }

                    if (!string.IsNullOrEmpty(d.NwkSKey))
                    {
                        d.NwkSKey = string.Concat(this.Configuration.DevicePrefix, d.NwkSKey.Substring(this.Configuration.DevicePrefix.Length, d.NwkSKey.Length - this.Configuration.DevicePrefix.Length));
                    }

                    if (!string.IsNullOrEmpty(d.DevAddr))
                    {
                        d.DevAddr = LoRaTools.Utils.NetIdHelper.SetNwkIdPart(string.Concat(this.Configuration.DevicePrefix, d.DevAddr.Substring(this.Configuration.DevicePrefix.Length, d.DevAddr.Length - this.Configuration.DevicePrefix.Length)), this.Configuration.NetId);
                    }
                }
                else
                {
                    d.DevAddr = LoRaTools.Utils.NetIdHelper.SetNwkIdPart(d.DevAddr, this.Configuration.NetId);
                }
            }
        }

        public abstract void SetupTestDevices();

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine("Unhandled exception: " + e.ExceptionObject?.ToString() ?? string.Empty);
        }

        // Helper method to return all devices
        IEnumerable<TestDeviceInfo> GetAllDevices()
        {
            var types = this.GetType();
            foreach (var prop in types.GetProperties())
            {
                if (prop.PropertyType == typeof(TestDeviceInfo))
                {
                    var device = (TestDeviceInfo)prop.GetValue(this);
                    yield return device;
                }
                else if (prop.PropertyType == typeof(IReadOnlyCollection<TestDeviceInfo>))
                {
                    var devices = (IReadOnlyCollection<TestDeviceInfo>)prop.GetValue(this);
                    foreach (var device in devices)
                    {
                        yield return device;
                    }
                }
            }
        }

        // Clear IoT Hub, Udp logs and Arduino serial logs
        public virtual void ClearLogs()
        {
            this.IoTHubMessages?.ResetEvents();
            this.udpLogListener?.ResetEvents();
        }

        public Task DisposeAsync() => Task.FromResult(0);

        RegistryManager GetRegistryManager()
        {
            return this.registryManager ?? (this.registryManager = RegistryManager.CreateFromConnectionString(this.Configuration.IoTHubConnectionString));
        }

        public async Task<Twin> GetTwinAsync(string deviceId)
        {
            return await this.GetRegistryManager().GetTwinAsync(deviceId);
        }

        public Task SendCloudToDeviceMessage(string deviceId, string messageText, Dictionary<string, string> messageProperties = null) => this.SendCloudToDeviceMessage(deviceId, null, messageText, messageProperties);

        public async Task SendCloudToDeviceMessage(string deviceId, string messageId, string messageText, Dictionary<string, string> messageProperties = null)
        {
            var msg = new Message(Encoding.UTF8.GetBytes(messageText));
            if (messageProperties != null)
            {
                foreach (var messageProperty in messageProperties)
                {
                    msg.Properties.Add(messageProperty.Key, messageProperty.Value);
                }
            }

            if (!string.IsNullOrEmpty(messageId))
            {
                msg.MessageId = messageId;
            }

            await this.SendCloudToDeviceMessage(deviceId, msg);
        }

        public async Task SendCloudToDeviceMessage(string deviceId, Message message)
        {
            ServiceClient sc = ServiceClient.CreateFromConnectionString(this.Configuration.IoTHubConnectionString);

            await sc.SendAsync(deviceId, message);
        }

        internal async Task<Twin> ReplaceTwinAsync(string deviceId, Twin updatedTwin, string etag)
        {
            try
            {
                return await this.GetRegistryManager().ReplaceTwinAsync(deviceId, updatedTwin, etag);
            }
            catch (Exception ex)
            {
                TestLogger.Log($"Error replacing twin for device {deviceId}: {ex.ToString()}");
                throw;
            }
        }

        public virtual async Task InitializeAsync()
        {
            if (this.Configuration.CreateDevices)
            {
                try
                {
                    await this.CreateOrUpdateDevicesAsync();
                }
                catch (Exception ex)
                {
                    TestLogger.Log($"[ERR] Failed to create devices in IoT Hub. {ex.ToString()}");
                }
            }

            if (!string.IsNullOrEmpty(this.Configuration.IoTHubEventHubConnectionString) && this.Configuration.NetworkServerModuleLogAssertLevel != LogValidationAssertLevel.Ignore)
            {
                this.IoTHubMessages = new EventHubDataCollector(this.Configuration.IoTHubEventHubConnectionString, this.Configuration.IoTHubEventHubConsumerGroup);
                await this.IoTHubMessages.StartAsync();
            }

            if (this.Configuration.UdpLog)
            {
                this.udpLogListener = new UdpLogListener(this.Configuration.UdpLogPort);
                this.udpLogListener.Start();
            }
        }

        private async Task CreateOrUpdateDevicesAsync()
        {
            TestLogger.Log($"Creating or updating IoT Hub devices...");
            var registryManager = this.GetRegistryManager();
            foreach (var testDevice in this.GetAllDevices().Where(x => x.IsIoTHubDevice))
            {
                var deviceID = testDevice.DeviceID;
                if (!string.IsNullOrEmpty(this.Configuration.DevicePrefix))
                {
                    deviceID = string.Concat(this.Configuration.DevicePrefix, deviceID.Substring(this.Configuration.DevicePrefix.Length, deviceID.Length - this.Configuration.DevicePrefix.Length));
                    testDevice.DeviceID = deviceID;
                }

                var getDeviceResult = await registryManager.GetDeviceAsync(testDevice.DeviceID);
                if (getDeviceResult == null)
                {
                    TestLogger.Log($"Device {testDevice.DeviceID} does not exist. Creating");
                    var device = new Device(testDevice.DeviceID);
                    var twin = new Twin(testDevice.DeviceID);
                    twin.Properties.Desired = new TwinCollection(JsonConvert.SerializeObject(testDevice.GetDesiredProperties()));

                    TestLogger.Log($"Creating device {testDevice.DeviceID}");
                    await registryManager.AddDeviceWithTwinAsync(device, twin);
                }
                else
                {
                    // compare device twin and make changes if needed
                    var deviceTwin = await registryManager.GetTwinAsync(testDevice.DeviceID);
                    var desiredProperties = testDevice.GetDesiredProperties();
                    foreach (var kv in desiredProperties)
                    {
                        if (!deviceTwin.Properties.Desired.Contains(kv.Key) || (string)deviceTwin.Properties.Desired[kv.Key] != kv.Value.ToString())
                        {
                            var existingValue = string.Empty;
                            if (deviceTwin.Properties.Desired.Contains(kv.Key))
                            {
                                existingValue = deviceTwin.Properties.Desired[kv.Key].ToString();
                            }

                            TestLogger.Log($"Unexpected value for device {testDevice.DeviceID} twin property {kv.Key}, expecting '{kv.Value}', actual is '{existingValue}'");

                            var patch = new Twin();
                            patch.Properties.Desired = new TwinCollection(JsonConvert.SerializeObject(desiredProperties));
                            await registryManager.UpdateTwinAsync(testDevice.DeviceID, patch, deviceTwin.ETag);
                            TestLogger.Log($"Update twin for device {testDevice.DeviceID}");
                            break;
                        }
                    }
                }
            }

            TestLogger.Log($"Done creating or updating IoT Hub devices.");
        }

        // Helper method to return TestDeviceInfo by a property name, NOT THE DEVICE ID!!
        // Usefull when running theories
        public TestDeviceInfo GetDeviceByPropertyName(string propertyName)
        {
            return (TestDeviceInfo)this.GetType().GetProperty(propertyName).GetValue(this);
        }

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            TestLogger.Log($"{nameof(IntegrationTestFixtureBase)} disposed");

            if (!this.disposedValue)
            {
                if (disposing)
                {
                    AppDomain.CurrentDomain.UnhandledException -= this.OnUnhandledException;

                    this.IoTHubMessages?.Dispose();
                    this.IoTHubMessages = null;
                    this.registryManager?.Dispose();
                    this.registryManager = null;

                    this.udpLogListener?.Dispose();
                    this.udpLogListener = null;
                }

                this.disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
