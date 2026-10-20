namespace CreditEngine.Domain.Tests;

public class RatingCalculatorTests
{
    [Theory]
    // netDebt, ebitda, revenue, score, esperado
    [InlineData(10, 10, 50, 800, Rating.A)]   // alavancagem 1,0→3, margem 20%→3, score 800→3 = 9
    [InlineData(25, 10, 100, 600, Rating.B)]  // 2,5→2, 10%→2, 600→2 = 6
    [InlineData(10, 10, 200, 400, Rating.C)]  // 3 + 1 (5%) + 1 = 5
    [InlineData(40, 10, 200, 400, Rating.D)]  // 4,0→1, 5%→1, 400→1 = 3
    [InlineData(50, 10, 1000, 100, Rating.E)] // 0 + 0 (1%) + 0 = 0
    [InlineData(0, 0, 100, 999, Rating.E)]    // EBITDA ≤ 0 → E
    public void Calculate_maps_points_to_band(int nd, int e, int rev, int s, Rating expected)
        => Assert.Equal(expected, RatingCalculator.Calculate(nd, e, rev, s));
}
