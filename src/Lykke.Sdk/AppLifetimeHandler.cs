using System;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Lykke.Sdk
{
    [UsedImplicitly]
    internal class AppLifetimeHandler
    {
        private readonly ILogFactory _logFactory;
        private readonly IHealthNotifier _healthNotifier;
        private readonly IStartupManager _startupManager;
        private readonly IShutdownManager _shutdownManager;
        private readonly IWebHostEnvironment _hostingEnvironment;

        private readonly ILog _log;

        public AppLifetimeHandler(
            ILogFactory logFactory,
            IHealthNotifier healthNotifier,
            IStartupManager startupManager,
            IShutdownManager shutdownManager,
            IWebHostEnvironment hostingEnvironment)
        {
            _logFactory = logFactory ?? throw new ArgumentNullException(nameof(logFactory));
            _healthNotifier = healthNotifier ?? throw new ArgumentNullException(nameof(healthNotifier));
            _startupManager = startupManager ?? throw new ArgumentNullException(nameof(startupManager));
            _shutdownManager = shutdownManager ?? throw new ArgumentNullException(nameof(shutdownManager));
            _hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));

            _log = logFactory.CreateLog(this);
        }

        public async Task HandleStartedAsync()
        {
            try
            {
                _healthNotifier.Notify("Initializing");

                await _startupManager.StartAsync();

                _healthNotifier.Notify("Application is started");

                if (_hostingEnvironment.IsDevelopment())
                    return;
            }
            catch (Exception ex)
            {
                _log.Critical(ex);
                throw;
            }
        }

        public Task HandleStoppingAsync()
        {
            try
            {
                return _shutdownManager.StopAsync();
            }
            catch (Exception ex)
            {
                _log.Critical(ex);
                throw;
            }
        }

        public void HandleStopped()
        {
            try
            {
                _healthNotifier.Notify("Application is being terminated");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                try
                {
                    _logFactory.Dispose();
                }
                catch (Exception ex1)
                {
                    Console.WriteLine(ex1);
                }

                throw;
            }
        }
    }
}