namespace CreditEngine.Domain.Policy;

public static class CreditPolicy
{
    public const string Version = "2026.10.1";

    public static IReadOnlyList<IEligibilityRule> Rules { get; } =
    [
        new ActiveRegistration(),
        new MinimumAge(),
        new RevenueRange(),
        new AmountToRevenue(),
        new TermRange(),
        new MinimumBureauScore(),
        new PostLoanLeverage(),
        new RatingNotE(),
    ];

    // Avalia todas as regras, sem curto-circuito, para a trilha de auditoria ficar completa.
    public static IReadOnlyList<RuleResult> Evaluate(EnrichedApplication a) => Rules.Select(r => r.Evaluate(a)).ToList();
}
