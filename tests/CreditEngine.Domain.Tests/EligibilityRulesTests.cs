using CreditEngine.Domain.Policy;

namespace CreditEngine.Domain.Tests;

public class EligibilityRulesTests
{
    private static readonly Cnpj AnyCnpj = Cnpj.TryParse("11222333000181", out var c) ? c : default;

    internal static EnrichedApplication Valid(
        decimal revenue = 50_000_000m, decimal ebitda = 10_000_000m, decimal netDebt = 10_000_000m,
        decimal amount = 5_000_000m, int term = 24, int score = 700,
        CompanyProfile? company = null, bool noCompany = false) =>
        new(new CreditApplication(AnyCnpj, revenue, ebitda, netDebt, amount, term, Collateral.None, Amortization.Price),
            noCompany ? null : company ?? new CompanyProfile("ATIVA", new DateOnly(2015, 1, 1), "6201501"),
            score, new DateOnly(2026, 10, 20));

    private static RuleResult Run<T>(EnrichedApplication a) where T : IEligibilityRule, new() => new T().Evaluate(a);

    [Fact] public void All_rules_pass_for_valid()
    {
        var results = CreditPolicy.Evaluate(Valid());
        Assert.Equal(8, results.Count);
        Assert.All(results, r => Assert.True(r.Passed, r.Name));
    }

    [Fact] public void ActiveRegistration_missing_company()
    {
        var r = Run<ActiveRegistration>(Valid(noCompany: true));
        Assert.False(r.Passed);
        Assert.Equal("dados cadastrais indisponíveis", r.Reason);
    }

    [Fact] public void ActiveRegistration_not_found()
    {
        var r = Run<ActiveRegistration>(Valid(company: new CompanyProfile("INEXISTENTE", default, "")));
        Assert.False(r.Passed);
        Assert.Equal("CNPJ não encontrado", r.Reason);
    }

    [Fact] public void ActiveRegistration_inactive() =>
        Assert.False(Run<ActiveRegistration>(Valid(company: new CompanyProfile("BAIXADA", new DateOnly(2015, 1, 1), "1"))).Passed);

    [Fact] public void MinimumAge_boundary()
    {
        Assert.True(Run<MinimumAge>(Valid(company: new CompanyProfile("ATIVA", new DateOnly(2024, 10, 20), "1"))).Passed);
        Assert.False(Run<MinimumAge>(Valid(company: new CompanyProfile("ATIVA", new DateOnly(2024, 10, 21), "1"))).Passed);
    }

    [Fact] public void RevenueRange_boundary()
    {
        Assert.True(Run<RevenueRange>(Valid(revenue: 4_800_000m)).Passed);
        Assert.True(Run<RevenueRange>(Valid(revenue: 300_000_000m)).Passed);
        Assert.False(Run<RevenueRange>(Valid(revenue: 4_799_999.99m)).Passed);
        Assert.False(Run<RevenueRange>(Valid(revenue: 300_000_000.01m)).Passed);
    }

    [Fact] public void AmountToRevenue_boundary()
    {
        Assert.True(Run<AmountToRevenue>(Valid(amount: 15_000_000m)).Passed);
        Assert.False(Run<AmountToRevenue>(Valid(amount: 15_000_000.01m)).Passed);
    }

    [Theory, InlineData(6, true), InlineData(60, true), InlineData(5, false), InlineData(61, false)]
    public void TermRange_boundary(int term, bool ok) => Assert.Equal(ok, Run<TermRange>(Valid(term: term)).Passed);

    [Fact] public void MinimumBureauScore_boundary()
    {
        Assert.True(Run<MinimumBureauScore>(Valid(score: 300)).Passed);
        Assert.False(Run<MinimumBureauScore>(Valid(score: 299)).Passed);
    }

    [Fact] public void PostLoanLeverage_boundary()
    {
        Assert.True(Run<PostLoanLeverage>(Valid(amount: 35_000_000m)).Passed); // (10 + 35) / 10 = 4,5
        Assert.False(Run<PostLoanLeverage>(Valid(amount: 35_000_001m)).Passed);
        Assert.False(Run<PostLoanLeverage>(Valid(ebitda: 0m)).Passed);
    }

    [Fact] public void RatingNotE_fails_for_negative_ebitda() =>
        Assert.False(Run<RatingNotE>(Valid(ebitda: -1m)).Passed);
}
