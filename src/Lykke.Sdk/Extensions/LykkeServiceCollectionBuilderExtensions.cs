using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using FluentValidation.AspNetCore;
using JetBrains.Annotations;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Logs;
using Lykke.Sdk.ActionFilters;
using Lykke.Sdk.Controllers;
using Lykke.SettingsReader;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Lykke.Sdk
{
    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/> class.
    /// </summary>
    [PublicAPI]
    public static class LykkeServiceCollectionBuilderExtensions
    {
        /// <summary>
        /// Build service provider for Lykke's service.
        /// </summary>
        public static (IConfigurationRoot, IReloadingManager<TAppSettings>) BuildServiceProvider<TAppSettings>(
            this IServiceCollection services,
            Action<LykkeServiceOptions<TAppSettings>> buildServiceOptions)
            where TAppSettings : class
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (buildServiceOptions == null)
                throw new ArgumentNullException(nameof(buildServiceOptions));

            var serviceOptions = new LykkeServiceOptions<TAppSettings>();

            buildServiceOptions(serviceOptions);

            if (serviceOptions.SwaggerOptions == null)
                throw new ArgumentException("Swagger options must be provided.");
            if (serviceOptions.Logs == null)
                throw new ArgumentException("Logs configuration delegate must be provided.");

            if (!LykkeStarter.IsDebug)
                services.AddApplicationInsightsTelemetry();

            var mvc = services
                .AddControllers(options =>
                {
                    if (!serviceOptions.HaveToDisableValidationFilter)
                    {
                        options.Filters.Add(new ActionValidationFilter());
                    }

                    serviceOptions.ConfigureMvcOptions?.Invoke(options);
                })
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                })
                .AddApplicationPart(typeof(IsAliveController).Assembly)
                .ConfigureApplicationPartManager(partsManager =>
                {
                    serviceOptions.ConfigureApplicationParts?.Invoke(partsManager);
                });

            if (!serviceOptions.HaveToDisableFluentValidation)
            {
                mvc.AddFluentValidation(x =>
                {
                    x.RegisterValidatorsFromAssembly(Assembly.GetEntryAssembly());
                    serviceOptions.ConfigureFluentValidation?.Invoke(x);
                });
            }

            serviceOptions.ConfigureMvcBuilder?.Invoke(mvc);

            services.AddSwaggerGen(options =>
            {
                options.DefaultLykkeConfiguration(
                    serviceOptions.SwaggerOptions.ApiVersion
                        ?? throw new ArgumentNullException($"{nameof(LykkeSwaggerOptions)}.{nameof(LykkeSwaggerOptions.ApiVersion)}"),
                    serviceOptions.SwaggerOptions.ApiTitle
                        ?? throw new ArgumentNullException($"{nameof(LykkeSwaggerOptions)}.{nameof(LykkeSwaggerOptions.ApiTitle)}"));

                if (serviceOptions.AdditionalSwaggerOptions.Any())
                {
                    foreach (var swaggerVersion in serviceOptions.AdditionalSwaggerOptions)
                    {
                        options.SwaggerDoc(
                            $"{swaggerVersion.ApiVersion}",
                            new OpenApiInfo
                            {
                                Version = swaggerVersion.ApiVersion
                                    ?? throw new ArgumentNullException($"{nameof(serviceOptions.AdditionalSwaggerOptions)}.{nameof(LykkeSwaggerOptions.ApiVersion)}"),
                                Title = swaggerVersion.ApiTitle
                                    ?? throw new ArgumentNullException($"{nameof(serviceOptions.AdditionalSwaggerOptions)}.{nameof(LykkeSwaggerOptions.ApiTitle)}")
                            });
                    }
                }

                serviceOptions.Swagger?.Invoke(options);
            });

            var configurationRoot = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            var settingsManager = configurationRoot.LoadSettings<TAppSettings>(options => {});

            ConfigureLogging(
                services,
                serviceOptions,
                settingsManager);

            serviceOptions.Extend?.Invoke(services, settingsManager);

            return (configurationRoot, settingsManager);
        }

        private static void ConfigureLogging<TAppSettings>(
            IServiceCollection services,
            LykkeServiceOptions<TAppSettings> serviceOptions,
            IReloadingManagerWithConfiguration<TAppSettings> settingsManager)
            where TAppSettings : class
        {
            services.AddLykkeLogging();

            var loggingOptions = new LykkeLoggingOptions<TAppSettings>();
            serviceOptions.Logs(loggingOptions);

            if (loggingOptions.HaveToUseEmptyLogging)
                return;

            var serilogConfigurator = new SerilogConfigurator();
            if (!LykkeStarter.IsDebug)
            {
                if (!string.IsNullOrWhiteSpace(loggingOptions.LogSettingsUrl))
                {
                    try
                    {
                        var configBuilder = new ConfigurationBuilder();
                        var settingsStream = new HttpClient().GetStreamAsync(loggingOptions.LogSettingsUrl).GetAwaiter().GetResult();
                        configBuilder.AddJsonStream(settingsStream);
                        var configuration = configBuilder.Build();
                        serilogConfigurator.AddFromConfiguration(configuration);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Couldn't download settings from {loggingOptions.LogSettingsUrl}: {ex}");
                    }
                }
                else if (!string.IsNullOrWhiteSpace(loggingOptions.ConfigurationFile))
                {
                    try
                    {
                        var configBuilder = new ConfigurationBuilder();
                        configBuilder.AddJsonFile(loggingOptions.ConfigurationFile);
                        var configuration = configBuilder.Build();
                        serilogConfigurator.AddFromConfiguration(configuration);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Couldn't load settings from {loggingOptions.ConfigurationFile}: {ex}");
                    }
                }
                else if (loggingOptions.UseConfiguration)
                {
                    var configuration = settingsManager.SettingsConfiguration;
                    serilogConfigurator.AddFromConfiguration(configuration);
                }

                if (loggingOptions.AzureTableConnectionStringResolver != null)
                    serilogConfigurator.AddAzureTable(
                        settingsManager.ConnectionString(loggingOptions.AzureTableConnectionStringResolver).CurrentValue,
                        loggingOptions.LogsTableName);
            }
            serilogConfigurator.Configure();
        }
    }
}
