using Microsoft.Extensions.Logging;
using SigortaPro.Application.Common.Interfaces;

namespace SigortaPro.Application.Features.Policies.Commands.ExpireOverduePolicies;

public sealed class ExpireOverduePoliciesCommandHandler : ICommandHandler<ExpireOverduePoliciesCommand, int>
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ExpireOverduePoliciesCommandHandler> _logger;

    public ExpireOverduePoliciesCommandHandler(
        IPolicyRepository policyRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ILogger<ExpireOverduePoliciesCommandHandler> logger)
    {
        _policyRepository = policyRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> Handle(ExpireOverduePoliciesCommand request, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow;

        var policies = await _policyRepository.GetOverdueActiveAsync(now, cancellationToken);
        if (policies.Count == 0)
        {
            return 0;
        }

        foreach (var policy in policies)
        {
            policy.ExpireIfPastEndDate(now);
            _policyRepository.Update(policy);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Arkaplan: {Count} poliçe bitiş tarihi geçtiği için Expired'a çekildi.", policies.Count);

        return policies.Count;
    }
}
