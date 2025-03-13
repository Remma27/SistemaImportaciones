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
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
})
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

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "keys")))
    .SetApplicationName("SistemaGestionImportaciones");

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

if (builder.Environment.IsDevelopment())
{
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
    .UseLowerCaseNamingConvention()
);

ServiceExtensions.AddApplicationServices(builder.Services, builder.Configuration);

builder.Services.AddControllers();

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Debug);

    if (!builder.Environment.IsDevelopment())
    {
        logging.AddEventSourceLogger();
    }
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllersWithViews();

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
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.Events.OnValidatePrincipal = async context =>
        {
            if (context.Properties.ExpiresUtc.HasValue &&
                context.Properties.ExpiresUtc.Value < DateTimeOffset.UtcNow)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };

        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api") ||
                context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                context.Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            var returnUrl = context.Request.Path + context.Request.QueryString;
            context.Response.Redirect($"/Auth/IniciarSesion?returnUrl={WebUtility.UrlEncode(returnUrl)}");
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api") ||
                context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                context.Request.Headers["Accept"].ToString().Contains("application/json"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(options.AccessDeniedPath);
            return Task.CompletedTask;
        };
    })
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

builder.Services.AddTransient<ApiAuthenticationHandler>();

builder.Services.AddHttpClient("API", (sp, client) =>
{
    var apiUrl = EnvironmentHelper.GetApiBaseUrl();
    if (string.IsNullOrEmpty(apiUrl))
    {
        throw new InvalidOperationException("API Base URL not configured");
    }
    client.BaseAddress = new Uri(apiUrl);
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

    Console.WriteLine($"Configured API client with base URL: {apiUrl}");
})
.AddHttpMessageHandler<ApiAuthenticationHandler>()
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
    UseCookies = true,
    CookieContainer = new System.Net.CookieContainer()
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

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.HeaderName = "X-CSRF-TOKEN";
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

var securityHeadersPolicy = new SecurityHeadersPolicy();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<AuthDebugMiddleware>();

app.UseMiddleware<SessionExpirationMiddleware>();

app.UseMiddleware<ApiLoggingMiddleware>();
app.UseMiddleware<RequestSanitizationMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>(securityHeadersPolicy);
app.UseMiddleware<CspFixMiddleware>();

app.UseMiddleware<ApiErrorHandlingMiddleware>();
app.UseMiddleware<AuthorizationExceptionMiddleware>();

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

app.MapGet("/health", () => "Healthy")
   .AllowAnonymous()
   .WithDisplayName("Health Check");

app.Run();