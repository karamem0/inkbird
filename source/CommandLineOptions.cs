//
// Copyright (c) 2021 karamem0
//
// This software is released under the MIT License.
//
// https://github.com/karamem0/inkbird/blob/master/LICENSE
//

using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Karamem0.Inkbird
{

    public class CommandLineOptions
    {

        [Option('m', "mac-address", Required = true)]
        public string MacAddress { get; set; }

        [Option('a', "attribute-handle", Required = true)]
        public string AttributeHandle { get; set; }

        [Option('h', "azure-iot-hub", Required = true)]
        public string AzureIoTHub { get; set; }

        [Option('l', "location", Required = true)]
        public string Location { get; set; }

        [Option('t', "timeout", Required = false)]
        public int? Timeout { get; set; }

    }

}
