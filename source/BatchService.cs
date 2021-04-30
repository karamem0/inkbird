//
// Copyright (c) 2021-2024 karamem0
//
// This software is released under the MIT License.
//
// https://github.com/karamem0/inkbird/blob/main/LICENSE
//

using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Karamem0.Inkbird
{

    public class BatchService : BackgroundService
    {

        private readonly IHostApplicationLifetime lifetime;

        private readonly ILogger logger;

        private readonly TelemetryConfiguration telemetry;

        private readonly CommandLineOptions options;

        private readonly BatchTask task;

        public BatchService(
            IHostApplicationLifetime lifetime,
            ILogger<BatchService> logger,
            TelemetryConfiguration telemetry,
            CommandLineOptions options,
            BatchTask task
        )
        {
            this.lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.task = task ?? throw new ArgumentNullException(nameof(task));
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation("Application started");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation("Application ended");
            this.telemetry.TelemetryChannel.Flush();
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                await this.task.ExecuteAsync();
                await Task.Delay(
                    this.options.Timeout.HasValue
                        ? TimeSpan.FromSeconds(this.options.Timeout.Value)
                        : Timeout.InfiniteTimeSpan,
                    this.task.CancellationToken
                );
                this.logger.LogError("Operation timeout");
            }
            catch (TaskCanceledException)
            {
            }
            finally
            {
                this.lifetime.StopApplication();
            }
        }

    }

}
