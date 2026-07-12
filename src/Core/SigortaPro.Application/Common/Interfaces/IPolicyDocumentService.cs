using SigortaPro.Application.Common.Documents;

namespace SigortaPro.Application.Common.Interfaces;

// Poliçe sertifikası PDF'ini üretir (ADR-006 QuestPDF). Saf render: DB/dosya I/O yapmaz, verilen modelden
// belge baytlarını döner. Implementasyonu Infrastructure'da (PolicyPdfDocumentService).
public interface IPolicyDocumentService
{
    byte[] Generate(PolicyDocumentModel model);
}
