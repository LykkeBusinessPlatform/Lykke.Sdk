﻿using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Sdk.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Lykke.Sdk
{
    /// <summary>
    /// Configuration options used in UseLykkeConfiguration extension method.
    /// </summary>
    [PublicAPI]
    public class LykkeConfigurationOptions
    {
        /// <summary>Default error handler.</summary>
        public CreateErrorResponse DefaultErrorHandler { get; set; }

        /// <summary>Lykke swagger options</summary>
        public LykkeSwaggerOptions SwaggerOptions { get; set; }

        /// <summary>Additional lykke swagger options</summary>
        public IReadOnlyCollection<LykkeSwaggerOptions> AdditionalSwaggerOptions { get; set; } =
            Array.Empty<LykkeSwaggerOptions>();

        /// <summary>Additional middleware</summary>
        public Action<IApplicationBuilder> WithMiddleware { get; set; }

        internal bool HaveToDisableValidationExceptionMiddleware { get; set; }
        internal bool HaveToDisableUnhandledExceptionLoggingMiddleware { get; set; }

        internal LykkeConfigurationOptions()
        {
            DefaultErrorHandler = ex => ErrorResponse.Create("Technical problem");
            SwaggerOptions = new LykkeSwaggerOptions();
        }

        /// <summary>
        /// Disables the middleware, which processes <see cref="ValidationApiException"/>
        /// and builds according error response.
        /// Also see <see cref="LykkeServiceOptions{TAppSettings}.DisableValidationFilter()"/>. 
        /// </summary>
        public void DisableValidationExceptionMiddleware()
        {
            HaveToDisableValidationExceptionMiddleware = true;
        }

        /// <summary>
        /// Disables the <see cref="UnhandledExceptionLoggingMiddleware"/> middleware, which logs unhandled exceptions.
        /// </summary>
        public void DisableUnhandledExceptionLoggingMiddleware()
        {
            HaveToDisableUnhandledExceptionLoggingMiddleware = true;
        }
    }
}
