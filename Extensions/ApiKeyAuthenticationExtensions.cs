using Microsoft.AspNetCore.Authentication;
using Sistema_de_Gestion_de_Importaciones.Auth;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ApiKeyAuthenticationExtensions
    {
        public static AuthenticationBuilder AddApiKeySupport(this AuthenticationBuilder builder, Action<ApiKeyAuthenticationOptions> configureOptions)
        {
            return builder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationOptions.DefaultScheme,
                configureOptions);
        }
    }
}