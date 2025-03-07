namespace Sistema_de_Gestion_de_Importaciones.Handlers
{
    public class CookieDelegatingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CookieDelegatingHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                if (httpContext.Request.Cookies.Count > 0)
                {
                    var cookies = string.Join("; ",
                        httpContext.Request.Cookies.Select(c => $"{c.Key}={c.Value}"));

                    if (!string.IsNullOrEmpty(cookies))
                    {
                        request.Headers.Add("Cookie", cookies);
                    }
                }

                var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader))
                {
                    request.Headers.Add("Authorization", authHeader);
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
