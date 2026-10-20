namespace CreditEngine.Domain;

public enum Rating { A, B, C, D, E }

public static class RatingCalculator
{
    public static Rating Calculate(decimal netDebt, decimal ebitda, decimal annualRevenue, int bureauScore)
    {
        if (ebitda <= 0 || annualRevenue <= 0) return Rating.E;

        var leverage = netDebt / ebitda;
        var margin = ebitda / annualRevenue;

        var points =
            (leverage <= 1.0m ? 3 : leverage <= 2.5m ? 2 : leverage <= 4.0m ? 1 : 0) +
            (margin >= 0.20m ? 3 : margin >= 0.10m ? 2 : margin >= 0.05m ? 1 : 0) +
            (bureauScore >= 800 ? 3 : bureauScore >= 600 ? 2 : bureauScore >= 400 ? 1 : 0);

        return points switch
        {
            >= 8 => Rating.A,
            >= 6 => Rating.B,
            >= 4 => Rating.C,
            >= 2 => Rating.D,
            _ => Rating.E,
        };
    }
}
