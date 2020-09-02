using JetBrains.Annotations;
using Serilog.Events;

namespace Lykke.Sdk.Settings
{
    /// <summary>
    /// Telegram settings.
    /// </summary>
    [PublicAPI]
    public class TelegramSettings
    {
        /// <summary>Bot token.</summary>
        public string BotToken { get; set; }

        /// <summary>Chat id.</summary>
        public string ChatId { get; set; }

        /// <summary>Minimal log level.</summary>
        public LogEventLevel MinimalLogLevel { get; set; }
    }
}
