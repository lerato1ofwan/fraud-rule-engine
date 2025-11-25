using FraudRuleEngine.Reporting.Api.Domain.DTOs;
using FraudRuleEngine.Shared.Common;
using MediatR;

namespace FraudRuleEngine.Reporting.Api.Services.Queries;

public record GetTopRulesQuery(int Top = 10) : IRequest<Result<List<TopRuleDto>>>;

