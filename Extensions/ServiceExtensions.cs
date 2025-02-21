using API.Services;
using Microsoft.Extensions.DependencyInjection;
using Sistema_de_Gestion_de_Importaciones.Services;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using SistemaDeGestionDeImportaciones.Services;

namespace Sistema_de_Gestion_de_Importaciones.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Remove any existing IMovimientoService registration if present
            string apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5079";

            services.AddHttpClient("API", client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
            });

            // Registro de servicios
            services.AddScoped<IImportacionService>(sp =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("API");
                var logger = sp.GetRequiredService<ILogger<ImportacionService>>();
                return new ImportacionService(httpClient, configuration, logger);
            });

            services.AddHttpClient<IUsuarioService, UsuarioService>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
            });

            services.AddHttpClient<IEmpresaService, EmpresaService>(client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            });

            services.AddScoped<IBarcoService>(sp =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("API");
                var logger = sp.GetRequiredService<ILogger<BarcoService>>();
                return new BarcoService(httpClient, configuration, logger);
            });

            services.AddScoped<IBodegaService>(sp =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("API");
                var configuration = sp.GetRequiredService<IConfiguration>();
                var logger = sp.GetRequiredService<ILogger<BodegaService>>();
                return new BodegaService(httpClient, configuration, logger);
            });

            services.AddScoped<PasswordHashService>();

            return services;
        }
    }
}