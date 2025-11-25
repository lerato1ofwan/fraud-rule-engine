using FraudRuleEngine.Reporting.Api.Data.Repositories;
using FraudRuleEngine.Reporting.Api.Domain.DTOs;
using FraudRuleEngine.Shared.Common;
using MediatR;

namespace FraudRuleEngine.Reporting.Api.Services.Queries;

public class GetTopRulesQueryHandler : IRequestHandler<GetTopRulesQuery, Result<List<TopRuleDto>>>
{
    private readonly IFraudReportingRepository _repository;

    public GetTopRulesQueryHandler(IFraudReportingRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<TopRuleDto>>> Handle(GetTopRulesQuery request, CancellationToken cancellationToken)
    {
        var topRules = await _repository.GetTopRules(request.Top, cancellationToken);

        return Result<List<TopRuleDto>>.Success(topRules);
    }
}

