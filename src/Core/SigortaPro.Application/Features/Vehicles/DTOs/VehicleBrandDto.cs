namespace SigortaPro.Application.Features.Vehicles.DTOs;

// Katalogdaki bir araç markası ve o markaya ait modeller (cascading select: marka → model).
public sealed record VehicleBrandDto(string Name, IReadOnlyList<string> Models);
