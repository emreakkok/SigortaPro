namespace SigortaPro.Application.Common.Validation;

// Projede birden fazla validator tarafından paylaşılan regex kalıpları.
public static class ValidationPatterns
{
    // Türk plaka formatı: 01-81 il kodu + 1-3 harf + 2-4 rakam (opsiyonel boşluklarla).
    public const string TurkishPlate = @"^(0[1-9]|[1-7][0-9]|8[01])\s?[A-Z]{1,3}\s?\d{2,4}$";

    // Telefon: +90 prefiksi + 10 hane.
    public const string PhoneNumber = @"^\+90\d{10}$";
}
