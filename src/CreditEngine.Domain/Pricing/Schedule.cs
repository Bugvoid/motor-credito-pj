namespace CreditEngine.Domain;

public sealed record Installment(int Number, decimal Payment, decimal Interest, decimal Amortization, decimal Balance);

public static class Schedule
{
    public static IReadOnlyList<Installment> Build(decimal principal, decimal monthlyRate, int months, Amortization type)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(months, 1);
        var price = type == Amortization.Price ? PricePayment(principal, monthlyRate, months) : 0m;
        var sacAmortization = Round(principal / months);
        var balance = principal;
        var list = new List<Installment>(months);

        for (var n = 1; n <= months; n++)
        {
            var interest = Round(balance * monthlyRate);
            var amortization = type == Amortization.Price ? price - interest : sacAmortization;
            if (n == months) amortization = balance; // última parcela absorve o resíduo de arredondamento
            balance -= amortization;
            list.Add(new Installment(n, amortization + interest, interest, amortization, balance));
        }
        return list;
    }

    private static decimal PricePayment(decimal principal, decimal rate, int months)
    {
        if (rate == 0) return Round(principal / months);
        var factor = (decimal)Math.Pow(1 + (double)rate, months);
        return Round(principal * rate * factor / (factor - 1));
    }

    internal static decimal Round(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
}
