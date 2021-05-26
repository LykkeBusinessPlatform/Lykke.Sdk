using System;
using JetBrains.Annotations;

namespace Lykke.Sdk
{
    /// <summary>
    /// Lykke logging options class.
    /// </summary>
    [PublicAPI]
    public class LykkeLoggingOptions<TAppSettings>
        where TAppSettings : class
    {
        /// <summary>Flag for component settings usage for logging configuration</summary>
        public bool UseConfiguration { get; set; }

        /// <summary>Path to file with logging configuration</summary>
        public string ConfigurationFile { get; set; }

        /// <summary>Logging setting url</summary>
        public string LogSettingsUrl { get; set; }

        /// <summary>Name of the Azure table for logs. Required</summary>
        public string LogsTableName { get; set; }

        /// <summary>Azure table connection string resolver delegate for Azure table logs. Optional</summary>
        public Func<TAppSettings, string> AzureTableConnectionStringResolver { get; set; }

        /// <summary>This flag indicates whether empty logging system should be used</summary>
        public bool HaveToUseEmptyLogging { get; private set; }

        /// <summary>Setup logging system to log nothing. Another options could be not specified in this case.</summary>
        public void UseEmptyLogging()
        {
            HaveToUseEmptyLogging = true;
        }
    }
}