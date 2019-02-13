// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.IntegrationTest.TestResults
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using LoRaWan.Test.Shared;
    using Microsoft.Azure.Devices;
    using Xunit;

    // Tests Cloud to Device messages
    [Collection(Constants.TestCollectionName)] // run in serial
    [Trait("Category", "SkipWhenLiveUnitTesting")]
    public sealed class ClassCTest : IntegrationTestBaseCi
    {
        public ClassCTest(IntegrationTestFixtureCi testFixture)
            : base(testFixture)
        {
        }

        // Ensures that class C devices can receive messages from a direct method call
        // Uses Device22_ABP
        [Fact]
        public async Task Test_ClassC_Send_Message_From_Direct_Method_Should_Be_Received()
        {
            var device = this.TestFixtureCi.Device22_ABP;
            this.LogTestStart(device);

            await this.ArduinoDevice.setDeviceResetAsync();
            await this.ArduinoDevice.setDeviceModeAsync(LoRaArduinoSerial._device_mode_t.LWABP);
            await this.ArduinoDevice.setIdAsync(device.DevAddr, device.DeviceID, null);
            await this.ArduinoDevice.setKeyAsync(device.NwkSKey, device.AppSKey, null);
            await this.ArduinoDevice.setClassTypeAsync(LoRaArduinoSerial._class_type_t.CLASS_C);
            await this.ArduinoDevice.SetupLora(this.TestFixtureCi.Configuration.LoraRegion);

            var c2d = new LoRaCloudToDeviceMessage()
            {
                DevEUI = device.DeviceID,
                MessageId = Guid.NewGuid().ToString(),
                Fport = 23,
                RawPayload = Convert.ToBase64String(new byte[] { 0x00, 0xFF }),
            };

            await this.TestFixtureCi.InvokeModuleDirectMethodAsync(this.TestFixture.Configuration.LeafDeviceGatewayID, this.TestFixture.Configuration.NetworkServerModuleID, "cloudtodevicemessage", c2d);

            await Task.Delay(Constants.DELAY_BETWEEN_MESSAGES);

            await AssertUtils.ContainsWithRetriesAsync("+MSG: PORT: 23: RXWIN0, RSSI", this.ArduinoDevice.SerialLogs);
        }
    }
}