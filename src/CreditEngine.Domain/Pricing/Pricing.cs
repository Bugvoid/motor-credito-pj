namespace CreditEngine.Domain;

public static class Pricing
{
    private const decimal FloorSpread = 0.01m;
    private const decimal PerMonthAbove12 = 0.0005m;

    public static decimal SpreadAnnual(Rating rating, int termMonths, Collateral collateral)
    {
        var baseSpread = rating switch
        {
            Rating.A => 0.020m,
            Rating.B => 0.035m,
            Rating.C => 0.050m,
            Rating.D => 0.075m,
            _ => throw new ArgumentOutOfRangeException(nameof(rating), rating, "rating E não é precificado"),
        };
        var termAdd = Math.Max(0, termMonths - 12) * PerMonthAbove12;
        var discount = collateral switch
        {
            Collateral.Receivables => 0.010m,
            Collateral.RealEstate => 0.015m,
            _ => 0m,
        };
        return Math.Max(FloorSpread, baseSpread + termAdd - discount);
    }

    public static decimal AnnualRate(decimal cdiAnnual, decimal spreadAnnual) => (1 + cdiAnnual) * (1 + spreadAnnual) - 1;

    // shortcut: potência em double (decimal não tem Pow); precisão sobra para exibir taxas com 4 casas.
    public static decimal MonthlyRate(decimal annualRate) => (decimal)(Math.Pow(1 + (double)annualRate, 1.0 / 12) - 1);
}
