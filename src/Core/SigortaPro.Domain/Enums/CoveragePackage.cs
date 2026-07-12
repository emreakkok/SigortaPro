namespace SigortaPro.Domain.Enums;

// Teklifin teminat seviyesi (paket). Karşılaştırmada 2-3 alternatif olarak sunulur; teminat limitlerini
// ve primi ölçekler (ölçek katsayıları Application katmanındaki fiyatlama/paket kurallarında tanımlıdır).
public enum CoveragePackage
{
    Standart,
    Genisletilmis,
    Premium
}
