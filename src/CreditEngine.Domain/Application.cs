namespace CreditEngine.Domain;

public enum Collateral { None, Receivables, RealEstate }

public enum Amortization { Price, Sac }

public sealed record CreditApplication(
    Cnpj Cnpj,
    decimal AnnualRevenue,
    decimal Ebitda,
    decimal NetDebt,
    decimal RequestedAmount,
    int TermMonths,
    Collateral Collateral,
    Amortization Amortization);

/// <summary>RegistrationStatus = "INEXISTENTE" quando a BrasilAPI responde 404.</summary>
public sealed record CompanyProfile(string RegistrationStatus, DateOnly OpenedOn, string Cnae);

/// <summary>Company é null quando o cadastro não pôde ser consultado (timeout/5xx).</summary>
public sealed record EnrichedApplication(CreditApplication Application, CompanyProfile? Company, int BureauScore, DateOnly Today);
