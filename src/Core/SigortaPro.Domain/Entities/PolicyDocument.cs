using SigortaPro.Domain.Common;

namespace SigortaPro.Domain.Entities;

public class PolicyDocument : BaseEntity
{
    protected PolicyDocument()
    {
    }

    public PolicyDocument(Guid policyId, string filePath, string fileName, DateTime generatedAt)
    {
        Id = Guid.NewGuid();
        PolicyId = policyId;
        FilePath = filePath;
        FileName = fileName;
        GeneratedAt = generatedAt;
    }

    public Guid PolicyId { get; private set; }
    public Policy? Policy { get; private set; }
    public string FilePath { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public DateTime GeneratedAt { get; private set; }
}
