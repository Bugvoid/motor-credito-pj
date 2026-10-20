using static CreditEngine.Domain.Tests.EligibilityRulesTests;

namespace CreditEngine.Domain.Tests;

public class CreditEngineTests
{
    private const decimal Cdi = 0.1365m;

    [Fact] public void Valid_is_approved_with_offer()
    {
        var d = CreditDecisionEngine.Decide(Valid(), Cdi);
        Assert.Equal(DecisionStatus.Approved, d.Status);
        Assert.Equal("2026.10.1", d.PolicyVersion);
        Assert.NotNull(d.Offer);
        Assert.Equal(24, d.Offer.Schedule.Count);
        Assert.True(d.Offer.CetAnnual > d.Offer.AnnualRate);
    }

    [Fact] public void Missing_company_is_referred_without_offer()
    {
        var d = CreditDecisionEngine.Decide(Valid(noCompany: true), Cdi);
        Assert.Equal(DecisionStatus.Referred, d.Status);
        Assert.Null(d.Offer);
        Assert.Equal(8, d.Rules.Count);
    }

    [Fact] public void Term_61_is_rejected() =>
        Assert.Equal(DecisionStatus.Rejected, CreditDecisionEngine.Decide(Valid(term: 61), Cdi).Status);

    [Fact] public void Zero_ebitda_is_rejected_with_rating_E()
    {
        var d = CreditDecisionEngine.Decide(Valid(ebitda: 0m), Cdi);
        Assert.Equal(DecisionStatus.Rejected, d.Status);
        Assert.Equal(Rating.E, d.Rating);
        Assert.Null(d.Offer);
    }
}
