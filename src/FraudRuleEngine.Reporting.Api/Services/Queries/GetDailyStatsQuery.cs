using FraudRuleEngine.Reporting.Api.Domain.DTOs;
using FraudRuleEngine.Shared.Common;
using MediatR;

namespace FraudRuleEngine.Reporting.Api.Services.Queries;

public record GetDailyStatsQuery(DateTime Date) : IRequest<Result<DailyStatsDto>>;

