using dotenv.net;
using dotenv.net.Utilities;
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
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using MySqlConnector;
using Sistema_de_Gestion_de_Importaciones.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file
DotEnv.Load(options: new DotEnvOptions(
    envFilePaths: new[] { ".env" },
    overwriteExistingVars: true
));

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5079);
    options.ListenLocalhost(7079, listenOptions =>
    {
        listenOptions.UseHttps();
    });

    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
    options.Limits.MaxRequestHeadersTotalSize = 32 * 1024;
});

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

    options.AddPolicy("api", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "localhost",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));
});

builder.Services.AddDataProtection()
    .SetApplicationName("SistemaGestionImportaciones")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(30))
    .PersistKeysToFileSystem(new DirectoryInfo(Environment.GetEnvironmentVariable("DATA_PROTECTION_PATH") ?? @"C:\DataProtection-Keys"));

// Build connection string from environment variables
var connectionString = $"Server={Environment.GetEnvironmentVariable("DB_HOST")};" +
                       $"Port={Environment.GetEnvironmentVariable("DB_PORT")};" +
                       $"Database={Environment.GetEnvironmentVariable("DB_NAME")};" +
                       $"User={Environment.GetEnvironmentVariable("DB_USER")};" +
                       $"Password={Environment.GetEnvironmentVariable("DB_PASSWORD")};" +
                       "Convert Zero Datetime=True;ConnectionTimeout=30;";

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection information not found in environment variables.");
}

// Añadir diagnóstico de conexión
try
{
    using var conn = new MySqlConnection(connectionString);
    Console.WriteLine("Intentando abrir conexión a AWS RDS MySQL...");
    conn.Open();
    Console.WriteLine("¡Conexión exitosa a la base de datos!");
    conn.Close();
}
catch (Exception ex)
{
    Console.WriteLine($"ERROR DE CONEXIÓN A BASE DE DATOS: {ex.Message}");
    Console.WriteLine($"StackTrace: {ex.StackTrace}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
    }
}

// Use this for direct testing with AWS
if (builder.Environment.IsDevelopment())
{
    // Log the connection string (without password) for verification
    var connBuilder = new MySqlConnectionStringBuilder(connectionString);
    var sanitizedConn = $"Server={connBuilder.Server};Database={connBuilder.Database};User={connBuilder.UserID};SslMode={connBuilder.SslMode}";
    Console.WriteLine($"Using connection: {sanitizedConn}");
}

builder.Services.AddDbContext<ApiContext>(options =>
    options.UseMySql(connectionString,
        ServerVersion.AutoDetect(connectionString),
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            mySqlOptions.CommandTimeout(60);
        })
    .UseLowerCaseNamingConvention() // Aplica convención de minúsculas a todas las tablas
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

builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER"),
            ValidateAudience = true,
            ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_KEY") ??
                throw new InvalidOperationException("JWT Key not found in environment variables."))
            )
        };
    })
    .AddApiKeySupport(options => { });

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<CookieDelegatingHandler>();

builder.Services.AddHttpClient("API", (sp, client) =>
{
    // Use EnvironmentHelper instead of configuration
    var apiUrl = EnvironmentHelper.GetApiBaseUrl();
    if (string.IsNullOrEmpty(apiUrl))
    {
        throw new InvalidOperationException("API Base URL not configured");
    }
    client.BaseAddress = new Uri(apiUrl);
    client.Timeout = TimeSpan.FromSeconds(60); // Longer timeout for AWS
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

    // For debugging
    Console.WriteLine($"Configured API client with base URL: {apiUrl}");
})
.AddHttpMessageHandler<CookieDelegatingHandler>()
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Don't validate SSL certs during testing
    ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
});

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
    httpClient.DefaultRequestHeaders.Accept.Clear();
    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json");
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

builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
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

// Configuración mejorada de archivos estáticos
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Corregir MIME types para archivos JavaScript
        if (ctx.File.Name.EndsWith(".js"))
        {
            ctx.Context.Response.Headers["Content-Type"] = "application/javascript";
        }
    }
});

app.UseRouting();

app.UseCors("ApiPolicy");

// Configurar el middleware de seguridad con una política más permisiva
var securityHeadersPolicy = new SecurityHeadersPolicy
{
    FrameOptions = "DENY",
    XssProtection = "1; mode=block",
    ReferrerPolicy = "strict-origin-when-cross-origin",
    PermissionsPolicy = "camera=(), microphone=(), geolocation=()"
    // No establecer ContentTypeOptions para permitir el funcionamiento correcto
    // No establecer ContentSecurityPolicy para usar la versión permisiva del middleware
};

// Orden de middleware - primero estáticos, después seguridad 
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<SecurityLoggingMiddleware>();
app.UseMiddleware<ApiLoggingMiddleware>();
app.UseMiddleware<RequestSanitizationMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>(securityHeadersPolicy);

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