namespace SigortaPro.Application.Common.Exceptions;

// Mock sanal POS ödemesi reddedildiğinde fırlatılır (yetersiz bakiye, 3D hata, geçersiz kart vb. — ADR-007).
// Global middleware bu tipi HTTP 402 (Payment Required) ProblemDetails'e eşler (ADR-018 uzantısı, ADR-022).
public sealed class PaymentFailedException : SigortaProException
{
    public PaymentFailedException(string message) : base(message)
    {
    }
}
