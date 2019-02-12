// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.NetworkServer
{
    using System.Threading.Tasks;
    using LoRaTools;

    public interface ILoRaCloudToDeviceMessage
    {
        string DevEUI { get; }

        byte Fport { get; }

        byte[] Body { get; }

        bool Confirmed { get; }

        string MessageId { get; }

        Task<bool> CompleteAsync();

        Task<bool> AbandonAsync();

        MacCommandHolder GetMacCommands();
    }
}