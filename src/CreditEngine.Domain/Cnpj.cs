namespace CreditEngine.Domain;

public readonly record struct Cnpj(string Digits)
{
    private static readonly int[] FirstWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] SecondWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    public static bool TryParse(string? input, out Cnpj cnpj)
    {
        cnpj = default;
        if (input is null) return false;

        var digits = input.Replace(".", "").Replace("/", "").Replace("-", "").Trim();
        if (digits.Length != 14 || !digits.All(char.IsAsciiDigit)) return false;
        if (digits.All(c => c == digits[0])) return false;
        if (CheckDigit(digits, FirstWeights) != digits[12] - '0') return false;
        if (CheckDigit(digits, SecondWeights) != digits[13] - '0') return false;

        cnpj = new Cnpj(digits);
        return true;
    }

    private static int CheckDigit(string digits, int[] weights)
    {
        var sum = weights.Select((w, i) => (digits[i] - '0') * w).Sum();
        var rest = sum % 11;
        return rest < 2 ? 0 : 11 - rest;
    }

    public override string ToString() => Digits;
}
