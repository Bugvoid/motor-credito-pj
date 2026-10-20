
namespace CreditEngine.Domain.Tests;

public class PricingTests
{
    [Fact] public void Spread_B_24m_receivables() => Assert.Equal(0.031m, Pricing.SpreadAnnual(Rating.B, 24, Collateral.Receivables)); // 3,5 + 0,6 − 1,0
    [Fact] public void Spread_floor() => Assert.Equal(0.01m, Pricing.SpreadAnnual(Rating.A, 12, Collateral.RealEstate)); // 2,0 − 1,5 = 0,5 → piso 1,0
    [Fact] public void Spread_E_throws() => Assert.Throws<ArgumentOutOfRangeException>(() => Pricing.SpreadAnnual(Rating.E, 12, Collateral.None));
    [Fact] public void AnnualRate_compounds() => Assert.Equal(0.189215m, Pricing.AnnualRate(0.149m, 0.035m));
    [Fact] public void MonthlyRate_of_12_68pct_is_1pct() => Assert.Equal(0.01m, Math.Round(Pricing.MonthlyRate(0.12682503013196972m), 6));

    [Fact] public void Price_100k_12m_1pct_payment() => Assert.Equal(8884.88m, Schedule.Build(100_000m, 0.01m, 12, Amortization.Price)[0].Payment);
    [Fact] public void Sac_first_payment() => Assert.Equal(9333.33m, Schedule.Build(100_000m, 0.01m, 12, Amortization.Sac)[0].Payment);

    [Theory, InlineData(1), InlineData(12), InlineData(60)]
    public void Schedule_amortizes_exactly(int n)
    {
        foreach (var t in new[] { Amortization.Price, Amortization.Sac })
        {
            var s = Schedule.Build(100_000m, 0.015m, n, t);
            Assert.Equal(n, s.Count);
            Assert.Equal(100_000m, s.Sum(i => i.Amortization));
            Assert.Equal(0m, s[^1].Balance);
        }
    }

    [Fact] public void Iof_price_100k_12m() => Assert.Equal(2008.16m, CreditIof.Calculate(Schedule.Build(100_000m, 0.01m, 12, Amortization.Price), 100_000m));

    [Fact] public void Iof_caps_at_365_days()
    {
        // 24 parcelas: a partir da 13ª (390 dias), o prazo conta como 365.
        var s = Schedule.Build(100_000m, 0.01m, 24, Amortization.Sac);
        var expected = s.Sum(i => i.Amortization * Math.Min(30 * i.Number, 365) * 0.000082m) + 100_000m * 0.0038m;
        Assert.Equal(Math.Round(expected, 2, MidpointRounding.AwayFromZero), CreditIof.Calculate(s, 100_000m));
    }

    [Fact] public void Cet_equals_rate_without_costs() =>
        Assert.InRange(Cet.Annual(100_000m, Schedule.Build(100_000m, 0.01m, 12, Amortization.Price)), 0.12682m, 0.12684m); // (1,01)^12 − 1

    [Fact] public void Cet_above_rate_with_iof() =>
        Assert.True(Cet.Annual(100_000m - 2008.16m, Schedule.Build(100_000m, 0.01m, 12, Amortization.Price)) > 0.12684m);
}
