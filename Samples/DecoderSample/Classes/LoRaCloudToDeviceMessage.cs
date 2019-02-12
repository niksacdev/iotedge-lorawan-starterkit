// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SensorDecoderModule.Classes
{
    public class LoRaCloudToDeviceMessage
    {
        public string DevEUI { get; set; }

        public byte Fport { get; set; }

        public byte[] Body { get; set; }

        public bool Confirmed { get; set; }

        public string MessageId { get; set; }
    }
}