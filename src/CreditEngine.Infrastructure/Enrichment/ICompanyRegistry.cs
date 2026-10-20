using CreditEngine.Domain;

namespace CreditEngine.Infrastructure.Enrichment;

public interface ICompanyRegistry
{
    /// <summary>404 → status "INEXISTENTE"; timeout, 5xx ou resposta inválida → null (vira análise manual).</summary>
    Task<CompanyProfile?> FindAsync(Cnpj cnpj, CancellationToken ct);
}

public interface ICreditBureau
{
    int Score(Cnpj cnpj);
}
