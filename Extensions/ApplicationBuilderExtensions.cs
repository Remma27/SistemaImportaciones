using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Sistema_de_Gestion_de_Importaciones.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static WebApplication ConfigureExceptionHandling(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            return app;
        }

        // Add middleware to log when routes are not found
        public static WebApplication UseRouteDebugging(this WebApplication app)
        {
            app.Use(async (context, next) =>
            {
                await next();

                if (context.Response.StatusCode == 404)
                {
                    app.Logger.LogWarning(
                        "Route not found: {Method} {Path}",
                        context.Request.Method,
                        context.Request.Path);
                }
            });

            return app;
        }
    }
}
