﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoraKeysManagerFacade.Test
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using LoRaWan.Shared;
    using Xunit;

    public class ApiVersionTest
    {
        [Fact]
        public void Version_02_Should_Be_Older_As_All()
        {
            Assert.True(ApiVersion.Version_0_2_Or_Earlier < ApiVersion.Version_2018_12_16_Preview);
            Assert.True(ApiVersion.Version_0_2_Or_Earlier < ApiVersion.Version_2019_02_12_Preview);
        }

        [Fact]
        public void Version_2019_02_12_Should_Be_Newer_As_All()
        {
            Assert.True(ApiVersion.Version_2019_02_12_Preview > ApiVersion.Version_2018_12_16_Preview);
            Assert.True(ApiVersion.Version_2019_02_12_Preview > ApiVersion.Version_0_2_Or_Earlier);
        }

        [Fact]
        public void Empty_String_Should_Parse_To_Version_02()
        {
            var actual = ApiVersion.Parse(string.Empty);
            Assert.Same(actual, ApiVersion.Version_0_2_Or_Earlier);
            Assert.Equal(actual, ApiVersion.Version_0_2_Or_Earlier);
        }

        [Fact]
        public void Parse_Null_String_Should_Return_Unkown_Version()
        {
            var actual = ApiVersion.Parse(null);
            Assert.False(actual.IsKnown);
        }

        [Fact]
        public void Parse_Unknown_Version_String_Should_Return_Unkown_Version()
        {
            var actual = ApiVersion.Parse("qwerty");
            Assert.False(actual.IsKnown);
            Assert.Equal("qwerty", actual.Version);
        }

        [Fact]
        public void Parse_Version_2018_12_16_Preview_Should_Return_Version()
        {
            var actual = ApiVersion.Parse("2018-12-16-preview");
            Assert.True(actual.IsKnown);
            Assert.Equal(actual, ApiVersion.Version_2018_12_16_Preview);
            Assert.Same(actual, ApiVersion.Version_2018_12_16_Preview);
        }

        [Fact]
        public void Parse_Version_2019_02_12_Preview_Should_Return_Version()
        {
            var actual = ApiVersion.Parse("2019-02-12-preview");
            Assert.True(actual.IsKnown);
            Assert.Equal(actual, ApiVersion.Version_2019_02_12_Preview);
            Assert.Same(actual, ApiVersion.Version_2019_02_12_Preview);
        }

        [Fact]
        public void Version_0_2_Is_Not_Compatible_With_Newer_Versions()
        {
            Assert.False(ApiVersion.Version_0_2_Or_Earlier.SupportsVersion(ApiVersion.Version_2018_12_16_Preview));
            Assert.False(ApiVersion.Version_0_2_Or_Earlier.SupportsVersion(ApiVersion.Version_2019_02_12_Preview));
        }

        [Fact]
        public void Version_2018_12_16_Preview_Is_Not_Compatible_With_0_2()
        {
            Assert.False(ApiVersion.Version_2018_12_16_Preview.SupportsVersion(ApiVersion.Version_0_2_Or_Earlier));
        }

        [Fact]
        public void Version_2018_12_16_Preview_Is_Not_Compatible_With_Version_2019_02_12_Preview()
        {
            Assert.False(ApiVersion.Version_2018_12_16_Preview.SupportsVersion(ApiVersion.Version_2019_02_12_Preview));
        }

        [Fact]
        public void Version_2019_02_12_Preview_Is_Compatible_With_2018_12_16_Preview()
        {
            Assert.True(ApiVersion.Version_2019_02_12_Preview.SupportsVersion(ApiVersion.Version_2018_12_16_Preview));
        }

        [Fact]
        public void Version_2019_02_12_Preview_Is_Not_Compatible_With_0_2_Or_Earlier()
        {
            Assert.False(ApiVersion.Version_2019_02_12_Preview.SupportsVersion(ApiVersion.Version_0_2_Or_Earlier));
        }
    }
}
