using System;
using System.Reflection;
using Autofac;
using JetBrains.Annotations;
using Lykke.Sdk.Health;
using Lykke.Sdk.Settings;
using Lykke.SettingsReader;
using Microsoft.Extensions.Configuration;

namespace Lykke.Sdk
{
    /// <summary>
    /// Extension methods for <see cref="ContainerBuilder"/> class.
    /// </summary>
    [PublicAPI]
    public static class ConfigureLykkeContainerExtensions
    {
        /// <summary>
        /// Configure IOC container for Lykke's service.
        /// </summary>
        public static void ConfigureLykkeContainer<TAppSettings>(
            this ContainerBuilder builder,
            IConfigurationRoot configurationRoot,
            IReloadingManager<TAppSettings> settings,
            Action<IModuleRegistration> registerAdditionalModules = null)
            where TAppSettings : class, IAppSettings
        {
            builder.RegisterInstance(configurationRoot).As<IConfigurationRoot>();
            builder.RegisterInstance(settings.Nested(x => x.MonitoringServiceClient))
                .As<IReloadingManager<MonitoringServiceClientSettings>>();

            builder.RegisterType<AppLifetimeHandler>()
                .AsSelf()
                .SingleInstance();

            builder.RegisterAssemblyModules(settings, registerAdditionalModules, Assembly.GetEntryAssembly());

            builder.RegisterType<EmptyStartupManager>()
                .As<IStartupManager>()
                .SingleInstance()
                .IfNotRegistered(typeof(IStartupManager));

            builder.RegisterType<EmptyShutdownManager>()
                .As<IShutdownManager>()
                .SingleInstance()
                .IfNotRegistered(typeof(IShutdownManager));

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance()
                .IfNotRegistered(typeof(IHealthService));
        }
    }
}
