namespace SigortaPro.Domain.Enums;

/// <summary>
/// Aracın kullanım amacı (ADR-057). Kasko/Trafik fiyatlamasında maruziyeti belirleyen kullanıcı beyanıdır:
/// ticari ve özellikle taksi kullanımı, hususi kullanıma göre belirgin biçimde daha yüksek yıllık kilometre
/// ve kaza sıklığı taşır. Diğer branşlar (Konut/DASK/Sağlık) bu bilgiyi kullanmaz.
/// </summary>
public enum VehicleUsage
{
    /// <summary>Hususi (kişisel) kullanım — referans seviye.</summary>
    Hususi,

    /// <summary>Ticari kullanım (esnaf/şirket aracı, yük veya iş amaçlı).</summary>
    Ticari,

    /// <summary>Taksi/ticari yolcu taşımacılığı — en yüksek maruziyet.</summary>
    Taksi
}
