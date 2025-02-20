using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using API.Data;
using SistemaDeGestionDeImportaciones.Services;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.Services;
using Sistema_de_Gestion_de_Importaciones.Middleware;
using Sistema_de_Gestion_de_Importaciones.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add this near the top of your configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

// ----------------------
// Configuración de la API (Base de datos, Swagger, Endpoints API)
// ----------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}

builder.Services.AddDbContext<ApiContext>(opt =>
    opt.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

// Reemplaza todas las configuraciones de servicios individuales con esta única línea
builder.Services.AddApplicationServices(builder.Configuration);

builder.Services.AddControllers(); // Para API controllers

// Agregar después de builder.Services.AddControllers();
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Information);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ----------------------
// Configuración del Frontend (MVC) y autenticación
// ----------------------
builder.Services.AddControllersWithViews(); // Para vistas MVC

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/IniciarSesion";
        options.LogoutPath = "/Auth/CerrarSesion";
        options.AccessDeniedPath = "/Auth/IniciarSesion";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Si tu API y frontend se comunican internamente, configura el HttpClient con el mismo puerto
string apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5079";

// Keep this HTTP client configuration
builder.Services.AddHttpClient("API", client =>
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
});

builder.Services.AddScoped<IImportacionService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("API");
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<ImportacionService>>();
    return new ImportacionService(httpClient, configuration);
});

// Registro de IUsuarioService (asegúrate de que UsuarioService implemente IUsuarioService)
builder.Services.AddHttpClient<IUsuarioService, UsuarioService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

// Reemplazar la configuración existente del HttpClient con esta:
builder.Services.AddHttpClient<IEmpresaService, EmpresaService>(client =>
{
    var apiUrl = builder.Configuration["ApiSettings:BaseUrl"];
    Console.WriteLine($"Configurando API URL: {apiUrl}"); // Para debug
    client.BaseAddress = new Uri(apiUrl ?? "http://localhost:5079/");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

// Register BarcoService
builder.Services.AddScoped<IBarcoService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("API");
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<BarcoService>>();
    return new BarcoService(httpClient, configuration, logger);
});

// Add this with the other service registrations
builder.Services.AddScoped<IBodegaService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("API");
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new BodegaService(httpClient, configuration);
});

// Remove any existing IMovimientoService registration
builder.Services.AddScoped<IMovimientoService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("API");
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new MovimientoService(httpClient, configuration);
});

var app = builder.Build();

// ----------------------
// Pipeline de la aplicación
// ----------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add this before app.UseRouting();
app.UseCors("AllowAll");

// Agregar el middleware de logging después de app.UseRouting();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Mapea las rutas MVC
app.MapControllerRoute(
    name: "auth",
    pattern: "Auth/{action=IniciarSesion}",
    defaults: new { controller = "Auth" }
);
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Mapea también los endpoints API
app.MapControllers();

app.Run();