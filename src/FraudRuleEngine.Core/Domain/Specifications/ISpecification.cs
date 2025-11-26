namespace FraudRuleEngine.Core.Domain.Specifications;

public interface ISpecification<T>
{
    bool IsSatisfiedBy(T candidate);
}
