namespace SigortaPro.WebAPI.Middleware;

// : Tüm API yanıtlarına temel güvenlik header'larını ekler.
// Header'lar OnStarting geri çağrımında set edilir; böylece hata yanıtları dahil her yanıtta yer alır.
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // MIME sniffing'i engeller.
            headers["X-Content-Type-Options"] = "nosniff";
            // Clickjacking korumasi (sayfa iframe içine gömülemez).
            headers["X-Frame-Options"] = "DENY";
            // Referrer bilgisini sınırlar.
            headers["Referrer-Policy"] = "no-referrer";
            // Tarayıcı özelliklerine erişimi kısıtlar (API için gerek yok).
            headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";

            return Task.CompletedTask;
        });

        return _next(context);
    }
}
