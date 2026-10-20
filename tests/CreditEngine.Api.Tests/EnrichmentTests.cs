using System.Net;
using CreditEngine.Domain;
using CreditEngine.Infrastructure.Enrichment;

namespace CreditEngine.Api.Tests;

public class EnrichmentTests
{
    private static readonly Cnpj Cnpj = Cnpj.TryParse("11222333000181", out var c) ? c : default;

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(respond(request));
    }

    private static BrasilApiCompanyRegistry Registry(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new HttpClient(new FakeHandler(respond)) { BaseAddress = new Uri("https://brasilapi.com.br/") });

    [Fact] public async Task Ok_maps_profile()
    {
        string? path = null;
        var r = Registry(req =>
        {
            path = req.RequestUri!.AbsolutePath;
            return new(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"descricao_situacao_cadastral":"ATIVA","data_inicio_atividade":"2015-03-10","cnae_fiscal":6201501}"""),
            };
        });
        var p = await r.FindAsync(Cnpj, CancellationToken.None);
        Assert.Equal("/api/cnpj/v1/11222333000181", path);
        Assert.Equal(new CompanyProfile("ATIVA", new DateOnly(2015, 3, 10), "6201501"), p);
    }

    [Fact] public async Task NotFound_is_inexistente() =>
        Assert.Equal("INEXISTENTE", (await Registry(_ => new(HttpStatusCode.NotFound)).FindAsync(Cnpj, default))!.RegistrationStatus);

    [Fact] public async Task ServerError_is_null() =>
        Assert.Null(await Registry(_ => new(HttpStatusCode.ServiceUnavailable)).FindAsync(Cnpj, default));

    [Fact] public async Task Timeout_is_null() =>
        Assert.Null(await Registry(_ => throw new TaskCanceledException()).FindAsync(Cnpj, default));

    [Fact] public async Task Invalid_json_is_null() =>
        Assert.Null(await Registry(_ => new(HttpStatusCode.OK) { Content = new StringContent("<html>") }).FindAsync(Cnpj, default));

    [Fact] public void Bureau_is_deterministic_and_in_range()
    {
        var b = new SimulatedCreditBureau();
        Assert.Equal(b.Score(Cnpj), b.Score(Cnpj));
        Assert.InRange(b.Score(Cnpj), 0, 1000);
    }
}
