using FraudRuleEngine.Reporting.Api.Domain.DTOs;
using FraudRuleEngine.Shared.Common;
using MediatR;

namespace FraudRuleEngine.Reporting.Api.Services.Queries;

public record GetFraudSummaryQuery(Guid TransactionId) : IRequest<Result<FraudSummaryDto?>>;

