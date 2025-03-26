using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace Sistema_de_Gestion_de_Importaciones.Extensions
{
    public static class CultureConfiguration
    {
        public static IServiceCollection ConfigureCulture(this IServiceCollection services)
        {
            var customCulture = new CultureInfo("es-GT");
            
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            customCulture.NumberFormat.NumberGroupSeparator = ",";
            customCulture.NumberFormat.PercentDecimalSeparator = ".";
            customCulture.NumberFormat.PercentGroupSeparator = ",";
            
            services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new RequestCulture(customCulture);
                options.SupportedCultures = new[] { customCulture };
                options.SupportedUICultures = new[] { customCulture };
            });

            return services;
        }
        
        public static IApplicationBuilder UseCultureConfiguration(this IApplicationBuilder app)
        {
            app.UseRequestLocalization();
            return app;
        }
    }
}
