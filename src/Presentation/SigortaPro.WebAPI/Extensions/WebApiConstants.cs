namespace SigortaPro.WebAPI.Extensions;

// Çapraz kesit altyapısında kullanılan sabit adlar (magic string kullanımını önler).
public static class WebApiConstants
{
    // CORS politikası adı (React SPA origin'lerine izin verir).
    public const string CorsPolicyName = "SigortaProCorsPolicy";

    // Kimlik doğrulama uçlarına (login/register/refresh) uygulanan rate limit politikası adı.
    public const string AuthRateLimitPolicy = "auth";

    // İstek korelasyon kimliği için HTTP header adı.
    public const string CorrelationIdHeader = "X-Correlation-ID";

    // Korelasyon kimliğinin HttpContext.Items içinde ve log context'inde saklandığı anahtar.
    public const string CorrelationIdItemKey = "CorrelationId";

    // RFC 7807 ProblemDetails "type" alanı için temel URI.
    public const string ErrorTypeBaseUri = "https://sigortapro.com/errors/";
}
