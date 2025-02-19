using Microsoft.AspNetCore.Authentication.Cookies;
using SistemaDeGestionDeImportaciones.Services;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.Services;

namespace SistemaDeGestionDeImportaciones
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Basic configuration
            builder.Services.AddControllersWithViews();

            // Configure Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Home/IniciarSesion"; // Usa el controlador Home
                    options.LogoutPath = "/Auth/CerrarSesion";
                });

            // Configuración de HttpClient y servicios
            string apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5078";

            builder.Services.AddHttpClient("API", client =>
            {
                client.BaseAddress = new Uri(apiBaseUrl);
            });

            builder.Services.AddScoped<IImportacionService>(sp =>
            {
                var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("API");
                var configuration = sp.GetRequiredService<IConfiguration>();
                return new ImportacionService(httpClient, configuration);
            });

            var app = builder.Build();

            // Configuración del pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}