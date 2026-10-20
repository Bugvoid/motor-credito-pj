namespace CreditEngine.Domain;

/// <summary>
/// IOF de crédito PJ (não Simples): 0,0082% ao dia sobre cada amortização (até 365 dias) + 0,38% adicional.
/// Decreto 6.306/2007, art. 7º, com a redação do Decreto 12.499/2025 (referência: 20/10/2026).
/// </summary>
public static class CreditIof
{
    private const decimal DailyRate = 0.000082m;
    private const decimal AdditionalRate = 0.0038m;
    private const int MaxDays = 365;

    public static decimal Calculate(IReadOnlyList<Installment> schedule, decimal principal)
    {
        var daily = schedule.Sum(i => i.Amortization * Math.Min(30 * i.Number, MaxDays) * DailyRate);
        return Schedule.Round(daily + principal * AdditionalRate);
    }
}
