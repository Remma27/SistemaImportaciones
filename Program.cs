using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using API.Data;
using SistemaDeGestionDeImportaciones.Services;
using Sistema_de_Gestion_de_Importaciones.Handlers;
using Sistema_de_Gestion_de_Importaciones.Middleware;
using Sistema_de_Gestion_de_Importaciones.Extensions;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http.Headers;
using Sistema_de_Gestion_de_Importaciones.Services;
using System.Net;
using Sistema_de_Gestion_de_Importaciones.ViewComponents;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

builder.Services.AddTransient<ResumenAgregadoViewComponent>();
builder.Services.AddTransient<ResumenGeneralEscotillasViewComponent>();
builder.Services.AddTransient<RegistrosIndividualesViewComponent>();
builder.Services.AddTransient<TotalesPorBodegaViewComponent>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiPolicy", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                             ?? new[] { "https://yourdomain.com", "https://www.yourdomain.com" };
        policy.WithOrigins(allowedOrigins)
              .SetIsOriginAllowedToAllowWildcardSubdomains()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "localhost",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "localhost",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(15)
            }));
});

builder.Services.AddDataProtection()
    .SetApplicationName("SistemaGestionImportaciones")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(30))
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\DataProtection-Keys"));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

builder.Services.AddDbContext<ApiContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

ServiceExtensions.AddApplicationServices(builder.Services, builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/IniciarSesion";
        options.LogoutPath = "/Auth/CerrarSesion";
        options.AccessDeniedPath = "/Auth/IniciarSesion";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = ".AspNetCore.Cookies";
        options.Cookie.HttpOnly = true;

        if (builder.Environment.IsDevelopment())
        {
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
        }
        else
        {
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        }

        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api") ||
                (context.Request.Headers.TryGetValue("Accept", out var acceptValues) &&
                 acceptValues.Any(v => v != null && v.Contains("application/json"))))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };

        options.Cookie.IsEssential = true;
        options.Cookie.MaxAge = TimeSpan.FromHours(8);

        options.SessionStore = new MemoryCookieSessionStore();
    });

builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<CookieDelegatingHandler>();

builder.Services.AddHttpClient("API", (sp, client) =>
{
    var apiUrl = builder.Configuration["ApiSettings:BaseUrl"];
    if (string.IsNullOrEmpty(apiUrl))
    {
        throw new InvalidOperationException("API Base URL not configured");
    }
    client.BaseAddress = new Uri(apiUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
})
.AddHttpMessageHandler<CookieDelegatingHandler>();

builder.Services.AddScoped<IImportacionService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("API");
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<ImportacionService>>();
    return new ImportacionService(httpClient, configuration, logger);
});

builder.Services.AddScoped<IUsuarioService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("API");
    return new UsuarioService(httpClient, builder.Configuration);
});

builder.Services.AddScoped<IEmpresaService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("API");
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<EmpresaService>>();
    return new EmpresaService(httpClient, configuration, logger);
});

builder.Services.AddScoped<IBarcoService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("API");
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<BarcoService>>();
    return new BarcoService(httpClient, configuration, logger);
});

builder.Services.AddScoped<IBodegaService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("API");
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<BodegaService>>();
    return new BodegaService(httpClient, configuration, logger);
});

builder.Services.AddHttpClient<IMovimientoService, MovimientoService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
    client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
    client.DefaultRequestHeaders.Connection.Add("keep-alive");
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    MaxConnectionsPerServer = 30,
    EnableMultipleHttp2Connections = true,
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Brotli
});

builder.Services.AddScoped<IMovimientoService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("API");
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<MovimientoService>>();
    var memoryCache = sp.GetRequiredService<IMemoryCache>();
    return new MovimientoService(httpClient, configuration, logger, memoryCache);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    await next();
});

app.UseCors("ApiPolicy");

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<SecurityLoggingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "mvc",
    pattern: "mvc/{controller=Home}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "api",
    pattern: "api/{controller}/{action}/{id?}"
);

app.MapControllers();

app.Run();