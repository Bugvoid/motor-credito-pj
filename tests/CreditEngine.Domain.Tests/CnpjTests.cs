using CreditEngine.Domain;

namespace CreditEngine.Domain.Tests;

public class CnpjTests
{
    [Theory]
    [InlineData("11.222.333/0001-81", true, "11222333000181")]
    [InlineData("11222333000181", true, "11222333000181")]
    [InlineData("11222333000182", false, null)]
    [InlineData("11111111111111", false, null)]
    [InlineData("1122233300018", false, null)]
    [InlineData(null, false, null)]
    public void TryParse_validates_check_digits(string? input, bool ok, string? digits)
    {
        Assert.Equal(ok, Cnpj.TryParse(input, out var c));
        if (ok) Assert.Equal(digits, c.Digits);
    }
}
