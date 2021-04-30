//
// Copyright (c) 2021-2024 karamem0
//
// This software is released under the MIT License.
//
// https://github.com/karamem0/inkbird/blob/main/LICENSE
//

using CommandLine;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Karamem0.Inkbird
{

    public static class Program
    {

        private static void Main(string[] args)
        {
            _ = Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed((options) =>
                    Host.CreateDefaultBuilder(args)
                        .ConfigureLogging(builder => builder
                            .ClearProviders()
                            .AddConsole()
                            .AddApplicationInsights()
                            .AddFilter<ApplicationInsightsLoggerProvider>(null, LogLevel.Trace))
                        .ConfigureServices((context, services) => services
                            .AddHostedService<BatchService>()
                            .AddApplicationInsightsTelemetryWorkerService(config =>
                            {
                                config.EnableDependencyTrackingTelemetryModule = false;
                                config.EnablePerformanceCounterCollectionModule = false;
                            })
                            .Configure<TelemetryConfiguration>(config =>
                            {
                                config.ConnectionString = ConfigurationManager.AppSettings["APPINSIGHTS_CONNECTIONSTRING"];
                                config.TelemetryChannel = new InMemoryChannel();
                            })
                            .AddTransient<BatchTask>()
                            .AddSingleton(options))
                        .Build()
                        .Run()
                );
        }

    }

}
