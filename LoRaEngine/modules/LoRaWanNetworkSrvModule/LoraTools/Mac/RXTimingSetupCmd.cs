﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaTools
{
    using System;

    /// <summary>
    /// RXTimingSetupAns Upstream & RXTimingSetupReq Downstream
    /// </summary>
    public class RXTimingSetupCmd : GenericMACCommand
    {
        private readonly uint delay;

        public RXTimingSetupCmd(uint delay)
        {
            this.Length = 2;
            this.Cid = CidEnum.RXTimingCmd;
            this.delay = delay;
        }

        public RXTimingSetupCmd()
        {
            this.Length = 1;
            this.Cid = CidEnum.RXTimingCmd;
        }

        public override byte[] ToBytes()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            throw new NotImplementedException();
        }
    }
}
