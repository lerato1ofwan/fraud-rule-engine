using FluentValidation;

namespace FraudRuleEngine.Transactions.Api.Services.Commands.Validators;

/// <summary>
/// Validator for CreateTransactionCommand.
/// Ensures all business rules and property constraints are validated before database operations.
/// </summary>
public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Transaction amount must be greater than zero.")
            .LessThanOrEqualTo(999999999999999.99m)
            .WithMessage("Transaction amount exceeds maximum allowed value.");

        RuleFor(x => x.MerchantId)
            .NotEmpty()
            .WithMessage("Merchant ID is required.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required.")
            .Must(currency => currency != null && currency.Length == 3 && System.Text.RegularExpressions.Regex.IsMatch(currency, "^[A-Z]{3}$"))
            .WithMessage("Currency must be exactly 3 uppercase letters (ISO 4217 format, e.g., USD, EUR, ZAR).");

        RuleFor(x => x.Timestamp)
            .NotEmpty()
            .WithMessage("Transaction timestamp is required.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Transaction timestamp cannot be more than 5 minutes in the future.")
            .GreaterThanOrEqualTo(DateTime.UtcNow.AddYears(-10))
            .WithMessage("Transaction timestamp cannot be more than 10 years in the past.");

        RuleFor(x => x.ExternalId)
            .NotEmpty()
            .WithMessage("External ID is required.")
            .MaximumLength(255)
            .WithMessage("External ID cannot exceed 255 characters.");

        RuleFor(x => x.Metadata)
            .Must(metadata => metadata == null || metadata.Count <= 50)
            .WithMessage("Metadata cannot contain more than 50 key-value pairs.")
            .Must(metadata => metadata == null || metadata.All(kvp => kvp.Key.Length <= 100 && kvp.Value.Length <= 1000))
            .WithMessage("Metadata keys must be 100 characters or less, and values must be 1000 characters or less.");
    }
}

