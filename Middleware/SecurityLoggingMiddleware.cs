public class SecurityLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityLoggingMiddleware> _logger;

    public SecurityLoggingMiddleware(RequestDelegate next, ILogger<SecurityLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/Auth/IniciarSesion") &&
            context.Request.Method == "POST")
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            _logger.LogInformation(
                "Login attempt from IP: {IpAddress}, User-Agent: {UserAgent}",
                ipAddress,
                context.Request.Headers["User-Agent"].ToString());
        }

        await _next(context);

        if (context.Response.StatusCode == 401)
        {
            _logger.LogWarning(
                "Authentication failure for path {Path} from IP: {IpAddress}",
                context.Request.Path,
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        }
    }
}

