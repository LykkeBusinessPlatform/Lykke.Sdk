using System;
using System.Linq;
using System.Reflection;
using FluentValidation.AspNetCore;
using JetBrains.Annotations;
using Lykke.Common;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Logs;
using Lykke.Sdk.ActionFilters;
using Lykke.Sdk.Controllers;
using Lykke.Sdk.Settings;
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

            where TAppSettings : class, IAppSettings
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

            var settingsManager = configurationRoot.LoadSettings<TAppSettings>(options =>
            {
                options.SetConnString(x => x.SlackNotifications?.AzureQueue.ConnectionString);
                options.SetQueueName(x => x.SlackNotifications?.AzureQueue.QueueName);
                options.SenderName = $"{AppEnvironment.Name} {AppEnvironment.Version}";
            });

            services.AddLykkeLogging();

            var loggingOptions = new LykkeLoggingOptions<TAppSettings>();
            serviceOptions.Logs(loggingOptions);

            var settings = settingsManager.CurrentValue;

            if (!loggingOptions.HaveToUseEmptyLogging)
            {
                var serilogConfiurator = new SerilogConfigurator();
                if (!LykkeStarter.IsDebug)
                {
                    if (loggingOptions.AzureTableConnectionStringResolver != null
                        && !string.IsNullOrWhiteSpace(loggingOptions.LogsTableName))
                        serilogConfiurator.AddAzureTable(
                            settingsManager.ConnectionString(loggingOptions.AzureTableConnectionStringResolver).CurrentValue,
                            loggingOptions.LogsTableName);

                    if (!string.IsNullOrWhiteSpace(settings.SlackNotifications.AzureQueue.ConnectionString)
                        && !string.IsNullOrWhiteSpace(settings.SlackNotifications.AzureQueue.QueueName))
                        serilogConfiurator.AddAzureQueue(
                            settings.SlackNotifications.AzureQueue.ConnectionString,
                            settings.SlackNotifications.AzureQueue.QueueName);

                    if (!string.IsNullOrWhiteSpace(settings.ElasticSearch.ElasticSearchUrl))
                        serilogConfiurator.AddElasticsearch(settings.ElasticSearch.ElasticSearchUrl);

                    if (!string.IsNullOrWhiteSpace(settings.Telegram.BotToken)
                        && !string.IsNullOrWhiteSpace(settings.Telegram.ChatId))
                        serilogConfiurator.AddTelegram(
                            settings.Telegram.BotToken,
                            settings.Telegram.ChatId,
                            settings.Telegram.MinimalLogLevel);
                }
                serilogConfiurator.Configure();
            }

            serviceOptions.Extend?.Invoke(services, settingsManager);

            if (settings.MonitoringServiceClient == null)
                throw new InvalidOperationException("MonitoringServiceClient config section is required");

            return (configurationRoot, settingsManager);
        }
    }
}
