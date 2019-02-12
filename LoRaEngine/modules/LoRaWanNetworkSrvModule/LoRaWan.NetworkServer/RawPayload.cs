// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.NetworkServer
{
    using System;

    public class RawPayload : ILoRaDeviceTelemetryPayload
    {
        private readonly string value;

        public RawPayload(byte[] payloadData)
        {
            this.value = Convert.ToBase64String(payloadData);
        }

        public override string ToString() => this.value;

        public override int GetHashCode() => this.value.GetHashCode();

        public static implicit operator string(RawPayload p) => p.value;
    }
}
