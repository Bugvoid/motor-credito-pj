using CreditEngine.Domain;
using CreditEngine.Domain.Policy;

namespace CreditEngine.Api;

public sealed record CreditRequestDto(
    string? Cnpj,
    decimal? AnnualRevenue,
    decimal? Ebitda,
    decimal? NetDebt,
    decimal? RequestedAmount,
    int? TermMonths,
    string? Collateral,
    string? Amortization)
{
    private const decimal MaxMoney = 1_000_000_000m;

    /// <summary>Valida a entrada (fronteira de confiança). Devolve a aplicação ou os erros por campo.</summary>
    public (CreditApplication? App, Dictionary<string, string[]> Errors) Validate()
    {
        var errors = new Dictionary<string, string[]>();

        if (!Domain.Cnpj.TryParse(Cnpj, out var cnpj)) errors["cnpj"] = ["CNPJ inválido"];
        if (AnnualRevenue is not (> 0 and <= MaxMoney)) errors["annualRevenue"] = ["deve ser maior que 0 e até 1 bilhão"];
        if (RequestedAmount is not (> 0 and <= MaxMoney)) errors["requestedAmount"] = ["deve ser maior que 0 e até 1 bilhão"];
        // EBITDA e dívida líquida podem ser negativos (prejuízo, caixa líquido): só limitamos o módulo.
        if (Ebitda is null || Math.Abs(Ebitda.Value) > MaxMoney) errors["ebitda"] = ["obrigatório, até 1 bilhão em módulo"];
        if (NetDebt is null || Math.Abs(NetDebt.Value) > MaxMoney) errors["netDebt"] = ["obrigatório, até 1 bilhão em módulo"];
        if (TermMonths is not (>= 1 and <= 120)) errors["termMonths"] = ["prazo deve estar entre 1 e 120 meses"];
        if (!TryParseEnum<Collateral>(Collateral, out var collateral)) errors["collateral"] = ["use None, Receivables ou RealEstate"];
        if (!TryParseEnum<Amortization>(Amortization, out var amortization)) errors["amortization"] = ["use Price ou Sac"];

        if (errors.Count > 0) return (null, errors);
        return (new CreditApplication(cnpj, AnnualRevenue!.Value, Ebitda!.Value, NetDebt!.Value, RequestedAmount!.Value,
            TermMonths!.Value, collateral, amortization), errors);
    }

    // Enum.TryParse aceita números ("7"); exigimos o nome.
    private static bool TryParseEnum<T>(string? value, out T result) where T : struct, Enum =>
        Enum.TryParse(value, ignoreCase: true, out result) && Enum.IsDefined(result) && !int.TryParse(value, out _);
}

public sealed record CreditDecisionDto(
    Guid Id,
    string Cnpj,
    DecisionStatus Status,
    Rating Rating,
    string PolicyVersion,
    IReadOnlyList<RuleResult> Rules,
    CreditOffer? Offer,
    DateTimeOffset CreatedAt);

public sealed class CreditOptions
{
    public decimal CdiAnnual { get; set; }
}
