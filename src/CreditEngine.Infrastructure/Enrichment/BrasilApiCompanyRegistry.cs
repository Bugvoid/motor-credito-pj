using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CreditEngine.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CreditEngine.Infrastructure.Enrichment;

public sealed class BrasilApiCompanyRegistry(HttpClient http, ILogger<BrasilApiCompanyRegistry>? log = null) : ICompanyRegistry
{
    private readonly ILogger _log = log ?? NullLogger<BrasilApiCompanyRegistry>.Instance;

    public async Task<CompanyProfile?> FindAsync(Cnpj cnpj, CancellationToken ct)
    {
        try
        {
            using var resp = await http.GetAsync($"api/cnpj/v1/{cnpj.Digits}", ct);
            if (resp.StatusCode == HttpStatusCode.NotFound) return new CompanyProfile("INEXISTENTE", default, "");
            if (!resp.IsSuccessStatusCode)
            {
                _log.LogWarning("BrasilAPI respondeu {Status} para {Cnpj}", (int)resp.StatusCode, cnpj);
                return null;
            }

            var body = await resp.Content.ReadFromJsonAsync<BrasilApiCnpj>(ct);
            if (body?.Status is null || body.OpenedOn is null) return null;
            return new CompanyProfile(
                body.Status.ToUpperInvariant(),
                DateOnly.ParseExact(body.OpenedOn, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                body.Cnae.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or FormatException)
        {
            _log.LogWarning(e, "BrasilAPI indisponível para {Cnpj}", cnpj);
            return null;
        }
    }

    private sealed record BrasilApiCnpj(
        [property: JsonPropertyName("descricao_situacao_cadastral")] string? Status,
        [property: JsonPropertyName("data_inicio_atividade")] string? OpenedOn,
        [property: JsonPropertyName("cnae_fiscal")] long Cnae);
}
