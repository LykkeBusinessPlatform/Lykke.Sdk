using System;
using System.IO;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using JetBrains.Annotations;
using Lykke.Common;
using Lykke.SettingsReader.ConfigurationProvider;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Lykke.Sdk
{
    /// <summary>
    /// Lykke default startup routine
    /// </summary>
    [PublicAPI]
    public static class LykkeStarter
    {
        /// <summary>DEBUG/RELEASE mode flag.</summary>
        public static bool IsDebug { get; private set; }

        /// <summary>Port for listening.</summary>
        public static int Port { get; private set; }

        /// <summary>Starts the service.</summary>
        /// <typeparam name="TStartup">The type of the startup.</typeparam>
        /// <param name="isDebug">DEBUG/RELEASE mode flag</param>
        public static Task Start<TStartup>(bool isDebug)
            where TStartup : class
        {
            return Start<TStartup>(isDebug, 5000, null);
        }

        /// <summary>Starts the service listening to provided port.</summary>
        /// <typeparam name="TStartup">The type of the startup.</typeparam>
        /// <param name="port">Port that the app is listening to.</param>
        /// /// <param name="isDebug">DEBUG/RELEASE mode flag</param>
        public static Task Start<TStartup>(bool isDebug, int port)
            where TStartup : class
        {
            return Start<TStartup>(isDebug, port, null);
        }

        /// <summary>Starts the service listening to provided port.</summary>
        /// <typeparam name="TStartup">The type of the startup.</typeparam>
        /// <param name="port">Port that the app is listening to.</param>
        /// <param name="configureHost">IHost additional configurtion</param>
        /// /// <param name="isDebug">DEBUG/RELEASE mode flag</param>
        public static async Task Start<TStartup>(
            bool isDebug,
            int port,
            Action<IHost> configureHost)
            where TStartup : class
        {
            IsDebug = isDebug;
            Port = port;

            Console.WriteLine($@"{AppEnvironment.Name} version {AppEnvironment.Version}");
            Console.WriteLine(isDebug ? "DEBUG mode" : "RELEASE mode");
            Console.WriteLine($@"ENV_INFO: {AppEnvironment.EnvInfo}");

            try
            {
                var configurationBuilder = new ConfigurationBuilder()
                    .AddEnvironmentVariables();

                if (Environment.GetEnvironmentVariable("SettingsUrl")?.StartsWith("http") ?? false)
                {
                    configurationBuilder.AddHttpSourceConfiguration();
                }

                var configuration = configurationBuilder.Build();

                var host = Host.CreateDefaultBuilder()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                    .UseSerilog()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.ConfigureKestrel(serverOptions =>
                        {
                            // Set properties and call methods on options
                        })
                        .UseUrls($"http://*:{port}")
                        .UseConfiguration(configuration)
                        .UseStartup<TStartup>();
                    })
                    .Build();
                if (configureHost != null)
                    configureHost(host);
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"Fatal error:");
                Console.WriteLine(ex);

                // Lets devops to see startup error in console between restarts in the Kubernetes
                var delay = TimeSpan.FromMinutes(1);

                Console.WriteLine();
                Console.WriteLine($@"Process will be terminated in {delay}. Press any key to terminate immediately.");

                await Task.WhenAny(
                    Task.Delay(delay),
                    Task.Run(() => Console.ReadKey(true)));
            }

            Console.WriteLine(@"Terminated");
        }
    }
}
