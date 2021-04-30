//
// Copyright (c) 2021-2024 karamem0
//
// This software is released under the MIT License.
//
// https://github.com/karamem0/inkbird/blob/main/LICENSE
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

        [Option("mac-address", Required = true)]
        public string MacAddress { get; set; }

        [Option("attribute-handle", Required = true)]
        public string AttributeHandle { get; set; }

        [Option("azure-storage-queue-url", Required = true)]
        public string AzureStorageQueueUrl { get; set; }

        [Option("microsoft-app-tenant-id", Required = true)]
        public string MicrosoftAppTenantId { get; set; }

        [Option("microsoft-app-client-id", Required = true)]
        public string MicrosoftAppClientId { get; set; }

        [Option("microsoft-app-client-secret", Required = true)]
        public string MicrosoftAppClientSecret { get; set; }

        [Option("device-id", Required = true)]
        public string DeviceId { get; set; }

        [Option("device-location", Required = true)]
        public string DeviceLocation { get; set; }

        [Option("timeout", Required = false)]
        public int? Timeout { get; set; }

    }

}
