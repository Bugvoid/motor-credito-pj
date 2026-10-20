namespace CreditEngine.Domain.Policy;

public sealed record RuleResult(string Name, bool Passed, string Reason);

public interface IEligibilityRule
{
    RuleResult Evaluate(EnrichedApplication a);
}
