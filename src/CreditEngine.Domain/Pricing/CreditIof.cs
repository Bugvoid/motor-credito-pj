namespace CreditEngine.Domain;

/// <summary>IOF de crédito PJ: 0,0041% ao dia sobre cada amortização (até 365 dias) + 0,38% adicional.</summary>
public static class CreditIof
{
    private const decimal DailyRate = 0.000041m;
    private const decimal AdditionalRate = 0.0038m;
    private const int MaxDays = 365;

    public static decimal Calculate(IReadOnlyList<Installment> schedule, decimal principal)
    {
        var daily = schedule.Sum(i => i.Amortization * Math.Min(30 * i.Number, MaxDays) * DailyRate);
        return Schedule.Round(daily + principal * AdditionalRate);
    }
}
