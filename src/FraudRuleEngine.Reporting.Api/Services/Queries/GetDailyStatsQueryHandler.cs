using FraudRuleEngine.Reporting.Api.Data.Repositories;
using FraudRuleEngine.Reporting.Api.Domain.DTOs;
using FraudRuleEngine.Shared.Common;
using MediatR;

namespace FraudRuleEngine.Reporting.Api.Services.Queries;

public class GetDailyStatsQueryHandler : IRequestHandler<GetDailyStatsQuery, Result<DailyStatsDto>>
{
    private readonly IFraudSummaryRepository _repository;

    public GetDailyStatsQueryHandler(IFraudSummaryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<DailyStatsDto>> Handle(GetDailyStatsQuery request, CancellationToken cancellationToken)
    {
        var stats = await _repository.GetDailyStatisticsAsync(request.Date.Date, cancellationToken);

        if (stats == null)
        {
            stats = new DailyStatsDto
            {
                Date = request.Date.Date,
                TotalEvaluations = 0,
                FlaggedCount = 0,
                AverageRiskScore = 0
            };
        }

        return Result<DailyStatsDto>.Success(stats);
    }
}

