using FraudRuleEngine.Reporting.Api.Data.Repositories;
using FraudRuleEngine.Reporting.Api.Domain.DTOs;
using FraudRuleEngine.Shared.Common;
using MediatR;

namespace FraudRuleEngine.Reporting.Api.Services.Queries;

public class GetFraudSummaryQueryHandler : IRequestHandler<GetFraudSummaryQuery, Result<FraudSummaryDto?>>
{
    private readonly IFraudSummaryRepository _repository;

    public GetFraudSummaryQueryHandler(IFraudSummaryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<FraudSummaryDto?>> Handle(GetFraudSummaryQuery request, CancellationToken cancellationToken)
    {
        var summary = await _repository.GetByTransactionIdAsync(request.TransactionId, cancellationToken);

        if (summary == null)
        {
            return Result<FraudSummaryDto?>.Success(null);
        }

        var dto = new FraudSummaryDto
        {
            TransactionId = summary.TransactionId,
            FraudCheckId = summary.FraudCheckId,
            IsFlagged = summary.IsFlagged,
            OverallRiskScore = summary.OverallRiskScore,
            EvaluatedAt = summary.EvaluatedAt
        };

        return Result<FraudSummaryDto?>.Success(dto);
    }
}
