namespace SigortaPro.Application.Common.Exceptions;

// Mock sanal POS ödemesi reddedildiğinde fırlatılır (yetersiz bakiye, 3D hata, geçersiz kart vb.).
// Global middleware bu tipi HTTP 402 (Payment Required) ProblemDetails'e eşler.
public sealed class PaymentFailedException : SigortaProException
{
    public PaymentFailedException(string message) : base(message)
    {
    }
}
