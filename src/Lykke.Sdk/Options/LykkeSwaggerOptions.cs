using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Lykke.Sdk
{
    /// <summary>
    /// Configuration options for swagger.
    /// </summary>
    [PublicAPI]
    public class LykkeSwaggerOptions
    {
        /// <summary>Swagger API title, Required</summary>
        [Required]
        public string ApiTitle { get; set; }


        /// <summary>API version, Required, Default = 'v1'</summary>
        [Required]
        public string ApiVersion { get; set; } = "v1";
    }
}