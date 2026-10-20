namespace CreditEngine.Domain;

/// <summary>Custo efetivo total: TIR dos fluxos (valor líquido recebido × parcelas), anualizada.</summary>
public static class Cet
{
    public static decimal Annual(decimal netReceived, IReadOnlyList<Installment> schedule)
    {
        double lo = 0, hi = 1, target = (double)netReceived;
        for (var i = 0; i < 200; i++)
        {
            var mid = (lo + hi) / 2;
            // Valor presente cai quando a taxa sobe: se ainda está acima do recebido, a taxa precisa subir.
            if (PresentValue(schedule, mid) > target) lo = mid; else hi = mid;
        }
        return (decimal)(Math.Pow(1 + (lo + hi) / 2, 12) - 1);
    }

    private static double PresentValue(IReadOnlyList<Installment> schedule, double rate) =>
        schedule.Sum(i => (double)i.Payment / Math.Pow(1 + rate, i.Number));
}
