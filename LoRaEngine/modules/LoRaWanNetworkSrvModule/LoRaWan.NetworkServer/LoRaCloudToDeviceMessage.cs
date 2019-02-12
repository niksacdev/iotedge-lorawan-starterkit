// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.NetworkServer
{
    using System.Threading.Tasks;
    using LoRaTools;

    public class LoRaCloudToDeviceMessage : ILoRaCloudToDeviceMessage
    {
        public string DevEUI { get; set; }

        public byte Fport { get; set; }

        public byte[] Body { get; set; }

        public bool Confirmed { get; set; }

        public string MessageId { get; set; }

        public Task<bool> AbandonAsync() => Task.FromResult(true);

        public Task<bool> CompleteAsync() => Task.FromResult(true);

        public MacCommandHolder GetMacCommands() => null;
    }
}
