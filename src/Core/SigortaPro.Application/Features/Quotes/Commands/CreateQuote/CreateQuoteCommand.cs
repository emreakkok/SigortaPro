using SigortaPro.Application.Common.Interfaces;
using SigortaPro.Application.Features.Quotes.DTOs;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Application.Features.Quotes.Commands.CreateQuote;

// Oturum sahibi müşteri, branş + risk objesi + teminat paketi seçerek teklif oluşturur.
// Fiyatlama motoru çağrılır ve teklif Priced durumunda (geçerlilik süresiyle) döner.
// InsuredPerson (ADR-041, additive): Sağlıkta "başkası adına" teklif için sigortalı beyanı;
// null = poliçe sahibi kendisi. Sağlık dışı branşlarda gönderilirse 409 (domain guard).
// IsSmoker (ADR-054, additive): SAĞLIK branşında sigortalanan kişinin sigara kullanım BEYANI.
// Sağlıkta zorunludur (varsayılan atanmaz — önceden sessizce "false" varsayılıyor ve fiyat yanlış
// hesaplanıyordu). Sağlık dışı branşlarda gönderilmez/yok sayılır. Yalnızca fiyatlama amacıyla kullanılır;
// bildirimlere, aktivite akışına veya admin listelerine taşınmaz (KVKK — veri minimizasyonu).
// CustomerId (acente destekli teklif, additive): dolu ise teklif bu müşteri ADINA oluşturulur ve çağıran
// YALNIZCA acente personeli (Admin/Personel) olabilir (controller'da nested route ile set edilir; istemci
// gövdesinden okunmaz). null = self-service (oturum sahibi müşteri kendi teklifini oluşturur — mevcut davranış).
public sealed record CreateQuoteCommand(
    InsuranceBranch Branch,
    Guid? VehicleId,
    Guid? PropertyId,
    CoveragePackage CoveragePackage,
    InsuredPersonInput? InsuredPerson = null,
    bool? IsSmoker = null,
    Guid? CustomerId = null) : ICommand<QuoteDto>;

// Sigortalı beyanı (gerçek akış: sigorta ettiren ≠ sigortalı). Kimlik/iletişim bilgileri poliçe
// sahibinin beyanıdır; sistemdeki diğer müşterilerle eşleştirilmez/aranmaz (kaynak sahipliği/KVKK —
// PROJECT_CONTEXT §3.2, ADR-041).
public sealed record InsuredPersonInput(
    string FirstName,
    string LastName,
    string Tckn,
    DateTime BirthDate,
    string PhoneNumber,
    string Relationship);
