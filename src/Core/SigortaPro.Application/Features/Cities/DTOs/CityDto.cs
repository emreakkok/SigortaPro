namespace SigortaPro.Application.Features.Cities.DTOs;

// Katalogdaki bir il. Nesne (düz string değil) olarak modellenir; böylece ileride ilçe desteği
// additive bir alanla (ör. IReadOnlyList<string> Districts) eklenebilir — endpoint/arayüz değişmeden (ADR-037).
public sealed record CityDto(string Name);
