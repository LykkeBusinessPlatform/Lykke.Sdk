using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Sdk.Settings
{
    /// <summary>
    /// Base class for lykke settings
    /// </summary>
    [PublicAPI]
    public class BaseAppSettings : IAppSettings
    {
        /// <inheritdoc />
        [Optional]
        public SlackNotificationsSettings SlackNotifications { get; set; }

        /// <inheritdoc />
        public MonitoringServiceClientSettings MonitoringServiceClient { get; set; }

        /// <inheritdoc />
        [Optional]
        public ElasticSearchSettings ElasticSearch { get; set; }

        /// <inheritdoc />
        [Optional]
        public TelegramSettings Telegram { get; set; }
    }
}
