using LaundryServiesAbstraction.IServices.BundleModule;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace LaundryServies.Services.BundleModule
{
    public class BundleExpiryBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BundleExpiryBackgroundService> _logger;

        public BundleExpiryBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<BundleExpiryBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Bundle Expiry Background Service started at {time}",
                DateTime.UtcNow);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var bundleService = scope.ServiceProvider
                        .GetRequiredService<IBundleService>();

                    await bundleService.ExpireOverdueBundlesAsync();

                    _logger.LogInformation(
                        "Bundle expiry check completed at {time}",
                        DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error occurred during bundle expiry check at {time}",
                        DateTime.UtcNow);
                }

                // Wait 24 hours before next check
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }

            _logger.LogInformation(
                "Bundle Expiry Background Service stopped at {time}",
                DateTime.UtcNow);
        }
    }
}
