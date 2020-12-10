using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Sdk.Settings
{
    /// <summary>
    /// General app settings abstraction
    /// </summary>
    [PublicAPI]
    public interface IAppSettings
    {
        /// <summary>
        /// The slack notifications settings.
        /// </summary>
        [Optional]
        SlackNotificationsSettings SlackNotifications { get; }

        /// <summary>
        /// The monitoring service settings.
        /// </summary>
        [Optional]
        MonitoringServiceClientSettings MonitoringServiceClient { get; }

        /// <summary>
        /// Elastic search logging settings.
        /// </summary>
        [Optional]
        ElasticSearchSettings ElasticSearch { get; }

        /// <summary>
        /// Telegram logging settings.
        /// </summary>
        [Optional]
        TelegramSettings Telegram { get; }
    }
}