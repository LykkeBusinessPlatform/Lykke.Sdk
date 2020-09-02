using JetBrains.Annotations;

namespace Lykke.Sdk.Settings
{
    /// <summary>
    /// ElasticSearch settings.
    /// </summary>
    [PublicAPI]
    public class ElasticSearchSettings
    {
        /// <summary>
        /// ElasticSearch url.
        /// </summary>
        public string ElasticSearchUrl { get; set; }
    }
}
