using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using API.Data; // Asegúrate de que ApiContext esté en este proyecto o en uno referenciado.
using SistemaDeGestionDeImportaciones.Services;
using Sistema_de_Gestion_de_Importaciones.Services.Interfaces;
using Sistema_de_Gestion_de_Importaciones.Services;

var builder = WebApplication.CreateBuilder(args);

// ----------------------
// Configuración de la API (Base de datos, Swagger, Endpoints API)
// ----------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApiContext>(opt =>
    opt.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

builder.Services.AddControllers(); // Para API controllers
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
    });

// Si tu API y frontend se comunican internamente, configura el HttpClient con el mismo puerto
string apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5079";

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

// Registro de IUsuarioService (asegúrate de que UsuarioService implemente IUsuarioService)
builder.Services.AddHttpClient<IUsuarioService, UsuarioService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
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
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// Mapea también los endpoints API
app.MapControllers();

app.Run();