using SigortaPro.Domain.Common;
using SigortaPro.Domain.Enums;

namespace SigortaPro.Domain.Entities;

public class InsuranceProduct : BaseEntity, IAggregateRoot
{
    protected InsuranceProduct()
    {
    }

    public InsuranceProduct(string name, InsuranceBranch branch, string? description)
    {
        Id = Guid.NewGuid();
        Name = name;
        Branch = branch;
        Description = description;
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;
    public InsuranceBranch Branch { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public ICollection<Coverage> Coverages { get; private set; } = new List<Coverage>();

    public void AddCoverage(Coverage coverage)
    {
        Coverages.Add(coverage);
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
