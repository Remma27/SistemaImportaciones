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
            // Crear una cultura personalizada basada en es-GT pero con formato de número estadounidense
            var customCulture = new CultureInfo("es-GT");
            
            // Configurar para usar "." como separador decimal y "," como separador de miles
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            customCulture.NumberFormat.NumberGroupSeparator = ",";
            customCulture.NumberFormat.PercentDecimalSeparator = ".";
            customCulture.NumberFormat.PercentGroupSeparator = ",";
            
            // Configuración de opciones de localización
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
